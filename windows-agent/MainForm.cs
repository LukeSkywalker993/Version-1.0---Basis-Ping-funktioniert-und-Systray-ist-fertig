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

        // Farben
        static readonly Color Accent = Color.FromArgb(0, 103, 192);
        static readonly Color AccentDark = Color.FromArgb(0, 84, 158);
        static readonly Color HeaderBack = Color.FromArgb(24, 43, 72);
        static readonly Color PageBack = Color.FromArgb(243, 246, 250);
        static readonly Color BorderGray = Color.FromArgb(205, 212, 222);
        static readonly Color TextDark = Color.FromArgb(32, 38, 46);
        static readonly Color TextMuted = Color.FromArgb(96, 106, 120);
        static readonly Color Danger = Color.FromArgb(176, 32, 32);

        readonly TextBox txtServer = MakeTextBox();
        readonly TextBox txtGroupKey = MakeTextBox(password: true);
        readonly Button btnConnect = MakeButton("Verbinden", primary: true);
        readonly Button btnDisconnect = MakeButton("Trennen");
        readonly Button btnUninstall = MakeButton("Deinstallieren", fore: Danger);
        readonly TextBox txtCode = MakeTextBox(readOnly: true, mono: true);
        readonly Button btnCopyCode = MakeButton("Code kopieren");
        readonly Button btnCopyPrompt = MakeButton("Text für Claude kopieren", primary: true);
        readonly TextBox txtLog = new()
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Height = 140, Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, ForeColor = TextMuted,
            Font = new Font("Consolas", 9f), Margin = new Padding(0, 4, 0, 0)
        };
        readonly Label lblStatus = new()
        {
            AutoSize = true, Anchor = AnchorStyles.Right, ForeColor = Color.White, BackColor = Color.Gray,
            Font = new Font("Segoe UI Semibold", 9.5f), Padding = new Padding(10, 4, 10, 4), Text = "…"
        };

        public MainForm()
        {
            Text = "12ps Claude Remote";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = PageBack;
            ForeColor = TextDark;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(600, 580);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // Kopfzeile
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top, Height = 72, BackColor = HeaderBack, ColumnCount = 2, RowCount = 2,
                Padding = new Padding(20, 12, 20, 10)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(new Label
            {
                Text = "12ps Claude Remote", AutoSize = true, ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 15f), Margin = new Padding(0)
            }, 0, 0);
            header.Controls.Add(new Label
            {
                Text = "Fernwartung über MeshCentral", AutoSize = true, ForeColor = Color.FromArgb(170, 190, 215),
                Font = new Font("Segoe UI", 9.5f), Margin = new Padding(2, 0, 0, 0)
            }, 0, 1);
            header.Controls.Add(lblStatus, 1, 0);
            header.SetRowSpan(lblStatus, 2);

            // Inhalt
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20, 12, 20, 16) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void Section(string title)
            {
                var l = new Label
                {
                    Text = title.ToUpperInvariant(), AutoSize = true, ForeColor = Accent,
                    Font = new Font("Segoe UI Semibold", 9f), Margin = new Padding(0, 14, 0, 6)
                };
                body.Controls.Add(l); body.SetColumnSpan(l, 2);
            }
            void Field(string label, Control c)
            {
                body.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = TextMuted, Margin = new Padding(0, 6, 12, 6) });
                c.Dock = DockStyle.Fill;
                body.Controls.Add(c);
            }
            void Row(params Control[] buttons)
            {
                var f = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
                f.Controls.AddRange(buttons);
                body.Controls.Add(f); body.SetColumnSpan(f, 2);
            }

            Section("Verbindung");
            Field("Server", txtServer);
            Field("Gruppen-Schlüssel", txtGroupKey);
            Row(btnConnect, btnDisconnect, btnUninstall);

            Section("Schlüsselcode für Claude");
            body.Controls.Add(txtCode); body.SetColumnSpan(txtCode, 2); txtCode.Dock = DockStyle.Fill;
            Row(btnCopyPrompt, btnCopyCode);

            Section("Protokoll");
            body.Controls.Add(txtLog); body.SetColumnSpan(txtLog, 2);
            body.RowStyles.Clear();
            for (int i = 0; i < body.Controls.Count; i++) body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles[^1] = new RowStyle(SizeType.Percent, 100);

            Controls.Add(body);
            Controls.Add(header);

            btnConnect.Click += async (_, _) => await RunGuarded(ConnectAsync);
            btnDisconnect.Click += async (_, _) => await RunGuarded(DisconnectAsync);
            btnUninstall.Click += async (_, _) => await RunGuarded(UninstallAsync);
            btnCopyCode.Click += (_, _) => CopyToClipboard(txtCode.Text);
            btnCopyPrompt.Click += (_, _) => CopyToClipboard(BuildClaudePrompt());

            LoadConfig();
            ShowNodeId(ReadNodeId());
            Log(ServiceInstalled() ? "Agent ist installiert." : "Agent ist noch nicht installiert.");
            Shown += async (_, _) => await UpdateStatusAsync();
        }

        static TextBox MakeTextBox(bool password = false, bool readOnly = false, bool mono = false) => new()
        {
            UseSystemPasswordChar = password, ReadOnly = readOnly, BorderStyle = BorderStyle.FixedSingle,
            BackColor = readOnly ? Color.FromArgb(234, 239, 245) : Color.White,
            Font = mono ? new Font("Consolas", 10.5f) : new Font("Segoe UI", 10.5f),
            Margin = new Padding(0, 4, 0, 4), MinimumSize = new Size(260, 0)
        };

        static Button MakeButton(string text, bool primary = false, Color? fore = null)
        {
            var b = new Button
            {
                Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlatStyle = FlatStyle.Flat,
                Padding = new Padding(14, 5, 14, 5), Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand,
                BackColor = primary ? Accent : Color.White, ForeColor = primary ? Color.White : fore ?? TextDark,
                Font = new Font(primary ? "Segoe UI Semibold" : "Segoe UI", 10f), UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = primary ? Accent : BorderGray;
            b.FlatAppearance.MouseOverBackColor = primary ? AccentDark : Color.FromArgb(233, 238, 245);
            b.FlatAppearance.MouseDownBackColor = primary ? AccentDark : Color.FromArgb(220, 228, 238);
            return b;
        }

        async Task UpdateStatusAsync()
        {
            string text; Color color;
            if (!ServiceInstalled()) { text = "Nicht installiert"; color = Color.FromArgb(110, 118, 130); }
            else
            {
                string q = await RunCaptureAsync("sc.exe", $"query {ServiceName}");
                if (q.Contains("RUNNING")) { text = "● Verbunden"; color = Color.FromArgb(22, 140, 70); }
                else { text = "Getrennt"; color = Color.FromArgb(200, 120, 0); }
            }
            lblStatus.Text = text;
            lblStatus.BackColor = color;
        }

        static async Task<string> RunCaptureAsync(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi);
                if (p == null) return "";
                string o = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
                return o;
            }
            catch { return ""; }
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
            finally
            {
                btnConnect.Enabled = btnDisconnect.Enabled = btnUninstall.Enabled = true;
                await UpdateStatusAsync();
            }
        }

        void Log(string msg) => txtLog.AppendText($"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}");
    }
}
