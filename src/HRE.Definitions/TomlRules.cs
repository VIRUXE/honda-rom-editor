using Tomlyn.Syntax;

namespace HRE.Definitions;

/// <summary>
/// TOML lets a key or a table be defined once. The library's reader keeps the last definition
/// without a word, and its own validation loses track of the keys given directly in an entry of
/// a list of tables: it lets a redefinition inside one entry through and takes the keys of two
/// different entries for the same one. So the rules are checked here, over the syntax tree.
/// </summary>
internal static class TomlRules {
    private enum Kind {
        /// <summary>Anything given with <c>=</c>: a scalar, an inline list or an inline table. Nothing can be added to it.</summary>
        Value,
        Table,

        /// <summary>A list of tables written with <c>[[name]]</c> headers.</summary>
        List,
    }

    private sealed class Entry(Kind kind) {
        public Kind Kind { get; } = kind;

        /// <summary>
        /// A table named by a header, or one that dotted keys reached in a section that has
        /// ended: a header may not open it again, and dotted keys may not add to it.
        /// </summary>
        public bool Closed { get; set; }

        /// <summary>The keys of a table.</summary>
        public Dictionary<string, Entry> Items { get; } = [];

        /// <summary>The tables of a list.</summary>
        public List<Entry> Elements { get; } = [];
    }

    /// <summary>Throws <see cref="HreFormatException"/> naming the line and the key of the first thing defined twice.</summary>
    public static void Check(DocumentSyntax document) {
        var root = new Entry(Kind.Table);
        var dotted = new List<Entry>();
        foreach (var pair in document.KeyValues) Define(root, pair, "", dotted);
        foreach (var table in document.Tables) {
            // The section before this header is over: what its dotted keys reached is settled.
            foreach (var entry in dotted) entry.Closed = true;
            dotted.Clear();
            var path = Names(table.Name);
            if (path.Count == 0) continue;
            var section = Open(root, path, table is TableArraySyntax, Line(table));
            foreach (var pair in table.Items) Define(section, pair, string.Join('.', path) + ".", dotted);
        }
    }

    /// <summary>The table a header names: <c>[a.b]</c>, or a new entry of the list <c>[[a.b]]</c>.</summary>
    private static Entry Open(Entry root, List<string> path, bool list, int line) {
        var parent = root;
        for (var i = 0; i < path.Count - 1; i++) {
            if (!parent.Items.TryGetValue(path[i], out var next)) parent.Items[path[i]] = next = new Entry(Kind.Table);
            // A name on the way that is a list means its newest entry.
            if (next.Kind == Kind.List) next = next.Elements[^1];
            if (next.Kind == Kind.Value) throw Defined(line, path.Take(i + 1));
            parent = next;
        }
        parent.Items.TryGetValue(path[^1], out var existing);
        if (list) {
            if (existing is null) parent.Items[path[^1]] = existing = new Entry(Kind.List);
            else if (existing.Kind != Kind.List) throw Defined(line, path);
            var element = new Entry(Kind.Table);
            existing.Elements.Add(element);
            return element;
        }
        // A table that only came into being as the parent of another may still be named once.
        if (existing is null) parent.Items[path[^1]] = existing = new Entry(Kind.Table);
        else if (existing.Kind != Kind.Table || existing.Closed) throw Defined(line, path);
        existing.Closed = true;
        return existing;
    }

    /// <summary>
    /// One <c>key = value</c> of a table. Dotted keys reach into tables of their own making or
    /// into ones that are still open; <paramref name="dotted"/> collects the tables reached.
    /// </summary>
    private static void Define(Entry table, KeyValueSyntax pair, string prefix, List<Entry> dotted) {
        var path = Names(pair.Key);
        if (path.Count == 0) return;
        var line = Line(pair);
        for (var i = 0; i < path.Count - 1; i++) {
            if (!table.Items.TryGetValue(path[i], out var next)) table.Items[path[i]] = next = new Entry(Kind.Table);
            else if (next.Kind != Kind.Table || next.Closed) throw Defined(line, path.Take(i + 1), prefix);
            dotted.Add(next);
            table = next;
        }
        if (!table.Items.TryAdd(path[^1], new Entry(Kind.Value))) throw Defined(line, path, prefix);
        CheckValue(pair.Value, prefix + string.Join('.', path) + ".");
    }

    /// <summary>An inline table is a table of its own, also when it sits in a list.</summary>
    private static void CheckValue(ValueSyntax? value, string prefix) {
        switch (value) {
            case InlineTableSyntax inline:
                var table = new Entry(Kind.Table);
                foreach (var item in inline.Items) {
                    if (item.KeyValue is { } pair) Define(table, pair, prefix, []);
                }
                break;
            case ArraySyntax array:
                foreach (var item in array.Items) CheckValue(item.Value, prefix);
                break;
        }
    }

    private static List<string> Names(KeySyntax? key) {
        var names = new List<string>();
        if (key is null) return names;
        names.Add(Name(key.Key));
        foreach (var item in key.DotKeys) names.Add(Name(item.Key));
        return names;
    }

    private static string Name(BareKeyOrStringValueSyntax? key) => key switch {
        BareKeySyntax bare => bare.Key?.Text ?? "",
        StringValueSyntax quoted => quoted.Value ?? "",
        _ => "",
    };

    private static int Line(SyntaxNode node) => node.Span.Start.Line + 1;

    private static HreFormatException Defined(int line, IEnumerable<string> path, string prefix = "") =>
        new($"line {line}: `{prefix}{string.Join('.', path)}` is already defined");
}
