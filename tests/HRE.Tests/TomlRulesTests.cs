using HRE.Definitions;

namespace HRE.Tests;

/// <summary>
/// A key or a table is defined once, in every project file. The rules are tried here through the
/// definitions reader, which ignores the keys it does not know.
/// </summary>
public class TomlRulesTests {
    [Theory]
    // A key given twice.
    [InlineData("a = 1\na = 2\n", 2, "a")]
    [InlineData("[t]\na = 1\na = 2\n", 3, "t.a")]
    [InlineData("a.b = 1\na = 2\n", 2, "a")]
    [InlineData("a = 1\na.b = 2\n", 2, "a")]
    // ... inside an inline table, also one that sits in a list.
    [InlineData("s = { old = [0], old = [9] }\n", 1, "s.old")]
    [InlineData("s = [{ x = 1 }, { x = 1, x = 2 }]\n", 1, "s.x")]
    // A table given twice.
    [InlineData("[t]\na = 1\n[t]\nb = 2\n", 3, "t")]
    [InlineData("[[p]]\n[p.q]\ny = 1\n[p.q]\ny = 2\n", 4, "p.q")]
    // A header for something that was given a value.
    [InlineData("a = 1\n[a]\nb = 2\n", 2, "a")]
    [InlineData("a = 1\n[a.b]\ny = 2\n", 2, "a")]
    [InlineData("a = [{ x = 1 }]\n[[a]]\nx = 2\n", 2, "a")]
    [InlineData("a = []\n[[a]]\nx = 2\n", 2, "a")]
    // ... inside one entry of a list of tables.
    [InlineData("[[p]]\nq = 1\n[p.q]\ny = 2\n", 3, "p.q")]
    [InlineData("[[p]]\nq = 1\n[[p.q]]\ny = 2\n", 3, "p.q")]
    [InlineData("[[p]]\nlist = [{ x = 1 }]\n[[p.list]]\nx = 2\n", 3, "p.list")]
    [InlineData("[[p]]\nlist = [{ x = 1 }]\n[p.list.sub]\nx = 2\n", 3, "p.list")]
    [InlineData("[[p]]\n[[p.q]]\nlist = [{ x = 1 }]\n[[p.q.list]]\nx = 2\n", 4, "p.q.list")]
    // A table and a list of tables under one name.
    [InlineData("[a]\nx = 1\n[[a]]\nx = 2\n", 3, "a")]
    [InlineData("[[a]]\nx = 1\n[a]\ny = 2\n", 3, "a")]
    // A table made by dotted keys is not opened again by a header, nor one made by a header by dotted keys.
    [InlineData("a.b = 1\n[a]\nc = 2\n", 2, "a")]
    [InlineData("[a]\nb.c = 1\n[a.b]\nd = 2\n", 3, "a.b")]
    [InlineData("[[p]]\nq.r = 1\n[p.q]\ny = 2\n", 3, "p.q")]
    [InlineData("[a.b]\nx = 1\n[a]\nb.y = 2\n", 4, "a.b")]
    public void DefiningSomethingTwiceIsRejected(string text, int line, string what) {
        var e = Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(text));
        Assert.Equal($"line {line}: `{what}` is already defined", e.Message);
    }

    /// <summary>An inline table is complete as written. The library's reader refuses these itself, in its own words.</summary>
    [Theory]
    [InlineData("a = { x = 1 }\n[a]\ny = 2\n")]
    [InlineData("a = { x = 1 }\n[a.b]\ny = 2\n")]
    [InlineData("[[p]]\nq = { x = 1 }\n[p.q]\ny = 2\n")]
    [InlineData("s = { a = { y = 1 }, a.z = 2 }\n")]
    public void AddingToAnInlineTableIsRejected(string text) {
        Assert.Throws<HreFormatException>(() => ParamDefinitions.Parse(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("# nothing but a comment\n")]
    // A table may be named after one of its sub-tables, and a table made by dotted keys may gain sub-tables.
    [InlineData("[a.b]\nx = 1\n[a]\ny = 2\n")]
    [InlineData("a.b = 1\na.c = 2\n[a.d]\ne = 3\n")]
    [InlineData("[a]\nb.c = 1\n[a.b.d]\ne = 2\n")]
    // Every entry of a list of tables has its own keys and its own sub-tables.
    [InlineData("[[p]]\nx = 1\n[p.q]\ny = 1\n[[p]]\nx = 2\n[p.q]\ny = 2\n")]
    [InlineData("[[p]]\nlist = [{ x = 1 }]\n[[p]]\n[[p.list]]\nx = 2\n")]
    [InlineData("[[p]]\n[[p.list]]\nx = 1\n[[p]]\nlist = [{ x = 2 }]\n")]
    [InlineData("[[p]]\n[p.q]\ny = 1\n[[p]]\nq = { y = 2 }\n")]
    [InlineData("[[a]]\n[[a.b]]\ny = 1\n[[a]]\n[[a.b]]\ny = 2\n[[a.b]]\ny = 3\n")]
    [InlineData("[a]\nx = 1\n[[a.b]]\ny = 1\n[[a.b]]\ny = 2\n")]
    // A key of an entry may be named like the list the entry is in.
    [InlineData("[[a]]\nx = 1\na = [{ x = 2 }]\n")]
    // The same key in different inline tables, and dotted keys inside one.
    [InlineData("s = [{ x = 1 }, { x = 2 }]\nt = { a.b = 1, a.c = 2 }\n")]
    [InlineData("\"a b\" = 1\n'c' = 2\n[t.\"x.y\"]\nz = 1\n[t.x]\ny = 1\n")]
    public void WhatTomlAllowsIsAccepted(string text) {
        Assert.Empty(ParamDefinitions.Parse(text).Params);
    }

    /// <summary>One parameter writes its axes as sub-tables, the next writes them inline: each parameter is a table of its own.</summary>
    [Fact]
    public void EachEntryOfAListHasItsOwnKeys() {
        var file = ParamDefinitions.Parse("""
            [[param]]
            category = 'a'
            name = 'first'
            symbol = 't1'
            kind = 'table'
            rows = 2
            cols = 2
            [param.row_axis]
            label = 'r'
            values = [1, 2]
            [param.col_axis]
            label = 'c'
            values = [1, 2]

            [[param]]
            category = 'a'
            name = 'second'
            symbol = 't2'
            kind = 'table'
            rows = 2
            cols = 2
            row_axis = { label = 'r', values = [3, 4] }
            col_axis = { label = 'c', values = [3, 4] }
            """);
        Assert.Equal(["first", "second"], file.Params.Select(p => p.Name));
        Assert.Equal([1, 2], ((AxisSpec.Fixed)file.Params[0].RowAxis!).Values);
        Assert.Equal([3, 4], ((AxisSpec.Fixed)file.Params[1].ColAxis!).Values);
    }
}
