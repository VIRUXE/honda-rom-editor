using System.Diagnostics;
using System.Text.Json;

namespace HondaROMEditor {
	public partial class MainForm : Form {
		public MainForm() {
			InitializeComponent();
		}

		private async void MainForm_Shown(object sender, EventArgs e) {
			// Wait 10 seconds before checking for updates
			await Task.Delay(10000);

			if (!await IsUpdateAvailableAsync()) return;

			var updateItem = new ToolStripStatusLabel {
				Text = "Update available!",
				Image = Properties.Resources.Update,
				Alignment = ToolStripItemAlignment.Right,
				ToolTipText = "Double-Click to download the latest version.",
				DoubleClickEnabled = true
			};

			updateItem.DoubleClick += (_, _) => Process.Start(new ProcessStartInfo("https://github.com/VIRUXE/honda-rom-editor/releases/latest") { UseShellExecute = true });

			StatusStrip.Items.Add(updateItem);

			// Toggle between bold and regular font
			for (int i = 0; i < 6; i++) {
				await Task.Delay(500);
				updateItem.Font = new Font(updateItem.Font, updateItem.Font.Style == FontStyle.Bold ? FontStyle.Regular : FontStyle.Bold);
			}
		}

		private static async Task<bool> IsUpdateAvailableAsync() {
			if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return false;

			using HttpClient client = new();
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Honda ROM Editor");

			var response = await client.GetAsync("https://api.github.com/repos/VIRUXE/honda-rom-editor/releases/latest");
			if (!response.IsSuccessStatusCode) return false;

			var responseBody = await response.Content.ReadAsStringAsync();
			var release = JsonSerializer.Deserialize<JsonElement>(responseBody);

			return release.TryGetProperty("tag_name", out JsonElement tagName) && tagName.GetString()! != Application.ProductVersion.Split('+')[0]; // We don't need the commit hash
		}

		private void AboutMenuItem_Click(object sender, EventArgs e) {
			MessageBox.Show($"Honda ROM Editor\nVersion {Application.ProductVersion.Split('+')[0]}\n\n" + 
			"Thank you to everyone at PGMFI.org for their hard work and dedication to the Honda community. Especially Blundar, Calvin Baank, a1k0n and John Cui.\n\n" +
			"This software is provided as-is with no warranty.\n\n" +
			"GitHub: VIRUXE/honda-rom-editor",
			"About Honda ROM Editor", MessageBoxButtons.OK, MessageBoxIcon.None);
		}
	}
}
