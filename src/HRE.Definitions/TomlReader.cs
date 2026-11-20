using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace HRE.Definitions;

/// <summary>A project file that cannot be understood: bad syntax, a missing key or a value of the wrong type.</summary>
public sealed class HreFormatException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Typed access to parsed TOML, with errors that name the key.</summary>
internal static class TomlReader {
    public static TomlTable ParseRoot(string text) {
        try {
            return TomlSerializer.Deserialize<TomlTable>(text, (TomlSerializerOptions?)null) ?? new TomlTable();
        } catch (TomlException e) {
            throw new HreFormatException(e.Message, e);
        }
    }

    /// <summary>The tables of an array of tables (<c>[[key]]</c>); none when the key is absent.</summary>
    public static IReadOnlyList<TomlTable> Tables(TomlTable table, string key) {
        if (!table.TryGetValue(key, out var value)) return [];
        return value switch {
            TomlTableArray array => [.. array],
            TomlArray { Count: 0 } => [],
            TomlArray array when array.All(item => item is TomlTable) => [.. array.Cast<TomlTable>()],
            _ => throw new HreFormatException($"`{key}` must be an array of tables"),
        };
    }

    public static string String(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) ? AsString(value, key) : throw Missing(key);

    public static string String(TomlTable table, string key, string fallback) =>
        table.TryGetValue(key, out var value) ? AsString(value, key) : fallback;

    public static string? OptionalString(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) ? AsString(value, key) : null;

    public static bool Bool(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) ? AsBool(value, key) : throw Missing(key);

    public static bool? OptionalBool(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) ? AsBool(value, key) : null;

    public static long Integer(TomlTable table, string key, long min, long max) =>
        table.TryGetValue(key, out var value) ? AsInteger(value, key, min, max) : throw Missing(key);

    public static long Integer(TomlTable table, string key, long min, long max, long fallback) =>
        table.TryGetValue(key, out var value) ? AsInteger(value, key, min, max) : fallback;

    /// <summary>A float, also accepting an integer literal.</summary>
    public static float Number(TomlTable table, string key, float fallback) {
        if (!table.TryGetValue(key, out var value)) return fallback;
        return value switch {
            double d => (float)d,
            float f => f,
            long l => l,
            int i => i,
            _ => throw new HreFormatException($"`{key}` must be a number"),
        };
    }

    public static long AsInteger(object? value, string key, long min, long max) {
        var n = value switch {
            long l => l,
            int i => i,
            _ => throw new HreFormatException($"`{key}` must be an integer"),
        };
        return n >= min && n <= max ? n : throw new HreFormatException($"`{key}` = {n} is outside {min}..{max}");
    }

    private static string AsString(object? value, string key) =>
        value as string ?? throw new HreFormatException($"`{key}` must be a string");

    private static bool AsBool(object? value, string key) =>
        value is bool b ? b : throw new HreFormatException($"`{key}` must be true or false");

    private static HreFormatException Missing(string key) => new($"missing `{key}`");

    /// <summary>A TOML basic string literal, quoted and escaped.</summary>
    public static string Quote(string s) {
        var sb = new StringBuilder(s.Length + 2).Append('"');
        foreach (var ch in s) {
            switch (ch) {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 0x20 || ch == 0x7F) sb.Append("\\u").Append(((int)ch).ToString("X4"));
                    else sb.Append(ch);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}
