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
        // `doc` is optional, and steps may be written inline.
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

    private const string Step = "step=[{address=1,old=[0],new=[1]}]";

    private static DefinitionPack WithPatch(string patch, string extra = "") =>
        DefinitionPacks.Parse(DefinitionPackTests.Minimal($"{extra}\n[[patch]]\n{patch}\n"));

    [Theory]
    [InlineData("name='P'\n" + Step, "every `[[patch]]` needs an `id`")]
    [InlineData("id=''\nname='P'\n" + Step, "every `[[patch]]` needs an `id`")]
    [InlineData("id='p'\n" + Step, "patch `p`: missing `name`")]
    [InlineData("id='p'\nname='P'", "patch `p`: missing `[[patch.step]]`")]
    [InlineData("id='p'\nname='P'\nstep=5", "patch `p`: `step` must be an array of tables")]
    [InlineData("id='p'\nname='P'\nstep=[{old=[0],new=[1]}]", "patch `p`: missing `address`")]
    [InlineData("id='p'\nname='P'\nstep=[{address=1,new=[1]}]", "patch `p`: missing `old`")]
    [InlineData("id='p'\nname='P'\nstep=[{address=1,old=[0]}]", "patch `p`: missing `new`")]
    [InlineData("id='p'\nname='P'\nstep=[{address=1,old=[],new=[]}]", "patch `p`: `old` must not be empty")]
    [InlineData("id='p'\nname='P'\nstep=[{address=1,old=[0,0],new=[1]}]", "patch `p`: `old` has 2 bytes, `new` has 1")]
    [InlineData("id='p'\nname='P'\nstep=[{address=1,old=[256],new=[1]}]", "patch `p`: `old` = 256 is outside 0..255")]
    [InlineData("id='p'\nname='P'\nstep=[{address=0x7FFF,old=[0,0],new=[1,1]}]", "patch `p`: the step at 0x7fff reaches past the 0x8000-byte ROM")]
    [InlineData("id='p'\nname='P'\nstep=[{address=0x7FFE,old=[0,0],new=[1,1]}]", "patch `p`: the step at 0x7ffe covers the checksum byte at 0x7fff")]
    [InlineData("id='p'\nname='P'\nstep=[{address=0x7FFF,old=[0],new=[1]}]", "patch `p`: the step at 0x7fff covers the checksum byte at 0x7fff")]
    [InlineData("id='p'\nname='P'\nstep=[{address=0x12,old=[0],new=[2]},{address=0x10,old=[0,0,0],new=[1,1,1]}]", "patch `p`: the steps at 0x10 and 0x12 overlap")]
    [InlineData("id='p'\nname='P'\nstep=[{address=1,old=[5,6],new=[5,6]},{address=9,old=[7],new=[7]}]", "patch `p`: `new` equals `old` in every step")]
    [InlineData("id='p'\nname='P'\n" + Step + "\n[[patch]]\nid='p'\nname='Q'\nstep=[{address=5,old=[0],new=[1]}]", "another patch already has id `p`")]
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
            "id='p'\nname='P'\nstep=[{address=0x12,old=[7],new=[7]},{address=0x10,old=[0,5],new=[1,5]},{address=0x7FFE,old=[0,0],new=[1,1]}]",
            "[checksum]\nlocation = 0x3B\n").Patches);
        Assert.Equal([0x12, 0x10, 0x7FFE], patch.Steps.Select(s => s.Address));
        // The bytes on either side of the checksum byte are free to patch.
        Assert.Single(WithPatch("id='p'\nname='P'\nstep=[{address=0x39,old=[0,0],new=[1,1]},{address=0x3C,old=[0],new=[1]}]", "[checksum]\nlocation = 0x3B\n").Patches);
        // A ROM that does not check itself has no checksum byte to keep clear of.
        Assert.Single(WithPatch("id='p'\nname='P'\nstep=[{address=0x7FFE,old=[0,0],new=[1,1]}]", "[checksum]\nkind = 'none'\n").Patches);
    }
}
