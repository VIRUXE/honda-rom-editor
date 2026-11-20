using HRE.Core;
using HRE.Definitions;

namespace HRE.App;

/// <summary>What the checksum of an opened ROM says.</summary>
public enum ChecksumState {
    /// <summary>The ROM does not check itself.</summary>
    None,

    Valid,
    Wrong,
}

/// <summary>Why a file was not opened.</summary>
public enum OpenProblem {
    NotFound,
    IsFolder,
    InUse,
    Denied,
    BadName,

    /// <summary>Windows gave another reason; <see cref="OpenResult.Refused.SystemMessage"/> has its words.</summary>
    Unreadable,

    Empty,
    TooSmall,
    TooLarge,

    /// <summary>The right size for a ROM, but no definition pack recognises it.</summary>
    UnknownRom,
}

/// <summary>What came of opening a file: a ROM to work on, or the reason there is none.</summary>
public abstract record OpenResult {
    public sealed record Opened(RomSession Session) : OpenResult;

    /// <summary>
    /// The file was left alone. <see cref="Size"/> is known when the file could be found,
    /// <see cref="Sha256"/> when it was read, <see cref="SystemMessage"/> when Windows refused.
    /// </summary>
    public sealed record Refused(OpenProblem Problem, string Path, long? Size = null, string? Sha256 = null, string? SystemMessage = null) : OpenResult {
        public string FileName => System.IO.Path.GetFileName(Path.TrimEnd('\\', '/')) is { Length: > 0 } name ? name : Path;
    }
}

/// <summary>Why a file was not saved.</summary>
public enum SaveProblem {
    InUse,
    Denied,
    BadName,

    /// <summary>Windows gave another reason; <see cref="SaveResult.Refused.SystemMessage"/> has its words.</summary>
    Unwritable,
}

/// <summary>What came of saving: the file that was written, or the reason there is none.</summary>
public abstract record SaveResult {
    /// <summary>
    /// The ROM is in the file. <see cref="Backup"/> is where the file as it was before went, when
    /// this save was the first to replace it. <see cref="ChecksumCorrected"/> says that the
    /// checksum had to be brought in line with the changes.
    /// </summary>
    public sealed record Saved(string Path, string? Backup, bool ChecksumCorrected) : SaveResult;

    /// <summary>Nothing was written: the file, if there was one, is as it was, and the changes are still in the document.</summary>
    public sealed record Refused(SaveProblem Problem, string Path, string? SystemMessage = null) : SaveResult {
        public string FileName => System.IO.Path.GetFileName(Path.TrimEnd('\\', '/')) is { Length: > 0 } name ? name : Path;
    }
}

/// <summary>
/// A ROM image opened from a file: the document being edited, the definition pack that
/// recognised it and the parameters that pack finds in it.
/// </summary>
public sealed class RomSession {
    /// <summary>No ROM of these engine computers comes near this size; a larger file is not read at all.</summary>
    public const long LargestFile = 1 << 20;

    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);

    /// <summary>What <see cref="File.Replace(string, string, string?)"/> reports when the file to replace is held by another program.</summary>
    private const int UnableToRemoveReplaced = unchecked((int)0x80070497);

    /// <summary>What is put after the name of a file to name the copy kept of it.</summary>
    public const string BackupSuffix = ".bak";

    private RomSession(string path, RomDocument document, DefinitionPack pack, IReadOnlyList<DefinitionPack> otherMatches, IReadOnlyList<ParamDef> defs, IReadOnlyList<string> problems) {
        Path = path;
        Document = document;
        Pack = pack;
        OtherMatches = otherMatches;
        Defs = defs;
        Problems = problems;
        Checksum = Judge(document.Image);
    }

    /// <summary>The file the ROM was opened from, or the one it was last saved to.</summary>
    public string Path { get; private set; }
    public RomDocument Document { get; }
    public DefinitionPack Pack { get; }

    /// <summary>Packs that matched the image as strongly as <see cref="Pack"/>: the image cannot be told apart by fingerprint alone.</summary>
    public IReadOnlyList<DefinitionPack> OtherMatches { get; }

    public IReadOnlyList<ParamDef> Defs { get; }

    /// <summary>Parameters of the pack that were left out, and why.</summary>
    public IReadOnlyList<string> Problems { get; }

    public RomImage Image => Document.Image;

    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// What the checksum says of the file, as it was opened or last saved. Changing a table does
    /// not make the file damaged: the checksum is corrected when the ROM is saved.
    /// </summary>
    public ChecksumState Checksum { get; private set; }

    /// <summary>
    /// Writes the ROM to <paramref name="path"/> with its checksum corrected, and makes that the
    /// file of this session. The bytes go to a temporary file first and take the place of the
    /// file in one step, so a crash cannot leave half a ROM. The first time a file is replaced,
    /// the file as it was is kept next to it under the same name with <see cref="BackupSuffix"/>
    /// added; a copy that is already there is never overwritten, so the oldest state stays.
    /// Never throws for a file that cannot be written: the result says why.
    /// </summary>
    public SaveResult Save(string path) {
        string full;
        try {
            full = System.IO.Path.GetFullPath(path);
        } catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) {
            return new SaveResult.Refused(SaveProblem.BadName, path);
        }
        if (Directory.Exists(full)) return new SaveResult.Refused(SaveProblem.BadName, full);

        var spec = Pack.Checksum;
        var image = spec.Kind != ChecksumKind.None && spec.End <= Image.Length ? Core.Checksum.Fix(Image, spec) : Image;
        var temporary = full + ".tmp";
        string? backup = null;
        try {
            File.WriteAllBytes(temporary, image.Bytes);
            if (File.Exists(full)) {
                if (!File.Exists(full + BackupSuffix)) backup = full + BackupSuffix;
                File.Replace(temporary, full, backup, ignoreMetadataErrors: true);
            } else {
                File.Move(temporary, full);
            }
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) {
            try {
                File.Delete(temporary);
            } catch (Exception inner) when (inner is IOException or UnauthorizedAccessException) {
                // The half-written copy stays behind; the ROM file itself was not touched.
            }
            var problem = e switch {
                UnauthorizedAccessException => SaveProblem.Denied,
                IOException { HResult: SharingViolation or LockViolation or UnableToRemoveReplaced } => SaveProblem.InUse,
                _ => SaveProblem.Unwritable,
            };
            return new SaveResult.Refused(problem, full, e.Message);
        }

        var corrected = !ReferenceEquals(image, Image);
        Path = full;
        Checksum = Judge(image);
        Document.MarkSaved(image);
        return new SaveResult.Saved(full, backup, corrected);
    }

    /// <summary>
    /// Reads the file and looks for the pack that describes it. Never throws for a file that is
    /// missing, unreadable or not a ROM: the result says which, in a way that can be told to the user.
    /// </summary>
    public static OpenResult Open(string path, IReadOnlyList<DefinitionPack> packs) {
        string full;
        try {
            full = System.IO.Path.GetFullPath(path);
        } catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) {
            return new OpenResult.Refused(OpenProblem.BadName, path);
        }
        if (Directory.Exists(full)) return new OpenResult.Refused(OpenProblem.IsFolder, full);

        long size;
        byte[] bytes;
        try {
            var info = new FileInfo(full);
            if (!info.Exists) return new OpenResult.Refused(OpenProblem.NotFound, full);
            size = info.Length;
            if (size == 0) return new OpenResult.Refused(OpenProblem.Empty, full, size);
            if (size > LargestFile) return new OpenResult.Refused(OpenProblem.TooLarge, full, size);
            bytes = File.ReadAllBytes(full);
        } catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) {
            return new OpenResult.Refused(OpenProblem.NotFound, full);
        } catch (UnauthorizedAccessException e) {
            return new OpenResult.Refused(OpenProblem.Denied, full, SystemMessage: e.Message);
        } catch (IOException e) when (e.HResult is SharingViolation or LockViolation) {
            return new OpenResult.Refused(OpenProblem.InUse, full, SystemMessage: e.Message);
        } catch (Exception e) when (e is IOException or ArgumentException or NotSupportedException) {
            return new OpenResult.Refused(OpenProblem.Unreadable, full, SystemMessage: e.Message);
        }

        var image = new RomImage(bytes);
        if (Recognise(full, image, packs) is { } session) return new OpenResult.Opened(session);
        var smallest = packs.Count == 0 ? 0 : packs.Min(p => p.RomSize);
        return new OpenResult.Refused(size < smallest ? OpenProblem.TooSmall : OpenProblem.UnknownRom, full, size, image.Sha256());
    }

    private ChecksumState Judge(RomImage image) =>
        Pack.Checksum.Kind == ChecksumKind.None ? ChecksumState.None
        : Core.Checksum.IsValid(image, Pack.Checksum) ? ChecksumState.Valid
        : ChecksumState.Wrong;

    /// <summary>The session for an image, when a pack recognises it. Of several packs the strongest match is taken.</summary>
    public static RomSession? Recognise(string path, RomImage image, IEnumerable<DefinitionPack> packs) {
        var matches = DefinitionPacks.Identify(packs, image);
        if (matches.Count == 0) return null;
        var (pack, best) = matches[0];
        var (defs, problems) = pack.Resolve(image);
        var others = matches.Skip(1).Where(m => m.Match.CompareTo(best) == 0).Select(m => m.Pack).ToList();
        return new RomSession(path, new RomDocument(image), pack, others, defs, problems);
    }
}
