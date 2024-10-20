using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace HondaROMEditor {
	public partial class MainForm : Form {
		public MainForm() {
			InitializeComponent();

			Program.Log("Setting up controls...");

			SetupHoverCursor(ReportingStatusLabel);

			// Dummy data for the RomParametersTree
			var random = new Random();
			for (int i = 0; i < 10; i++) {
				var parentNode = new TreeNode($"Parent {random.Next(1, 100)}");

				for (int j = 0; j < 5; j++) {
					var childNode = new TreeNode($"Child {random.Next(1, 100)}");
					for (int k = 0; k < 3; k++) childNode.Nodes.Add(new TreeNode($"GrandChild {random.Next(1, 100)}"));
					parentNode.Nodes.Add(childNode);
				}

				RomParametersTree.Nodes.Add(parentNode);
			}

			// Dummy data for the ErrorListView
			for (int i = 0; i < 10; i++) ErrorListView.Items.Add(new ListViewItem([$"Severity {i}", $"Description {i}", $"Location {i}"]));

			RomParametersSearchBox.MaxLength = RomParametersTree.Nodes.Cast<TreeNode>() // parent nodes
				.SelectMany(parent => new[] { parent }
				.Concat(parent.Nodes.Cast<TreeNode>() // child nodes
				.SelectMany(child => new[] { child }
				.Concat(child.Nodes.Cast<TreeNode>())))) // grandchild nodes
				.Select(node => node.Text.Length)
				.Max();

			Program.Log("Controls initialized.");
		}

		private void MainForm_Load(object sender, EventArgs e) {
			MinimumSize = Size;

			// Have the panel for ReportingSplitContainer have enough height so ErrorListView can display 5 items
			// ! Something is up with this
			SplitContainer.SplitterDistance = SplitContainer.Size.Height - (DebugLogListBox.ItemHeight * 10);

			// Setup auto-complete for RomParametersSearchBox, using the text from RomParametersTree
			// ? Not sure about this
			var autoComplete = new AutoCompleteStringCollection();
			autoComplete.AddRange(RomParametersTree.Nodes.Cast<TreeNode>()
				.SelectMany(parent => new[] { parent }
				.Concat(parent.Nodes.Cast<TreeNode>()
				.SelectMany(child => new[] { child }
				.Concat(child.Nodes.Cast<TreeNode>()))))
				.Select(node => node.Text).ToArray());
				
			RomParametersSearchBox.AutoCompleteCustomSource = autoComplete;
			RomParametersSearchBox.AutoCompleteMode         = AutoCompleteMode.Suggest;
			RomParametersSearchBox.AutoCompleteSource       = AutoCompleteSource.CustomSource;
			
			// Get annoyed with this IQ quiz on EFI fundamentals
			if (!Settings.DoesSettingsFileExist || !Settings.Get("IsUserGoodEnough", false)) {
				// Shown on the "main panel" of the form
			}
		}

		private async void MainForm_Shown(object sender, EventArgs e) {
			// Wait 10 seconds before checking for updates
			await Task.Delay(10000);

			// Check for updates
			Program.Log("Update", "Checking for updates...");
			if (await IsUpdateAvailableAsync()) {
				Program.Log("Update", "Update available.");

				var updateItem = new ToolStripStatusLabel {
					Text               = "Update available!",
					Image              = Properties.Resources.Update,
					Alignment          = ToolStripItemAlignment.Right,
					ToolTipText        = "Double-Click to download the latest version.",
					DoubleClickEnabled = true
				};

				updateItem.DoubleClick += (_, _) => Process.Start(new ProcessStartInfo($"https://{Program.Repository}/releases/latest") { UseShellExecute = true });

				StatusStrip.Items.Add(updateItem);

				// Toggle between bold and regular font
				for (int i = 0; i < 6; i++) {
					await Task.Delay(500);
					updateItem.Font = new Font(updateItem.Font, updateItem.Font.Style == FontStyle.Bold ? FontStyle.Regular : FontStyle.Bold);
				}
			} else
				Program.Log("Update", "No update available.");
		}

		private static async Task<bool> IsUpdateAvailableAsync() {
			if (!NetworkInterface.GetIsNetworkAvailable() || !NetworkInterface.GetAllNetworkInterfaces().Any(ni => ni.GetIPProperties().GetIPv4Properties().IsDhcpEnabled)) return false;

			using HttpClient client = new();
			client.DefaultRequestHeaders.UserAgent.ParseAdd(Program.VersionLabel);

			try {
				var response = await client.GetAsync($"https://api.{Program.Repository}/releases/latest");
				if (!response.IsSuccessStatusCode) return false;

				var responseBody = await response.Content.ReadAsStringAsync();
				var release = JsonSerializer.Deserialize<JsonElement>(responseBody);

				if (!release.TryGetProperty("tag_name", out JsonElement tagName)) {
					Program.Log("Update", "Failed to retrieve the tag name from the release.");
					return false;
				}

				return tagName.GetString()! != Program.Version;
			} catch {
				Program.Log("Network", "An error occurred while checking for updates.");
			}

			return false;
		}

		private void AboutMenuItem_Click(object sender, EventArgs e) {
			MessageBox.Show($"{Application.ProductName}\nVersion {Program.Version}\n\n" +
			"An open-source ROM editor for OBD1 Honda ECUs.\n\n" +
			"Thank you to everyone at PGMFI.org for their hard work and dedication to the Honda community. Especially Blundar, Calvin Baank, a1k0n and John Cui.\n\n" +
			"This software is provided as-is with no warranty.\n\n" +
			"GitHub: VIRUXE/honda-rom-editor",
			"About " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.None);
		}

		private void RomSettingsButton_Click(object sender, EventArgs e) {
			var collapsed = !RomSplitContainer.Panel1Collapsed;
			RomSplitContainer.Panel1Collapsed = collapsed;

			Settings.Set("CollapseRomSettingsPanel", collapsed);
		}

		private void NewRomButton_Click(object sender, EventArgs e) {
			if (Program.ROM != null && MessageBox.Show("Are you sure you want to create a new ROM? All unsaved changes will be lost.", "New ROM", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No) return;

			Program.Log("ROM", "Creating a new file.");
			Program.ROM = new RomFile();
			Program.Log("ROM", "New file created.");

			RomParametersButton.Enabled = true;

			// Clear errors with "ROM" location
			foreach (var error in Program.Errors.Where(e => e.Location == "ROM").ToList()) Program.Errors.Remove(error);

			Program.Errors.Add((0, "Newly created ROM. No changes have been made.", "ROM"));

			var shouldCollapseRomSettingsPanel = Settings.Get("CollapseRomSettingsPanel", true);

			if (RomSplitContainer.Panel1Collapsed == shouldCollapseRomSettingsPanel) return; // It's already what we want it to be

			RomSplitContainer.Panel1Collapsed = shouldCollapseRomSettingsPanel;
		}

		// F1 key opens the wiki
		private void MainForm_HelpRequested(object sender, HelpEventArgs hlpevent) => Process.Start(new ProcessStartInfo("https://github.com/VIRUXE/honda-rom-editor/wiki") { UseShellExecute = true });

		// Double-clicking an item in the debug log copies the item to the clipboard
		private void DebugLogListBox_MouseDoubleClick(object sender, MouseEventArgs e) {
			if (DebugLogListBox.SelectedItem is string item)
				Clipboard.SetText(item);
		}

		#region ROM Settings
		// Key Down
		private void RomSettingsSearchBox_KeyDown(object sender, KeyEventArgs e) {
			// Clear the search box when the user presses the Escape key
			if (e.KeyCode == Keys.Escape) {
				e.SuppressKeyPress = true;

				RomParametersSearchBox.Clear();
			} else if (e.KeyCode == Keys.Enter) {
				e.SuppressKeyPress = true;

				// Search for the text in the search box
				var search = RomParametersSearchBox.Text;
				if (string.IsNullOrWhiteSpace(search)) return;

				// Search the entire tree for the text
				static TreeNode? FindNodeByText(TreeNodeCollection nodes, string searchText) {
					foreach (TreeNode node in nodes) {
						if (node.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase)) return node;

						TreeNode? foundNode = FindNodeByText(node.Nodes, searchText);
						if (foundNode != null) return foundNode;
					}

					return null;
				}

				var node = FindNodeByText(RomParametersTree.Nodes, search);

				if (node != null) {
					RomParametersTree.SelectedNode = node;
					node?.EnsureVisible();
				} else {
					RomParametersSearchBox.SelectAll();
				}

			}
		}
		#endregion

		private void SetupHoverCursor(ToolStripStatusLabel label) {
			label.MouseEnter += (_, _) => Cursor = Cursors.Hand;
			label.MouseLeave += (_, _) => Cursor = Cursors.Default;
		}

		private void ReportingStatusLabel_Click(object sender, EventArgs e) {
			SplitContainer.Panel2Collapsed = !SplitContainer.Panel2Collapsed;
		}
	}
}
