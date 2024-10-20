#pragma warning disable WFO5001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace HondaROMEditor {
	internal static class Program {
		public static readonly string[] Arguments  = Environment.GetCommandLineArgs();
		public static readonly string Version      = Application.ProductVersion.Split('+')[0];
		public static readonly string VersionLabel = Application.ProductName + " v" + Version;

		public  static readonly string Repository    = "github.com/VIRUXE/honda-rom-editor";
		public  static readonly string Directory     = AppContext.BaseDirectory;
		public  static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName!);
		public  static readonly string SettingsFile  = Path.Combine(DataDirectory, "settings.json");
		private static readonly string debugLogFile  = Path.Combine(DataDirectory, "debug.log");

		private static readonly BindingList<string> debugLogEntries = [VersionLabel];
		private static readonly MainForm mainForm = new();

		public static ObservableCollection<(int Severity, string Description, string Location)> Errors = [];
		public static RomFile? ROM;

		[STAThread]
		static void Main() {
			ApplicationConfiguration.Initialize();

			// Blank out the debug log file
			File.WriteAllText(debugLogFile, $"[{DateTime.Now}] {VersionLabel}\n");

			// For everytime debugLogEntries is updated, refresh the debug log list box (DebugLogListBox in MainForm)
			mainForm.HandleCreated += (s, e) => { // Triggers when Windows creates the handle for the form
				Log("Window", "Handle created.");
				mainForm.DebugLogListBox.DataSource = debugLogEntries;
				debugLogEntries.ListChanged += (object? sender, ListChangedEventArgs e) => {
					if (!mainForm.IsHandleCreated) return;
					mainForm.Invoke(new Action(() => mainForm.DebugLogListBox.TopIndex = mainForm.DebugLogListBox.Items.Count - 1));
				};
			};

			// Create the data directory if it doesn't exist
			if (!System.IO.Directory.Exists(DataDirectory)) { // My Documents
				try {
					System.IO.Directory.CreateDirectory(DataDirectory);
					Log($"Created the data directory at '{DataDirectory}'.");
				} catch (Exception ex) {
					MessageBox.Show($"An error occurred while creating the data directory at '{DataDirectory}':\n\n{ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Log("An error occurred while creating the data directory.");
					Application.Exit();
				}
			}

			Log("Settings", "Loading...");
			Log("Settings", Settings.Load() ? "Loaded successfully." : "Failed to load.");

			//Application.SetDefaultFont(new Font("Cascadia Code", 10.0f));

			Application.SetColorMode(SystemColorMode.Dark);

			// For everytime Errors is updated, refresh the error list (ErrorListView in MainForm)
			Errors.CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs e) => mainForm.Invoke(new Action(() => {
                mainForm.ErrorListView.Items.Clear();

                foreach (var (Severity, Description, Location) in Errors) mainForm.ErrorListView.Items.Add(new ListViewItem([Severity.ToString(), Description, Location]));
            }));

			Log("Starting the application...");
			Application.Run(mainForm);
			Log("Application terminated.");

			Log("Settings", "Saving...");
			Settings.Save();
			Log("Terminated.");
		}

		public static void Log(string message) {
			debugLogEntries.Add(message);
			File.AppendAllText(debugLogFile, $"[{DateTime.Now}] {message}\n");
		}

		public static void Log(string location, string message) => Log($"[{location}] {message}");

		public static void Exit() => Application.Exit();
	}
}