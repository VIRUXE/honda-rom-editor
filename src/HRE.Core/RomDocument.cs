namespace HRE.Core;

/// <summary>One changed byte.</summary>
public readonly record struct RomEdit(int Address, byte Old, byte New);

/// <summary>A group of byte changes that is undone and redone as one step.</summary>
public sealed record EditTransaction(string Label, IReadOnlyList<RomEdit> Edits);

/// <summary>A ROM being edited: the current image plus its undo/redo history.</summary>
public sealed class RomDocument(RomImage image) {
    private readonly Stack<EditTransaction> _done = new();
    private readonly Stack<EditTransaction> _undone = new();
    private EditTransaction? _saved;

    public RomImage Image { get; private set; } = image;

    public event Action? Changed;

    public bool CanUndo => _done.Count > 0;
    public bool CanRedo => _undone.Count > 0;
    public string? UndoLabel => CanUndo ? _done.Peek().Label : null;
    public string? RedoLabel => CanRedo ? _undone.Peek().Label : null;

    /// <summary>Whether the image differs from the last saved state.</summary>
    public bool IsDirty => !ReferenceEquals(_done.Count > 0 ? _done.Peek() : null, _saved);

    /// <summary>
    /// Takes the current state as the saved one. <paramref name="written"/> is the image as it
    /// went into the file, when saving changed it: the checksum is corrected on the way out, and
    /// that correction is not a step to undo.
    /// </summary>
    public void MarkSaved(RomImage? written = null) {
        _saved = _done.Count > 0 ? _done.Peek() : null;
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
        _done.Push(new EditTransaction(label, edits));
        _undone.Clear();
        Image = updated;
        Changed?.Invoke();
        return true;
    }

    public bool Undo() {
        if (!_done.TryPop(out var step)) return false;
        Image = Image.With(step.Edits.Select(e => (e.Address, e.Old)));
        _undone.Push(step);
        Changed?.Invoke();
        return true;
    }

    public bool Redo() {
        if (!_undone.TryPop(out var step)) return false;
        Image = Image.With(step.Edits.Select(e => (e.Address, e.New)));
        _done.Push(step);
        Changed?.Invoke();
        return true;
    }
}
