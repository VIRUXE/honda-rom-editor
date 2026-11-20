using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

public class PatchTests {
    /// <summary>A 64-byte image: zeros, then 0xFF from 0x30 on like unused ROM space.</summary>
    private static RomImage Rom() {
        var data = new byte[0x40];
        Array.Fill(data, (byte)0xFF, 0x30, 0x10);
        return new RomImage(data);
    }

    /// <summary>Points three bytes at 0x10 to a routine it installs in the unused space at 0x30.</summary>
    private static PatchRecipe Recipe() => new("hook", "Hook", "", [
        new PatchStep(0x10, [0x00, 0x00, 0x00], [0x03, 0x00, 0x30]),
        new PatchStep(0x30, [0xFF, 0xFF, 0xFF, 0xFF], [0x21, 0x10, 0x03, 0xFF]),
    ]);

    /// <summary><see cref="Rom"/> with every byte of <see cref="Recipe"/> in place, written by hand.</summary>
    private static RomImage Patched() => Rom().With([(0x10, 0x03), (0x12, 0x30), (0x30, 0x21), (0x31, 0x10), (0x32, 0x03)]);

    [Fact]
    public void ProbeReportsNotAppliedForTheOldBytesAndAppliedForTheNew() {
        var recipe = Recipe();
        Assert.Equal(PatchState.NotApplied, PatchEngine.Probe(recipe, Rom()));
        Assert.Equal(PatchState.Applied, PatchEngine.Probe(recipe, Patched()));
        // Bytes outside the steps do not matter.
        Assert.Equal(PatchState.NotApplied, PatchEngine.Probe(recipe, Rom().With(0x20, 0x77)));
        Assert.Equal(PatchState.Applied, PatchEngine.Probe(recipe, Patched().With(0x20, 0x77)));
    }

    [Fact]
    public void ProbeReportsAConflictForAnyOtherBytes() {
        var recipe = Recipe();
        // A byte that is neither the old one nor the new one.
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, Rom().With(0x11, 0x55)));
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, Patched().With(0x31, 0x55)));
        // Only one of the two steps in place.
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, Rom().With([(0x10, 0x03), (0x12, 0x30)])));
        // 0x33 is 0xFF before and after: a byte the recipe leaves alone must still be the expected one.
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, Rom().With(0x33, 0x00)));
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, Patched().With(0x33, 0x00)));
        // An image too short for a step holds the recipe neither way.
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, new RomImage(Rom().Bytes[..0x32])));
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(recipe, new RomImage(Patched().Bytes[..0x32])));
    }

    [Fact]
    public void ApplyThenRevertIsByteIdentical() {
        var recipe = Recipe();
        // A byte outside the steps rides along untouched.
        var rom = Rom().With(0x20, 0x77);
        var patched = PatchEngine.Apply(recipe, rom);
        Assert.Equal(Patched().With(0x20, 0x77).ToArray(), patched.ToArray());
        Assert.Equal(PatchState.Applied, PatchEngine.Probe(recipe, patched));
        var reverted = PatchEngine.Revert(recipe, patched);
        Assert.Equal(rom.ToArray(), reverted.ToArray());
        Assert.Equal(PatchState.NotApplied, PatchEngine.Probe(recipe, reverted));
    }

    /// <summary>Only a conflict is refused: a recipe already in the state asked for is not one.</summary>
    [Fact]
    public void ARecipeAlreadyInPlaceIsLeftAlone() {
        var recipe = Recipe();
        var (rom, patched) = (Rom(), Patched());
        Assert.Same(patched, PatchEngine.Apply(recipe, patched));
        Assert.Same(rom, PatchEngine.Revert(recipe, rom));
    }

    [Fact]
    public void AConflictRefusesToApplyOrRevert() {
        var recipe = Recipe();
        // The first step would fit, the second finds a byte it does not expect: the recipe is refused whole.
        var stray = Assert.Throws<PatchConflictException>(() => PatchEngine.Apply(recipe, Rom().With(0x31, 0x55)));
        Assert.Equal("hook", stray.Patch);
        Assert.Equal(0x31, stray.Address);
        Assert.Equal("patch `hook`: cannot apply: byte at 0x31 is 0x55, expected 0xff", stray.Message);

        // Half applied: applying finds the first step already replaced, reverting finds the second one missing.
        var half = Rom().With([(0x10, 0x03), (0x12, 0x30)]);
        Assert.Equal(0x10, Assert.Throws<PatchConflictException>(() => PatchEngine.Apply(recipe, half)).Address);
        var missing = Assert.Throws<PatchConflictException>(() => PatchEngine.Revert(recipe, half));
        Assert.Equal(0x30, missing.Address);
        Assert.Equal("patch `hook`: cannot revert: byte at 0x30 is 0xff, expected 0x21", missing.Message);

        // An image that ends inside a step.
        var cut = Assert.Throws<PatchConflictException>(() => PatchEngine.Apply(recipe, new RomImage(Rom().Bytes[..0x32])));
        Assert.Equal(0x32, cut.Address);
        Assert.Equal("patch `hook`: cannot apply: byte at 0x32 is past the end of the 0x32-byte image", cut.Message);
    }

    /// <summary>A recipe that could not be probed or put back reliably cannot be built at all, in a pack or in code.</summary>
    [Fact]
    public void MalformedRecipesCannotBeBuilt() {
        Assert.Equal("`old` has 1 bytes, `new` has 2", Assert.Throws<ArgumentException>(() => new PatchStep(0x10, [0], [1, 2])).Message);
        Assert.Equal("`old` has 2 bytes, `new` has 1", Assert.Throws<ArgumentException>(() => new PatchStep(0x10, [0, 0], [1])).Message);
        Assert.Equal("`old` must not be empty", Assert.Throws<ArgumentException>(() => new PatchStep(0x10, [], [])).Message);
        // Every byte of a step has an address.
        Assert.Throws<ArgumentException>(() => new PatchStep(-1, [0], [1]));
        Assert.Throws<ArgumentException>(() => new PatchStep(int.MaxValue - 1, [0, 0], [1, 1]));
        Assert.Equal(int.MaxValue, new PatchStep(int.MaxValue - 2, [0, 0], [1, 1]).End);

        PatchStep wide = new(0x10, [0, 0, 0], [1, 1, 1]), inside = new(0x12, [0], [2]), pin = new(0x20, [7], [7]);
        Assert.Equal("the steps at 0x10 and 0x12 overlap", Assert.Throws<ArgumentException>(() => new PatchRecipe("p", "P", "", [inside, wide])).Message);
        Assert.Equal("a patch needs at least one step", Assert.Throws<ArgumentException>(() => new PatchRecipe("p", "P", "", [])).Message);
        Assert.Equal("`new` equals `old` in every step: the patch changes nothing", Assert.Throws<ArgumentException>(() => new PatchRecipe("p", "P", "", [pin])).Message);
        // Next to a step that changes something, a step that only pins bytes is fine.
        Assert.Equal(2, new PatchRecipe("p", "P", "", [wide, pin]).Steps.Count);
    }

    [Fact]
    public void PacksListTheirPatches() {
        var pack = DefinitionPackTests.Mini();
        Assert.Equal(["fast-serial", "slow-serial"], pack.Patches.Select(p => p.Id));
        var fast = pack.Patches[0];
        Assert.Equal("Fast serial routine", fast.Name);
        Assert.Equal("Answers a memory address with the byte stored there.", fast.Doc);
        Assert.Equal([0x0500, 0x7F00], fast.Steps.Select(s => s.Address));
        Assert.Equal([0x00, 0x00, 0x00], fast.Steps[0].Old);
        Assert.Equal([0x03, 0x00, 0x7F], fast.Steps[0].New);
        Assert.Equal([0xFF, 0xFF, 0xFF, 0xFF], fast.Steps[1].Old);
        Assert.Equal([0x21, 0x10, 0x03, 0xFF], fast.Steps[1].New);
        // `doc` is optional.
        var slow = pack.Patches[1];
        Assert.Equal("", slow.Doc);
        Assert.Equal([0x03, 0x80, 0x7F], Assert.Single(slow.Steps).New);
        // A pack need not have any.
        Assert.Empty(DefinitionPacks.Parse(DefinitionPackTests.Minimal()).Patches);
    }

    /// <summary>Two patches that want the same bytes: with one in place the other is a conflict until the first is reverted.</summary>
    [Fact]
    public void AlternativePatchesExcludeEachOther() {
        var (pack, rom) = (DefinitionPackTests.Mini(), DefinitionPackTests.MiniRom());
        var (fast, slow) = (pack.Patches[0], pack.Patches[1]);
        Assert.Equal(PatchState.NotApplied, PatchEngine.Probe(fast, rom));
        Assert.Equal(PatchState.NotApplied, PatchEngine.Probe(slow, rom));

        var patched = PatchEngine.Apply(fast, rom);
        Assert.Equal(PatchState.Applied, PatchEngine.Probe(fast, patched));
        Assert.Equal(PatchState.Conflict, PatchEngine.Probe(slow, patched));
        Assert.Equal(0x0500, Assert.Throws<PatchConflictException>(() => PatchEngine.Apply(slow, patched)).Address);

        var reverted = PatchEngine.Revert(fast, patched);
        Assert.Equal(rom.ToArray(), reverted.ToArray());
        Assert.Equal(PatchState.Applied, PatchEngine.Probe(slow, PatchEngine.Apply(slow, reverted)));
    }

    /// <summary>
    /// The checksum is corrected after patching as after any other edit. Reverting and correcting
    /// again gives back every byte of a ROM whose checksum was right to begin with.
    /// </summary>
    [Fact]
    public void PatchingAndRevertingWithTheChecksumCorrectedRestoresEveryByte() {
        var pack = DefinitionPackTests.Mini();
        var fast = pack.Patches[0];
        var rom = Checksum.Fix(DefinitionPackTests.MiniRom(), pack.Checksum);

        var patched = Checksum.Fix(PatchEngine.Apply(fast, rom), pack.Checksum);
        Assert.NotEqual(rom[pack.Checksum.Location], patched[pack.Checksum.Location]);
        Assert.Equal(PatchState.Applied, PatchEngine.Probe(fast, patched));

        var reverted = Checksum.Fix(PatchEngine.Revert(fast, patched), pack.Checksum);
        Assert.Equal(rom.ToArray(), reverted.ToArray());
    }

    private const string Steps = "steps=[{address=1,old=[0],new=[1]}]";

    private static DefinitionPack WithPatch(string patch, string extra = "") =>
        DefinitionPacks.Parse(DefinitionPackTests.Minimal($"{extra}\n[[patch]]\n{patch}\n"));

    [Theory]
    [InlineData("name='P'\n" + Steps, "every `[[patch]]` needs an `id`")]
    [InlineData("id=''\nname='P'\n" + Steps, "every `[[patch]]` needs an `id`")]
    [InlineData("id=' '\nname='P'\n" + Steps, "every `[[patch]]` needs an `id`")]
    [InlineData("id=5\nname='P'\n" + Steps, "the `id` of a `[[patch]]` must be a string")]
    [InlineData("id='p'\n" + Steps, "patch `p`: missing `name`")]
    [InlineData("id='p'\nname=''\n" + Steps, "patch `p`: `name` must not be empty")]
    [InlineData("id='p'\nname='  '\n" + Steps, "patch `p`: `name` must not be empty")]
    [InlineData("id='p'\nname='P'", "patch `p`: missing `steps`")]
    [InlineData("id='p'\nname='P'\nsteps=[]", "patch `p`: a patch needs at least one step")]
    [InlineData("id='p'\nname='P'\nsteps=5", "patch `p`: `steps` must be an inline list")]
    [InlineData("id='p'\nname='P'\nsteps=[5]", "patch `p`: `steps` must be an inline list")]
    // Steps have one spelling, and tables written after an inline list do not take its place.
    [InlineData("id='p'\nname='P'\n[[patch.steps]]\naddress=1\nold=[0]\nnew=[1]", "patch `p`: `steps` must be an inline list")]
    [InlineData("id='p'\nname='P'\n" + Steps + "\n[[patch.steps]]\naddress=5\nold=[0]\nnew=[1]", "`patch.steps` is already defined")]
    // A second `steps`, or a second `old`, would otherwise take the place of the first.
    [InlineData("id='p'\nname='P'\n" + Steps + "\nsteps=[{address=5,old=[0],new=[1]}]", "`patch.steps` is already defined")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[0],old=[9],new=[1]}]", "`patch.steps.old` is already defined")]
    [InlineData("id='p'\nname='P'\nsteps=[{old=[0],new=[1]}]", "patch `p`: step 1: missing `address`")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,new=[1]}]", "patch `p`: step 1: missing `old`")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[0]}]", "patch `p`: step 1: missing `new`")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[],new=[]}]", "patch `p`: step 1: `old` must not be empty")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[0,0],new=[1]}]", "patch `p`: step 1: `old` has 2 bytes, `new` has 1")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[256],new=[1]}]", "patch `p`: step 1: `old` = 256 is outside 0..255")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=-1,old=[0],new=[1]}]", "patch `p`: step 1: `address` = -1 is outside 0..2147483647")]
    // A key the editor does not know could change what the step means.
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[0],new=[1],mask=[15]}]", "patch `p`: step 1: unknown key `mask`")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=0x7FFF,old=[0,0],new=[1,1]}]", "patch `p`: step 1: reaches past the 0x8000-byte ROM")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=2147483647,old=[0,0],new=[1,1]}]", "patch `p`: step 1: reaches past the 0x8000-byte ROM")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[0],new=[1]},{address=0x7FFF,old=[0,0],new=[1,1]}]", "patch `p`: step 2: reaches past the 0x8000-byte ROM")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=0x7FFE,old=[0,0],new=[1,1]}]", "patch `p`: step 1: covers the checksum byte at 0x7fff")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=0x7FFF,old=[0],new=[1]}]", "patch `p`: step 1: covers the checksum byte at 0x7fff")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[0],new=[1]},{address=0x7FFF,old=[0],new=[1]}]", "patch `p`: step 2: covers the checksum byte at 0x7fff")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=0x12,old=[0],new=[2]},{address=0x10,old=[0,0,0],new=[1,1,1]}]", "patch `p`: the steps at 0x10 and 0x12 overlap")]
    [InlineData("id='p'\nname='P'\nsteps=[{address=1,old=[5,6],new=[5,6]},{address=9,old=[7],new=[7]}]", "patch `p`: `new` equals `old` in every step")]
    [InlineData("id='p'\nname='P'\n" + Steps + "\n[[patch]]\nid='p'\nname='Q'\nsteps=[{address=5,old=[0],new=[1]}]", "another patch already has id `p`")]
    [InlineData("id='p'\nname='P'\n" + Steps + "\n[[patch]]\nid='q'\nname='Q'\nsteps=[{address=5,old=[0],new=[1]}]\n[[patch]]\nid='p'\nname='R'\nsteps=[{address=9,old=[0],new=[1]}]", "another patch already has id `p`")]
    public void MalformedPatchesAreRejected(string patch, string reason) {
        var e = Assert.Throws<HreFormatException>(() => WithPatch(patch));
        Assert.Contains(reason, e.Message);
    }

    /// <summary>What the checks above must not catch.</summary>
    [Fact]
    public void StepsMayTouchRepeatBytesAndEndWithTheRom() {
        // Steps that touch, in any order; a byte or a whole step that stays the same pins down
        // something the patch relies on; the last step ends on the last byte of the ROM.
        var patch = Assert.Single(WithPatch(
            "id='p'\nname='P'\nsteps=[{address=0x12,old=[7],new=[7]},{address=0x10,old=[0,5],new=[1,5]},{address=0x7FFE,old=[0,0],new=[1,1]}]",
            "[checksum]\nlocation = 0x3B\n").Patches);
        Assert.Equal([0x12, 0x10, 0x7FFE], patch.Steps.Select(s => s.Address));
        // The bytes on either side of the checksum byte are free to patch.
        Assert.Single(WithPatch("id='p'\nname='P'\nsteps=[{address=0x39,old=[0,0],new=[1,1]},{address=0x3C,old=[0],new=[1]}]", "[checksum]\nlocation = 0x3B\n").Patches);
        // A ROM that does not check itself has no checksum byte to keep clear of.
        Assert.Single(WithPatch("id='p'\nname='P'\nsteps=[{address=0x7FFE,old=[0,0],new=[1,1]}]", "[checksum]\nkind = 'none'\n").Patches);
    }

    private static DefinitionPack WithPatches(params string[] steps) =>
        DefinitionPacks.Parse(DefinitionPackTests.Minimal(string.Concat(steps.Select((s, i) => $"\n[[patch]]\nid='{(char)('a' + i)}'\nname='P'\nsteps=[{s}]\n"))));

    /// <summary>
    /// Each pair loads fine one patch at a time. Together, switching one could leave the other
    /// half in place while every call succeeds, so the pack is refused.
    /// </summary>
    [Theory]
    // The second is one step of the first: reverting it would take the routine from under the first's jump.
    [InlineData("{address=0x500,old=[0,0,0],new=[3,0,0x7F]},{address=0x7F00,old=[255,255],new=[0x21,0x10]}", "{address=0x7F00,old=[255,255],new=[0x21,0x10]}", 0x7F00)]
    // The second pins the routine the first installs: the first could be reverted from under it.
    [InlineData("{address=0x7F00,old=[255,255],new=[0x21,0x10]}", "{address=0x500,old=[0],new=[3]},{address=0x7F00,old=[0x21,0x10],new=[0x21,0x10]}", 0x7F00)]
    // The second writes a byte the first leaves as it found it.
    [InlineData("{address=0x7F00,old=[255,255],new=[0x21,255]}", "{address=0x7F01,old=[255],new=[0x55]}", 0x7F01)]
    // The second changes a byte the first pins.
    [InlineData("{address=0x500,old=[0],new=[3]},{address=0x600,old=[0x77],new=[0x77]}", "{address=0x600,old=[0x77],new=[0x78]}", 0x600)]
    // The second builds on the first.
    [InlineData("{address=0x500,old=[0],new=[3]}", "{address=0x500,old=[3],new=[4]}", 0x500)]
    // The same patch twice under two ids.
    [InlineData("{address=0x500,old=[0],new=[3]}", "{address=0x500,old=[0],new=[3]}", 0x500)]
    public void PatchesThatShareBytesMustExcludeEachOther(string first, string second, int shared) {
        var e = Assert.Throws<HreFormatException>(() => WithPatches(first, second));
        Assert.Contains($"patches `a` and `b` share the byte at 0x{shared:x} but do not exclude each other", e.Message);
        // The order they are written in makes no difference, and a patch in between does not hide the pair.
        Assert.Contains("do not exclude each other", Assert.Throws<HreFormatException>(() => WithPatches(second, first)).Message);
        e = Assert.Throws<HreFormatException>(() => WithPatches(first, "{address=0x10,old=[0],new=[1]}", second));
        Assert.Contains("patches `a` and `c` share", e.Message);
    }

    [Fact]
    public void PatchesMayShareBytesAsAlternativesOrAsPins() {
        // Alternatives: each one's new bytes are neither the other's old bytes nor its new ones.
        Assert.Equal(2, WithPatches("{address=0x500,old=[0,0,0],new=[3,0,0x7F]}", "{address=0x500,old=[0,0,0],new=[3,0x80,0x7F]}").Patches.Count);
        // Two patches that rely on the same code and leave it alone.
        Assert.Equal(2, WithPatches("{address=0x500,old=[0],new=[3]},{address=0x600,old=[0x77],new=[0x77]}", "{address=0x510,old=[0],new=[3]},{address=0x600,old=[0x77],new=[0x77]}").Patches.Count);
        // Patches for two variants of the ROM pin different code at the same place.
        Assert.Equal(2, WithPatches("{address=0x500,old=[0],new=[3]},{address=0x600,old=[0x77],new=[0x77]}", "{address=0x510,old=[0],new=[3]},{address=0x600,old=[0x78],new=[0x78]}").Patches.Count);
    }

    /// <summary>
    /// The shared-byte rule against every case of a tiny ROM: every recipe over a few addresses
    /// and byte values, and every image. The rule turns a pair away exactly when some image
    /// exists in which switching one recipe takes the other out of the applied state.
    /// </summary>
    [Theory]
    [InlineData(2, 3, 84)]      // three values: a byte can differ from both the old and the new byte of another recipe
    [InlineData(3, 2, 98)]      // three addresses: two recipes can pin one byte and each change another
    public void TheSharedByteRuleRefusesExactlyThePairsThatCanBreakEachOther(int addresses, int byteValues, int recipeCount) {
        static IEnumerable<T[]> Tuples<T>(IReadOnlyList<T> items, int length) {
            if (length == 0) return [[]];
            return from head in Tuples(items, length - 1) from item in items select head.Append(item).ToArray();
        }

        var values = Enumerable.Range(0, byteValues).Select(v => (byte)v).ToArray();
        // What a recipe does at one address: nothing, or one of the (old, new) pairs.
        var options = new List<(byte Old, byte New)?> { null };
        options.AddRange(from o in values from n in values select ((byte Old, byte New)?)(o, n));
        var recipes = new List<PatchRecipe>();
        foreach (var choice in Tuples(options, addresses)) {
            var steps = choice.Select((c, at) => c is { } x ? new PatchStep(at, [x.Old], [x.New]) : null).OfType<PatchStep>().ToList();
            if (steps.Any(s => s.Old[0] != s.New[0])) recipes.Add(new PatchRecipe("r", "R", "", steps));
        }
        Assert.Equal(recipeCount, recipes.Count);
        var images = Tuples(values, addresses).Select(bytes => new RomImage(bytes)).ToList();

        static RomImage Switched(PatchRecipe recipe, RomImage image) =>
            PatchEngine.Probe(recipe, image) == PatchState.NotApplied ? PatchEngine.Apply(recipe, image) : PatchEngine.Revert(recipe, image);
        bool Breaks(PatchRecipe switched, PatchRecipe other) => images.Any(image =>
            PatchEngine.Probe(other, image) == PatchState.Applied
            && PatchEngine.Probe(switched, image) != PatchState.Conflict
            && PatchEngine.Probe(other, Switched(switched, image)) != PatchState.Applied);

        var (refused, admitted) = (0, 0);
        foreach (var a in recipes) {
            foreach (var b in recipes) {
                var canBreak = Breaks(a, b) || Breaks(b, a);
                Assert.Equal(canBreak, PatchEngine.Clash(a, b) is not null);
                if (canBreak) refused++;
                else admitted++;
            }
        }
        // Both answers occur often, so the comparison above is not vacuous.
        Assert.True(refused > 1000 && admitted > 1000, $"{refused} refused, {admitted} admitted");
    }

    /// <summary>A patch that changed what a fingerprint compares would leave a ROM that its own pack no longer recognises.</summary>
    [Fact]
    public void APatchMayNotChangeWhatAFingerprintCompares() {
        const string fingerprints = "[[fingerprint]]\nbytes = [{ address = 0x23, value = [0x96, 0x10], mask = [0xFF, 0xF0] }, { address = 0x30, value = [7] }]\n\n"
            + "[[fingerprint]]\nbytes = [{ address = 0x40, value = [0xA5] }]\n";
        static DefinitionPack Pack(string steps, string fingerprint = fingerprints) =>
            DefinitionPacks.Parse(DefinitionPackTests.Minimal($"\n[[patch]]\nid='p'\nname='P'\nsteps=[{steps}]\n", fingerprint));

        var e = Assert.Throws<HreFormatException>(() => Pack("{address=0x20,old=[0,0,0,0x96],new=[1,1,1,0x97]}"));
        Assert.Contains("patch `p`: step 1: changes the byte at 0x23, which a fingerprint compares", e.Message);
        // Every compared range of every fingerprint counts.
        Assert.Contains("step 2: changes the byte at 0x30", Assert.Throws<HreFormatException>(() => Pack("{address=0x50,old=[0],new=[1]},{address=0x30,old=[7],new=[8]}")).Message);
        Assert.Contains("changes the byte at 0x40", Assert.Throws<HreFormatException>(() => Pack("{address=0x40,old=[0xA5],new=[0]}")).Message);
        // Under a mask only the compared bits count: at 0x24 that is the high nibble.
        Assert.Contains("changes the byte at 0x24", Assert.Throws<HreFormatException>(() => Pack("{address=0x24,old=[0x15],new=[0x25]}")).Message);
        Assert.Single(Pack("{address=0x24,old=[0x15],new=[0x1F]}").Patches);
        // A step may pin a compared byte, and may change the bytes right beside a compared range.
        Assert.Single(Pack("{address=0x22,old=[0,0x96],new=[1,0x96]},{address=0x25,old=[0],new=[1]}").Patches);
        // A hash names one exact image, which no edited ROM is: it puts no bytes out of reach.
        Assert.Single(Pack("{address=0x23,old=[0x96],new=[0x97]}", $"[[fingerprint]]\nsha256 = '{new string('0', 64)}'\n").Patches);
    }

    [Fact]
    public void StepsAreHeldToTheRomSizeThePackStates() {
        var e = Assert.Throws<HreFormatException>(() => WithPatch("id='p'\nname='P'\nsteps=[{address=0x3FFF,old=[0,0],new=[1,1]}]", "rom_size = 0x4000\n"));
        Assert.Contains("patch `p`: step 1: reaches past the 0x4000-byte ROM", e.Message);
        Assert.Single(WithPatch("id='p'\nname='P'\nsteps=[{address=0x8000,old=[0,0],new=[1,1]}]", "rom_size = 0x10000\n").Patches);
    }
}
