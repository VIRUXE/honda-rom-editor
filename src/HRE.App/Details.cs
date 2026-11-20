using System.Globalization;
using HRE.Core;
using HRE.Definitions;

namespace HRE.App;

/// <summary>One line of a detail page: a label and its value. An empty label makes the value a line of its own.</summary>
public sealed record DetailRow(string Label, string Value);

public sealed record DetailSection(string? Heading, IReadOnlyList<DetailRow> Rows);

/// <summary>What the detail view shows: a title, a line under it and sections of rows.</summary>
public sealed record DetailPage(string Title, string Subtitle, IReadOnlyList<DetailSection> Sections);

/// <summary>Builds the pages of the detail view: the start page, an opened ROM, one parameter, a file that could not be opened.</summary>
public static class Details {
    public static DetailPage Start(IReadOnlyList<DefinitionPack> packs, IReadOnlyList<ProjectException> packErrors) {
        var sections = new List<DetailSection> {
            new("Definition packs", packs.Count == 0
                ? [new("", "No definition packs were found, so no ROM will be recognised.")]
                : [.. packs.Select(p => new DetailRow(p.Id, p.Name))]),
        };
        if (packErrors.Count > 0) sections.Add(new("Packs that could not be read", [.. packErrors.Select(e => new DetailRow("", e.Message))]));
        return new DetailPage("Honda ROM Editor", "Open a ROM with File > Open ROM (Ctrl+O).", sections);
    }

    public static DetailPage Failure(string path, string message) =>
        new(Path.GetFileName(path), "This file could not be opened.", [new(null, [new("File", path), new("Reason", message)])]);

    public static DetailPage Rom(RomSession session) {
        var image = session.Image;
        var file = new List<DetailRow> {
            new("File", session.Path),
            new("Size", $"{image.Length} bytes (0x{image.Length:X})"),
            new("SHA-256", image.Sha256()),
        };
        if (session.Pack is not { } pack) {
            return new DetailPage(session.FileName, "No definition pack recognises this ROM.", [
                new(null, file),
                new(null, [new("", "Its tables cannot be shown: nothing says where they are.")]),
            ]);
        }

        var rom = new List<DetailRow> {
            new("ROM", pack.Name),
            new("Pack", pack.Id),
            new("Platform", PlatformName(pack.Platform)),
            new("Checksum", ChecksumText(session)),
            new("Datalog", pack.Datalog is { } d ? d.Baud is { } baud ? $"{d.Protocol}, {baud} baud" : d.Protocol : "Not stated by the pack"),
        };
        var sections = new List<DetailSection> { new(null, file), new("Recognised as", rom) };
        if (session.OtherMatches.Count > 0) {
            sections.Add(new("Also matches", [.. session.OtherMatches.Select(p => new DetailRow(p.Id, p.Name))]));
        }
        var categories = ParamTree.Build(session.Defs);
        if (categories.Count > 0) {
            sections.Add(new("Parameters", [.. categories.Select(c => new DetailRow(c.Name, string.Join(", ", c.Params.Select(p => p.Name))))]));
        }
        if (pack.Patches.Count > 0) {
            sections.Add(new("Patches", [.. pack.Patches.Select(p => new DetailRow(p.Name, PatchEngine.Probe(p, image) switch {
                PatchState.Applied => "Applied",
                PatchState.NotApplied => "Not applied",
                _ => "Conflict: the ROM does not hold the bytes this patch expects",
            }))]));
        }
        if (session.Problems.Count > 0) sections.Add(new("Left out", [.. session.Problems.Select(p => new DetailRow("", p))]));
        return new DetailPage(session.FileName, pack.Name, sections);
    }

    public static DetailPage Category(ParamCategory category) =>
        new(category.Name, category.Params.Count == 1 ? "1 parameter" : $"{category.Params.Count} parameters", [
            new(null, [.. category.Params.Select(p => new DetailRow(p.Name, p.Kind switch {
                ParamKind.Table t => $"Table, {t.Rows} x {t.Cols}{UnitSuffix(p)}",
                _ => $"Value{UnitSuffix(p)}",
            }))]),
        ]);

    public static DetailPage Param(RomImage image, ParamDef def) {
        var storage = new List<DetailRow>();
        var sections = new List<DetailSection> { new(null, storage) };
        switch (def.Kind) {
            case ParamKind.Table table:
                storage.Add(new("Kind", $"Table, {table.Rows} rows x {table.Cols} columns"));
                storage.Add(new("Address", Range(def.Offset, def.Length)));
                storage.Add(new("Cell", CellName(def.Cell)));
                AddDisplay(storage, def);
                if (def.Multiplier is { } m) {
                    storage.Add(new("Column multiplier", $"{(m.Kind == MultiplierKind.Linear ? "Cell x byte" : "Cell x 2^byte")}, {Range(m.Offset, table.Cols)}"));
                }
                if (def.Vtec is { } vtec) storage.Add(new("VTEC", vtec ? "Read with VTEC engaged" : "Read with VTEC disengaged"));
                var (min, max) = TableRange(image, def, table);
                storage.Add(new("Values", $"{Number(min)} to {Number(max)}{UnitSuffix(def)}"));
                sections.Add(new("Row axis", AxisRows(image, def.RowAxis, table.Rows)));
                sections.Add(new("Column axis", AxisRows(image, def.ColAxis, table.Cols)));
                break;
            case ParamKind.Word word:
                var raw = ParamAccess.Word(image, def);
                storage.Add(new("Kind", word.Min == word.Max ? "Value, read-only" : "Value"));
                storage.Add(new("Address", Range(def.Offset, def.Length)));
                storage.Add(new("Stored as", def.LittleEndian ? "16-bit, low byte first" : "16-bit, high byte first"));
                AddDisplay(storage, def);
                storage.Add(new("Value", $"{Number(def.Display(raw))}{UnitSuffix(def)} (stored {raw})"));
                if (word.Min != word.Max) storage.Add(new("Allowed", $"{word.Min} to {word.Max}, as stored"));
                break;
        }
        if (def.Symbol is { } symbol) storage.Insert(0, new("Symbol", symbol));
        return new DetailPage(def.Name, def.Category, sections);
    }

    private static List<DetailRow> AxisRows(RomImage image, Axis axis, int n) {
        var rows = new List<DetailRow> { new("Label", axis.Label), new("Breakpoints", n.ToString(CultureInfo.InvariantCulture)) };
        rows.Add(axis.IsFixed
            ? new("Stored", "Not in the ROM: fixed by the definition")
            : new("Stored", $"{EncodingName(axis.Encoding)}, {Range(axis.Offset, axis.StoredLength(n))}"));
        if (n > 0) {
            var values = ParamAccess.AxisValues(image, axis, n);
            rows.Add(new("Range", $"{Number(values[0])} to {Number(values[^1])}"));
        }
        return rows;
    }

    private static void AddDisplay(List<DetailRow> rows, ParamDef def) {
        if (def.Unit.Length > 0) rows.Add(new("Unit", def.Unit));
        if (def.Scale != 1f || def.Bias != 0f) {
            var bias = def.Bias == 0f ? "" : def.Bias > 0f ? $" + {Number(def.Bias)}" : $" - {Number(-def.Bias)}";
            rows.Add(new("Shown as", $"stored x {Number(def.Scale)}{bias}"));
        }
    }

    private static (float Min, float Max) TableRange(RomImage image, ParamDef def, ParamKind.Table table) {
        var (min, max) = (float.MaxValue, float.MinValue);
        for (var row = 0; row < table.Rows; row++) {
            for (var col = 0; col < table.Cols; col++) {
                var value = ParamAccess.CellDisplay(image, def, row, col);
                (min, max) = (Math.Min(min, value), Math.Max(max, value));
            }
        }
        return table.Rows * table.Cols == 0 ? (0f, 0f) : (min, max);
    }

    private static string ChecksumText(RomSession session) {
        var spec = session.Pack!.Checksum;
        var where = $"bytes {Range(spec.Start, spec.End - spec.Start)} sum to zero, corrected at 0x{spec.Location:X4}";
        return session.Checksum switch {
            ChecksumState.Valid => $"Correct: {where}",
            ChecksumState.Wrong => $"Wrong: {where}",
            _ => "This ROM does not check itself",
        };
    }

    private static string PlatformName(RomPlatform platform) => platform switch {
        RomPlatform.Obd1Oki66207 => "OBD1, OKI MSM66207",
        RomPlatform.Obd0Oki66k => "OBD0, OKI 66k family",
        _ => "OBD0, 8051 family",
    };

    private static string CellName(CellFormat cell) => cell switch {
        CellFormat.U8 => "8-bit",
        CellFormat.U16BigEndian => "16-bit, high byte first",
        _ => "16-bit, low byte first",
    };

    private static string EncodingName(AxisEncoding encoding) => encoding switch {
        AxisEncoding.Words => "16-bit words",
        AxisEncoding.DeltaBytes => "Bytes, each the step from the breakpoint before",
        AxisEncoding.Bytes => "Bytes",
        _ => "Bytes on a logarithmic scale",
    };

    /// <summary>A byte range the way a hex editor shows it: first and last address.</summary>
    private static string Range(int start, int length) =>
        length <= 0 ? $"0x{start:X4}" : $"0x{start:X4} to 0x{start + length - 1:X4} ({length} {(length == 1 ? "byte" : "bytes")})";

    private static string UnitSuffix(ParamDef def) => def.Unit.Length > 0 ? " " + def.Unit : "";

    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
