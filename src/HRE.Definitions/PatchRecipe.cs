using HRE.Core;

namespace HRE.Definitions;

/// <summary>
/// One replacement of a recipe: the bytes at <see cref="Address"/> are expected to be
/// <see cref="Old"/> and become <see cref="New"/>. Both have the same length.
/// </summary>
public sealed record PatchStep(int Address, IReadOnlyList<byte> Old, IReadOnlyList<byte> New);

/// <summary>A named, reversible change to a ROM image, made of steps that do not overlap.</summary>
public sealed record PatchRecipe(string Id, string Name, string Doc, IReadOnlyList<PatchStep> Steps);

/// <summary>What an image holds where a recipe writes.</summary>
public enum PatchState {
    /// <summary>Every step holds its old bytes.</summary>
    NotApplied,

    /// <summary>Every step holds its new bytes.</summary>
    Applied,

    /// <summary>Neither: a byte is not what the recipe expects, or only some steps are in place.</summary>
    Conflict,
}

/// <summary>A recipe that was refused: the image does not hold the bytes the recipe expects.</summary>
public sealed class PatchConflictException(string patch, int address, string message) : Exception($"patch `{patch}`: {message}") {
    /// <summary>The id of the recipe.</summary>
    public string Patch { get; } = patch;

    /// <summary>The first byte that is not the expected one.</summary>
    public int Address { get; } = address;
}

/// <summary>Probes, applies and reverts patch recipes.</summary>
public static class PatchEngine {
    /// <summary>What the image holds where the recipe writes. An image too short for a step is a conflict.</summary>
    public static PatchState Probe(PatchRecipe recipe, RomImage image) =>
        Mismatch(recipe, image, applied: false) is null ? PatchState.NotApplied
        : Mismatch(recipe, image, applied: true) is null ? PatchState.Applied
        : PatchState.Conflict;

    /// <summary>
    /// Replaces every step's old bytes with its new ones. Returns the image itself when the recipe
    /// is already applied; a conflict is refused with a <see cref="PatchConflictException"/>.
    /// </summary>
    public static RomImage Apply(PatchRecipe recipe, RomImage image) => Switch(recipe, image, apply: true);

    /// <summary>
    /// Puts every step's old bytes back. Returns the image itself when the recipe is not applied;
    /// a conflict is refused with a <see cref="PatchConflictException"/>.
    /// </summary>
    public static RomImage Revert(PatchRecipe recipe, RomImage image) => Switch(recipe, image, apply: false);

    /// <summary>Brings the recipe to the applied (<paramref name="apply"/>) or the not-applied state, or refuses.</summary>
    private static RomImage Switch(PatchRecipe recipe, RomImage image, bool apply) {
        // Already there: nothing to write.
        if (Mismatch(recipe, image, applied: apply) is null) return image;
        if (Mismatch(recipe, image, applied: !apply) is not { } m) return image.With(Writes(recipe, applied: apply));
        var at = m.Step.Address + m.Index;
        var reason = (uint)at < (uint)image.Length
            ? $"byte at 0x{at:x} is 0x{image[at]:x2}, expected 0x{(apply ? m.Step.Old : m.Step.New)[m.Index]:x2}"
            : $"byte at 0x{at:x} is past the end of the 0x{image.Length:x}-byte image";
        throw new PatchConflictException(recipe.Id, at, $"cannot {(apply ? "apply" : "revert")}: {reason}");
    }

    /// <summary>The byte writes that put every step's new bytes (<paramref name="applied"/>) or old bytes in place.</summary>
    private static IEnumerable<(int Address, byte Value)> Writes(PatchRecipe recipe, bool applied) {
        foreach (var step in recipe.Steps) {
            var bytes = applied ? step.New : step.Old;
            for (var i = 0; i < bytes.Count; i++) yield return (step.Address + i, bytes[i]);
        }
    }

    /// <summary>
    /// The first byte, as a step and an index into it, that is not the step's new byte
    /// (<paramref name="applied"/>) or old byte, or lies outside the image. Null when the image
    /// holds them all.
    /// </summary>
    private static (PatchStep Step, int Index)? Mismatch(PatchRecipe recipe, RomImage image, bool applied) {
        foreach (var step in recipe.Steps) {
            var wanted = applied ? step.New : step.Old;
            for (var i = 0; i < wanted.Count; i++) {
                var at = step.Address + i;
                if ((uint)at >= (uint)image.Length || image[at] != wanted[i]) return (step, i);
            }
        }
        return null;
    }
}
