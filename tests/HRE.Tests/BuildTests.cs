using HRE.Build;
using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>A test that runs the assembler; skipped when it cannot be found.</summary>
public sealed class AssemblerFactAttribute : FactAttribute {
    public AssemblerFactAttribute(params string[] requiredInWorkspace) {
        if (Tools.Assembler is null) {
            Skip = "the rasm66k assembler was not found (set HRE_RASM66K)";
            return;
        }
        var missing = requiredInWorkspace.Select(r => Path.Combine(Workspace.Root, r)).FirstOrDefault(p => !File.Exists(p) && !Directory.Exists(p));
        if (missing is not null) Skip = $"not in the workspace: {missing}";
    }
}

internal static class Tools {
    /// <summary>The assembler: wherever <see cref="Rasm66kTool.Locate"/> finds it, or its build output in the workspace.</summary>
    public static Rasm66kTool? Assembler { get; } = Rasm66kTool.Locate() ?? InWorkspace();

    private static Rasm66kTool? InWorkspace() {
        var path = Path.Combine(Workspace.Root, "rasm66k", "target", "release", OperatingSystem.IsWindows() ? "rasm66k.exe" : "rasm66k");
        return File.Exists(path) ? new Rasm66kTool(path) : null;
    }
}

public class DiagnosticsTests {
    [Fact]
    public void ReadsJsonDiagnostics() {
        var diagnostics = Rasm66kTool.ParseDiagnostics("""
            [ { "col": 9, "file": "main.asm", "line": 2, "message": "unexpected identifier `FOO` (missing `:`?)", "severity": "error" } ]
            """);
        Assert.Equal([new Diagnostic("main.asm", 2, 9, DiagnosticSeverity.Error, "unexpected identifier `FOO` (missing `:`?)")], diagnostics);
    }

    [Fact]
    public void ReadsTextDiagnostics() {
        var diagnostics = Rasm66kTool.ParseDiagnostics("src/a.asm:12:3: warning: label is never used\r\nsomething unexpected\n");
        Assert.Equal([
            new Diagnostic("src/a.asm", 12, 3, DiagnosticSeverity.Warning, "label is never used"),
            new Diagnostic("", 0, 0, DiagnosticSeverity.Error, "something unexpected"),
        ], diagnostics);
        Assert.Empty(Rasm66kTool.ParseDiagnostics("  \n"));
        Assert.Equal("src/a.asm:12:3: warning: label is never used", diagnostics[0].ToString());
    }
}

[Trait("Category", "Tools")]
public class BuildTests {
    [AssemblerFact]
    public async Task BuildsTheMiniProject() {
        var project = Project.Open(Fixtures.CopyMiniProject("build"));
        var result = await ProjectBuilder.BuildAsync(project, Tools.Assembler!);
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics));
        Assert.Empty(result.Problems);
        var image = result.Image!;
        Assert.Equal(0x8000, image.Length);
        Assert.True(Checksum.IsValid(image));
        Assert.Equal(new byte[] { 10, 20, 30, 40, 50, 60 }, image.Bytes.Slice(0x100, 6).ToArray());
        Assert.Equal(image.Bytes.ToArray(), File.ReadAllBytes(project.OutputPath));

        Assert.Equal(2, result.Defs.Count);
        var fuel = result.Defs[0];
        Assert.Equal(0x100, fuel.Offset);
        Assert.Equal([1000f, 2000f], ParamAccess.AxisValues(image, fuel.RowAxis, 2));
        Assert.Equal([30f, 60f, 90f], ParamAccess.AxisValues(image, fuel.ColAxis, 3));
        Assert.Equal(8200, image.Word(result.Defs[1].Offset));
        Assert.Equal(45f, TableMath.Lookup(image, fuel, 2000, 45));
    }

    [AssemblerFact]
    public async Task FeatureFlagsChangeTheAssembledCode() {
        var project = Project.Open(Fixtures.CopyMiniProject("flags"));
        var on = await ProjectBuilder.BuildAsync(project, Tools.Assembler!);
        var feature = (int)on.Map!.Get("feat_x")!.Value;
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44 }, on.Image!.Bytes.Slice(feature, 4).ToArray());

        project.SetFlag("FEATURE_X", false);
        var off = await ProjectBuilder.BuildAsync(project, Tools.Assembler!);
        Assert.True(off.Succeeded, string.Join("\n", off.Diagnostics));
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF }, off.Image!.Bytes.Slice(feature + 1, 3).ToArray());
        Assert.True(Checksum.IsValid(off.Image));

        // Only the feature's four bytes and the checksum byte differ.
        var differing = Enumerable.Range(0, 0x8000).Where(at => on.Image.Bytes[at] != off.Image.Bytes[at]).ToList();
        Assert.All(differing, at => Assert.True(at >= feature && at < feature + 4 || at == 0x7FFF, $"unexpected difference at 0x{at:X4}"));
        Assert.Equal("FEATURE_X EQU 1", File.ReadAllLines(Path.Combine(project.Root, "config.inc"))[1]);
    }

    [AssemblerFact]
    public async Task OverlayEditsAreAppliedAndRecoveredByDiff() {
        var project = Project.Open(Fixtures.CopyMiniProject("overlay"));
        project.Overlay = new Overlay([new OverlayEdit("tbl_fuel", 4, 99), new OverlayEdit("rev_limit", 1, 0x10)]);
        var result = await ProjectBuilder.BuildAsync(project, Tools.Assembler!);
        Assert.True(result.Succeeded);
        Assert.Equal(99, result.Image!.Bytes[0x104]);
        Assert.Equal(50, result.Assembled!.Bytes[0x104]);
        Assert.True(Checksum.IsValid(result.Image));
        Assert.Equal(project.Overlay.Edits, OverlayOps.FromDiff(result.Assembled, result.Image, result.Defs, 0x7FFF).Edits);
    }

    [AssemblerFact]
    public async Task SourceErrorsAndBadOverlaysAreReported() {
        var dir = Fixtures.CopyMiniProject("errors");
        var project = Project.Open(dir);
        project.Overlay = new Overlay([new OverlayEdit("no_such_symbol", 0, 1)]);
        var badOverlay = await ProjectBuilder.BuildAsync(project, Tools.Assembler!);
        Assert.False(badOverlay.Succeeded);
        Assert.Contains("overlay refers to unknown symbol `no_such_symbol`", badOverlay.Problems);
        Assert.NotNull(badOverlay.Assembled);

        File.AppendAllText(Path.Combine(dir, "main.asm"), "\n                FOO BAR\n");
        var badSource = await ProjectBuilder.BuildAsync(Project.Open(dir), Tools.Assembler!);
        Assert.False(badSource.Succeeded);
        var error = Assert.Single(badSource.Diagnostics);
        Assert.Equal(("main.asm", DiagnosticSeverity.Error), (error.File, error.Severity));
        Assert.True(error.Line > 1);
    }

    [AssemblerFact]
    public async Task MissingAssemblerIsABuildToolError() {
        var project = Project.Open(Fixtures.CopyMiniProject("notool"));
        await Assert.ThrowsAsync<BuildToolException>(() => ProjectBuilder.BuildAsync(project, new Rasm66kTool(Path.Combine(project.Root, "no-such-assembler.exe"))));
    }

    /// <summary>The P28-230 source project rebuilds the stock ROM byte for byte.</summary>
    [AssemblerFact("hondaromeditor/roms/p28-230/project.toml")]
    public async Task P28ProjectBuildsToItsBaseHash() {
        // Build a copy so the workspace project's build folder is left alone.
        var copy = Path.Combine(Path.GetTempPath(), "hre_project_tests", $"p28_{Environment.ProcessId}");
        if (Directory.Exists(copy)) Directory.Delete(copy, recursive: true);
        CopyDirectory(Workspace.P28Project, copy);
        var project = Project.Open(copy);
        var result = await ProjectBuilder.BuildAsync(project, Tools.Assembler!);
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Concat<object>(result.Problems)));
        Assert.Empty(result.Problems);
        Assert.Equal(project.Manifest.BaseSha256, result.Image!.Sha256());
        Assert.Equal(7, result.Defs.Count);
    }

    private static void CopyDirectory(string from, string to) {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(from)) {
            if (Path.GetFileName(dir) is not ("build" or ".git")) CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
        }
    }
}
