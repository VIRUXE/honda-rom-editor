using HRE.Core;
using Tomlyn.Model;

namespace HRE.Definitions;

/// <summary>
/// A number a definitions file either gives outright or says where the ROM keeps: an address the
/// firmware loads, a column count it compares against, the range of a sensor a tuning program
/// wrote down next to a table.
/// </summary>
public abstract record RomValue {
    /// <summary>Given in the file.</summary>
    public sealed record Constant(long Value) : RomValue;

    /// <summary>
    /// Read from the ROM: the cell at <see cref="Address"/> (which may itself be read from the
    /// ROM) in the given format, plus <see cref="Bias"/>.
    /// </summary>
    public sealed record At(RomValue Address, CellFormat Cell, long Bias) : RomValue {
        /// <summary>The value kept in the cell after this one.</summary>
        public At Next() => this with { Address = Address.Plus(Width(Cell)) };
    }

    public static implicit operator RomValue(long value) => new Constant(value);

    /// <summary>
    /// The value. Null when it has to be read and there is no image to read it from, or the
    /// address it is read from lies outside the image.
    /// </summary>
    public long? Read(RomImage? image) {
        switch (this) {
            case Constant constant:
                return constant.Value;
            case At at when image is not null && at.Address.Read(image) is { } address && address >= 0 && address + Width(at.Cell) <= image.Length:
                var first = image[(int)address];
                if (at.Cell == CellFormat.U8) return first + at.Bias;
                var second = image[(int)address + 1];
                return (at.Cell == CellFormat.U16LittleEndian ? second << 8 | first : first << 8 | second) + at.Bias;
            default:
                return null;
        }
    }

    /// <summary>This value plus <paramref name="delta"/>.</summary>
    private RomValue Plus(long delta) => this switch {
        Constant constant => new Constant(constant.Value + delta),
        At at => at with { Bias = at.Bias + delta },
        _ => throw new InvalidOperationException("unknown ROM value"),
    };

    private static int Width(CellFormat cell) => cell == CellFormat.U8 ? 1 : 2;

    /// <summary>
    /// Reads a value written as an integer or as <c>{ at, cell, bias }</c>. <c>at</c> is where the
    /// ROM keeps the value and may be such a table itself; <c>cell</c> is <c>u8</c>,
    /// <c>u16le</c> or <c>u16be</c> (default <paramref name="cell"/>); <c>bias</c> is added to
    /// what is read. A constant must lie in <paramref name="min"/>..<paramref name="max"/>.
    /// </summary>
    internal static RomValue Parse(object? value, string key, CellFormat cell, long min, long max) {
        if (value is not TomlTable table) return new Constant(TomlReader.AsInteger(value, key, min, max));
        if (table.Keys.FirstOrDefault(k => k is not ("at" or "cell" or "bias")) is { } unknown) throw new HreFormatException($"`{key}`: unknown key `{unknown}`; a value read from the ROM has `at`, `cell` and `bias`");
        if (!table.TryGetValue("at", out var at)) throw new HreFormatException($"`{key}` must be a number or {{ at = ... }}, the address the ROM keeps it at");
        var format = TomlReader.OptionalString(table, "cell") switch {
            null => cell,
            "u8" => CellFormat.U8,
            "u16le" => CellFormat.U16LittleEndian,
            "u16be" => CellFormat.U16BigEndian,
            var other => throw new HreFormatException($"`{key}`: unknown cell `{other}`; expected u8, u16le or u16be"),
        };
        // An address in the ROM is a little-endian word, as the processor stores them.
        return new At(Parse(at, $"{key}.at", CellFormat.U16LittleEndian, 0, int.MaxValue), format, TomlReader.Integer(table, "bias", int.MinValue, int.MaxValue, 0));
    }
}
