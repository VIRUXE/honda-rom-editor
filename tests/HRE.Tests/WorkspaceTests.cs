using HRE.Core;
using HRE.Definitions;

namespace HRE.Tests;

/// <summary>
/// The folder that holds this repository next to the ROM source projects and ROM images, which
/// are not part of the repository. Set <c>HRE_WORKSPACE</c> to point somewhere else.
/// </summary>
internal static class Workspace {
    public static string Root { get; } = Environment.GetEnvironmentVariable("HRE_WORKSPACE") ?? FindParentOfRepository();

    public static string P28Project => Path.Combine(Root, "hondaromeditor", "roms", "p28-230");
    public static string P28Stock => Path.Combine(Root, "hondaecu", "P28-230.bin");

    private static string FindParentOfRepository() {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent) {
            if (File.Exists(Path.Combine(dir.FullName, "HondaRomEditor.slnx"))) return dir.Parent?.FullName ?? dir.FullName;
        }
        return AppContext.BaseDirectory;
    }
}

/// <summary>A test that needs files from the workspace; skipped when they are not there.</summary>
public sealed class WorkspaceFactAttribute : FactAttribute {
    public WorkspaceFactAttribute(params string[] required) {
        var missing = required.Select(r => Path.Combine(Workspace.Root, r)).FirstOrDefault(p => !File.Exists(p) && !Directory.Exists(p));
        if (missing is not null) Skip = $"not in the workspace: {missing}";
    }
}

[Trait("Category", "Corpus")]
public class WorkspaceTests {
    [WorkspaceFact("hondaromeditor/roms/p28-230/project.toml")]
    public void OpensTheP28Project() {
        var p = Project.Open(Workspace.P28Project);
        Assert.Equal("P28-230", p.Manifest.Name);
        Assert.Equal(0x8000, p.Manifest.RomSize);
        Assert.Equal(64, p.Manifest.BaseSha256!.Length);
        Assert.Equal(["FEATURE_DTC_FREEZE_FRAME", "FEATURE_EVAP_PURGE"], p.Manifest.Features.Select(f => f.Key));
        Assert.All(p.Manifest.Features, f => Assert.True(p.Flags[f.Key]));
        Assert.Equal(7, p.Definitions.Params.Count);
        Assert.Equal(4, p.Definitions.Params.Count(d => d.Kind == "table"));
        Assert.All(p.Definitions.Params.Where(d => d.Kind == "table"), d => {
            Assert.Equal((20, 10), (d.Rows, d.Cols));
            Assert.IsType<AxisSpec.Fixed>(d.RowAxis);
            Assert.IsType<AxisSpec.Fixed>(d.ColAxis);
        });
    }

    [WorkspaceFact("hondaromeditor/roms/p28-230/project.toml", "hondaecu/P28-230.bin")]
    public void StockP28MatchesTheProjectBaseHashAndChecksum() {
        var p = Project.Open(Workspace.P28Project);
        var stock = new RomImage(File.ReadAllBytes(Workspace.P28Stock).AsSpan(0, p.Manifest.RomSize));
        Assert.Equal(p.Manifest.BaseSha256, stock.Sha256());
        Assert.True(Checksum.IsValid(stock));
        Assert.Same(stock, Checksum.Fix(stock));
    }
}
