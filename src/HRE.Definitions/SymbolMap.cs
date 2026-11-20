using System.Text.Json;

namespace HRE.Definitions;

public enum SymbolKind { Label, Equ, Define, Sfr }

public sealed record SymbolEntry(string Name, long Value, SymbolKind Kind, string File, int Line);

/// <summary>A block of the image and the source file that produced it.</summary>
public sealed record SourceBlock(int Start, int End, string File);

/// <summary>The assembler's symbol map: every symbol's value and where it was defined.</summary>
public sealed class SymbolMap {
    private readonly Dictionary<string, SymbolEntry> byName = new(StringComparer.Ordinal);

    public SymbolMap(int romSize, string entry, IReadOnlyList<SymbolEntry> symbols, IReadOnlyList<SourceBlock>? blocks = null) {
        RomSize = romSize;
        Entry = entry;
        Symbols = symbols;
        Blocks = blocks ?? [];
        foreach (var s in symbols) byName.TryAdd(s.Name, s);
    }

    public int RomSize { get; }
    public string Entry { get; }
    public IReadOnlyList<SymbolEntry> Symbols { get; }
    public IReadOnlyList<SourceBlock> Blocks { get; }

    public SymbolEntry? Get(string name) => byName.GetValueOrDefault(name);

    /// <summary>Reads the JSON the assembler writes with <c>--map</c>.</summary>
    public static SymbolMap FromJson(string json) {
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var version = root.GetProperty("version").GetInt32();
            if (version != 1) throw new HreFormatException($"symbol map version {version} is not supported");
            var symbols = new List<SymbolEntry>();
            foreach (var s in root.GetProperty("symbols").EnumerateArray()) {
                var kind = s.GetProperty("kind").GetString();
                if (!Enum.TryParse<SymbolKind>(kind, out var parsed)) throw new HreFormatException($"symbol map has unknown symbol kind `{kind}`");
                symbols.Add(new SymbolEntry(s.GetProperty("name").GetString()!, s.GetProperty("value").GetInt64(), parsed,
                    s.GetProperty("file").GetString() ?? "", s.GetProperty("line").GetInt32()));
            }
            var blocks = new List<SourceBlock>();
            if (root.TryGetProperty("blocks", out var blockArray)) {
                foreach (var b in blockArray.EnumerateArray()) {
                    blocks.Add(new SourceBlock(b.GetProperty("start").GetInt32(), b.GetProperty("end").GetInt32(), b.GetProperty("file").GetString() ?? ""));
                }
            }
            return new SymbolMap(root.GetProperty("rom_size").GetInt32(), root.GetProperty("entry").GetString() ?? "", symbols, blocks);
        } catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) {
            throw new HreFormatException($"symbol map is not valid: {e.Message}", e);
        }
    }
}
