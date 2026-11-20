namespace HRE.Core;

/// <summary>One changed byte.</summary>
public readonly record struct RomEdit(int Address, byte Old, byte New);

/// <summary>A group of byte changes that is undone and redone as one step.</summary>
public sealed record EditTransaction(string Label, IReadOnlyList<RomEdit> Edits);

/// <summary>A ROM being edited: the current image plus its undo/redo history.</summary>
public sealed class RomDocument(RomImage image) {
    private readonly Stack<EditTransaction> done = new();
    private readonly Stack<EditTransaction> undone = new();
    private EditTransaction? saved;

    public RomImage Image { get; private set; } = image;

    public event Action? Changed;

    public bool CanUndo => done.Count > 0;
    public bool CanRedo => undone.Count > 0;
    public string? UndoLabel => CanUndo ? done.Peek().Label : null;
    public string? RedoLabel => CanRedo ? undone.Peek().Label : null;

    /// <summary>Whether the image differs from the last saved state.</summary>
    public bool IsDirty => !ReferenceEquals(done.Count > 0 ? done.Peek() : null, saved);

    /// <summary>
    /// Takes the current state as the saved one. <paramref name="written"/> is the image as it
    /// went into the file, when saving changed it: the checksum is corrected on the way out, and
    /// that correction is not a step to undo.
    /// </summary>
    public void MarkSaved(RomImage? written = null) {
        saved = done.Count > 0 ? done.Peek() : null;
        if (written is null || written.Bytes.SequenceEqual(Image.Bytes)) return;
        ArgumentOutOfRangeException.ThrowIfNotEqual(written.Length, Image.Length);
        Image = written;
        Changed?.Invoke();
    }

    /// <summary>
    /// Applies the writes as one undoable step. Writes that do not change a byte are dropped;
    /// returns false, recording nothing, when no byte changes. Throws when a write is outside the image.
    /// </summary>
    public bool Apply(string label, IEnumerable<(int Address, byte Value)> writes) {
        var updated = Image.With(writes);
        if (ReferenceEquals(updated, Image)) return false;
        var edits = new List<RomEdit>();
        for (var at = 0; at < updated.Length; at++)
            if (Image[at] != updated[at]) edits.Add(new RomEdit(at, Image[at], updated[at]));
        done.Push(new EditTransaction(label, edits));
        undone.Clear();
        Image = updated;
        Changed?.Invoke();
        return true;
    }

    public bool Undo() {
        if (!done.TryPop(out var step)) return false;
        Image = Image.With(step.Edits.Select(e => (e.Address, e.Old)));
        undone.Push(step);
        Changed?.Invoke();
        return true;
    }

    public bool Redo() {
        if (!undone.TryPop(out var step)) return false;
        Image = Image.With(step.Edits.Select(e => (e.Address, e.New)));
        done.Push(step);
        Changed?.Invoke();
        return true;
    }
}
