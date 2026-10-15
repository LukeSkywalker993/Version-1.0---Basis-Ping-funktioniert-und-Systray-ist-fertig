using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace ClaudeRemote
{
    // Ein Fenster: Server eingeben -> Agent installieren/starten -> Node-ID (Schlüsselcode) anzeigen.
    public class MainForm : Form
    {
        const string ServiceName = "12ps_claude_remote";          // = agentCustomization.serviceName in config.json
        const string AgentRegKey = @"SOFTWARE\Open Source\" + ServiceName;
        const string WindowsX64AgentId = "4";                     // MeshCentral-Agenttyp "Windows x64"
        const string InstallFlagsBackgroundOnly = "2";

        static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "12ps_claude_remote.json");

        readonly TextBox txtServer = new() { Width = 300 };
        readonly TextBox txtGroupKey = new() { Width = 300, UseSystemPasswordChar = true };
        readonly Button btnConnect = new() { Text = "Verbinden", Width = 110 };
        readonly Button btnDisconnect = new() { Text = "Trennen", Width = 110 };
        readonly Button btnUninstall = new() { Text = "Deinstallieren", Width = 110 };
        readonly TextBox txtCode = new() { ReadOnly = true, Width = 420, Font = new Font(FontFamily.GenericMonospace, 10f) };
        readonly Button btnCopyCode = new() { Text = "Code kopieren", Width = 140 };
        readonly Button btnCopyPrompt = new() { Text = "Text für Claude kopieren", Width = 180 };
        readonly TextBox txtLog = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Height = 150, Width = 560 };

        public MainForm()
        {
            Text = "12ps_claude_remote";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            grid.Controls.Add(new Label { Text = "Server (DNS-Name oder IP):", AutoSize = true, Anchor = AnchorStyles.Left });
            grid.Controls.Add(txtServer);
            grid.Controls.Add(new Label { Text = "Gruppen-Schlüssel:", AutoSize = true, Anchor = AnchorStyles.Left });
            grid.Controls.Add(txtGroupKey);

            var actions = new FlowLayoutPanel { AutoSize = true };
            actions.Controls.AddRange(new Control[] { btnConnect, btnDisconnect, btnUninstall });
            grid.Controls.Add(new Label());
            grid.Controls.Add(actions);

            grid.Controls.Add(new Label { Text = "Schlüsselcode (Node-ID):", AutoSize = true, Anchor = AnchorStyles.Left });
            grid.Controls.Add(txtCode);
            var copy = new FlowLayoutPanel { AutoSize = true };
            copy.Controls.AddRange(new Control[] { btnCopyCode, btnCopyPrompt });
            grid.Controls.Add(new Label());
            grid.Controls.Add(copy);

            grid.Controls.Add(txtLog);
            grid.SetColumnSpan(txtLog, 2);
            Controls.Add(grid);

            btnConnect.Click += async (_, _) => await RunGuarded(ConnectAsync);
            btnDisconnect.Click += async (_, _) => await RunGuarded(DisconnectAsync);
            btnUninstall.Click += async (_, _) => await RunGuarded(UninstallAsync);
            btnCopyCode.Click += (_, _) => CopyToClipboard(txtCode.Text);
            btnCopyPrompt.Click += (_, _) => CopyToClipboard(BuildClaudePrompt());

            LoadConfig();
            ShowNodeId(ReadNodeId());
            Log(ServiceInstalled() ? "Agent ist installiert." : "Agent ist noch nicht installiert.");
        }

        // ---------------------------------------------------------------- Aktionen

        async Task ConnectAsync()
        {
            string server = NormalizeServer(txtServer.Text);
            if (server.Length == 0) { Log("Bitte den Server eintragen."); return; }
            if (!await ServerReachableAsync(server)) { Log($"Server https://{server} ist nicht erreichbar."); return; }
            Log($"Server https://{server} erreichbar.");

            if (!ServiceInstalled())
            {
                string key = txtGroupKey.Text.Trim();
                if (key.Length == 0) { Log("Für die Erstinstallation wird der Gruppen-Schlüssel gebraucht."); return; }

                string setup = await DownloadAgentAsync(server, key);
                Log("Installiere Agent als Dienst ...");
                int exit = await RunAsync(setup, "-fullinstall");
                TryDelete(setup);
                if (exit != 0) Log($"Installer meldet Exit-Code {exit}.");
                if (!ServiceInstalled()) { Log("Installation fehlgeschlagen: Dienst wurde nicht angelegt."); return; }
                SaveConfig(server, key);
            }
            else
            {
                Log("Starte Dienst ...");
                await RunAsync("sc.exe", $"start {ServiceName}");
            }

            Log("Warte auf Node-ID ...");
            for (int i = 0; i < 60; i++)
            {
                string? id = ReadNodeId();
                if (id != null) { ShowNodeId(id); Log("Verbunden. Schlüsselcode kann jetzt in Claude eingefügt werden."); return; }
                await Task.Delay(1000);
            }
            Log("Keine Node-ID erhalten. Läuft der Dienst? (services.msc > " + ServiceName + ")");
        }

        async Task DisconnectAsync()
        {
            if (!ServiceInstalled()) { Log("Agent ist nicht installiert."); return; }
            await RunAsync("sc.exe", $"stop {ServiceName}");
            Log("Dienst gestoppt: Verbindung zum Server ist getrennt. \"Verbinden\" startet ihn wieder.");
        }

        async Task UninstallAsync()
        {
            string? exe = InstalledAgentPath();
            if (exe == null) { Log("Agent ist nicht installiert."); return; }
            if (MessageBox.Show(this, "Agent wirklich deinstallieren?", Text, MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Log("Deinstalliere Agent ...");
            await RunAsync(exe, "-fulluninstall");
            ShowNodeId(null);
            Log(ServiceInstalled() ? "Deinstallation fehlgeschlagen." : "Agent deinstalliert.");
        }

        // ---------------------------------------------------------------- MeshCentral / Agent

        static HttpClient CreateHttpClient() => new(new HttpClientHandler
        {
            // MeshCentral im LAN nutzt ein selbstsigniertes Zertifikat. Der Agent selbst prüft
            // den Server danach über den im Installer eingebetteten Zertifikats-Hash.
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        }) { Timeout = TimeSpan.FromMinutes(2) };

        static async Task<bool> ServerReachableAsync(string server)
        {
            try
            {
                using var http = CreateHttpClient();
                http.Timeout = TimeSpan.FromSeconds(8);
                using var resp = await http.GetAsync($"https://{server}/");
                return true;
            }
            catch { return false; }
        }

        async Task<string> DownloadAgentAsync(string server, string groupKey)
        {
            string url = $"https://{server}/meshagents?id={WindowsX64AgentId}&meshid={Uri.EscapeDataString(groupKey)}&installflags={InstallFlagsBackgroundOnly}";
            Log("Lade Agent vom Server ...");
            using var http = CreateHttpClient();
            using var resp = await http.GetAsync(url);
            if ((int)resp.StatusCode == 401) throw new InvalidOperationException("Gruppen-Schlüssel ist ungültig.");
            resp.EnsureSuccessStatusCode();

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ServiceName);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "agent-setup.exe");
            await using (var fs = File.Create(path)) await resp.Content.CopyToAsync(fs);
            return path;
        }

        // Der Agent legt seine Node-ID beim Start unter HKLM\SOFTWARE\Open Source\<Dienstname>\NodeId ab
        // (Base64, '+' -> '@', '/' -> '$'), wie sie auch der Server verwendet.
        static string? ReadNodeId()
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(AgentRegKey);
            return key?.GetValue("NodeId") is string s && s.Length > 0 ? "node//" + s : null;
        }

        static string? InstalledAgentPath()
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + ServiceName);
            if (key?.GetValue("ImagePath") is not string image) return null;
            image = image.Trim();
            if (image.StartsWith('"')) image = image[1..image.IndexOf('"', 1)];
            return File.Exists(image) ? image : null;
        }

        static bool ServiceInstalled()
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + ServiceName);
            return key != null;
        }

        // ---------------------------------------------------------------- Hilfsfunktionen

        string BuildClaudePrompt()
        {
            string server = NormalizeServer(txtServer.Text);
            return $"Verbinde dich über MeshCentral unter https://{server} mit der Node-ID {txtCode.Text} " +
                   "(Zugangsdaten: token_user/token_pass aus secrets\\zugang.txt). " +
                   "Sobald die Verbindung steht, führe in PowerShell Folgendes aus: <Aufgabe>";
        }

        static string NormalizeServer(string s)
        {
            s = s.Trim();
            foreach (var prefix in new[] { "https://", "http://", "wss://" })
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) s = s[prefix.Length..];
            return s.TrimEnd('/');
        }

        void ShowNodeId(string? id)
        {
            txtCode.Text = id ?? "";
            btnCopyCode.Enabled = btnCopyPrompt.Enabled = id != null;
        }

        void CopyToClipboard(string text)
        {
            if (text.Length == 0) return;
            Clipboard.SetText(text);
            Log("In die Zwischenablage kopiert.");
        }

        void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ConfigPath));
                if (cfg == null) return;
                if (cfg.TryGetValue("server", out var s)) txtServer.Text = s;
                if (cfg.TryGetValue("groupKey", out var k)) txtGroupKey.Text = k;
            }
            catch (Exception ex) { Log("Konfigdatei nicht lesbar: " + ex.Message); }
        }

        void SaveConfig(string server, string groupKey)
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["server"] = server, ["groupKey"] = groupKey
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* Ordner evtl. schreibgeschützt, Werte sind dann beim nächsten Start neu einzugeben */ }
        }

        static async Task<int> RunAsync(string file, string args)
        {
            var psi = new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Start fehlgeschlagen: " + file);
            await p.WaitForExitAsync();
            return p.ExitCode;
        }

        static void TryDelete(string path) { try { File.Delete(path); } catch { } }

        async Task RunGuarded(Func<Task> action)
        {
            btnConnect.Enabled = btnDisconnect.Enabled = btnUninstall.Enabled = false;
            try { await action(); }
            catch (Exception ex) { Log("Fehler: " + ex.Message); }
            finally { btnConnect.Enabled = btnDisconnect.Enabled = btnUninstall.Enabled = true; }
        }

        void Log(string msg) => txtLog.AppendText($"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}");
    }
}
