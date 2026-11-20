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

/// <summary>
/// A ROM image opened from a file: the document being edited, the definition pack that
/// recognised it and the parameters that pack finds in it.
/// </summary>
public sealed class RomSession {
    /// <summary>No ROM of these engine computers comes near this size; a larger file is not read at all.</summary>
    public const long LargestFile = 1 << 20;

    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);

    private RomSession(string path, RomDocument document, DefinitionPack pack, IReadOnlyList<DefinitionPack> otherMatches, IReadOnlyList<ParamDef> defs, IReadOnlyList<string> problems) {
        Path = path;
        Document = document;
        Pack = pack;
        OtherMatches = otherMatches;
        Defs = defs;
        Problems = problems;
    }

    public string Path { get; }
    public RomDocument Document { get; }
    public DefinitionPack Pack { get; }

    /// <summary>Packs that matched the image as strongly as <see cref="Pack"/>: the image cannot be told apart by fingerprint alone.</summary>
    public IReadOnlyList<DefinitionPack> OtherMatches { get; }

    public IReadOnlyList<ParamDef> Defs { get; }

    /// <summary>Parameters of the pack that were left out, and why.</summary>
    public IReadOnlyList<string> Problems { get; }

    public RomImage Image => Document.Image;

    public string FileName => System.IO.Path.GetFileName(Path);

    public ChecksumState Checksum =>
        Pack.Checksum.Kind == ChecksumKind.None ? ChecksumState.None
        : Core.Checksum.IsValid(Image, Pack.Checksum) ? ChecksumState.Valid
        : ChecksumState.Wrong;

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
