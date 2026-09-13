using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace AspCpwLauncher
{
    internal sealed class NativeOverlayContext : ApplicationContext
    {
        private readonly Timer timer = new Timer();
        private readonly DateTime startedAt = DateTime.UtcNow;
        private LauncherSettings settings;
        private Process patcher;
        private string patcherExecutable;
        private NewsOverlayForm overlay;
        private Rectangle mapRegion;
        private Size mapSize;
        private IntPtr parentHandle;
        private bool attached;
        private readonly bool diagnostic;
        private Rectangle lastOverlayBounds;
        private bool hasOverlayBounds;
        private bool parentWasHidden;

        public NativeOverlayContext(string clientOverride = null, bool diagnosticMode = false)
        {
            diagnostic = diagnosticMode;
            LauncherLog.Write("ASP Launcher overlay starting.");
            settings = LauncherSettings.Load();
            if (!String.IsNullOrWhiteSpace(clientOverride)) settings.ClientPath = clientOverride;
            else
            {
                string installedClient = DiscoverInstalledClient();
                if (!String.IsNullOrWhiteSpace(installedClient)) settings.ClientPath = installedClient;
            }
            LauncherLog.Write("Client setting: " + (settings.ClientPath ?? ""));
            if (!EnsureClient()) { LauncherLog.Write("Client selection cancelled or invalid."); ExitThread(); return; }
            try
            {
                patcherExecutable = ResolvePatcherExecutable(settings.ClientPath);
                if (String.IsNullOrWhiteSpace(patcherExecutable))
                    throw new FileNotFoundException("The original patcher executable was not found.");
                ReadBrowserRegion();
                LauncherLog.Write(String.Format("News region: X={0}, Y={1}, W={2}, H={3}.", mapRegion.X, mapRegion.Y, mapRegion.Width, mapRegion.Height));
                patcher = FindRunningPatcher();
                if (patcher == null)
                {
                    string file = patcherExecutable;
                    LauncherLog.Write("Starting original patcher: " + file);
                    patcher = Process.Start(new ProcessStartInfo(file) {
                        WorkingDirectory = Path.GetDirectoryName(file), UseShellExecute = true
                    });
                }
                else LauncherLog.Write("Attaching to running patcher, PID " + patcher.Id + ".");
                overlay = new NewsOverlayForm(settings.NewsUrl);
                timer.Interval = 33;
                timer.Tick += MonitorPatcher;
                timer.Start();
            }
            catch (Exception ex)
            {
                LauncherLog.Write("Startup error: " + ex);
                MessageBox.Show("ASP News Overlay could not start.\r\n\r\n" + ex.Message,
                    "ASP Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitThread();
            }
        }

        private bool EnsureClient()
        {
            if (ValidClient(settings.ClientPath)) return true;
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the Perfect World client folder";
                if (dialog.ShowDialog() != DialogResult.OK) return false;
                if (!ValidClient(dialog.SelectedPath))
                {
                    MessageBox.Show("The selected folder does not contain patcher\\patcher.exe and element\\elementclient.exe.",
                        "Invalid client folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                LauncherSettings.SaveClientPath(dialog.SelectedPath);
                settings = LauncherSettings.Load();
                return true;
            }
        }

        private static bool ValidClient(string path)
        {
            return !String.IsNullOrWhiteSpace(path) &&
                !String.IsNullOrWhiteSpace(ResolvePatcherExecutable(path)) &&
                File.Exists(Path.Combine(path, "element", "elementclient.exe"));
        }

        private static string DiscoverInstalledClient()
        {
            try
            {
                string executable = Process.GetCurrentProcess().MainModule.FileName;
                string directory = Path.GetDirectoryName(executable);
                if (File.Exists(Path.Combine(directory, "patcher", "patcher-core.exe")) &&
                    File.Exists(Path.Combine(directory, "element", "elementclient.exe"))) return directory;
                DirectoryInfo folder = new DirectoryInfo(directory);
                if ((folder.Name.Equals("patcher", StringComparison.OrdinalIgnoreCase) ||
                     folder.Name.Equals("launcher", StringComparison.OrdinalIgnoreCase)) && folder.Parent != null)
                {
                    string root = folder.Parent.FullName;
                    if (File.Exists(Path.Combine(root, "patcher", "patcher-core.exe")) &&
                        File.Exists(Path.Combine(root, "element", "elementclient.exe"))) return root;
                }
            }
            catch { }
            return null;
        }

        private static string ResolvePatcherExecutable(string clientPath)
        {
            if (String.IsNullOrWhiteSpace(clientPath)) return null;
            string core = Path.Combine(clientPath, "patcher", "patcher-core.exe");
            if (File.Exists(core)) return core;
            string normal = Path.Combine(clientPath, "patcher", "patcher.exe");
            try
            {
                string current = Process.GetCurrentProcess().MainModule.FileName;
                if (File.Exists(normal) && !String.Equals(Path.GetFullPath(current), Path.GetFullPath(normal), StringComparison.OrdinalIgnoreCase))
                    return normal;
            }
            catch { }
            return null;
        }

        private Process FindRunningPatcher()
        {
            string processName = Path.GetFileNameWithoutExtension(patcherExecutable);
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (String.Equals(process.MainModule.FileName,
                        patcherExecutable,
                        StringComparison.OrdinalIgnoreCase)) return process;
                }
                catch { }
            }
            return null;
        }

        private void ReadBrowserRegion()
        {
            string patcherRoot = Path.Combine(settings.ClientPath, "patcher");
            string xmlPath = Path.Combine(patcherRoot, "skin", "mainuni.xml");
            XmlDocument document = new XmlDocument();
            document.Load(xmlPath);
            XmlElement browser = document.SelectSingleNode("//SkinBrowser[@Name='UpdateBrowser']") as XmlElement;
            XmlElement mapImage = browser == null ? null : browser.SelectSingleNode("ancestor::SkinWindow[1]//Image[@Type='MapImage']") as XmlElement;
            if (browser == null || mapImage == null) throw new InvalidDataException("UpdateBrowser or MapImage was not found in mainuni.xml.");

            Color marker = ParseColor(browser.GetAttribute("MapColor"));
            string relative = mapImage.GetAttribute("Path").Replace('/', Path.DirectorySeparatorChar);
            string imagePath = Path.Combine(patcherRoot, relative);
            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                mapSize = bitmap.Size;
                int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
                BitmapData data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    int stride = Math.Abs(data.Stride);
                    byte[] pixels = new byte[stride * bitmap.Height];
                    Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int row = y * stride;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            int offset = row + x * 4;
                            if (pixels[offset + 2] != marker.R || pixels[offset + 1] != marker.G || pixels[offset] != marker.B) continue;
                            if (x < left) left = x; if (x > right) right = x;
                            if (y < top) top = y; if (y > bottom) bottom = y;
                        }
                    }
                }
                finally { bitmap.UnlockBits(data); }
                if (right < left || bottom < top) throw new InvalidDataException("The UpdateBrowser color was not found in the launcher map image.");
                mapRegion = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
            }
        }

        private static Color ParseColor(string value)
        {
            string[] parts = value.Split(',');
            int r, g, b;
            if (parts.Length != 3 || !Int32.TryParse(parts[0], out r) ||
                !Int32.TryParse(parts[1], out g) || !Int32.TryParse(parts[2], out b))
                throw new InvalidDataException("UpdateBrowser MapColor is invalid.");
            return Color.FromArgb(r, g, b);
        }

        private void MonitorPatcher(object sender, EventArgs e)
        {
            try
            {
                if (patcher == null || patcher.HasExited)
                {
                    timer.Stop();
                    if (overlay != null) overlay.Close();
                    ExitThread();
                    return;
                }
                patcher.Refresh();
                IntPtr current = patcher.MainWindowHandle;
                if (current == IntPtr.Zero)
                {
                    if (!attached && DateTime.UtcNow.Subtract(startedAt).TotalSeconds > 25)
                        throw new InvalidOperationException("The original patcher window was not detected within 25 seconds.");
                    return;
                }
                parentHandle = current;
                if (!attached)
                {
                    IntPtr overlayHandle = overlay.Handle;
                    NativeMethods.SetWindowLongPtr(overlayHandle, NativeMethods.GWLP_HWNDPARENT, parentHandle);
                    overlay.Show();
                    attached = true;
                    LauncherLog.Write("Native News panel attached to the patcher window.");
                }
                PositionOverlay();
            }
            catch (Exception ex)
            {
                LauncherLog.Write("Overlay error: " + ex);
                timer.Stop();
                if (overlay != null) overlay.Close();
                MessageBox.Show("ASP News Overlay stopped.\r\n\r\n" + ex.Message,
                    "ASP Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitThread();
            }
        }

        private void PositionOverlay()
        {
            if (NativeMethods.IsIconic(parentHandle) || !NativeMethods.IsWindowVisible(parentHandle))
            {
                if (overlay.Visible) overlay.Hide();
                parentWasHidden = true;
                return;
            }
            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(parentHandle, out client)) return;
            NativeMethods.POINT origin = new NativeMethods.POINT { X = 0, Y = 0 };
            if (!NativeMethods.ClientToScreen(parentHandle, ref origin)) return;
            double scaleX = Math.Max(0.1, (client.Right - client.Left) / (double)mapSize.Width);
            double scaleY = Math.Max(0.1, (client.Bottom - client.Top) / (double)mapSize.Height);
            Rectangle bounds = new Rectangle(
                origin.X + (int)Math.Round(mapRegion.X * scaleX) + 1,
                origin.Y + (int)Math.Round(mapRegion.Y * scaleY) + 1,
                Math.Max(20, (int)Math.Round(mapRegion.Width * scaleX) - 2),
                Math.Max(20, (int)Math.Round(mapRegion.Height * scaleY) - 2));
            if (diagnostic && (!hasOverlayBounds || bounds != lastOverlayBounds))
            {
                LauncherLog.Write(String.Format("Child bounds {0},{1},{2},{3} within the patcher client area.",
                    bounds.X, bounds.Y, bounds.Width, bounds.Height));
            }
            bool changed = !hasOverlayBounds || bounds != lastOverlayBounds;
            if (!overlay.Visible) overlay.Show();
            if (changed || parentWasHidden)
            {
                NativeMethods.SetWindowPos(overlay.Handle, NativeMethods.HWND_TOP, bounds.X, bounds.Y,
                    bounds.Width, bounds.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
                lastOverlayBounds = bounds;
                hasOverlayBounds = true;
            }
            if (parentWasHidden)
            {
                overlay.RecoverBrowserSurface();
                parentWasHidden = false;
            }
            else if (NativeMethods.GetForegroundWindow() == parentHandle ||
                     NativeMethods.GetForegroundWindow() == overlay.Handle)
            {
                NativeMethods.SetWindowPos(overlay.Handle, NativeMethods.HWND_TOP, 0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE |
                    NativeMethods.SWP_SHOWWINDOW);
            }
        }

        protected override void ExitThreadCore()
        {
            timer.Stop();
            timer.Dispose();
            if (overlay != null && !overlay.IsDisposed) overlay.Close();
            base.ExitThreadCore();
        }
    }

    internal sealed class NewsOverlayForm : Form
    {
        private static readonly Color PageBackground = Color.FromArgb(8, 13, 22);
        private static readonly Color HeaderBackground = Color.FromArgb(12, 20, 32);
        private static readonly Color CardBackground = Color.FromArgb(17, 26, 40);
        private static readonly Color FeaturedBackground = Color.FromArgb(22, 36, 58);
        private static readonly Color TextColor = Color.FromArgb(231, 237, 245);
        private static readonly Color MutedColor = Color.FromArgb(148, 163, 184);
        private static readonly Color AccentColor = Color.FromArgb(77, 142, 255);
        private static readonly Color GoldColor = Color.FromArgb(217, 180, 110);
        private static readonly Color LineColor = Color.FromArgb(37, 50, 70);
        private readonly FlowLayoutPanel newsList;
        private readonly Label message;
        private readonly string newsUrl;

        public NewsOverlayForm(string url)
        {
            newsUrl = url;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = PageBackground;

            Panel header = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = HeaderBackground };
            header.Controls.Add(new Label {
                Text = "PW155 NEWS", ForeColor = TextColor,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), AutoSize = true, Location = new Point(13, 9)
            });
            Label latest = new Label {
                Text = "Latest realm updates", ForeColor = GoldColor,
                Font = new Font("Segoe UI", 8F), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            latest.Location = new Point(330, 12);
            header.Controls.Add(latest);
            header.Resize += delegate { latest.Left = Math.Max(150, header.ClientSize.Width - latest.Width - 12); };

            newsList = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoScroll = true, BackColor = PageBackground, Padding = new Padding(8)
            };
            message = new Label {
                Text = "Loading news…", ForeColor = MutedColor,
                Font = new Font("Segoe UI", 9F), AutoSize = false, Height = 50,
                TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0)
            };
            newsList.Controls.Add(message);
            newsList.SizeChanged += delegate { ResizeNewsRows(); };
            Controls.Add(newsList);
            Controls.Add(header);
            Load += delegate { DownloadNews(); };
        }

        internal void RecoverBrowserSurface()
        {
            if (!IsHandleCreated || IsDisposed) return;
            Invalidate(true);
            Update();
        }

        private void DownloadNews()
        {
            Uri uri;
            if (!Uri.TryCreate(newsUrl, UriKind.Absolute, out uri)) { ShowMessage("News URL is invalid."); return; }
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    string html;
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = System.Text.Encoding.UTF8;
                        client.Headers[HttpRequestHeader.UserAgent] = "ASP-Launcher/0.4";
                        html = client.DownloadString(uri);
                    }
                    NewsItem[] items = ParseNews(html);
                    if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)(() => ShowNews(items)));
                }
                catch (Exception ex)
                {
                    LauncherLog.Write("News download error: " + ex.Message);
                    if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)(() => ShowMessage("Unable to load news. Check ASP-PWPANEL.")));
                }
            });
        }

        private static NewsItem[] ParseNews(string html)
        {
            MatchCollection matches = Regex.Matches(html,
                "<div class=\"news-item[^\"]*\">\\s*<span class=\"news-date\">(.*?)</span>\\s*<h2 class=\"news-title\">(.*?)</h2>\\s*<p class=\"news-body\">(.*?)</p>\\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            NewsItem[] items = new NewsItem[matches.Count];
            for (int i = 0; i < matches.Count; i++)
                items[i] = new NewsItem(Clean(matches[i].Groups[2].Value), Clean(matches[i].Groups[1].Value), Clean(matches[i].Groups[3].Value));
            return items;
        }

        private static string Clean(string value)
        {
            return WebUtility.HtmlDecode(Regex.Replace(value ?? "", "<[^>]+>", "")).Trim();
        }

        private void ShowNews(NewsItem[] items)
        {
            newsList.SuspendLayout();
            newsList.Controls.Clear();
            if (items.Length == 0) { ShowMessage("No published news yet."); newsList.ResumeLayout(); return; }
            for (int i = 0; i < items.Length; i++) newsList.Controls.Add(CreateNewsRow(items[i], i == 0));
            ResizeNewsRows();
            newsList.ResumeLayout();
        }

        private Control CreateNewsRow(NewsItem item, bool first)
        {
            Panel row = new Panel { Height = 62, Margin = new Padding(0, 0, 0, 1), BackColor = first ? FeaturedBackground : CardBackground };
            Label date = new Label { Text = item.Date, AutoSize = false, Width = 112, Height = 18,
                TextAlign = ContentAlignment.TopRight, ForeColor = GoldColor, Font = new Font("Segoe UI", 7.5F), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Label title = new Label { Text = item.Title, AutoSize = false, Height = 20,
                ForeColor = TextColor, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Location = new Point(10, 7), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            Label body = new Label { Text = item.Body, AutoSize = false, Height = 31,
                ForeColor = MutedColor, Font = new Font("Segoe UI", 8.5F), Location = new Point(10, 28), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            date.Location = new Point(330, 8);
            row.Controls.Add(date); row.Controls.Add(title); row.Controls.Add(body);
            row.Resize += delegate {
                date.Left = Math.Max(130, row.ClientSize.Width - date.Width - 8);
                title.Width = Math.Max(100, date.Left - title.Left - 8);
                body.Width = Math.Max(100, row.ClientSize.Width - body.Left - 8);
            };
            row.Paint += delegate(object sender, PaintEventArgs e) {
                using (Pen line = new Pen(LineColor)) e.Graphics.DrawLine(line, 0, row.Height - 1, row.Width, row.Height - 1);
                if (first) using (Pen accent = new Pen(AccentColor, 3)) e.Graphics.DrawLine(accent, 1, 0, 1, row.Height);
            };
            return row;
        }

        private void ResizeNewsRows()
        {
            int width = Math.Max(100, newsList.ClientSize.Width - newsList.Padding.Horizontal - (newsList.VerticalScroll.Visible ? 18 : 2));
            foreach (Control control in newsList.Controls) control.Width = width;
        }

        private void ShowMessage(string text)
        {
            if (IsDisposed) return;
            newsList.Controls.Clear();
            message.Text = text;
            newsList.Controls.Add(message);
            ResizeNewsRows();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        private sealed class NewsItem
        {
            internal readonly string Title, Date, Body;
            internal NewsItem(string title, string date, string body) { Title = title; Date = date; Body = body; }
        }
    }

    internal static class NativeMethods
    {
        internal const int GWLP_HWNDPARENT = -8;
        internal static readonly IntPtr HWND_TOP = IntPtr.Zero;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_SHOWWINDOW = 0x0040;
        internal const uint RDW_INVALIDATE = 0x0001;
        internal const uint RDW_ERASE = 0x0004;
        internal const uint RDW_ALLCHILDREN = 0x0080;
        internal const uint RDW_UPDATENOW = 0x0100;

        [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] internal static extern bool RedrawWindow(IntPtr hWnd, IntPtr updateRect, IntPtr updateRegion, uint flags);

        internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, index, value) : new IntPtr(SetWindowLong32(hWnd, index, value.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] private static extern int SetWindowLong32(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);
    }
}
