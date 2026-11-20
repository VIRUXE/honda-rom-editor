using System.Text.Json;
using System.Text.Json.Serialization;

namespace HRE.App;

/// <summary>Where the window was, in device pixels.</summary>
public sealed record WindowPlace(int X, int Y, int Width, int Height, bool Maximised);

/// <summary>What the program remembers between runs.</summary>
public sealed class AppSettings {
    public const int MostRecentFiles = 8;
    public const double SmallestZoom = 1.0;
    public const double LargestZoom = 2.0;

    public WindowPlace? Window { get; set; }

    /// <summary>Width of the list of tables, in pixels at 100 % scaling.</summary>
    public int ListWidth { get; set; } = 300;

    /// <summary>Text size as a factor of the normal size: 1 to 2.</summary>
    public double TextZoom { get; set; } = 1.0;

    public bool ShowTechnical { get; set; }

    /// <summary>Files opened lately, the latest first.</summary>
    public List<string> RecentFiles { get; set; } = [];

    /// <summary>Puts a file at the head of the recent files.</summary>
    public void Remember(string path) {
        Forget(path);
        RecentFiles.Insert(0, path);
        if (RecentFiles.Count > MostRecentFiles) RecentFiles.RemoveRange(MostRecentFiles, RecentFiles.Count - MostRecentFiles);
    }

    public void Forget(string path) => RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));

    /// <summary>Brings values read from a file back into what the program can work with.</summary>
    internal void Repair() {
        TextZoom = double.IsFinite(TextZoom) ? Math.Clamp(TextZoom, SmallestZoom, LargestZoom) : 1.0;
        ListWidth = Math.Clamp(ListWidth, 160, 2000);
        RecentFiles = [.. (RecentFiles ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).Take(MostRecentFiles)];
    }
}

/// <summary>Reads and writes the settings file. A missing or damaged file means the defaults, without a word.</summary>
public static partial class SettingsFile {
    /// <summary>The settings file of this user.</summary>
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HondaRomEditor", "settings.json");

    public static AppSettings Load(string path) {
        AppSettings? settings = null;
        try {
            if (File.Exists(path)) settings = JsonSerializer.Deserialize(File.ReadAllText(path), Json.Default.AppSettings);
        } catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException) {
            // Settings are a convenience: start afresh.
        }
        settings ??= new AppSettings();
        settings.Repair();
        return settings;
    }

    /// <summary>Writes the file through a temporary one, so a crash cannot leave half a file. Returns false when it could not be written.</summary>
    public static bool Save(string path, AppSettings settings) {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Json.Default.AppSettings));
            File.Move(temporary, path, overwrite: true);
            return true;
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException) {
            return false;
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(AppSettings))]
    private sealed partial class Json : JsonSerializerContext;
}
