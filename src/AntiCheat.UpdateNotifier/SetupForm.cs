namespace AntiCheat.UpdateNotifier;

internal sealed class SetupForm : Form
{
    private readonly TextBox pluginPath = new() { Dock = DockStyle.Fill };
    private readonly ComboBox edition = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckBox prereleases = new() { Text = "Include experimental prereleases", AutoSize = true, Checked = true };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(550, 0) };

    internal SetupForm(NotifierSettings? saved)
    {
        Text = "Among Us Anti Cheat — Update notifications";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(650, 410);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var description = new Label
        {
            Text = "Get a notification when a newer mod version is released. After Windows sign-in, the notifier checks GitHub once and exits. Downloads and installation stay manual.",
            AutoSize = true, MaximumSize = new Size(555, 0), Margin = new Padding(0, 0, 0, 16)
        };
        layout.Controls.Add(description, 0, 0); layout.SetColumnSpan(description, 2);
        layout.Controls.Add(new Label { Text = "Installed mod: BepInEx/plugins/AntiCheat/AmongUs.AntiCheat.dll", AutoSize = true }, 0, 1);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 1)!, 2);
        layout.Controls.Add(pluginPath, 0, 2);
        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) =>
        {
            using var picker = new OpenFileDialog { Filter = "Anti Cheat DLL|AmongUs.AntiCheat.dll", CheckFileExists = true };
            if (picker.ShowDialog(this) == DialogResult.OK) pluginPath.Text = picker.FileName;
        };
        layout.Controls.Add(browse, 1, 2);
        edition.Items.AddRange(new object[] { "MicrosoftStore", "Steam", "EpicGames" });
        edition.SelectedItem = saved?.Edition ?? "MicrosoftStore";
        layout.Controls.Add(new Label { Text = "Game edition", AutoSize = true }, 0, 3);
        layout.Controls.Add(edition, 0, 4); layout.SetColumnSpan(edition, 2);
        layout.Controls.Add(prereleases, 0, 5); layout.SetColumnSpan(prereleases, 2);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 8) };
        var enable = new Button { Text = "Enable at Windows sign-in", AutoSize = true };
        enable.Click += (_, _) =>
        {
            try
            {
                new NotifierSettings { PluginPath = pluginPath.Text, Edition = edition.Text, IncludePrereleases = prereleases.Checked }.Install();
                status.Text = "Enabled for your Windows account. Restarting isn't needed; the next check runs at your next sign-in.";
            }
            catch (Exception error) { status.Text = error.Message; }
        };
        var disable = new Button { Text = "Disable startup checks", AutoSize = true };
        disable.Click += (_, _) =>
        {
            try { NotifierSettings.Disable(); status.Text = "Startup checks disabled."; }
            catch (Exception error) { status.Text = error.Message; }
        };
        actions.Controls.Add(enable); actions.Controls.Add(disable);
        layout.Controls.Add(actions, 0, 6); layout.SetColumnSpan(actions, 2);
        status.Text = NotifierSettings.IsEnabled() ? "Startup checks are enabled for this Windows account." : "Optional — no startup entry is added until you click Enable.";
        layout.Controls.Add(status, 0, 7); layout.SetColumnSpan(status, 2);
        Controls.Add(layout);
        pluginPath.Text = saved?.PluginPath ?? "";
        prereleases.Checked = saved?.IncludePrereleases ?? true;
    }
}
