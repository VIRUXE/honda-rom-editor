using HRE.Core;

namespace HRE.Definitions;

/// <summary>
/// One replacement of a recipe: the bytes at <see cref="Address"/> are expected to be
/// <see cref="Old"/> and become <see cref="New"/>. Both have the same length, at least one byte,
/// and every byte has an address; a step that breaks this cannot be built
/// (<see cref="ArgumentException"/>).
/// </summary>
public sealed record PatchStep(int Address, IReadOnlyList<byte> Old, IReadOnlyList<byte> New) {
    public int Address { get; } = Address >= 0 && Address <= int.MaxValue - Old.Count ? Address : throw new ArgumentException($"a step of {Old.Count} bytes cannot start at {Address}");
    public IReadOnlyList<byte> Old { get; } = Old.Count > 0 ? Old : throw new ArgumentException("`old` must not be empty");
    public IReadOnlyList<byte> New { get; } = New.Count == Old.Count ? New : throw new ArgumentException($"`old` has {Old.Count} bytes, `new` has {New.Count}");

    /// <summary>One past the last address the step covers.</summary>
    public int End => Address + Old.Count;
}

/// <summary>
/// A named, reversible change to a ROM image. It has at least one step, its steps do not overlap
/// and it changes at least one byte; a recipe that breaks this cannot be built
/// (<see cref="ArgumentException"/>), so the engine never writes one.
/// </summary>
public sealed record PatchRecipe(string Id, string Name, string Doc, IReadOnlyList<PatchStep> Steps) {
    public IReadOnlyList<PatchStep> Steps { get; } = Checked(Steps);

    private static IReadOnlyList<PatchStep> Checked(IReadOnlyList<PatchStep> steps) {
        if (steps.Count == 0) throw new ArgumentException("a patch needs at least one step");
        var ordered = steps.OrderBy(s => s.Address).ToList();
        for (var i = 1; i < ordered.Count; i++) {
            var (before, after) = (ordered[i - 1], ordered[i]);
            if (before.End > after.Address) throw new ArgumentException($"the steps at 0x{before.Address:x} and 0x{after.Address:x} overlap");
        }
        // Such a recipe would probe as applied and as not applied at once.
        if (steps.All(s => s.Old.SequenceEqual(s.New))) throw new ArgumentException("`new` equals `old` in every step: the patch changes nothing");
        return steps;
    }
}

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

    /// <summary>
    /// Whether two recipes can be offered for the same image. They cannot when they share a byte
    /// that either of them changes, unless each is a conflict whenever the other is applied: only
    /// then is there no image in which switching one takes the other out of the applied state.
    /// Returns the first address they share when they cannot, null when they can.
    /// </summary>
    public static int? Clash(PatchRecipe a, PatchRecipe b) {
        var first = new Dictionary<int, (byte Old, byte New)>();
        foreach (var step in a.Steps)
            for (var i = 0; i < step.Old.Count; i++) first[step.Address + i] = (step.Old[i], step.New[i]);
        var shared = new List<(int At, byte AOld, byte ANew, byte BOld, byte BNew)>();
        foreach (var step in b.Steps) {
            for (var i = 0; i < step.Old.Count; i++) {
                if (first.TryGetValue(step.Address + i, out var x)) shared.Add((step.Address + i, x.Old, x.New, step.Old[i], step.New[i]));
            }
        }
        // Bytes that both only pin are written by neither.
        if (shared.All(s => s.AOld == s.ANew && s.BOld == s.BNew)) return null;
        // Each rules the other out when, somewhere on the shared bytes, its new byte is not the
        // other's new byte, and somewhere it is not the other's old byte: with either applied
        // the other is then neither applied nor ready to be.
        var exclusive = shared.Any(s => s.ANew != s.BNew) && shared.Any(s => s.ANew != s.BOld) && shared.Any(s => s.BNew != s.AOld);
        return exclusive ? null : shared[0].At;
    }

    /// <summary>Brings the recipe to the applied (<paramref name="apply"/>) or the not-applied state, or refuses.</summary>
    private static RomImage Switch(PatchRecipe recipe, RomImage image, bool apply) {
        // Already there: nothing to write.
        if (Mismatch(recipe, image, applied: apply) is null) return image;
        if (Mismatch(recipe, image, applied: !apply) is not { } m) return image.With(Writes(recipe, applied: apply));
        var at = m.Step.Address + m.Index;
        var reason = at < image.Length
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
                if (at >= image.Length || image[at] != wanted[i]) return (step, i);
            }
        }
        return null;
    }
}
