using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>
/// The shipped packs against the ROM images of the workspace: factory images, tunes made with
/// several programs, the base images tuning programs hand out, and files that are not OBD1
/// ROMs at all. <c>fixtures/corpus/expected-packs.tsv</c> states what each one is.
/// </summary>
[Trait("Category", "Corpus")]
public class PackCorpusTests {
    private const string CorpusDir = "hondaecu/test-roms";
    private const string Manifest = CorpusDir + "/hondabase/MANIFEST.tsv";
    private const string Editors = CorpusDir + "/editors";

    private static string CorpusPath(string relative) => Path.Combine(Workspace.Root, CorpusDir, relative);

    /// <summary>Path relative to the corpus, with forward slashes, and the pack expected for it (null for none).</summary>
    private static IReadOnlyList<(string Path, string? Pack)> Expected() => [
        .. File.ReadLines(Path.Combine(AppContext.BaseDirectory, "fixtures", "corpus", "expected-packs.tsv"))
            .Where(line => line.Length > 0 && !line.StartsWith('#') && !line.StartsWith("pack\t"))
            .Select(line => line.Split('\t'))
            .Select(fields => (fields[1], fields[0] == "-" ? null : fields[0])),
    ];

    /// <summary>The images a pack is expected to describe, loaded.</summary>
    private static IEnumerable<(string Path, DefinitionPack Pack, RomImage Image)> Described() {
        var packs = ShippedPackTests.Packs().ToDictionary(p => p.Id);
        return Expected().Where(e => e.Pack is not null).Select(e => (e.Path, packs[e.Pack!], RomImage.Load(CorpusPath(e.Path))));
    }

    private static bool Increases(IEnumerable<float> values) {
        var list = values.ToList();
        return list.Zip(list.Skip(1)).All(pair => pair.First < pair.Second);
    }

    [WorkspaceFact(Manifest, Editors)]
    public void TheListCoversEveryImageOfTheCorpus() {
        var root = Path.Combine(Workspace.Root, CorpusDir);
        var onDisk = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => new FileInfo(path).Length is 0x8000 or 0x8001)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'));
        Assert.Equal(Expected().Select(e => e.Path).Order(StringComparer.Ordinal), onDisk.Order(StringComparer.Ordinal));
    }

    [WorkspaceFact(Manifest, Editors)]
    public void EveryImageIsIdentifiedAsListed() {
        var packs = ShippedPackTests.Packs();
        var wrong = new List<string>();
        foreach (var (path, expected) in Expected()) {
            var matches = DefinitionPacks.Identify(packs, RomImage.Load(CorpusPath(path)));
            var found = matches.Count switch { 0 => null, 1 => matches[0].Pack.Id, _ => string.Join("+", matches.Select(m => m.Pack.Id)) };
            if (found != expected) wrong.Add($"{path}: expected {expected ?? "no pack"}, found {found ?? "no pack"}");
        }
        Assert.Empty(wrong);
        // Factory images, tunes that kept the factory layout, and the editors' base images.
        Assert.Equal(71, Expected().Count(e => e.Pack is not null));
    }

    [WorkspaceFact(Manifest, Editors)]
    public void EveryParameterLiesInsideItsImage() {
        Assert.All(Described(), d => Assert.All(d.Pack.Defs, def => Assert.True(ParamAccess.Contains(d.Image, def), $"{d.Path}: {def.Name}")));
    }

    /// <summary>
    /// Axes read from the ROM are real axes. The low-cam rpm axis and the MAP axis of a factory
    /// layout strictly increase. A high-cam axis does too, unless the engine has no second cam
    /// profile and the ROM leaves it empty. The editors' 24-column maps pad unused columns by
    /// repeating a breakpoint.
    /// </summary>
    [WorkspaceFact(Manifest, Editors)]
    public void AxesReadFromTheImagesAreOrdered() {
        var wrong = new List<string>();
        var withoutHighCam = 0;
        foreach (var (path, pack, image) in Described()) {
            var padded = pack.Id is "crome-gold" or "ectune-273";
            foreach (var def in pack.Defs) {
                var table = (ParamKind.Table)def.Kind;
                var rows = ParamAccess.AxisValues(image, def.RowAxis, table.Rows);
                var cols = ParamAccess.AxisValues(image, def.ColAxis, table.Cols);
                var unused = def.Vtec == true && ParamAccess.AxisStoredValues(image, def.RowAxis, table.Rows).Skip(1).All(v => v == 256f);
                if (unused && def.Category == "Fuel") withoutHighCam++;
                if (!unused && !Increases(rows)) wrong.Add($"{path}: {def.Name}: rows {string.Join(' ', rows)}");
                var ordered = padded ? cols.Zip(cols.Skip(1)).All(pair => pair.First <= pair.Second) : Increases(cols);
                if (!ordered) wrong.Add($"{path}: {def.Name}: columns {string.Join(' ', cols)}");
                // Engine speeds and pressures an engine can see.
                if (!unused && (rows[0] < 0f || rows[^1] > 11100f)) wrong.Add($"{path}: {def.Name}: rpm {rows[0]}..{rows[^1]}");
                if (!padded && (cols[0] < 11f || cols[^1] > 104f)) wrong.Add($"{path}: {def.Name}: kPa {cols[0]}..{cols[^1]}");
            }
        }
        Assert.Empty(wrong);
        // The four P72-type images for engines without VTEC.
        Assert.Equal(4, withoutHighCam);
    }

    /// <summary>The checksum each pack describes is the one the images actually satisfy.</summary>
    [WorkspaceFact(Manifest, Editors)]
    public void ImagesThatSumToZeroPassTheirPacksChecksum() {
        var (valid, invalid) = (0, 0);
        foreach (var (path, pack, image) in Described()) {
            if (pack.Checksum.Kind == ChecksumKind.None) continue;
            var sumsToZero = Checksum.Sum8(image.Bytes[..0x8000]) == 0;
            Assert.True(Checksum.IsValid(image, pack.Checksum) == sumsToZero, path);
            var corrected = Checksum.Fix(image, pack.Checksum);
            Assert.True(Checksum.IsValid(corrected, pack.Checksum), path);
            if (sumsToZero) {
                Assert.Same(image, corrected);
                valid++;
            } else {
                // Only the correction byte moves.
                Assert.Equal(image.With(pack.Checksum.Location, corrected[pack.Checksum.Location]).Sha256(), corrected.Sha256());
                invalid++;
            }
        }
        // 327, 332 and 215 as archived, a baseline another editor left uncorrected, and one tune.
        Assert.Equal((65, 5), (valid, invalid));
    }

    /// <summary>
    /// Tuning a ROM does not make it unrecognisable: with every cell, multiplier and breakpoint
    /// of every map rewritten and the checksum corrected, the image is still its pack's ROM.
    /// </summary>
    [WorkspaceFact(Manifest, Editors)]
    public void AFullyRetunedImageIsStillRecognised() {
        var packs = ShippedPackTests.Packs();
        foreach (var (path, pack, image) in Described()) {
            var writes = pack.Defs.SelectMany(d => d.Spans()).SelectMany(span => Enumerable.Range(span.Start, span.Length)).Select(address => (address, (byte)(image[address] ^ 0x5A)));
            var tuned = Checksum.Fix(image.With(writes), pack.Checksum);
            Assert.NotSame(image, tuned);
            Assert.True(Checksum.IsValid(tuned, pack.Checksum), path);
            Assert.Equal([pack.Id], DefinitionPacks.Identify(packs, tuned).Select(m => m.Pack.Id));
        }
    }

    /// <summary>
    /// The P28 with ROM number 230, read through the P30-type pack, against what its source
    /// project derived for the same ROM by other means: the axes of its definitions file, the
    /// factory column multipliers and the idle ignition angle.
    /// </summary>
    [WorkspaceFact(CorpusDir + "/hondabase/ecu-roms/P28-230.bin")]
    public void TheP28230ReadsAsItsSourceProjectSays() {
        var pack = ShippedPackTests.Pack("obd1-p30");
        var image = RomImage.Load(CorpusPath("hondabase/ecu-roms/P28-230.bin"));
        var (fuelLow, fuelHigh, ignLow, ignHigh) = (pack.Defs[0], pack.Defs[1], pack.Defs[2], pack.Defs[3]);
        float[] Rounded(float[] values) => [.. values.Select(v => MathF.Round(v))];

        Assert.Equal([500, 602, 703, 797, 1000, 1297, 1500, 1703, 2000, 2188, 2594, 2906, 3000, 3406, 4000, 4438, 5000, 6000, 7000, 8000], Rounded(ParamAccess.AxisValues(image, fuelLow.RowAxis, 20)));
        Assert.Equal([0, 599, 986, 1514, 2007, 2500, 2993, 3521, 4014, 4507, 4754, 5000, 5247, 5493, 6021, 6514, 7007, 7500, 8028, 9014], Rounded(ParamAccess.AxisValues(image, fuelHigh.RowAxis, 20)));
        Assert.All(pack.Defs, d => Assert.Equal([11, 29, 40, 52, 63, 75, 87, 92, 98, 104], Rounded(ParamAccess.AxisValues(image, d.ColAxis, 10))));

        Assert.Equal([1f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 9f, 10f], Enumerable.Range(0, 10).Select(c => ParamAccess.ColumnFactor(image, fuelLow, c)));
        Assert.Equal([1f, 3f, 4f, 6f, 7f, 8f, 9f, 9f, 10f, 10f], Enumerable.Range(0, 10).Select(c => ParamAccess.ColumnFactor(image, fuelHigh, c)));
        // 16.5 degrees at idle: the first four rows and columns of the low-cam map.
        Assert.All(from row in Enumerable.Range(0, 4) from col in Enumerable.Range(0, 4) select ParamAccess.CellDisplay(image, ignLow, row, col), angle => Assert.Equal(16.5f, angle));
        Assert.InRange(ParamAccess.CellDisplay(image, ignHigh, 19, 9), 20f, 40f);
        // A point on the map: where 3200 rpm and 70 kPa fall, and the fuel value read there.
        Assert.Equal((12.49f, 4.57f), Round2(TableMath.TablePos(image, fuelLow, 3200f, 70f)!.Value));
        Assert.InRange(TableMath.Lookup(image, fuelLow, 3200f, 70f)!.Value, 300f, 1300f);
    }

    private static (float, float) Round2((float Row, float Col) pos) => (MathF.Round(pos.Row, 2), MathF.Round(pos.Col, 2));

    /// <summary>The source project names the same four addresses for its maps.</summary>
    [WorkspaceFact("hondaromeditor/roms/p28-230/aliases.inc")]
    public void TheP30PackAgreesWithTheP28SourceProjectOnWhereTheMapsAre() {
        var aliases = File.ReadAllLines(Path.Combine(Workspace.P28Project, "aliases.inc"))
            .Select(line => line.Split(';')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(words => words is [_, "EQU", var target] && target.StartsWith("tbl_"))
            .ToDictionary(words => words[0], words => Convert.ToInt32(words[2][4..], 16));
        var defs = ShippedPackTests.Pack("obd1-p30").Defs;
        Assert.Equal([aliases["fuel_low_cam"], aliases["fuel_high_cam"], aliases["ign_low_cam"], aliases["ign_high_cam"]], defs.Select(d => d.Offset));
    }

    /// <summary>The same ROM under two names is one image; the pack reads both alike.</summary>
    [WorkspaceFact(Manifest, Editors)]
    public void TheEditorsTemplatesAreTheFactoryImages() {
        Assert.Equal(RomImage.Load(CorpusPath("hondabase/ecu-roms/203stock.bin")).Sha256(), RomImage.Load(CorpusPath("editors/crome/P30 JDM Civic B16A (203).NEW")).Sha256());
        Assert.Equal(RomImage.Load(CorpusPath("hondabase/ecu-roms/304stock.bin")).Sha256(), RomImage.Load(CorpusPath("editors/crome/P28 USDM Civic D16Z (304).NEW")).Sha256());
        Assert.Equal(RomImage.Load(CorpusPath("hondabase/ecu-roms/273stock.bin")).Sha256(), RomImage.Load(CorpusPath("editors/crome/P72 USDM Integra B18C1-3 (273).NEW")).Sha256());
    }

    /// <summary>
    /// What the 24-column bases hold: ten columns in use in the Gold base and fifteen in the
    /// eCtune base, the rest padded, and rpm axes that reach past 10000 rpm on the high cam.
    /// </summary>
    [WorkspaceFact(Editors)]
    public void TheEditorsBasesReadAsTheirOwnProgramsShowThem() {
        var gold = RomImage.Load(CorpusPath("editors/crome/CROME GOLD BASE v0.94.NEW"));
        var goldFuel = ShippedPackTests.Pack("crome-gold").Defs[0];
        Assert.Equal(10, ParamAccess.AxisStoredValues(gold, goldFuel.ColAxis, 24).Distinct().Count() - 1);
        Assert.Equal(500f, ParamAccess.AxisValues(gold, goldFuel.RowAxis, 20)[0]);

        var ectune = RomImage.Load(CorpusPath("editors/ectune/eCt-273-v0.0.65.bin"));
        var defs = ShippedPackTests.Pack("ectune-273").Defs;
        Assert.Equal(8, defs.Count);
        Assert.Equal(15, ParamAccess.AxisStoredValues(ectune, defs[0].ColAxis, 24).Distinct().Count() - 1);
        var high = ParamAccess.AxisValues(ectune, defs[1].RowAxis, 20);
        Assert.Equal((2499f, 11029f), (MathF.Round(high[0]), MathF.Round(high[^1])));
        // The second set of maps has axes of its own.
        Assert.NotEqual(defs[0].RowAxis, defs[4].RowAxis);
        Assert.NotEqual(defs[0].ColAxis, defs[4].ColAxis);
    }
}
