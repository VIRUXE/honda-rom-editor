using HRE.Core;
using HRE.Definitions;

namespace HRE.Build;

/// <summary>What a build produced. <see cref="Image"/> is null when the source did not assemble or the overlay could not be applied.</summary>
public sealed record BuildResult {
    /// <summary>The finished ROM: assembled, overlay applied, checksum fixed.</summary>
    public RomImage? Image { get; init; }

    /// <summary>The image as assembled, before the overlay. Overlay edits are measured against this.</summary>
    public RomImage? Assembled { get; init; }

    public SymbolMap? Map { get; init; }

    /// <summary>The parameters whose symbols resolved.</summary>
    public IReadOnlyList<ParamDef> Defs { get; init; } = [];

    /// <summary>Definitions that could not be resolved, and an overlay that could not be applied.</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];

    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    public bool Succeeded => Image is not null;
}

/// <summary>Turns a source project into a ROM image.</summary>
public static class ProjectBuilder {
    /// <summary>
    /// Assembles the project with its feature flags, applies the calibration overlay, fixes the
    /// checksum and resolves the definitions against the symbol map. The image is written to the
    /// project's <c>build/</c> folder, next to the symbol map.
    /// </summary>
    public static async Task<BuildResult> BuildAsync(Project project, Rasm66kTool assembler, CancellationToken cancel = default) {
        var buildDir = Path.GetDirectoryName(project.OutputPath)!;
        Directory.CreateDirectory(buildDir);
        var assembledPath = project.OutputPath + ".asm";
        var mapPath = Path.ChangeExtension(project.OutputPath, ".map.json");
        var defines = project.Flags.ToDictionary(f => f.Key, f => f.Value ? 1L : 0L);

        var assembly = await assembler.AssembleAsync(project.EntryPath, assembledPath, mapPath, defines, project.Manifest.RomSize, cancel);
        if (!assembly.Succeeded) return new BuildResult { Diagnostics = assembly.Diagnostics };

        var assembled = RomImage.Load(assembledPath);
        File.Delete(assembledPath);
        var map = SymbolMap.FromJson(await File.ReadAllTextAsync(mapPath, cancel));
        var (defs, problems) = ParamDefinitions.Resolve(project.Definitions, map);
        var result = new BuildResult { Assembled = assembled, Map = map, Defs = defs, Problems = problems, Diagnostics = assembly.Diagnostics };

        RomImage image;
        try {
            image = Checksum.Fix(OverlayOps.Apply(project.Overlay, assembled, map));
        } catch (OverlayException e) {
            return result with { Problems = [.. problems, e.Message] };
        }
        image.Save(project.OutputPath);
        return result with { Image = image };
    }
}
