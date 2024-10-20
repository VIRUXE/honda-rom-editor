using System.Text.Json;

namespace HondaROMEditor {
	public static class Settings {
		private static readonly FileSystemWatcher settingsFileWatcher;
		private static System.Threading.Timer debounceTimer;
		private static readonly Lock debounceLock = new();

		private static Dictionary<string, JsonElement> Values = [];

		public static event EventHandler SettingsLoaded;
		public static event EventHandler SettingsChangedExternally;
		public static event EventHandler<SettingChangedEventArgs> SettingChanged;

		static Settings() {
			settingsFileWatcher = new(Program.DataDirectory) {
				Filter = Path.GetFileName(Program.SettingsFile),
				NotifyFilter = NotifyFilters.LastWrite
			};

			// Try to reload the settings file when it changes
			settingsFileWatcher.Changed += (object sender, FileSystemEventArgs e) => {
				if (e.ChangeType != WatcherChangeTypes.Changed) return;

				// * This bullshit triggers twice with every save so we need to debounce it
				lock (debounceLock) {
					debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
					debounceTimer = new System.Threading.Timer(_ => {
						Program.Log("Settings", "External changes detected in settings file. Reloading...");

						Thread.Sleep(100); // Do we really need to? Meh, it's just 100ms
						Program.Log("Settings", Load() ? "Reloaded successfully." : "Failed to reload. Check JSON syntax."); 

						SettingsChangedExternally?.Invoke(null, EventArgs.Empty);
					}, null, 100, Timeout.Infinite);
				}
			};

			settingsFileWatcher.EnableRaisingEvents = true;

			Program.Log("Settings", "Watching for external changes.");
		}

		public static bool DoesSettingsFileExist => File.Exists(Program.SettingsFile);

		public static bool DoesSettingExist(string key) => Values.ContainsKey(key);

		public static bool Load() {
			if (!DoesSettingsFileExist) return false;

			try {
				string json = File.ReadAllText(Program.SettingsFile);
				using var document = JsonDocument.Parse(json);
				Values = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

				SettingsLoaded?.Invoke(null, EventArgs.Empty);
			} catch {
				return false;
			}

			return true;
		}

		public static void Save() {
			string json = JsonSerializer.Serialize(Values, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(Program.SettingsFile, json);
		}

		public static T? Get<T>(string key, T? defaultValue) {
			if (!Values.TryGetValue(key, out JsonElement element)) return defaultValue ?? default;
			try { return element.Deserialize<T>()!; } catch { throw; }
		}

		public static void Set<T>(string key, T value) {
			var element  = JsonSerializer.SerializeToElement(value);
			var oldValue = Values.TryGetValue(key, out var existingElement) ? existingElement : default;

			Values[key] = element;

			Program.Log("Settings", $"Set value for '{key}'");

			SettingChanged?.Invoke(null, new SettingChangedEventArgs(key, oldValue, element));
		}
	}

	public class SettingChangedEventArgs(string key, JsonElement oldValue, JsonElement newValue) : EventArgs {
		public string Key { get; } = key;
		public JsonElement OldValue { get; } = oldValue;
		public JsonElement NewValue { get; } = newValue;
	}
}