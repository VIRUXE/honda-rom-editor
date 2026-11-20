namespace HRE.Definitions;

/// <summary>A build-time feature toggle declared in <c>project.toml</c>.</summary>
public sealed record FeatureDef(string Key, string Label, bool Default, string Doc);

/// <summary>The contents of <c>project.toml</c>.</summary>
public sealed record ProjectManifest {
    public required string Name { get; init; }
    public required string Entry { get; init; }
    public required string Config { get; init; }
    public required string Definitions { get; init; }
    public required string Overlay { get; init; }
    public required string Output { get; init; }
    public int RomSize { get; init; } = 0x8000;
    public string? BaseSha256 { get; init; }
    public IReadOnlyList<FeatureDef> Features { get; init; } = [];

    public static ProjectManifest Parse(string text) {
        var root = TomlReader.ParseRoot(text);
        var manifest = new ProjectManifest {
            Name = TomlReader.String(root, "name"),
            Entry = TomlReader.String(root, "entry"),
            Config = TomlReader.String(root, "config"),
            Definitions = TomlReader.String(root, "definitions"),
            Overlay = TomlReader.String(root, "overlay"),
            Output = TomlReader.String(root, "output"),
            RomSize = (int)TomlReader.Integer(root, "rom_size", 0, int.MaxValue, 0x8000),
            BaseSha256 = TomlReader.OptionalString(root, "base_sha256"),
            Features = [.. TomlReader.Tables(root, "features").Select(f =>
                new FeatureDef(TomlReader.String(f, "key"), TomlReader.String(f, "label"), TomlReader.Bool(f, "default"), TomlReader.String(f, "doc", "")))],
        };
        manifest.CheckPaths();
        return manifest;
    }

    /// <summary>
    /// The manifest's file paths stay inside the project: <c>entry</c>, <c>config</c>,
    /// <c>definitions</c> and <c>overlay</c> are relative paths made only of plain names (no root,
    /// drive, <c>..</c> or <c>.</c>), and <c>output</c>, which is written to <c>build/</c>, is a
    /// bare file name.
    /// </summary>
    private void CheckPaths() {
        foreach (var (key, value) in new[] { ("entry", Entry), ("config", Config), ("definitions", Definitions), ("overlay", Overlay) }) {
            if (!IsPlain(value)) throw new HreFormatException($"{key} = \"{value}\" must be a relative path inside the project");
        }
        if (!IsPlain(Output) || Output.Contains('/') || Output.Contains('\\')) {
            throw new HreFormatException($"output = \"{Output}\" must be a bare file name; it is written to build/ inside the project");
        }
    }

    private static bool IsPlain(string path) =>
        path.Length > 0 && !path.Contains(':') && path.Split('/', '\\').All(part => part is not ("" or "." or ".."));
}

public enum ProjectErrorKind { Io, Format }

/// <summary>A project file that could not be read, or whose contents could not be understood.</summary>
public sealed class ProjectException(ProjectErrorKind kind, string path, string message, Exception? inner = null)
    : Exception($"{path}: {message}", inner) {
    public ProjectErrorKind Kind { get; } = kind;
    public string Path { get; } = path;
}

/// <summary>A ROM project on disk: manifest, feature flags, definitions and the calibration overlay.</summary>
public sealed class Project {
    private Project(string root, ProjectManifest manifest, SortedDictionary<string, bool> flags, DefinitionsFile definitions, Overlay overlay) {
        Root = root;
        Manifest = manifest;
        _flags = flags;
        Definitions = definitions;
        Overlay = overlay;
    }

    private readonly SortedDictionary<string, bool> _flags;

    public string Root { get; }
    public ProjectManifest Manifest { get; }
    public IReadOnlyDictionary<string, bool> Flags => _flags;
    public DefinitionsFile Definitions { get; }
    public Overlay Overlay { get; set; }
    public bool FlagsDirty { get; private set; }

    public string EntryPath => System.IO.Path.Combine(Root, Manifest.Entry);
    public string OutputPath => System.IO.Path.Combine(Root, "build", Manifest.Output);

    public static Project Open(string root) {
        var manifestPath = System.IO.Path.Combine(root, "project.toml");
        var manifest = Parsed(manifestPath, Read(manifestPath), ProjectManifest.Parse);
        var flags = ConfigInc.Parse(ReadOptional(System.IO.Path.Combine(root, manifest.Config)));
        foreach (var f in manifest.Features) flags.TryAdd(f.Key, f.Default);
        var definitionsPath = System.IO.Path.Combine(root, manifest.Definitions);
        var definitions = Parsed(definitionsPath, Read(definitionsPath), ParamDefinitions.Parse);
        var overlayPath = System.IO.Path.Combine(root, manifest.Overlay);
        var overlay = Parsed(overlayPath, ReadOptional(overlayPath), OverlayOps.Parse);
        return new Project(root, manifest, flags, definitions, overlay);
    }

    /// <summary>Writes the feature flags (when changed) and the overlay. Nothing else in the project is touched.</summary>
    public void Save() {
        if (FlagsDirty) {
            Write(System.IO.Path.Combine(Root, Manifest.Config), ConfigInc.Render(_flags));
            FlagsDirty = false;
        }
        Write(System.IO.Path.Combine(Root, Manifest.Overlay), OverlayOps.ToToml(Overlay));
    }

    public void SetFlag(string key, bool on) {
        if (_flags.TryGetValue(key, out var current) && current == on) return;
        _flags[key] = on;
        FlagsDirty = true;
    }

    private static T Parsed<T>(string path, string text, Func<string, T> parse) {
        try {
            return parse(text);
        } catch (HreFormatException e) {
            throw new ProjectException(ProjectErrorKind.Format, path, e.Message, e);
        }
    }

    private static string Read(string path) {
        try {
            return File.ReadAllText(path);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            throw new ProjectException(ProjectErrorKind.Io, path, e.Message, e);
        }
    }

    /// <summary>
    /// Like <see cref="Read"/>, but a file that does not exist reads as empty. Any other failure is
    /// an error: reading it as empty would lose its contents on the next save.
    /// </summary>
    private static string ReadOptional(string path) {
        try {
            return File.ReadAllText(path);
        } catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) {
            return "";
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            throw new ProjectException(ProjectErrorKind.Io, path, e.Message, e);
        }
    }

    private static void Write(string path, string text) {
        try {
            File.WriteAllText(path, text);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            throw new ProjectException(ProjectErrorKind.Io, path, e.Message, e);
        }
    }
}
