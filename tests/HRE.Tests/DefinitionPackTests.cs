using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

public class DefinitionPackTests {
    private static string PackDir => Path.Combine(AppContext.BaseDirectory, "fixtures", "packs");

    private static string MiniText() => File.ReadAllText(Path.Combine(PackDir, "mini.pack.toml"));

    private static DefinitionPack Mini() => DefinitionPacks.Parse(MiniText());

    /// <summary>The ROM the mini pack describes.</summary>
    private static RomImage MiniRom() {
        var data = new byte[0x8000];
        Array.Fill(data, (byte)0xFF, 0x4000, 0x4000);
        data[0x23] = 0x96;
        data[0x24] = 0x15;
        data[0x40] = 0xA5;
        new byte[] { 10, 20 }.CopyTo(data, 0x0F0);                    // RPM breakpoints 10, 30
        new byte[] { 1, 2, 3, 4, 5, 6 }.CopyTo(data, 0x100);          // fuel cells
        new byte[] { 0, 30, 0, 60, 0, 90 }.CopyTo(data, 0x110);       // MAP breakpoints 30, 60, 90
        new byte[] { 0, 1, 2 }.CopyTo(data, 0x120);                   // column exponents
        new byte[] { 0x10, 0x00, 0x20, 0x00, 0x00, 0x01, 0x34, 0x12 }.CopyTo(data, 0x200);
        new byte[] { 0x28, 0x23 }.CopyTo(data, 0x300);                // 9000, low byte first
        return new RomImage(data);
    }

    private static string Minimal(string extra = "", string fingerprint = "[[fingerprint]]\nbytes = [{ address = 0, value = [1] }]\n") =>
        $"id = 'x'\nname = 'X'\n{extra}\n{fingerprint}";

    [Fact]
    public void ParsesTheFixture() {
        var pack = Mini();
        Assert.Equal("mini", pack.Id);
        Assert.Equal("Mini test ROM", pack.Name);
        Assert.Equal(RomPlatform.Obd0Oki66k, pack.Platform);
        Assert.Equal(0x8000, pack.RomSize);
        Assert.Equal(new ChecksumSpec(ChecksumKind.Sum8, 0x3FFF, 0, 0x4000), pack.Checksum);
        Assert.Equal(new DatalogSpec("stock", 9600), pack.Datalog);
        Assert.Equal(2, pack.Fingerprints.Count);
        Assert.Equal(0x8000, pack.Fingerprints[0].Size);
        Assert.Null(pack.Fingerprints[1].Size);
        Assert.Equal([0xFF, 0xF0], pack.Fingerprints[1].Bytes[0].Mask);
        Assert.Empty(pack.Problems);
        Assert.Equal(3, pack.Defs.Count);

        var fuel = pack.Defs[0];
        Assert.Null(fuel.Symbol);
        Assert.Equal(0x100, fuel.Offset);
        Assert.Equal(CellFormat.U8, fuel.Cell);
        Assert.Equal(Axis.At("RPM", 0x0F0, null, AxisEncoding.DeltaBytes), fuel.RowAxis);
        Assert.Equal(Axis.At("MAP", 0x110), fuel.ColAxis);
        Assert.Equal(new ColumnMultiplier(0x120, MultiplierKind.Exponent), fuel.Multiplier);
        Assert.Equal([new ByteRange(0x100, 0x106), new ByteRange(0x0F0, 0x0F2), new ByteRange(0x110, 0x116), new ByteRange(0x120, 0x123)], fuel.Spans());

        var wide = pack.Defs[1];
        Assert.Equal(CellFormat.U16LittleEndian, wide.Cell);
        Assert.Equal(8, wide.Length);
        Assert.True(wide.RowAxis.IsFixed);

        Assert.Equal(new ParamKind.Word(3000, 11000), pack.Defs[2].Kind);
        Assert.True(pack.Defs[2].LittleEndian);
    }

    [Fact]
    public void ReadsDeltaAxesMultipliersAndWideCells() {
        var (pack, rom) = (Mini(), MiniRom());
        var fuel = pack.Defs[0];
        Assert.Equal([10f, 30f], ParamAccess.AxisValues(rom, fuel.RowAxis, 2));
        Assert.Equal([30f, 60f, 90f], ParamAccess.AxisValues(rom, fuel.ColAxis, 3));
        Assert.Equal(6, ParamAccess.Cell(rom, fuel, 1, 2));
        Assert.Equal([1f, 2f, 4f], Enumerable.Range(0, 3).Select(c => ParamAccess.ColumnFactor(rom, fuel, c)));
        Assert.Equal(24f, ParamAccess.CellDisplay(rom, fuel, 1, 2));
        // Halfway between columns 1 and 2 of row 0: (2 * 2 + 3 * 4) / 2.
        Assert.Equal(8f, TableMath.Lookup(rom, fuel, 10, 75));

        var wide = pack.Defs[1];
        Assert.Equal(0x0010, ParamAccess.Cell(rom, wide, 0, 0));
        Assert.Equal(0x1234, ParamAccess.Cell(rom, wide, 1, 1));
        Assert.Equal(0x0100 * 0.5f, ParamAccess.CellDisplay(rom, wide, 1, 0));
        Assert.Equal(1f, ParamAccess.ColumnFactor(rom, wide, 1));

        Assert.Equal(9000, ParamAccess.Word(rom, pack.Defs[2]));
    }

    [Fact]
    public void WritesWideCellsAndLittleEndianWords() {
        var (pack, rom) = (Mini(), MiniRom());
        var wide = pack.Defs[1];
        var edited = rom.With(ParamAccess.CellWrites(wide, 0, 1, 0xABCD)).With(ParamAccess.WordWrites(pack.Defs[2], 7000));
        Assert.Equal(0xCD, edited[0x202]);
        Assert.Equal(0xAB, edited[0x203]);
        Assert.Equal(0xABCD, ParamAccess.Cell(edited, wide, 0, 1));
        Assert.Equal(0x0010, ParamAccess.Cell(edited, wide, 0, 0));
        Assert.Equal(7000, ParamAccess.Word(edited, pack.Defs[2]));
        // Values are clamped to what a cell holds.
        Assert.Equal(0xFFFF, ParamAccess.Cell(rom.With(ParamAccess.CellWrites(wide, 0, 0, 70000)), wide, 0, 0));
        Assert.Equal(255, ParamAccess.Cell(rom.With(ParamAccess.CellWrites(pack.Defs[0], 0, 0, 300)), pack.Defs[0], 0, 0));
    }

    /// <summary>Moving one delta-encoded breakpoint leaves the others where they were.</summary>
    [Fact]
    public void EditsADeltaAxisWithoutMovingItsNeighbours() {
        var data = new byte[0x100];
        new byte[] { 10, 20, 30, 40 }.CopyTo(data, 0x10);             // 10, 30, 60, 100
        var rom = new RomImage(data);
        var axis = Axis.At("RPM", 0x10, null, AxisEncoding.DeltaBytes);
        Assert.Equal([10f, 30f, 60f, 100f], ParamAccess.AxisValues(rom, axis, 4));

        var moved = rom.With(ParamAccess.AxisWrites(rom, axis, 4, 1, 45));
        Assert.Equal([10f, 45f, 60f, 100f], ParamAccess.AxisValues(moved, axis, 4));
        Assert.Equal([10, 35, 15, 40], moved.Bytes.Slice(0x10, 4).ToArray());

        // Strictly between the neighbours.
        Assert.Equal(59, ParamAccess.ClampAxisValue(rom, axis, 4, 1, 9999));
        Assert.Equal(11, ParamAccess.ClampAxisValue(rom, axis, 4, 1, 0));
        // The last breakpoint may rise only as far as one byte of step allows.
        Assert.Equal(60 + 255, ParamAccess.ClampAxisValue(rom, axis, 4, 3, 9999));
        var last = rom.With(ParamAccess.AxisWrites(rom, axis, 4, 3, 9999));
        Assert.Equal([10f, 30f, 60f, 315f], ParamAccess.AxisValues(last, axis, 4));
        // The first breakpoint is its own byte.
        Assert.Equal([5f, 30f, 60f, 100f], ParamAccess.AxisValues(rom.With(ParamAccess.AxisWrites(rom, axis, 4, 0, 5)), axis, 4));

        // A step to the next breakpoint that would not fit one byte limits how far down a point can go.
        var wideGap = new RomImage([200, 250]);                       // 200, 450
        var gapAxis = Axis.At("RPM", 0, null, AxisEncoding.DeltaBytes);
        Assert.Equal(195, ParamAccess.ClampAxisValue(wideGap, gapAxis, 2, 0, 0));
        Assert.Empty(ParamAccess.AxisWrites(rom, Axis.Fixed("x", [1, 2]), 2, 0, 9));
    }

    [Fact]
    public void FingerprintsMatchBySizeBytesMaskAndHash() {
        var (pack, rom) = (Mini(), MiniRom());
        // Both fingerprints match; the second compares 8 + 4 + 8 bits, the first 16.
        Assert.Equal(new FingerprintMatch(false, 16), Fingerprints.Match(pack.Fingerprints[0], rom));
        Assert.Equal(new FingerprintMatch(false, 20), Fingerprints.Match(pack.Fingerprints[1], rom));
        Assert.Equal(new FingerprintMatch(false, 20), Fingerprints.Match(pack, rom));

        // The masked nibble may differ; the first fingerprint then fails, the second still holds.
        var variant = rom.With(0x24, 0x1F);
        Assert.Null(Fingerprints.Match(pack.Fingerprints[0], variant));
        Assert.NotNull(Fingerprints.Match(pack.Fingerprints[1], variant));
        Assert.Null(Fingerprints.Match(pack, variant.With(0x40, 0)));
        Assert.Null(Fingerprints.Match(pack, rom.With(0x23, 0x97)));

        // Size is a condition, and bytes past the end of a short image never match.
        var half = new RomImage(rom.Bytes[..0x4000]);
        Assert.Null(Fingerprints.Match(pack.Fingerprints[0], half));
        Assert.NotNull(Fingerprints.Match(pack.Fingerprints[1], half));
        Assert.Null(Fingerprints.Match(pack.Fingerprints[1], new RomImage(rom.Bytes[..0x30])));

        var hashed = new Fingerprint(null, rom.Sha256(), []);
        Assert.Equal(new FingerprintMatch(true, 0), Fingerprints.Match(hashed, rom));
        Assert.Null(Fingerprints.Match(hashed, variant));
    }

    [Fact]
    public void IdentifyRanksAnExactMatchFirst() {
        var rom = MiniRom();
        var loose = Mini();
        var exact = DefinitionPacks.Parse(Minimal(fingerprint: $"[[fingerprint]]\nsha256 = '{rom.Sha256().ToUpperInvariant()}'\n")) with { Id = "exact" };
        var other = DefinitionPacks.Parse(Minimal()) with { Id = "other" };
        Assert.Equal(["exact", "mini"], DefinitionPacks.Identify([loose, other, exact], rom).Select(m => m.Pack.Id));
        Assert.Equal(["mini"], DefinitionPacks.Identify([loose, other, exact], rom.With(0x7000, 0)).Select(m => m.Pack.Id));
        Assert.Empty(DefinitionPacks.Identify([other], rom));
    }

    [Fact]
    public void ChecksumCoversTheStatedRangeOnly() {
        var (pack, rom) = (Mini(), MiniRom());
        Assert.False(Checksum.IsValid(rom, pack.Checksum));
        var fixedRom = Checksum.Fix(rom, pack.Checksum);
        Assert.True(Checksum.IsValid(fixedRom, pack.Checksum));
        Assert.NotEqual(rom[0x3FFF], fixedRom[0x3FFF]);
        // Bytes outside the range do not matter; bytes inside do.
        Assert.True(Checksum.IsValid(fixedRom.With(0x7000, 0x12), pack.Checksum));
        Assert.False(Checksum.IsValid(fixedRom.With(0x100, 0x77), pack.Checksum));
        // An image too short for the range fails rather than throws.
        Assert.False(Checksum.IsValid(new RomImage(fixedRom.Bytes[..0x2000]), pack.Checksum));

        var none = new ChecksumSpec(ChecksumKind.None, 0, 0, 1);
        Assert.True(Checksum.IsValid(rom, none));
        Assert.Same(rom, Checksum.Fix(rom, none));
    }

    [Fact]
    public void DefaultsAreAnObd1RomWithAWholeImageChecksum() {
        var pack = DefinitionPacks.Parse(Minimal());
        Assert.Equal(RomPlatform.Obd1Oki66207, pack.Platform);
        Assert.Equal(0x8000, pack.RomSize);
        Assert.Equal(ChecksumSpec.WholeImage(0x8000), pack.Checksum);
        Assert.Equal(new ChecksumSpec(ChecksumKind.Sum8, 0x7FFF, 0, 0x8000), pack.Checksum);
        Assert.Null(pack.Datalog);
        Assert.Empty(pack.Defs);

        // The correction byte defaults to the end of the range; CROME-style ROMs keep it elsewhere.
        Assert.Equal(new ChecksumSpec(ChecksumKind.Sum8, 0x3FFF, 0, 0x4000), DefinitionPacks.Parse(Minimal("[checksum]\nend = 0x4000\n")).Checksum);
        Assert.Equal(new ChecksumSpec(ChecksumKind.Sum8, 0x3B, 0, 0x8000), DefinitionPacks.Parse(Minimal("[checksum]\nlocation = 0x3B\n")).Checksum);
        Assert.Equal(ChecksumKind.None, DefinitionPacks.Parse(Minimal("[checksum]\nkind = 'none'\n")).Checksum.Kind);
        Assert.Equal(new DatalogSpec("crome-qd2", null), DefinitionPacks.Parse(Minimal("[datalog]\nprotocol = 'crome-qd2'\n")).Datalog);
    }

    [Theory]
    [InlineData("", "")]                                                             // no fingerprint
    [InlineData("", "[[fingerprint]]\nsize = 0x8000\n")]                             // size alone
    [InlineData("", "[[fingerprint]]\nsha256 = 'abc'\n")]
    [InlineData("", "[[fingerprint]]\nbytes = [{ address = 0, value = [] }]\n")]
    [InlineData("", "[[fingerprint]]\nbytes = [{ address = 0, value = [1, 2], mask = [255] }]\n")]
    [InlineData("", "[[fingerprint]]\nbytes = [{ address = 0, value = [256] }]\n")]
    [InlineData("platform = 'obd3'", null)]
    [InlineData("[checksum]\nkind = 'crc'\n", null)]
    [InlineData("[checksum]\nstart = 0x100\nend = 0x100\n", null)]
    [InlineData("[checksum]\nend = 0x4000\nlocation = 0x4000\n", null)]
    [InlineData("[checksum]\nend = 0x8001\n", null)]
    [InlineData("[datalog]\nbaud = 9600\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='w'\nsymbol='w'\nkind='word'\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='w'\naddress=1\nsymbol='w'\nkind='word'\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='w'\naddress=1\nkind='word'\ncell='u24'\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='t'\naddress=1\nkind='table'\nrows=2\ncols=2\nrow_axis={label='r',symbol='s'}\ncol_axis={label='c',values=[1,2]}\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='t'\naddress=1\nkind='table'\nrows=2\ncols=2\nrow_axis={label='r',address=9,encoding='values'}\ncol_axis={label='c',values=[1,2]}\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='t'\naddress=1\nkind='table'\nrows=2\ncols=2\nrow_axis={label='r',values=[1,2],encoding='words'}\ncol_axis={label='c',values=[1,2]}\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='t'\naddress=1\nkind='table'\nrows=2\ncols=2\nrow_axis={label='r',values=[1,2]}\ncol_axis={label='c',values=[1,2]}\nmultiplier={address=9,kind='cubic'}\n", null)]
    [InlineData("[[param]]\ncategory='a'\nname='t'\naddress=1\nkind='table'\nrows=2\ncols=2\nrow_axis={label='r',values=[1,2]}\ncol_axis={label='c',values=[1,2]}\nmultiplier={kind='linear'}\n", null)]
    public void MalformedPacksAreRejected(string extra, string? fingerprint) {
        var text = fingerprint is null ? Minimal(extra) : Minimal(extra, fingerprint);
        Assert.Throws<HreFormatException>(() => DefinitionPacks.Parse(text));
    }

    [Fact]
    public void MissingIdOrNameIsRejected() {
        Assert.Throws<HreFormatException>(() => DefinitionPacks.Parse("name = 'X'\n[[fingerprint]]\nsha256 = '" + new string('0', 64) + "'\n"));
        Assert.Throws<HreFormatException>(() => DefinitionPacks.Parse("id = 'x'\n[[fingerprint]]\nsha256 = '" + new string('0', 64) + "'\n"));
    }

    /// <summary>A parameter that cannot exist in the ROM is reported and left out; the rest of the pack still loads.</summary>
    [Fact]
    public void ImpossibleParametersAreReportedByName() {
        const string axes = "row_axis={label='r',values=[1,2]}\ncol_axis={label='c',values=[1,2]}\n";
        var pack = DefinitionPacks.Parse(Minimal(fingerprint: "[[fingerprint]]\nbytes = [{ address = 0, value = [1] }]\n\n"
            + "[[param]]\ncategory='a'\nname='Ok'\naddress=0x10\nkind='word'\n\n"
            + "[[param]]\ncategory='a'\nname='Past'\naddress=0x7FFD\nkind='table'\nrows=2\ncols=2\n" + axes + "\n"
            + "[[param]]\ncategory='a'\nname='Wide past'\naddress=0x7FFA\nkind='table'\nrows=2\ncols=2\ncell='u16be'\n" + axes + "\n"
            + "[[param]]\ncategory='a'\nname='Mult past'\naddress=0x10\nkind='table'\nrows=2\ncols=2\n" + axes + "multiplier={address=0x7FFF}\n\n"
            + "[[param]]\ncategory='a'\nname='Delta past'\naddress=0x10\nkind='table'\nrows=2\ncols=2\nrow_axis={label='r',address=0x7FFF,encoding='delta-bytes'}\ncol_axis={label='c',values=[1,2]}\n\n"
            + "[[param]]\ncategory='a'\nname='Byte word'\naddress=0x10\nkind='word'\ncell='u8'\n\n"
            + "[[param]]\ncategory='a'\nname='Word mult'\naddress=0x10\nkind='word'\nmultiplier={address=0x20}\n"));
        Assert.Equal(["Ok"], pack.Defs.Select(d => d.Name));
        Assert.Equal([
            "Past: data is outside the 0x8000-byte ROM",
            "Wide past: data is outside the 0x8000-byte ROM",
            "Mult past: multiplier is outside the 0x8000-byte ROM",
            "Delta past: row_axis is outside the 0x8000-byte ROM",
            "Byte word: a word is u16be or u16le, not u8",
            "Word mult: only a table has a multiplier",
        ], pack.Problems);
        Assert.Equal(7, pack.Params.Count);
    }

    [Fact]
    public void LoadDirectoryCollectsPacksAndReportsBadFiles() {
        var dir = Path.Combine(Path.GetTempPath(), "hre_pack_tests", $"dir_{Environment.ProcessId}");
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        Directory.CreateDirectory(Path.Combine(dir, "obd0"));
        File.WriteAllText(Path.Combine(dir, "a.pack.toml"), MiniText());
        File.WriteAllText(Path.Combine(dir, "obd0", "b.pack.toml"), Minimal());
        File.WriteAllText(Path.Combine(dir, "obd0", "c.pack.toml"), MiniText());
        File.WriteAllText(Path.Combine(dir, "obd0", "d.pack.toml"), "id = ");
        File.WriteAllText(Path.Combine(dir, "notes.toml"), "not a pack");
        var (packs, errors) = DefinitionPacks.LoadDirectory(dir);
        Assert.Equal(["mini", "x"], packs.Select(p => p.Id));
        Assert.Equal(2, errors.Count);
        Assert.EndsWith("c.pack.toml", errors[0].Path);
        Assert.Contains("already has id `mini`", errors[0].Message);
        Assert.EndsWith("d.pack.toml", errors[1].Path);
        Assert.All(errors, e => Assert.Equal(ProjectErrorKind.Format, e.Kind));
        Assert.Empty(DefinitionPacks.LoadDirectory(Path.Combine(dir, "missing")).Packs);
        Assert.Equal(ProjectErrorKind.Io, Assert.Throws<ProjectException>(() => DefinitionPacks.Load(Path.Combine(dir, "missing.pack.toml"))).Kind);
    }

    /// <summary>The new keys are optional in a source project's definitions too, next to symbols.</summary>
    [Fact]
    public void SourceProjectDefinitionsAcceptTheSameKeys() {
        var file = ParamDefinitions.Parse("""
            platform = "obd0-66k"

            [[param]]
            category = 'Fuel'
            name = 'F'
            symbol = 'fuel'
            kind = 'table'
            rows = 2
            cols = 3
            cell = 'u16be'
            row_axis = { label = 'RPM', symbol = 'fuel_rpm', encoding = 'delta-bytes' }
            col_axis = { label = 'MAP', address = 0x300 }
            multiplier = { symbol = 'fuel_mult', kind = 'exponent' }
            """);
        Assert.Equal(RomPlatform.Obd0Oki66k, file.Platform);
        Assert.Null(ParamDefinitions.Parse("").Platform);
        var map = new SymbolMap(0x8000, "main.asm", [.. new[] { ("fuel", 0x100L), ("fuel_rpm", 0x200L), ("fuel_mult", 0x400L) }
            .Select(s => new SymbolEntry(s.Item1, s.Item2, SymbolKind.Label, "main.asm", 1))]);
        var (defs, problems) = ParamDefinitions.Resolve(file, map);
        Assert.Empty(problems);
        var def = Assert.Single(defs);
        Assert.Equal(12, def.Length);
        Assert.Equal(Axis.At("RPM", 0x200, "fuel_rpm", AxisEncoding.DeltaBytes), def.RowAxis);
        Assert.Equal(Axis.At("MAP", 0x300), def.ColAxis);
        Assert.Equal(new ColumnMultiplier(0x400, MultiplierKind.Exponent, "fuel_mult"), def.Multiplier);
        Assert.Equal(["fuel_mult"], ParamDefinitions.Resolve(file, new SymbolMap(0x8000, "main.asm", [.. map.Symbols.Where(s => s.Name != "fuel_mult")])).Problems);
    }
}
