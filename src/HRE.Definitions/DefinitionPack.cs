using HRE.Core;
using Tomlyn.Model;

namespace HRE.Definitions;

/// <summary>
/// Bytes expected at an address. With a <see cref="Mask"/> only the bits set in it are compared;
/// without one every bit is.
/// </summary>
public sealed record ByteMatch(int Address, IReadOnlyList<byte> Value, IReadOnlyList<byte>? Mask);

/// <summary>
/// One way to recognise a ROM: every condition given must hold. <see cref="Size"/> is the image
/// length, <see cref="Sha256"/> the hash of the whole image, <see cref="Bytes"/> masked byte ranges.
/// </summary>
public sealed record Fingerprint(int? Size, string? Sha256, IReadOnlyList<ByteMatch> Bytes);

/// <summary>
/// How well a fingerprint matched: an exact (hash) match beats any other, then the one that
/// compared more bits wins.
/// </summary>
public readonly record struct FingerprintMatch(bool Exact, int Bits) : IComparable<FingerprintMatch> {
    public int CompareTo(FingerprintMatch other) =>
        Exact != other.Exact ? Exact.CompareTo(other.Exact) : Bits.CompareTo(other.Bits);
}

/// <summary>The datalog protocol a ROM's firmware speaks, and its baud rate when the protocol does not fix it.</summary>
public sealed record DatalogSpec(string Protocol, int? Baud);

/// <summary>
/// Describes an existing ROM image: how to recognise it, where its parameters are (absolute
/// addresses), where its checksum is and which datalog protocol it speaks.
/// </summary>
public sealed record DefinitionPack {
    /// <summary>Short stable identifier, unique among packs.</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }
    public RomPlatform Platform { get; init; } = RomPlatform.Obd1Oki66207;
    public int RomSize { get; init; } = 0x8000;
    public required IReadOnlyList<Fingerprint> Fingerprints { get; init; }
    public required ChecksumSpec Checksum { get; init; }
    public DatalogSpec? Datalog { get; init; }
    public IReadOnlyList<ParamSpec> Params { get; init; } = [];

    /// <summary>The verified changes that can be made to the ROM, each reversible.</summary>
    public IReadOnlyList<PatchRecipe> Patches { get; init; } = [];

    /// <summary>The parameters that resolved inside the ROM.</summary>
    public IReadOnlyList<ParamDef> Defs { get; init; } = [];

    /// <summary>Why the other parameters were left out.</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];
}

/// <summary>Matching ROM images against fingerprints.</summary>
public static class Fingerprints {
    /// <summary>How well the image matches the fingerprint; null when a condition fails.</summary>
    public static FingerprintMatch? Match(Fingerprint fingerprint, RomImage image) {
        if (fingerprint.Size is { } size && size != image.Length) return null;
        var bits = 0;
        foreach (var m in fingerprint.Bytes) {
            if (m.Address + m.Value.Count > image.Length) return null;
            for (var i = 0; i < m.Value.Count; i++) {
                var mask = m.Mask is { } masks ? masks[i] : (byte)0xFF;
                if (((image[m.Address + i] ^ m.Value[i]) & mask) != 0) return null;
                bits += byte.PopCount(mask);
            }
        }
        if (fingerprint.Sha256 is { } hash && hash != image.Sha256()) return null;
        return new FingerprintMatch(fingerprint.Sha256 is not null, bits);
    }

    /// <summary>The best match among a pack's fingerprints; null when none matches.</summary>
    public static FingerprintMatch? Match(DefinitionPack pack, RomImage image) {
        FingerprintMatch? best = null;
        foreach (var fingerprint in pack.Fingerprints) {
            if (Match(fingerprint, image) is { } match && (best is null || match.CompareTo(best.Value) > 0)) best = match;
        }
        return best;
    }
}

/// <summary>Definition packs (<c>*.pack.toml</c>).</summary>
public static class DefinitionPacks {
    public const string FileSuffix = ".pack.toml";

    public static DefinitionPack Parse(string text) {
        var root = TomlReader.ParseRoot(text);
        var romSize = (int)TomlReader.Integer(root, "rom_size", 1, int.MaxValue, 0x8000);
        var specs = ParamDefinitions.ParseParams(root);
        foreach (var p in specs) {
            var symbol = p.Symbol ?? (p.RowAxis as AxisSpec.AtSymbol)?.Symbol ?? (p.ColAxis as AxisSpec.AtSymbol)?.Symbol ?? p.Multiplier?.Symbol;
            if (symbol is not null) throw new HreFormatException($"`{symbol}`: a definition pack locates everything by `address`; symbols belong to source projects");
        }
        var fingerprints = TomlReader.Tables(root, "fingerprint").Select(ParseFingerprint).ToList();
        if (fingerprints.Count == 0) throw new HreFormatException("missing `[[fingerprint]]`: a pack must say which ROM it describes");
        var (defs, problems) = ParamDefinitions.Resolve(specs, null, romSize);
        var checksum = ParseChecksum(root, romSize);
        var patches = new List<PatchRecipe>();
        foreach (var t in TomlReader.Tables(root, "patch")) {
            var patch = ParsePatch(t, romSize, checksum);
            if (patches.Any(p => p.Id == patch.Id)) throw new HreFormatException($"another patch already has id `{patch.Id}`");
            patches.Add(patch);
        }
        return new DefinitionPack {
            Id = TomlReader.String(root, "id"),
            Name = TomlReader.String(root, "name"),
            Platform = ParamDefinitions.ParsePlatform(root) ?? RomPlatform.Obd1Oki66207,
            RomSize = romSize,
            Fingerprints = fingerprints,
            Checksum = checksum,
            Datalog = ParseDatalog(root),
            Params = specs,
            Patches = patches,
            Defs = defs,
            Problems = problems,
        };
    }

    /// <summary>Reads one pack file. Errors name the file.</summary>
    public static DefinitionPack Load(string path) {
        string text;
        try {
            text = File.ReadAllText(path);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            throw new ProjectException(ProjectErrorKind.Io, path, e.Message, e);
        }
        try {
            return Parse(text);
        } catch (HreFormatException e) {
            throw new ProjectException(ProjectErrorKind.Format, path, e.Message, e);
        }
    }

    /// <summary>
    /// Every <c>*.pack.toml</c> under a directory, in path order. Files that cannot be read or
    /// understood are reported, not fatal, and so is a pack whose id repeats an earlier one.
    /// </summary>
    public static (IReadOnlyList<DefinitionPack> Packs, IReadOnlyList<ProjectException> Errors) LoadDirectory(string directory) {
        var packs = new List<DefinitionPack>();
        var errors = new List<ProjectException>();
        if (!Directory.Exists(directory)) return (packs, errors);
        foreach (var path in Directory.EnumerateFiles(directory, "*" + FileSuffix, SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase)) {
            try {
                var pack = Load(path);
                if (packs.Any(p => p.Id == pack.Id)) errors.Add(new ProjectException(ProjectErrorKind.Format, path, $"another pack already has id `{pack.Id}`"));
                else packs.Add(pack);
            } catch (ProjectException e) {
                errors.Add(e);
            }
        }
        return (packs, errors);
    }

    /// <summary>
    /// The packs whose fingerprints match the image, best match first. Several entries of equal
    /// strength mean the image cannot be told apart by fingerprint alone.
    /// </summary>
    public static IReadOnlyList<(DefinitionPack Pack, FingerprintMatch Match)> Identify(IEnumerable<DefinitionPack> packs, RomImage image) {
        var matches = new List<(DefinitionPack Pack, FingerprintMatch Match)>();
        foreach (var pack in packs) {
            if (Fingerprints.Match(pack, image) is { } match) matches.Add((pack, match));
        }
        return [.. matches.OrderByDescending(m => m.Match)];
    }

    private static Fingerprint ParseFingerprint(TomlTable t) {
        var size = (int?)TomlReader.OptionalInteger(t, "size", 1, int.MaxValue);
        var sha256 = TomlReader.OptionalString(t, "sha256")?.ToLowerInvariant();
        if (sha256 is not null && (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))) throw new HreFormatException("`sha256` must be 64 hex digits");
        var bytes = new List<ByteMatch>();
        foreach (var b in TomlReader.Tables(t, "bytes")) {
            var value = TomlReader.OptionalBytes(b, "value") ?? throw new HreFormatException("missing `value`");
            var mask = TomlReader.OptionalBytes(b, "mask");
            if (value.Length == 0) throw new HreFormatException("`value` must not be empty");
            if (mask is not null && mask.Length != value.Length) throw new HreFormatException($"`mask` has {mask.Length} bytes, `value` has {value.Length}");
            bytes.Add(new ByteMatch((int)TomlReader.Integer(b, "address", 0, int.MaxValue - value.Length), value, mask));
        }
        // A size alone would claim every image of that length.
        if (sha256 is null && bytes.Count == 0) throw new HreFormatException("a fingerprint needs a `sha256` or `bytes`");
        return new Fingerprint(size, sha256, bytes);
    }

    /// <summary>
    /// <c>[checksum]</c>: <c>kind</c> (<c>sum8</c>, the default, or <c>none</c>), the summed range
    /// <c>start</c>..<c>end</c> (default: the whole ROM) and the <c>location</c> of the correction
    /// byte (default: the last byte of the range).
    /// </summary>
    private static ChecksumSpec ParseChecksum(TomlTable root, int romSize) {
        if (TomlReader.OptionalTable(root, "checksum") is not { } t) return ChecksumSpec.WholeImage(romSize);
        var kind = TomlReader.String(t, "kind", "sum8") switch {
            "sum8" => ChecksumKind.Sum8,
            "none" => ChecksumKind.None,
            var other => throw new HreFormatException($"unknown checksum kind `{other}`; expected sum8 or none"),
        };
        var start = (int)TomlReader.Integer(t, "start", 0, romSize - 1, 0);
        var end = (int)TomlReader.Integer(t, "end", start + 1, romSize, romSize);
        var location = (int)TomlReader.Integer(t, "location", start, end - 1, end - 1);
        return new ChecksumSpec(kind, location, start, end);
    }

    /// <summary>
    /// <c>[[patch]]</c>: <c>id</c>, <c>name</c>, an optional <c>doc</c> and one or more
    /// <c>[[patch.step]]</c>, each an <c>address</c>, the <c>old</c> bytes expected there and the
    /// <c>new</c> bytes that replace them. A recipe that could not be probed unambiguously is
    /// rejected: steps that overlap, reach past the ROM or cover the checksum byte, and a recipe
    /// that changes nothing. Errors name the patch.
    /// </summary>
    private static PatchRecipe ParsePatch(TomlTable t, int romSize, ChecksumSpec checksum) {
        var id = TomlReader.OptionalString(t, "id");
        if (string.IsNullOrEmpty(id)) throw new HreFormatException("every `[[patch]]` needs an `id`");
        try {
            var name = TomlReader.String(t, "name");
            var steps = TomlReader.Tables(t, "step").Select(ParseStep).ToList();
            if (steps.Count == 0) throw new HreFormatException("missing `[[patch.step]]`: a patch needs at least one step");
            foreach (var step in steps) {
                var end = (long)step.Address + step.Old.Count;
                if (end > romSize) throw new HreFormatException($"the step at 0x{step.Address:x} reaches past the 0x{romSize:x}-byte ROM");
                // The correction byte follows every edit, so its value can never be an expected one.
                if (checksum.Kind != ChecksumKind.None && checksum.Location >= step.Address && checksum.Location < end) {
                    throw new HreFormatException($"the step at 0x{step.Address:x} covers the checksum byte at 0x{checksum.Location:x}, which changes with every edit");
                }
            }
            var ordered = steps.OrderBy(s => s.Address).ToList();
            for (var i = 1; i < ordered.Count; i++) {
                var (before, after) = (ordered[i - 1], ordered[i]);
                if (before.Address + before.Old.Count > after.Address) throw new HreFormatException($"the steps at 0x{before.Address:x} and 0x{after.Address:x} overlap");
            }
            // Such a recipe would probe as applied and as not applied at once.
            if (steps.All(s => s.Old.SequenceEqual(s.New))) throw new HreFormatException("`new` equals `old` in every step: the patch changes nothing");
            return new PatchRecipe(id, name, TomlReader.String(t, "doc", ""), steps);
        } catch (HreFormatException e) {
            throw new HreFormatException($"patch `{id}`: {e.Message}", e);
        }
    }

    private static PatchStep ParseStep(TomlTable t) {
        var old = TomlReader.OptionalBytes(t, "old") ?? throw new HreFormatException("missing `old`");
        var replacement = TomlReader.OptionalBytes(t, "new") ?? throw new HreFormatException("missing `new`");
        if (old.Length == 0) throw new HreFormatException("`old` must not be empty");
        if (replacement.Length != old.Length) throw new HreFormatException($"`old` has {old.Length} bytes, `new` has {replacement.Length}");
        return new PatchStep((int)TomlReader.Integer(t, "address", 0, int.MaxValue), old, replacement);
    }

    private static DatalogSpec? ParseDatalog(TomlTable root) {
        if (TomlReader.OptionalTable(root, "datalog") is not { } t) return null;
        var protocol = TomlReader.String(t, "protocol");
        if (protocol.Length == 0) throw new HreFormatException("`protocol` must not be empty");
        return new DatalogSpec(protocol, (int?)TomlReader.OptionalInteger(t, "baud", 1, int.MaxValue));
    }
}
