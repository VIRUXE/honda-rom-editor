using HRE.Core;
using HRE.Definitions;

namespace HRE.App;

/// <summary>What the checksum of an opened ROM says.</summary>
public enum ChecksumState {
    /// <summary>No pack describes the ROM, so nothing is known about its checksum.</summary>
    Unknown,

    /// <summary>The ROM does not check itself.</summary>
    None,

    Valid,
    Wrong,
}

/// <summary>
/// A ROM image opened from a file: the document being edited, the definition pack that
/// recognised it (none when no pack did) and the parameters that pack finds in it.
/// </summary>
public sealed class RomSession {
    private RomSession(string path, RomDocument document, DefinitionPack? pack, IReadOnlyList<DefinitionPack> otherMatches, IReadOnlyList<ParamDef> defs, IReadOnlyList<string> problems) {
        Path = path;
        Document = document;
        Pack = pack;
        OtherMatches = otherMatches;
        Defs = defs;
        Problems = problems;
    }

    public string Path { get; }
    public RomDocument Document { get; }
    public DefinitionPack? Pack { get; }

    /// <summary>Packs that matched the image as strongly as <see cref="Pack"/>: the image cannot be told apart by fingerprint alone.</summary>
    public IReadOnlyList<DefinitionPack> OtherMatches { get; }

    public IReadOnlyList<ParamDef> Defs { get; }

    /// <summary>Parameters of the pack that were left out, and why.</summary>
    public IReadOnlyList<string> Problems { get; }

    public RomImage Image => Document.Image;

    public string FileName => System.IO.Path.GetFileName(Path);

    public ChecksumState Checksum => Pack switch {
        null => ChecksumState.Unknown,
        { Checksum.Kind: ChecksumKind.None } => ChecksumState.None,
        _ => Core.Checksum.IsValid(Image, Pack.Checksum) ? ChecksumState.Valid : ChecksumState.Wrong,
    };

    /// <summary>Reads the file and looks for the pack that describes it. Throws what reading the file throws.</summary>
    public static RomSession Open(string path, IEnumerable<DefinitionPack> packs) {
        var full = System.IO.Path.GetFullPath(path);
        return Open(full, RomImage.Load(full), packs);
    }

    public static RomSession Open(string path, RomImage image, IEnumerable<DefinitionPack> packs) {
        var document = new RomDocument(image);
        var matches = DefinitionPacks.Identify(packs, image);
        if (matches.Count == 0) return new RomSession(path, document, null, [], [], []);
        var (pack, best) = matches[0];
        var (defs, problems) = pack.Resolve(image);
        var others = matches.Skip(1).Where(m => m.Match.CompareTo(best) == 0).Select(m => m.Pack).ToList();
        return new RomSession(path, document, pack, others, defs, problems);
    }
}
