using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Systrayping
{
    // WICHTIG: Das hier MUSS Form1 heißen, sonst reißt die Verbindung zum Designer ab!
    public partial class Form1 : Form
    {
        bool isRunning = false;

        public Form1()
        {
            InitializeComponent();

            this.Load += Form1_Load;
            this.Resize += Form1_Resize;
            notifyIcon1.MouseDoubleClick += notifyIcon1_MouseDoubleClick;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            this.Text = "SystrayPing";
            notifyIcon1.Text = "SystrayPing";
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();

            notifyIcon1.Icon = ErstelleIcon(Color.LightGray);
            notifyIcon1.Visible = true;

            StartePing();
        }

        private async void StartePing()
        {
            if (isRunning) return;

            isRunning = true;
            button1.Text = "Stop";
            int fehlerZaehler = 0;

            while (isRunning)
            {
                try
                {
                    Ping meinPing = new Ping();

                    // Bulletproof-Trick: Wir suchen uns die ComboBox einfach selbst, 
                    // um den IPAddress-Namenskonflikt in C# zu umgehen!
                    string aktuelleIp = "8.8.8.8";
                    foreach (Control c in this.Controls)
                    {
                        if (c is ComboBox cmb && !string.IsNullOrWhiteSpace(cmb.Text))
                        {
                            aktuelleIp = cmb.Text;
                            break; // Gefunden, Suche beenden
                        }
                    }

                    PingReply antwort = await meinPing.SendPingAsync(aktuelleIp, 1000);

                    if (antwort.Status == IPStatus.Success)
                    {
                        fehlerZaehler = 0;

                        button1.BackColor = Color.LightGreen;
                        notifyIcon1.Icon = ErstelleIcon(Color.LimeGreen);

                        AktualisiereLatenz($"{antwort.RoundtripTime} ms");
                    }
                    else
                    {
                        fehlerZaehler++;
                        AktualisiereLatenz("Timeout");
                    }
                }
                catch
                {
                    fehlerZaehler++;
                    AktualisiereLatenz("Fehler");
                }

                if (fehlerZaehler >= numericUpDown3.Value)
                {
                    button1.BackColor = Color.Salmon;
                    notifyIcon1.Icon = ErstelleIcon(Color.Red);
                }
                else if (fehlerZaehler >= numericUpDown2.Value)
                {
                    button1.BackColor = Color.LightYellow;
                    notifyIcon1.Icon = ErstelleIcon(Color.Yellow);
                }

                // Hier greifen wir nun sauber auf dein neues Feld zu
                int pingsProMinute = Math.Max(1, (int)PingMin.Value);
                int wartezeitMs = 60000 / pingsProMinute;

                await Task.Delay(wartezeitMs);
            }
        }

        private void button1_Click(object? sender, EventArgs e)
        {
            if (isRunning == true)
            {
                isRunning = false;
                button1.Text = "Start";
                button1.BackColor = Color.White;
                AktualisiereLatenz("- ms");

                notifyIcon1.Icon = ErstelleIcon(Color.LightGray);
            }
            else
            {
                StartePing();
            }
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private Icon ErstelleIcon(Color farbe)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (Brush pinsel = new SolidBrush(farbe))
                {
                    g.FillEllipse(pinsel, 0, 0, 15, 15);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        private void AktualisiereLatenz(string text)
        {
            Label? zielLabel = null;
            foreach (Control c in this.Controls)
            {
                if (c is Label lbl)
                {
                    if (zielLabel == null || lbl.Left > zielLabel.Left)
                    {
                        zielLabel = lbl;
                    }
                }
            }
            if (zielLabel != null)
            {
                zielLabel.Text = text;
            }
        }

        // =====================================================================
        // Dummy-Methoden für den Designer (beruhigt Visual Studio im Hintergrund)
        // =====================================================================
        private void numericUpDown1_ValueChanged(object? sender, EventArgs e) { }
        private void numericUpDown2_ValueChanged(object? sender, EventArgs e) { }
        private void Systrayping_MouseDoubleClick(object? sender, MouseEventArgs e) { }
        private void PingSec_ValueChanged(object? sender, EventArgs e) { }
    }
}