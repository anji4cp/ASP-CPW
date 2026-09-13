using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AspCpwLauncher
{
    internal sealed class LauncherForm : Form
    {
        private readonly Color dark = Color.FromArgb(13, 27, 35);
        private readonly Color panel = Color.FromArgb(21, 42, 52);
        private readonly Color accent = Color.FromArgb(29, 179, 138);
        private readonly WebBrowser news = new WebBrowser();
        private readonly Label status = new Label();
        private readonly Label clientLabel = new Label();
        private readonly Timer monitor = new Timer();
        private LauncherSettings settings;
        private DateTime navigationStarted;
        private bool navigationFinished;

        public LauncherForm()
        {
            settings = LauncherSettings.Load();
            Text = "ASP Launcher — Perfect World";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 620);
            ClientSize = new Size(1080, 700);
            BackColor = dark;
            Font = new Font("Segoe UI", 9F);
            BuildInterface();

            Shown += delegate { LoadNews(); };
            Resize += delegate { LayoutInterface(); };
            monitor.Interval = 1000;
            monitor.Tick += delegate { RefreshStatus(); };
            monitor.Start();
        }

        private void BuildInterface()
        {
            Label brand = new Label {
                Name = "Brand", Text = "ASP  LAUNCHER", ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 20F), AutoSize = true
            };
            Label subtitle = new Label {
                Name = "Subtitle", Text = "PERFECT WORLD CLIENT", ForeColor = Color.FromArgb(126, 151, 164),
                Font = new Font("Segoe UI", 8F), AutoSize = true
            };
            Controls.Add(brand); Controls.Add(subtitle);

            AddLinkButton("HOME", settings.HomeUrl);
            AddLinkButton("REGISTER", settings.RegisterUrl);
            AddLinkButton("FORUM", settings.ForumUrl);
            AddLinkButton("SUPPORT", settings.SupportUrl);

            Button choose = Button("CLIENT FOLDER", panel, ChooseClient);
            choose.Name = "ChooseClient"; Controls.Add(choose);
            Button reload = Button("RELOAD NEWS", panel, delegate { LoadNews(); });
            reload.Name = "Reload"; Controls.Add(reload);

            Panel browserFrame = new Panel { Name = "BrowserFrame", BackColor = Color.FromArgb(45, 65, 74), Padding = new Padding(1) };
            news.Dock = DockStyle.Fill;
            news.ScriptErrorsSuppressed = true;
            news.AllowWebBrowserDrop = false;
            news.IsWebBrowserContextMenuEnabled = false;
            news.WebBrowserShortcutsEnabled = false;
            news.Navigating += delegate { navigationStarted = DateTime.UtcNow; navigationFinished = false; SetStatus("Loading news…", Color.Gold); };
            news.DocumentCompleted += NewsLoaded;
            browserFrame.Controls.Add(news);
            Controls.Add(browserFrame);

            clientLabel.Name = "ClientLabel";
            clientLabel.ForeColor = Color.FromArgb(145, 166, 176);
            clientLabel.AutoEllipsis = true;
            clientLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(clientLabel);

            status.Name = "Status";
            status.AutoSize = false;
            status.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(status);

            Button update = Button("UPDATE & VERIFY", Color.FromArgb(36, 101, 156), RunUpdater);
            update.Name = "Update"; Controls.Add(update);
            Button play = Button("PLAY", accent, PlayGame);
            play.Name = "Play"; play.Font = new Font("Segoe UI Semibold", 14F); Controls.Add(play);
            LayoutInterface();
            RefreshStatus();
        }

        private void AddLinkButton(string label, string url)
        {
            Button button = Button(label, dark, delegate { OpenUrl(url); });
            button.Name = "Link" + label;
            button.FlatAppearance.BorderColor = Color.FromArgb(48, 71, 82);
            button.FlatAppearance.BorderSize = 1;
            Controls.Add(button);
        }

        private Button Button(string text, Color color, EventHandler click)
        {
            Button result = new Button {
                Text = text, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            result.FlatAppearance.BorderSize = 0;
            result.Click += click;
            return result;
        }

        private void LayoutInterface()
        {
            int margin = 34;
            Control brand = Controls["Brand"], subtitle = Controls["Subtitle"];
            brand.Location = new Point(margin, 22); subtitle.Location = new Point(margin + 3, 59);
            int x = Math.Max(330, ClientSize.Width - 530);
            string[] links = { "LinkHOME", "LinkREGISTER", "LinkFORUM", "LinkSUPPORT" };
            foreach (string name in links) { Controls[name].SetBounds(x, 27, 96, 34); x += 101; }
            Controls["Reload"].SetBounds(ClientSize.Width - 232, 82, 96, 30);
            Controls["ChooseClient"].SetBounds(ClientSize.Width - 131, 82, 97, 30);
            Controls["BrowserFrame"].SetBounds(margin, 126, ClientSize.Width - margin * 2, ClientSize.Height - 242);
            clientLabel.SetBounds(margin, ClientSize.Height - 103, ClientSize.Width - 430, 25);
            status.SetBounds(margin, ClientSize.Height - 72, ClientSize.Width - 430, 32);
            Controls["Update"].SetBounds(ClientSize.Width - 374, ClientSize.Height - 91, 170, 52);
            Controls["Play"].SetBounds(ClientSize.Width - 194, ClientSize.Height - 91, 160, 52);
        }

        private void LoadNews()
        {
            settings = LauncherSettings.Load();
            Uri uri;
            if (!Uri.TryCreate(settings.NewsUrl, UriKind.Absolute, out uri))
            {
                ShowNewsMessage("News URL is not configured. Open ASP CPW Desktop → Launcher Links.");
                return;
            }
            navigationStarted = DateTime.UtcNow;
            navigationFinished = false;
            news.Navigate(uri);
        }

        private void NewsLoaded(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (news.ReadyState != WebBrowserReadyState.Complete) return;
            navigationFinished = true;
            SetStatus("News connected", accent);
        }

        private void ShowNewsMessage(string message)
        {
            string encoded = System.Net.WebUtility.HtmlEncode(message);
            news.DocumentText = "<html><body style='background:#f4f7f8;color:#263942;font-family:Segoe UI,Arial;padding:40px'>" +
                "<h2>ASP Launcher</h2><p>" + encoded + "</p></body></html>";
            SetStatus(message, Color.OrangeRed);
        }

        private void ChooseClient(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the Perfect World client folder";
                dialog.SelectedPath = Directory.Exists(settings.ClientPath) ? settings.ClientPath : "";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!File.Exists(Path.Combine(dialog.SelectedPath, "patcher", "patcher.exe")) ||
                    !File.Exists(Path.Combine(dialog.SelectedPath, "element", "elementclient.exe")))
                {
                    MessageBox.Show(this, "This folder does not contain patcher\\patcher.exe and element\\elementclient.exe.",
                        "Invalid client folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LauncherSettings.SaveClientPath(dialog.SelectedPath);
                settings = LauncherSettings.Load();
                RefreshStatus();
            }
        }

        private void RunUpdater(object sender, EventArgs e)
        {
            string file;
            if (!GetClientFile(Path.Combine("patcher", "patcher.exe"), out file)) return;
            Start(file, Path.GetDirectoryName(file), "");
        }

        private void PlayGame(object sender, EventArgs e)
        {
            string file;
            if (!GetClientFile(Path.Combine("element", "elementclient.exe"), out file)) return;
            Start(file, Path.GetDirectoryName(file), "game:cpw");
        }

        private bool GetClientFile(string relativePath, out string file)
        {
            settings = LauncherSettings.Load();
            file = Path.Combine(settings.ClientPath ?? "", relativePath);
            if (File.Exists(file)) return true;
            MessageBox.Show(this, "Choose the Perfect World client folder first.", "Client required",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            ChooseClient(this, EventArgs.Empty);
            settings = LauncherSettings.Load();
            file = Path.Combine(settings.ClientPath ?? "", relativePath);
            return File.Exists(file);
        }

        private void Start(string file, string directory, string arguments)
        {
            try
            {
                Process.Start(new ProcessStartInfo(file, arguments) { WorkingDirectory = directory, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return;
            try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Unable to open link"); }
        }

        private void RefreshStatus()
        {
            settings = LauncherSettings.Load();
            clientLabel.Text = Directory.Exists(settings.ClientPath) ? "Client: " + settings.ClientPath : "Client folder is not selected";
            bool game = Process.GetProcessesByName("elementclient").Length > 0;
            bool updater = Process.GetProcessesByName("patcher").Length > 0;
            if (game) SetStatus("Game is running", accent);
            else if (updater) SetStatus("Updater is running", Color.Gold);
            else if (!navigationFinished && navigationStarted != DateTime.MinValue && DateTime.UtcNow.Subtract(navigationStarted).TotalSeconds > 12)
                SetStatus("News did not load. Check the News URL or web server.", Color.OrangeRed);
            else if (navigationFinished) SetStatus("Ready to play", accent);
            else SetStatus("Ready", Color.White);
        }

        private void SetStatus(string text, Color color)
        {
            status.Text = "●  " + text;
            status.ForeColor = color;
        }
    }
}
