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
    private const string Uberdata = CorpusDir + "/hondabase/_from-archives/uberdata";

    /// <summary>The packs whose MAP axis is in the raw reading of whichever sensor the tune was made for.</summary>
    private static readonly string[] SensorPacks = ["crome-p30-boost", "crome-p30-boost-rev", "crome-p30-boost1", "crome-p72-boost"];

    private static string CorpusPath(string relative) => Path.Combine(Workspace.Root, CorpusDir, relative);

    private static RomImage Image(string relative) => RomImage.Load(CorpusPath(relative));

    /// <summary>The parameters of an image, read through the pack with the given id.</summary>
    private static IReadOnlyList<ParamDef> Defs(string pack, RomImage image) {
        var (defs, problems) = ShippedPackTests.Pack(pack).Resolve(image);
        Assert.Empty(problems);
        return defs;
    }

    /// <summary>Path relative to the corpus, with forward slashes, and the pack expected for it (null for none).</summary>
    private static IReadOnlyList<(string Path, string? Pack)> Expected() => [
        .. File.ReadLines(Path.Combine(AppContext.BaseDirectory, "fixtures", "corpus", "expected-packs.tsv"))
            .Where(line => line.Length > 0 && !line.StartsWith('#') && !line.StartsWith("pack\t"))
            .Select(line => line.Split('\t'))
            .Select(fields => (fields[1], fields[0] == "-" ? null : fields[0])),
    ];

    /// <summary>The images a pack is expected to describe, loaded, with the parameters the pack resolves to on each.</summary>
    private static IEnumerable<(string Path, DefinitionPack Pack, RomImage Image, IReadOnlyList<ParamDef> Defs)> Described() {
        var packs = ShippedPackTests.Packs().ToDictionary(p => p.Id);
        foreach (var (path, id) in Expected()) {
            if (id is null) continue;
            var image = Image(path);
            yield return (path, packs[id], image, packs[id].Resolve(image).Defs);
        }
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
            var matches = DefinitionPacks.Identify(packs, Image(path));
            var found = matches.Count switch { 0 => null, 1 => matches[0].Pack.Id, _ => string.Join("+", matches.Select(m => m.Pack.Id)) };
            if (found != expected) wrong.Add($"{path}: expected {expected ?? "no pack"}, found {found ?? "no pack"}");
        }
        Assert.Empty(wrong);
        var counts = Expected().Where(e => e.Pack is not null).GroupBy(e => e.Pack!).ToDictionary(g => g.Key, g => g.Count());
        // Factory images, tunes that kept the factory layout, and the base images of the tuning programs.
        Assert.Equal((38, 34, 20, 2, 1), (counts["obd1-p30"], counts["obd1-p28"], counts["obd1-p72"], counts["crome-gold"], counts["ectune-273"]));
        // Tunes whose maps a tuning program enlarged or moved: every one of them in the corpus.
        Assert.Equal((23, 5, 2, 4, 7), (counts["crome-p30-boost"], counts["crome-p30-boost-rev"], counts["crome-p30-boost1"], counts["crome-p72-boost"], counts["uberdata-p72"]));
        Assert.Equal(136, counts.Values.Sum());
    }

    /// <summary>
    /// Files that are not exactly one ROM long are judged by the ROM they hold. Text after the
    /// ROM, or the ROM written twice to fill a larger chip, does not hide it; a dump that stops
    /// short is not that ROM; a ROM in the upper half of a larger file is not looked for there.
    /// </summary>
    [WorkspaceFact(CorpusDir + "/hondabase/ecu-roms/368-d14a2-p1j-stock.bin", CorpusDir + "/hondabase/hts/bins/B20 Unknown Joey.BIN")]
    public void FilesOfOtherSizesAreJudgedByTheRomTheyHold() {
        var packs = ShippedPackTests.Packs();
        (int Length, string? Pack) Found(string path) {
            var image = Image(path);
            return (image.Length, DefinitionPacks.Identify(packs, image).Select(m => m.Pack.Id).SingleOrDefault());
        }

        // A factory ROM with a line of text after it, and one written twice into 64 KiB.
        Assert.Equal((32860, "obd1-p28"), Found("hondabase/ecu-roms/368-d14a2-p1j-stock.bin"));
        Assert.Equal((65536, "obd1-p30"), Found("hondabase/ecu-roms/OKI_P30.BIN"));
        // A tune with enlarged maps and the notes its author kept after the ROM.
        Assert.Equal((32923, "crome-p30-boost"), Found("hondabase/xenocron/crome/d/final-notes.bin"));
        // Dumps of 32000 bytes and a file that is short of 32 KiB.
        Assert.Equal((32000, null), Found("hondabase/ecu-roms/p30-010-244MCU-extensionG8.bin"));
        Assert.Equal((32000, null), Found("hondabase/ecu-roms/P54-J00-204MCUwithE2Ext.bin"));
        Assert.Equal((32000, null), Found("hondabase/ecu-roms/P91-900-253MCU.bin"));
        Assert.Equal((32673, null), Found("hondabase/xenocron/crome/h/Accord-SOHC_VTEC-P28_datalogge"));
        // 64 KiB with the ROM in the upper half.
        Assert.Equal((65536, null), Found("hondabase/hts/bins/B20 Unknown Joey.BIN"));

        // The longer files read like the ROM alone: the P1J's low-cam axis starts at 500 rpm.
        var p1j = Image("hondabase/ecu-roms/368-d14a2-p1j-stock.bin");
        Assert.True(Checksum.IsValid(p1j, ShippedPackTests.Pack("obd1-p28").Checksum));
        Assert.Equal(500f, ParamAccess.AxisValues(p1j, Defs("obd1-p28", p1j)[0].RowAxis, 20)[0]);
        var notes = Image("hondabase/xenocron/crome/d/final-notes.bin");
        Assert.Equal(new ParamKind.Table(20, 16), Defs("crome-p30-boost", notes)[0].Kind);
    }

    /// <summary>
    /// On every image a pack describes, every parameter resolves, lies inside the image, and
    /// has the layout of a main map. Nothing a pack reads from a ROM leads into its code, its
    /// checksum byte or another map.
    /// </summary>
    [WorkspaceFact(Manifest, Editors)]
    public void EveryParameterResolvesOnEveryImage() {
        foreach (var (path, pack, image, defs) in Described()) {
            Assert.True(pack.Resolve(image).Problems.Count == 0, $"{path}: {string.Join("; ", pack.Resolve(image).Problems)}");
            ShippedPackTests.AssertMainMaps(pack, defs);
            Assert.All(defs, def => Assert.True(ParamAccess.Contains(image, def), $"{path}: {def.Name}"));
        }
    }

    /// <summary>
    /// Axes read from the ROM are real axes. The low-cam rpm axis and the MAP axis strictly
    /// increase. A high-cam axis does too, unless the engine has no second cam profile and the
    /// ROM leaves it empty. The 24-column base ROMs pad unused columns by repeating a breakpoint.
    /// </summary>
    [WorkspaceFact(Manifest, Editors)]
    public void AxesReadFromTheImagesAreOrdered() {
        var wrong = new List<string>();
        var withoutHighCam = new List<(string Pack, string Path)>();
        foreach (var (path, pack, image, defs) in Described()) {
            var padded = pack.Id is "crome-gold" or "ectune-273";
            // What a column can stand for: the factory sensor, a boost sensor, or a stored code.
            var (lowest, highest) = pack.Id switch {
                "obd1-p30" or "obd1-p28" or "obd1-p72" => (11f, 104f),
                "uberdata-p72" => (11f, 197f),
                "crome-gold" or "ectune-273" => (0f, 256f),
                _ => (0f, 400f),
            };
            foreach (var def in defs) {
                var table = (ParamKind.Table)def.Kind;
                var rows = ParamAccess.AxisValues(image, def.RowAxis, table.Rows);
                var cols = ParamAccess.AxisValues(image, def.ColAxis, table.Cols);
                var unused = def.Vtec == true && ParamAccess.AxisStoredValues(image, def.RowAxis, table.Rows).Skip(1).All(v => v == 256f);
                if (unused && def.Category == "Fuel") withoutHighCam.Add((pack.Id, path));
                if (!unused && !Increases(rows)) wrong.Add($"{path}: {def.Name}: rows {string.Join(' ', rows)}");
                var ordered = padded ? cols.Zip(cols.Skip(1)).All(pair => pair.First <= pair.Second) : Increases(cols);
                if (!ordered) wrong.Add($"{path}: {def.Name}: columns {string.Join(' ', cols)}");
                // Engine speeds and pressures an engine can see.
                if (!unused && (rows[0] < 0f || rows[^1] > 11100f)) wrong.Add($"{path}: {def.Name}: rpm {rows[0]}..{rows[^1]}");
                if (cols[0] < lowest || cols[^1] > highest) wrong.Add($"{path}: {def.Name}: columns {cols[0]}..{cols[^1]}");
            }
        }
        Assert.Empty(wrong);
        // ROMs of the P72 type for engines without VTEC: four as archived and five more copies
        // among the base images of a tuning program, whose boost bases are for such engines too.
        Assert.Equal([("obd1-p72", 9), ("uberdata-p72", 7)], withoutHighCam.GroupBy(w => w.Pack).Select(g => (g.Key, g.Count())).Order());
    }

    /// <summary>The checksum each pack describes is the one the images actually satisfy.</summary>
    [WorkspaceFact(Manifest, Editors)]
    public void ImagesThatSumToZeroPassTheirPacksChecksum() {
        var (valid, invalid) = (0, 0);
        foreach (var (path, pack, image, _) in Described()) {
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
        // Not summing to zero: 327, 332 and 215 as archived, a baseline another editor left
        // uncorrected, and one tune. Every tune with enlarged maps sums to zero.
        Assert.Equal((123, 5), (valid, invalid));
    }

    /// <summary>
    /// Tuning a ROM does not make it unrecognisable: with every cell, multiplier and breakpoint
    /// of every map rewritten and the checksum corrected, the image is still its pack's ROM and
    /// the pack still finds the same maps in it.
    /// </summary>
    [WorkspaceFact(Manifest, Editors)]
    public void AFullyRetunedImageIsStillRecognised() {
        var packs = ShippedPackTests.Packs();
        foreach (var (path, pack, image, defs) in Described()) {
            var writes = defs.SelectMany(d => d.Spans()).SelectMany(span => Enumerable.Range(span.Start, span.Length)).Select(address => (address, (byte)(image[address] ^ 0x5A)));
            var tuned = Checksum.Fix(image.With(writes), pack.Checksum);
            Assert.NotSame(image, tuned);
            Assert.True(Checksum.IsValid(tuned, pack.Checksum), path);
            Assert.Equal([pack.Id], DefinitionPacks.Identify(packs, tuned).Select(m => m.Pack.Id));
            Assert.Equal(defs, pack.Resolve(tuned).Defs);
        }
    }

    /// <summary>
    /// The P28 with ROM number 230, read through the P30-type pack, against what its source
    /// project derived for the same ROM by other means: the axes of its definitions file, the
    /// factory column multipliers and the idle ignition angle.
    /// </summary>
    [WorkspaceFact(CorpusDir + "/hondabase/ecu-roms/P28-230.bin")]
    public void TheP28230ReadsAsItsSourceProjectSays() {
        var image = Image("hondabase/ecu-roms/P28-230.bin");
        var defs = Defs("obd1-p30", image);
        var (fuelLow, fuelHigh, ignLow, ignHigh) = (defs[0], defs[1], defs[2], defs[3]);
        float[] Rounded(float[] values) => [.. values.Select(v => MathF.Round(v))];

        Assert.Equal([500, 602, 703, 797, 1000, 1297, 1500, 1703, 2000, 2188, 2594, 2906, 3000, 3406, 4000, 4438, 5000, 6000, 7000, 8000], Rounded(ParamAccess.AxisValues(image, fuelLow.RowAxis, 20)));
        Assert.Equal([0, 599, 986, 1514, 2007, 2500, 2993, 3521, 4014, 4507, 4754, 5000, 5247, 5493, 6021, 6514, 7007, 7500, 8028, 9014], Rounded(ParamAccess.AxisValues(image, fuelHigh.RowAxis, 20)));
        Assert.All(defs, d => Assert.Equal([11, 29, 40, 52, 63, 75, 87, 92, 98, 104], Rounded(ParamAccess.AxisValues(image, d.ColAxis, 10))));

        Assert.Equal([1f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 9f, 10f], Enumerable.Range(0, 10).Select(c => ParamAccess.ColumnFactor(image, fuelLow, c)));
        Assert.Equal([1f, 3f, 4f, 6f, 7f, 8f, 9f, 9f, 10f, 10f], Enumerable.Range(0, 10).Select(c => ParamAccess.ColumnFactor(image, fuelHigh, c)));
        // 16.5 degrees at idle: the first four rows and columns of the low-cam map.
        Assert.All(from row in Enumerable.Range(0, 4) from col in Enumerable.Range(0, 4) select ParamAccess.CellDisplay(image, ignLow, row, col), angle => Assert.Equal(16.5f, angle));
        Assert.InRange(ParamAccess.CellDisplay(image, ignHigh, 19, 9), 20f, 40f);
        // A point on the map: where 3200 rpm and 70 kPa fall, and the fuel value read there.
        Assert.Equal((12.49f, 4.57f), Round2(TableMath.TablePos(image, fuelLow, 3200f, 70f)!.Value));
        Assert.InRange(TableMath.Lookup(image, fuelLow, 3200f, 70f)!.Value, 75f, 325f);
    }

    private static (float, float) Round2((float Row, float Col) pos) => (MathF.Round(pos.Row, 2), MathF.Round(pos.Col, 2));

    /// <summary>
    /// The P28 304 as CROME shows the template it ships: low-cam fuel from 4 to 600, a high-cam
    /// rpm axis whose last stored byte but one is 8028 rpm. CROME reads the closing zero of an
    /// axis as 255 and shows 7936 and 8979 rpm there; the ECU, and this editor, count it as 256.
    /// </summary>
    [WorkspaceFact(Editors)]
    public void TheP28304ReadsAsCromeShowsIt() {
        var image = Image("editors/crome/P28 USDM Civic D16Z (304).NEW");
        var defs = Defs("obd1-p28", image);
        var (fuelLow, fuelHigh) = (defs[0], defs[1]);
        var fuel = (from row in Enumerable.Range(0, 20) from col in Enumerable.Range(0, 10) select ParamAccess.CellDisplay(image, fuelLow, row, col)).ToList();
        Assert.Equal((4f, 600f), (fuel.Min(), fuel.Max()));

        var low = ParamAccess.AxisValues(image, fuelLow.RowAxis, 20);
        var high = ParamAccess.AxisValues(image, fuelHigh.RowAxis, 20);
        Assert.Equal((500f, 8000f), (low[0], low[^1]));
        Assert.Equal((0f, 8028f, 9014f), (high[0], MathF.Round(high[^2]), MathF.Round(high[^1])));
        // Within CROME's rounding.
        Assert.InRange(fuelLow.RowAxis.Display(255f), 7935f, 7939f);
        Assert.Equal(8979f, MathF.Round(fuelHigh.RowAxis.Display(255f)));
    }

    /// <summary>The source project names the same four addresses for its maps.</summary>
    [WorkspaceFact("hondaromeditor/roms/p28-230/aliases.inc", "hondaecu/P28-230.bin")]
    public void TheP30PackAgreesWithTheP28SourceProjectOnWhereTheMapsAre() {
        var aliases = File.ReadAllLines(Path.Combine(Workspace.P28Project, "aliases.inc"))
            .Select(line => line.Split(';')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(words => words is [_, "EQU", var target] && target.StartsWith("tbl_"))
            .ToDictionary(words => words[0], words => Convert.ToInt32(words[2][4..], 16));
        var defs = Defs("obd1-p30", RomImage.Load(Workspace.P28Stock));
        Assert.Equal([aliases["fuel_low_cam"], aliases["fuel_high_cam"], aliases["ign_low_cam"], aliases["ign_high_cam"]], defs.Select(d => d.Offset));
    }

    /// <summary>The same ROM under two names is one image; the pack reads both alike.</summary>
    [WorkspaceFact(Manifest, Editors)]
    public void TheEditorsTemplatesAreTheFactoryImages() {
        Assert.Equal(Image("hondabase/ecu-roms/203stock.bin").Sha256(), Image("editors/crome/P30 JDM Civic B16A (203).NEW").Sha256());
        Assert.Equal(Image("hondabase/ecu-roms/304stock.bin").Sha256(), Image("editors/crome/P28 USDM Civic D16Z (304).NEW").Sha256());
        Assert.Equal(Image("hondabase/ecu-roms/273stock.bin").Sha256(), Image("editors/crome/P72 USDM Integra B18C1-3 (273).NEW").Sha256());
    }

    /// <summary>
    /// What the 24-column bases hold: ten columns in use in the Gold base and fifteen in the
    /// eCtune base, the rest padded, and rpm axes that reach past 10000 rpm on the high cam.
    /// </summary>
    [WorkspaceFact(Editors)]
    public void TheEditorsBasesReadAsTheirOwnProgramsShowThem() {
        var gold = Image("editors/crome/CROME GOLD BASE v0.94.NEW");
        var goldFuel = Defs("crome-gold", gold)[0];
        Assert.Equal(10, ParamAccess.AxisStoredValues(gold, goldFuel.ColAxis, 24).Distinct().Count() - 1);
        Assert.Equal(500f, ParamAccess.AxisValues(gold, goldFuel.RowAxis, 20)[0]);

        var ectune = Image("editors/ectune/eCt-273-v0.0.65.bin");
        var defs = Defs("ectune-273", ectune);
        Assert.Equal(8, defs.Count);
        Assert.Equal(15, ParamAccess.AxisStoredValues(ectune, defs[0].ColAxis, 24).Distinct().Count() - 1);
        var high = ParamAccess.AxisValues(ectune, defs[1].RowAxis, 20);
        Assert.Equal((2499f, 11029f), (MathF.Round(high[0]), MathF.Round(high[^1])));
        // The second set of maps has axes of its own.
        Assert.NotEqual(defs[0].RowAxis, defs[4].RowAxis);
        Assert.NotEqual(defs[0].ColAxis, defs[4].ColAxis);
    }

    /// <summary>
    /// One pack reads every size and position CROME's Boost Tools+ gives the maps of a P30-type
    /// ROM: 16 to 24 columns, in six different places among the tunes of the corpus.
    /// </summary>
    [WorkspaceFact(Manifest)]
    public void OnePackReadsEveryEnlargedLayout() {
        var layouts = Described().Where(d => d.Pack.Id is "crome-p30-boost" or "crome-p30-boost-rev")
            .Select(d => (((ParamKind.Table)d.Defs[0].Kind).Cols, FuelLow: d.Defs[0].Offset, MapAxis: d.Defs[0].ColAxis.Offset)).Distinct().Order().ToList();
        Assert.Equal([(16, 0x6EAA, 0x6E72), (16, 0x712A, 0x70F2), (18, 0x7086, 0x704C), (20, 0x6FE2, 0x6FA6), (22, 0x6F3E, 0x6F00), (24, 0x6E9A, 0x6E5A)], layouts);
        // On the P72 type the high-cam maps keep their 15 rows.
        var p72 = Described().Where(d => d.Pack.Id == "crome-p72-boost").Select(d => (d.Defs[0].Kind, d.Defs[1].Kind)).Distinct().ToList();
        Assert.Equal(3, p72.Count);
        Assert.All(p72, kinds => Assert.Equal((20, 15), (((ParamKind.Table)kinds.Item1).Rows, ((ParamKind.Table)kinds.Item2).Rows)));
    }

    /// <summary>
    /// A boosted ROM keeps the factory columns and adds boost columns after them. Read through
    /// the sensor range the tune carries, its first ten columns are the pressures of the factory
    /// axis, which the factory pack derives from the firmware instead.
    /// </summary>
    [WorkspaceFact(Manifest, Editors)]
    public void ABoostedRomKeepsTheFactoryColumnsAndAddsBoost() {
        var stock = Image("editors/crome/P30 JDM Civic B16A (203).NEW");
        var factory = ParamAccess.AxisValues(stock, Defs("obd1-p30", stock)[0].ColAxis, 10);
        var image = Image("hondabase/xenocron/crome/d/jeffsciv23.bin");
        var defs = Defs("crome-p30-boost", image);
        Assert.All(defs, d => Assert.Equal(16, ((ParamKind.Table)d.Kind).Cols));
        Assert.Equal([0x6EAA, 0x6FFA, 0x714A, 0x728A], defs.Select(d => d.Offset));
        var columns = ParamAccess.AxisValues(image, defs[0].ColAxis, 16);
        Assert.All(Enumerable.Range(0, 10), c => Assert.InRange(columns[c] - factory[c], -0.2f, 0.2f));
        // Six boost columns up to 0.78 bar over atmospheric, on the factory sensor (-59 to 1782 mbar).
        Assert.Equal((116f, 179f), (MathF.Round(columns[10]), MathF.Round(columns[15])));
        Assert.Equal((-5.9f, 178.2f), (MathF.Round(defs[0].ColAxis.Display(0f), 1), MathF.Round(defs[0].ColAxis.Display(255f), 1)));

        // Other sensors, by the range each tune carries: a 3 bar sensor reads to 2975 mbar.
        var threeBar = Image("hondabase/xenocron/crome/b/chris550-ls-101-3bar.bin");
        Assert.Equal(297.5f, MathF.Round(Defs("crome-p30-boost", threeBar)[0].ColAxis.Display(255f), 1));
        // The first version of the tool writes the range down without the offset later ones add.
        var first = Image("hondabase/xenocron/crome/d/Stevefinal.bin");
        var firstAxis = Defs("crome-p30-boost1", first)[0].ColAxis;
        Assert.Equal((2.5f, 176.4f), (MathF.Round(firstAxis.Display(0f), 1), MathF.Round(firstAxis.Display(255f), 1)));
    }

    /// <summary>Rev Tools moves the high-cam rpm lookup and gives it a scale that reaches 11000 rpm.</summary>
    [WorkspaceFact(Manifest)]
    public void RevToolsGivesTheHighCamAxisAnotherScale() {
        var image = Image("hondabase/xenocron/crome/b/New turbo civic map.bin");
        var defs = Defs("crome-p30-boost-rev", image);
        Assert.Equal((0x7116, 43.0836f), (defs[1].RowAxis.Offset, defs[1].RowAxis.Scale));
        Assert.Equal(11029f, MathF.Round(defs[1].RowAxis.Display(256f)));
        // The low-cam axis is the factory one.
        Assert.Equal((0x7102, AxisEncoding.LogBytes, 500f), (defs[0].RowAxis.Offset, defs[0].RowAxis.Encoding, defs[0].RowAxis.Scale));
    }

    /// <summary>
    /// Uberdata's boost bases halve the resolution of the MAP code to double its range. Read
    /// with that scale, their first nine columns are the pressures of the factory P72 axis.
    /// </summary>
    [WorkspaceFact(Uberdata, Editors)]
    public void TheUberdataBasesKeepTheFactoryPressuresOnAWiderCode() {
        var stock = Image("editors/crome/P72 USDM Integra B18C1-3 (273).NEW");
        var factory = ParamAccess.AxisValues(stock, Defs("obd1-p72", stock)[0].ColAxis, 10);
        var image = Image("hondabase/_from-archives/uberdata/Uberdata1.66-blake/Blake1.bin");
        var defs = Defs("uberdata-p72", image);
        Assert.Equal([new ParamKind.Table(20, 15), new ParamKind.Table(15, 15), new ParamKind.Table(20, 15), new ParamKind.Table(15, 15)], defs.Select(d => d.Kind));
        var columns = ParamAccess.AxisValues(image, defs[0].ColAxis, 15);
        Assert.All(Enumerable.Range(0, 9), c => Assert.InRange(columns[c] - factory[c], -0.01f, 0.01f));
        Assert.Equal([107f, 123f, 139f, 154f, 170f, 196f], columns[9..].Select(v => MathF.Round(v)));
    }
}
