using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AspCpwDesktop
{
    internal sealed class MainForm : Form
    {
        private static readonly string[] CommonClientFiles = {
            @"element\data\elements.data",
            @"element\data\tasks.data",
            @"element\data\gshop.data",
            @"element\interfaces.pck",
            @"element\surfaces.pck",
            @"element\configs.pck",
            @"launcher\Launcher.exe",
            @"patcher\patcher.exe",
            @"patcher\skin\mainuni.xml",
            @"patcher\server\updateserver.txt",
            @"patcher\server\serverlist.txt"
        };

        private readonly Color Navy = Color.FromArgb(22, 31, 48);
        private readonly Color Blue = Color.FromArgb(39, 112, 219);
        private readonly Color Green = Color.FromArgb(28, 155, 100);
        private readonly Color Orange = Color.FromArgb(218, 132, 35);
        private readonly string repoRoot;
        private readonly string publisherRoot;
        private readonly string patchRoot;
        private readonly string publishedRoot;
        private AppSettings settings;

        private Label installStatus;
        private Label prepareStatus;
        private TextBox clientPathBox;
        private CheckedListBox commonFiles;
        private ListView queueList;
        private Label queueSummary;
        private TextBox serverBox;
        private TextBox portBox;
        private TextBox userBox;
        private TextBox urlBox;
        private TextBox gameAddressBox;
        private TextBox gamePortBox;
        private TextBox newsUrlBox;
        private TextBox registerUrlBox;
        private TextBox homeUrlBox;
        private TextBox supportUrlBox;
        private TextBox forumUrlBox;
        private ListBox activityLog;
        private TabControl mainTabs;

        public MainForm()
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            repoRoot = Path.GetFullPath(Path.Combine(executableDirectory, "..", ".."));
            if (!File.Exists(Path.Combine(repoRoot, "INSTALL-ASP-CPW.cmd")))
                repoRoot = Environment.CurrentDirectory;
            publisherRoot = Path.Combine(repoRoot, "tools", "patch-publisher");
            patchRoot = Path.Combine(publisherRoot, "PATCH-FILES");
            publishedRoot = Path.Combine(publisherRoot, "PUBLISHED");
            settings = AppSettings.Load();

            Text = "ASP CPW Desktop Manager 0.5.1";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 680);
            Size = new Size(1120, 760);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(243, 246, 250);
            BuildInterface();
            LoadSettingsIntoControls();
            RefreshDashboard();
            RefreshQueue();
            Log("Application ready. Passwords are never stored.");
        }

        private void BuildInterface()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Navy };
            Label title = new Label {
                Text = "ASP CPW Desktop Manager", ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 20F), AutoSize = true, Location = new Point(24, 12)
            };
            Label subtitle = new Label {
                Text = "Safe client patch workflow • Alur patch client yang aman", ForeColor = Color.FromArgb(184, 196, 214),
                AutoSize = true, Location = new Point(27, 49)
            };
            header.Controls.Add(title); header.Controls.Add(subtitle);
            Controls.Add(header);

            mainTabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 7) };
            mainTabs.TabPages.Add(BuildDashboardTab());
            mainTabs.TabPages.Add(BuildUpdateTab());
            mainTabs.TabPages.Add(BuildPublishTab());
            mainTabs.TabPages.Add(BuildSettingsTab());
            mainTabs.TabPages.Add(BuildLauncherLinksTab());
            mainTabs.TabPages.Add(BuildHelpTab());
            Controls.Add(mainTabs);
            mainTabs.BringToFront();
        }

        private TabPage NewTab(string name)
        {
            return new TabPage(name) { BackColor = Color.FromArgb(243, 246, 250), Padding = new Padding(18) };
        }

        private TabPage BuildDashboardTab()
        {
            TabPage page = NewTab("Dashboard");
            Label heading = Heading("Start here / Mulai dari sini", 16, 14);
            page.Controls.Add(heading);

            Panel installCard = Card(18, 58, 500, 220);
            installCard.Controls.Add(SectionTitle("ONE-TIME SETUP", Orange, 18, 16));
            installCard.Controls.Add(WrappingTextLabel("1. Install ASP CPW on Ubuntu", 18, 51, 450));
            installCard.Controls.Add(WrappingTextLabel("Run once, or after updating this repository.", 18, 82, 450, Color.DimGray));
            Button install = ActionButton("Install / Update Server", 18, 132, 210, Blue);
            install.Click += delegate { StartServerInstallation(); };
            installCard.Controls.Add(install);
            installStatus = StatusLabel(248, 142);
            installCard.Controls.Add(installStatus);
            page.Controls.Add(installCard);

            Panel prepareCard = Card(536, 58, 500, 220);
            prepareCard.Controls.Add(SectionTitle("ONE-TIME PER CLIENT", Orange, 18, 16));
            prepareCard.Controls.Add(WrappingTextLabel("2. Prepare Launcher and Client", 18, 51, 450));
            prepareCard.Controls.Add(WrappingTextLabel("Repeat only for a new client, URL, executable, or RSA key.", 18, 82, 450, Color.DimGray));
            Button prepare = ActionButton("Prepare Client", 18, 132, 210, Blue);
            prepare.Click += delegate { StartClientPreparation(); };
            prepareCard.Controls.Add(prepare);
            prepareStatus = StatusLabel(248, 142);
            prepareCard.Controls.Add(prepareStatus);
            page.Controls.Add(prepareCard);

            Panel repeatCard = Card(18, 298, 1018, 225);
            repeatCard.Controls.Add(SectionTitle("EVERY UPDATE / SETIAP UPDATE", Green, 18, 16));
            repeatCard.Controls.Add(WrappingTextLabel("1. Select changed files  →  2. Preview  →  3. Publish  →  4. Verify and test", 18, 53, 970));
            repeatCard.Controls.Add(WrappingTextLabel("Do not reinstall the server and do not prepare the same client for ordinary data updates.", 18, 86, 970, Color.DimGray));
            Button create = ActionButton("Create Update", 18, 145, 190, Green);
            create.Click += delegate { ((TabControl)page.Parent).SelectedIndex = 1; };
            Button publish = ActionButton("Preview & Publish", 222, 145, 190, Blue);
            publish.Click += delegate { ((TabControl)page.Parent).SelectedIndex = 2; };
            repeatCard.Controls.Add(create); repeatCard.Controls.Add(publish);
            page.Controls.Add(repeatCard);

            Label note = WrappingTextLabel("Important: CPW distributes client files only. Server files such as gshopsev.data, npcgen.data and domain.sev belong to server deployment.", 22, 545, 980, Color.FromArgb(165, 65, 35));
            note.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(note);
            return page;
        }

        private TabPage BuildUpdateTab()
        {
            TabPage page = NewTab("Create Update (Repeat)");
            page.Controls.Add(Heading("Create a client update", 16, 12));
            page.Controls.Add(TextLabel("Choose the game client, then take only files that changed.", 17, 43, 700, Color.DimGray));

            clientPathBox = new TextBox { Location = new Point(18, 78), Width = 790, ReadOnly = true };
            Button browse = ActionButton("Choose Client Folder", 824, 75, 180, Blue);
            browse.Click += ChooseClientFolder;
            page.Controls.Add(clientPathBox); page.Controls.Add(browse);

            commonFiles = new CheckedListBox {
                Location = new Point(18, 122), Size = new Size(500, 340),
                CheckOnClick = true, IntegralHeight = false, BackColor = Color.White
            };
            foreach (string file in CommonClientFiles) commonFiles.Items.Add(file);
            page.Controls.Add(commonFiles);

            Label guide = SectionTitle("Common client files", Navy, 540, 126);
            page.Controls.Add(guide);
            Label fileGuide = new Label {
                Text = "elements.data   • Items, NPCs, monsters and definitions\r\n\r\n" +
                       "tasks.data        • Quest data\r\n\r\n" +
                       "gshop.data       • Boutique client catalog\r\n\r\n" +
                       "interfaces.pck  • Game UI and interface resources\r\n\r\n" +
                       "configs.pck      • Client configuration package",
                Location = new Point(540, 160), Size = new Size(450, 180),
                AutoSize = false, ForeColor = Navy, BackColor = Color.Transparent
            };
            page.Controls.Add(fileGuide);
            Label warning = TextLabel("For gshop.data, deploy and verify the compatible gshopsev.data on the game server before publishing the client update.", 540, 360, 450, Color.FromArgb(165, 65, 35));
            warning.Height = 70;
            page.Controls.Add(warning);

            Button add = ActionButton("Add Checked Files", 18, 480, 200, Green);
            add.Click += AddCheckedFiles;
            Button custom = ActionButton("Add Custom Client File", 232, 480, 210, Blue);
            custom.Click += AddCustomFile;
            Button open = ActionButton("Open PATCH-FILES", 456, 480, 190, Navy);
            open.Click += delegate { OpenFolder(patchRoot); };
            page.Controls.Add(add); page.Controls.Add(custom); page.Controls.Add(open);
            return page;
        }

        private TabPage BuildPublishTab()
        {
            TabPage page = NewTab("Preview & Publish (Repeat)");
            page.Controls.Add(Heading("Review before publishing", 16, 12));
            page.Controls.Add(TextLabel("The Publish button remains a deliberate final action. A password is requested in the console and is never saved.", 17, 43, 900, Color.DimGray));

            queueList = new ListView {
                Location = new Point(18, 78), Size = new Size(1000, 360), View = View.Details,
                FullRowSelect = true, GridLines = true, HideSelection = false
            };
            queueList.Columns.Add("Channel", 90);
            queueList.Columns.Add("Client path", 455);
            queueList.Columns.Add("Size", 110);
            queueList.Columns.Add("Status", 300);
            page.Controls.Add(queueList);
            queueSummary = TextLabel("", 18, 447, 900);
            page.Controls.Add(queueSummary);

            Button refresh = ActionButton("Refresh Preview", 18, 485, 165, Navy);
            refresh.Click += delegate { RefreshQueue(); };
            Button remove = ActionButton("Remove Selected", 196, 485, 165, Color.FromArgb(172, 60, 60));
            remove.Click += RemoveSelectedQueueItem;
            Button serverPreview = ActionButton("Server Staging", 374, 485, 165, Orange);
            serverPreview.Click += delegate { RunServerCommand("sudo asp-cpw-control preview", "Server staging preview"); };
            Button publish = ActionButton("Publish Update", 552, 485, 190, Green);
            publish.Click += PublishUpdate;
            Button verify = ActionButton("Verify Release", 755, 485, 165, Blue);
            verify.Click += delegate { RunServerCommand("sudo asp-cpw-control verify", "Release verification"); };
            page.Controls.Add(refresh); page.Controls.Add(remove); page.Controls.Add(serverPreview); page.Controls.Add(publish); page.Controls.Add(verify);

            Button publishExisting = ActionButton("Publish Existing Staging (Recovery)", 18, 535, 250, Orange);
            publishExisting.Click += PublishExistingStaging;
            page.Controls.Add(publishExisting);
            page.Controls.Add(TextLabel("Use only when Server Staging is not empty and shows exactly the expected paths.", 288, 543, 700, Color.FromArgb(165, 65, 35)));
            return page;
        }

        private TabPage BuildSettingsTab()
        {
            TabPage page = NewTab("Settings");
            page.Controls.Add(Heading("Connection settings", 16, 12));
            page.Controls.Add(TextLabel("Saved locally without passwords. Existing command tools may still ask for these values for confirmation.", 17, 43, 850, Color.DimGray));
            int y = 90;
            serverBox = SettingsField(page, "Ubuntu address", settings.Server, y); y += 52;
            portBox = SettingsField(page, "SSH port", settings.Port, y); y += 52;
            userBox = SettingsField(page, "SSH username", settings.User, y); y += 52;
            urlBox = SettingsField(page, "Public patch URL", settings.PatchUrl, y); y += 65;
            gameAddressBox = SettingsField(page, "Game address", settings.GameAddress, y); y += 52;
            gamePortBox = SettingsField(page, "Game port", settings.GamePort, y); y += 65;
            Button save = ActionButton("Save Settings", 194, y, 170, Green);
            save.Click += SaveSettings;
            Button test = ActionButton("Test SSH / Status", 380, y, 190, Blue);
            test.Click += delegate { if (SaveSettingsInternal()) RunServerCommand("sudo asp-cpw-control status", "Server status"); };
            page.Controls.Add(save); page.Controls.Add(test);
            page.Controls.Add(TextLabel("Security: the Ubuntu password, sudo password, database password and RSA private key are never stored by this application.", 194, y + 58, 780, Color.FromArgb(42, 88, 130)));
            return page;
        }

        private TabPage BuildHelpTab()
        {
            TabPage page = NewTab("Help & Logs");
            page.Controls.Add(Heading("Quick guide and activity", 16, 12));
            TextBox guide = new TextBox {
                Location = new Point(18, 55), Size = new Size(490, 450), Multiline = true,
                ReadOnly = true, BackColor = Color.White, ScrollBars = ScrollBars.Vertical,
                Text = "ONE TIME / SEKALI SAJA\r\n1. Install or update ASP CPW Server.\r\n2. Prepare each game client.\r\n\r\nEVERY UPDATE / SETIAP UPDATE\r\n1. Select only changed client files.\r\n2. Refresh and review Preview.\r\n3. Publish once.\r\n4. Verify the release.\r\n5. Test using a test client.\r\n\r\nDUPLICATE UPDATE\r\nAn identical locally archived file is flagged. Do not publish it again. If server staging is occupied after a failure, inspect Server Staging and publish that existing staging after confirming its paths.\r\n\r\nROLLBACK NOTE\r\nRollback changes the public server release. It cannot automatically downgrade clients that already installed a newer revision; publish a corrective higher revision for those clients."
            };
            page.Controls.Add(guide);
            activityLog = new ListBox { Location = new Point(526, 55), Size = new Size(492, 450) };
            page.Controls.Add(activityLog);
            Button docsId = ActionButton("Open Indonesian Guide", 18, 525, 210, Navy);
            docsId.Click += delegate { OpenFile(Path.Combine(repoRoot, "README.id.md")); };
            Button docsEn = ActionButton("Open English Guide", 242, 525, 190, Navy);
            docsEn.Click += delegate { OpenFile(Path.Combine(repoRoot, "README.md")); };
            Button patchManager = ActionButton("Open Patch Service", 526, 525, 210, Blue);
            patchManager.Click += delegate { OpenUrl(settings.PatchUrl); };
            page.Controls.Add(docsId); page.Controls.Add(docsEn); page.Controls.Add(patchManager);
            return page;
        }

        private TabPage BuildLauncherLinksTab()
        {
            TabPage page = NewTab("Launcher Links");
            page.Controls.Add(Heading("Launcher website links", 16, 12));
            page.Controls.Add(TextLabel("These URLs are written to patcher\\skin\\mainuni.xml. News appears in the launcher's center panel.", 17, 43, 940, Color.DimGray));
            int y = 90;
            newsUrlBox = SettingsField(page, "News panel URL", settings.NewsUrl, y); y += 58;
            registerUrlBox = SettingsField(page, "Register URL", settings.RegisterUrl, y); y += 58;
            homeUrlBox = SettingsField(page, "Arc / Website URL", settings.HomeUrl, y); y += 58;
            supportUrlBox = SettingsField(page, "Support URL", settings.SupportUrl, y); y += 58;
            forumUrlBox = SettingsField(page, "Forum URL", settings.ForumUrl, y); y += 72;
            Button save = ActionButton("Save Link Settings", 194, y, 190, Green);
            save.Click += SaveSettings;
            Button apply = ActionButton("Apply to Selected Client", 400, y, 225, Blue);
            apply.Click += delegate { ApplyLauncherLinks(); };
            page.Controls.Add(save); page.Controls.Add(apply);
            page.Controls.Add(TextLabel("For existing players, add patcher\\skin\\mainuni.xml to Create Update and publish it through the patcher channel.", 194, y + 55, 760, Color.FromArgb(165, 65, 35)));
            return page;
        }

        private void LoadSettingsIntoControls()
        {
            clientPathBox.Text = settings.ClientPath;
            serverBox.Text = settings.Server;
            portBox.Text = settings.Port;
            userBox.Text = settings.User;
            urlBox.Text = settings.PatchUrl;
            gameAddressBox.Text = settings.GameAddress;
            gamePortBox.Text = settings.GamePort;
            newsUrlBox.Text = settings.NewsUrl;
            registerUrlBox.Text = settings.RegisterUrl;
            homeUrlBox.Text = settings.HomeUrl;
            supportUrlBox.Text = settings.SupportUrl;
            forumUrlBox.Text = settings.ForumUrl;
        }

        private void RefreshDashboard()
        {
            installStatus.Text = settings.ServerInstalled ? "✓ Completed" : "○ Not marked";
            installStatus.ForeColor = settings.ServerInstalled ? Green : Color.DimGray;
            prepareStatus.Text = settings.ClientPrepared ? "✓ Completed" : "○ Not marked";
            prepareStatus.ForeColor = settings.ClientPrepared ? Green : Color.DimGray;
        }

        private void ChooseClientFolder(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the Perfect World client root containing element, launcher and patcher folders.";
                dialog.SelectedPath = Directory.Exists(settings.ClientPath) ? settings.ClientPath : repoRoot;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!Directory.Exists(Path.Combine(dialog.SelectedPath, "element")))
                {
                    MessageBox.Show(this, "The selected folder does not contain an element folder.", "Invalid client folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                settings.ClientPath = dialog.SelectedPath;
                settings.Save();
                clientPathBox.Text = settings.ClientPath;
                Log("Client selected: " + settings.ClientPath);
            }
        }

        private void AddCheckedFiles(object sender, EventArgs e)
        {
            if (!EnsureClientSelected()) return;
            if (commonFiles.CheckedItems.Count == 0)
            {
                MessageBox.Show(this, "Check at least one changed file.", "No files selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int added = 0;
            foreach (object selected in commonFiles.CheckedItems)
                if (QueueClientRelativeFile(selected.ToString())) added++;
            RefreshQueue();
            Log(String.Format("Added {0} selected file(s) to the local patch queue.", added));
        }

        private void AddCustomFile(object sender, EventArgs e)
        {
            if (!EnsureClientSelected()) return;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = settings.ClientPath;
                dialog.Title = "Select a file inside element, launcher, or patcher";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string relative = MakeRelativePath(settings.ClientPath, dialog.FileName);
                if (relative.StartsWith(".." + Path.DirectorySeparatorChar) || relative == "..")
                {
                    MessageBox.Show(this, "Custom files must be located inside the selected client folder.", "Unsafe path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!(relative.StartsWith("element" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                      relative.StartsWith("launcher" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                      relative.StartsWith("patcher" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(this, "The file must be inside the element, launcher, or patcher folder.", "Unknown CPW channel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                QueueClientRelativeFile(relative);
                RefreshQueue();
            }
        }

        private bool QueueClientRelativeFile(string clientRelative)
        {
            string normalized = clientRelative.Replace('/', Path.DirectorySeparatorChar);
            string[] parts = normalized.Split(Path.DirectorySeparatorChar);
            if (parts.Length < 2) return false;
            string channel = parts[0].ToLowerInvariant();
            if (channel != "element" && channel != "launcher" && channel != "patcher") return false;
            string channelRelative = String.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(1).ToArray());
            string source = Path.Combine(settings.ClientPath, normalized);
            if (!File.Exists(source))
            {
                MessageBox.Show(this, "File not found:\r\n" + source, "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            string destination = Path.Combine(patchRoot, channel, channelRelative);
            string archived = FindLatestArchivedFile(channel, channelRelative);
            if (archived != null && SameHash(source, archived))
            {
                DialogResult result = MessageBox.Show(this,
                    "This exact file is already present in local published history:\r\n\r\n" + clientRelative +
                    "\r\n\r\nPublishing it again normally creates an unnecessary revision. Queue anyway?",
                    "Duplicate update detected", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes) return false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (File.Exists(destination) && SameHash(source, destination))
            {
                MessageBox.Show(this, "This file is already queued with identical content.", "Already queued", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (File.Exists(destination))
            {
                DialogResult replace = MessageBox.Show(this, "Replace the queued copy of:\r\n" + clientRelative + "?", "Replace queued file", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (replace != DialogResult.Yes) return false;
            }
            File.Copy(source, destination, true);
            return true;
        }

        private void RefreshQueue()
        {
            if (queueList == null) return;
            queueList.Items.Clear();
            long total = 0;
            int duplicates = 0;
            if (Directory.Exists(patchRoot))
            {
                foreach (string file in Directory.GetFiles(patchRoot, "*", SearchOption.AllDirectories).OrderBy(p => p))
                {
                    if (String.Equals(Path.GetFileName(file), "PLACE-FILES-HERE.txt", StringComparison.OrdinalIgnoreCase)) continue;
                    string relative = MakeRelativePath(patchRoot, file);
                    string[] parts = relative.Split(Path.DirectorySeparatorChar);
                    string channel = parts[0];
                    string channelRelative = String.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(1).ToArray());
                    FileInfo info = new FileInfo(file);
                    total += info.Length;
                    string archived = FindLatestArchivedFile(channel, channelRelative);
                    bool duplicate = archived != null && SameHash(file, archived);
                    if (duplicate) duplicates++;
                    ListViewItem item = new ListViewItem(channel);
                    item.SubItems.Add(relative.Replace(Path.DirectorySeparatorChar, '/'));
                    item.SubItems.Add(FormatBytes(info.Length));
                    item.SubItems.Add(duplicate ? "Already published locally — review" : "Ready");
                    item.Tag = file;
                    if (duplicate) item.ForeColor = Color.FromArgb(180, 90, 20);
                    queueList.Items.Add(item);
                }
            }
            queueSummary.Text = String.Format("{0} file(s), {1}{2}", queueList.Items.Count, FormatBytes(total),
                duplicates > 0 ? "  •  " + duplicates + " possible duplicate(s)" : "");
        }

        private void RemoveSelectedQueueItem(object sender, EventArgs e)
        {
            if (queueList.SelectedItems.Count == 0) return;
            DialogResult answer = MessageBox.Show(this, "Remove the selected local queue file(s)? Source client files are not affected.", "Remove from queue", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            foreach (ListViewItem item in queueList.SelectedItems)
            {
                string path = item.Tag as string;
                if (path != null && File.Exists(path)) File.Delete(path);
            }
            RefreshQueue();
        }

        private void PublishUpdate(object sender, EventArgs e)
        {
            RefreshQueue();
            if (queueList.Items.Count == 0)
            {
                MessageBox.Show(this, "The local patch queue is empty.", "Nothing to publish", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool hasDuplicate = queueList.Items.Cast<ListViewItem>().Any(i => i.SubItems[3].Text.StartsWith("Already"));
            if (hasDuplicate)
            {
                MessageBox.Show(this, "At least one queued file is identical to local published history. Remove or replace it before publishing.", "Duplicate publish blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult answer = MessageBox.Show(this,
                "Publish exactly the files shown in Preview?\r\n\r\nThe console will ask for the Ubuntu password. Do not publish the same revision twice.",
                "Confirm publication", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;
            StartPatchPublication();
        }

        private void PublishExistingStaging(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(this,
                "Have you clicked Server Staging and confirmed that every remote path is expected?\r\n\r\nThis publishes files already uploaded to Ubuntu; it does not upload the local queue.",
                "Publish existing server staging", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;
            RunServerCommand("sudo asp-cpw-control publish --actor pwadmin", "Existing staging publication");
        }

        private void StartServerInstallation()
        {
            if (!RequireConnectionSettings()) return;
            string script = Path.Combine(repoRoot, "installer", "install-from-windows.ps1");
            string arguments = "-Server " + Quote(settings.Server) + " -Port " + Quote(settings.Port) + " -User " + Quote(settings.User);
            RunPowerShellScript(script, arguments, "Server installation", true);
        }

        private void StartClientPreparation()
        {
            if (!EnsureClientSelected() || !RequireConnectionSettings()) return;
            string script = Path.Combine(repoRoot, "tools", "client-setup", "prepare-client.ps1");
            string arguments = "-ClientPath " + Quote(settings.ClientPath) +
                " -Server " + Quote(settings.Server) + " -Port " + Quote(settings.Port) +
                " -User " + Quote(settings.User) + " -PatchUrl " + Quote(settings.PatchUrl) +
                " -GameAddress " + Quote(settings.GameAddress) + " -GamePort " + Quote(settings.GamePort) +
                " -NewsUrl " + Quote(settings.NewsUrl) + " -RegisterUrl " + Quote(settings.RegisterUrl) +
                " -HomeUrl " + Quote(settings.HomeUrl) + " -SupportUrl " + Quote(settings.SupportUrl) +
                " -ForumUrl " + Quote(settings.ForumUrl);
            RunPowerShellScript(script, arguments, "Client preparation", false);
        }

        private void ApplyLauncherLinks()
        {
            if (!EnsureClientSelected() || !SaveSettingsInternal()) return;
            string mainUni = Path.Combine(settings.ClientPath, "patcher", "skin", "mainuni.xml");
            if (!File.Exists(mainUni))
            {
                MessageBox.Show(this, "Launcher skin file was not found:\r\n" + mainUni, "Missing mainuni.xml", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string script = Path.Combine(repoRoot, "tools", "client-setup", "set-launcher-links.ps1");
            string arguments = "-MainUniPath " + Quote(mainUni) +
                " -NewsUrl " + Quote(settings.NewsUrl) + " -RegisterUrl " + Quote(settings.RegisterUrl) +
                " -HomeUrl " + Quote(settings.HomeUrl) + " -SupportUrl " + Quote(settings.SupportUrl) +
                " -ForumUrl " + Quote(settings.ForumUrl);
            RunPowerShellScript(script, arguments, "Launcher link update", false);
        }

        private void StartPatchPublication()
        {
            if (!RequireConnectionSettings()) return;
            string script = Path.Combine(publisherRoot, "publisher", "publish-patch.ps1");
            string arguments = "-Server " + Quote(settings.Server) + " -Port " + Quote(settings.Port) + " -User " + Quote(settings.User);
            RunPowerShellScript(script, arguments, "Patch publication", false);
        }

        private void RunPowerShellScript(string script, string arguments, string description, bool marksServerInstalled)
        {
            if (!File.Exists(script))
            {
                MessageBox.Show(this, "Tool not found:\r\n" + script, "Missing tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Log("Started: " + description);
            ProcessStartInfo info = new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -File " + Quote(script) + " " + arguments);
            info.WorkingDirectory = Path.GetDirectoryName(script);
            info.UseShellExecute = false;
            info.CreateNoWindow = false;
            StartTrackedProcess(info, description, marksServerInstalled);
        }

        private void StartTrackedProcess(ProcessStartInfo info, string description, bool marksServerInstalled)
        {
            try
            {
                Process process = Process.Start(info);
                process.EnableRaisingEvents = true;
                process.Exited += delegate
                {
                    int exitCode = process.ExitCode;
                    BeginInvoke((MethodInvoker)delegate
                    {
                        Log(description + " finished with exit code " + exitCode + ".");
                        if (exitCode == 0)
                        {
                            if (marksServerInstalled) settings.ServerInstalled = true;
                            else if (description == "Client preparation") settings.ClientPrepared = true;
                            settings.Save();
                            RefreshDashboard();
                            RefreshQueue();
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to start tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool RequireConnectionSettings()
        {
            if (SaveSettingsInternal()) return true;
            mainTabs.SelectedIndex = 3;
            MessageBox.Show(this, "Complete and save SSH settings before continuing. Only the password will be requested in the console.", "SSH settings required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\\\"") + "\"";
        }

        private void RunServerCommand(string command, string description)
        {
            if (!RequireConnectionSettings()) return;
            if (!Regex.IsMatch(settings.Server, "^[A-Za-z0-9.-]+$") ||
                !Regex.IsMatch(settings.Port, "^[0-9]{1,5}$") ||
                !Regex.IsMatch(settings.User, "^[a-z_][a-z0-9_-]{0,31}$"))
            {
                MessageBox.Show(this, "Server, port, or SSH username is invalid.", "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string ssh = String.Format("ssh -t -p {0} {1}@{2} \"{3}; status=$?; printf '\\nPress Enter to close...'; read -r; exit $status\"",
                settings.Port, settings.User, settings.Server, command.Replace("\"", ""));
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/c " + ssh) { UseShellExecute = true, WorkingDirectory = repoRoot };
            try { Process.Start(info); Log("Opened: " + description); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Unable to open SSH", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void SaveSettings(object sender, EventArgs e)
        {
            if (SaveSettingsInternal()) MessageBox.Show(this, "Settings saved. Passwords are not stored.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool SaveSettingsInternal()
        {
            string server = serverBox.Text.Trim();
            string port = portBox.Text.Trim();
            string user = userBox.Text.Trim();
            string url = urlBox.Text.Trim();
            string gameAddress = gameAddressBox.Text.Trim();
            string gamePort = gamePortBox.Text.Trim();
            string newsUrl = newsUrlBox.Text.Trim();
            string registerUrl = registerUrlBox.Text.Trim();
            string homeUrl = homeUrlBox.Text.Trim();
            string supportUrl = supportUrlBox.Text.Trim();
            string forumUrl = forumUrlBox.Text.Trim();
            int portNumber;
            int gamePortNumber;
            Uri uri;
            Uri newsUri, registerUri, homeUri, supportUri, forumUri;
            if (!Regex.IsMatch(server, "^[A-Za-z0-9.-]+$") || !Int32.TryParse(port, out portNumber) || portNumber < 1 || portNumber > 65535 ||
                !Regex.IsMatch(user, "^[a-z_][a-z0-9_-]{0,31}$") || !Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                !Regex.IsMatch(gameAddress, "^[A-Za-z0-9.-]+$") || !Int32.TryParse(gamePort, out gamePortNumber) || gamePortNumber < 1 || gamePortNumber > 65535 ||
                !WebUri(newsUrl, out newsUri) || !WebUri(registerUrl, out registerUri) || !WebUri(homeUrl, out homeUri) ||
                !WebUri(supportUrl, out supportUri) || !WebUri(forumUrl, out forumUri))
            {
                MessageBox.Show(this, "Check the connection values and all launcher URLs. URLs must begin with http:// or https://.", "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            settings.Server = server; settings.Port = port; settings.User = user; settings.PatchUrl = url;
            settings.GameAddress = gameAddress; settings.GamePort = gamePort;
            settings.NewsUrl = newsUrl; settings.RegisterUrl = registerUrl; settings.HomeUrl = homeUrl;
            settings.SupportUrl = supportUrl; settings.ForumUrl = forumUrl;
            settings.Save();
            Log("Connection settings saved.");
            return true;
        }

        private static bool WebUri(string value, out Uri uri)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private bool EnsureClientSelected()
        {
            if (!String.IsNullOrWhiteSpace(settings.ClientPath) && Directory.Exists(settings.ClientPath)) return true;
            MessageBox.Show(this, "Choose the Perfect World client folder first.", "Client required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private string FindLatestArchivedFile(string channel, string channelRelative)
        {
            if (!Directory.Exists(publishedRoot)) return null;
            string suffix = Path.Combine(channel, channelRelative);
            return Directory.GetFiles(publishedRoot, Path.GetFileName(channelRelative), SearchOption.AllDirectories)
                .Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => File.GetLastWriteTimeUtc(p)).FirstOrDefault();
        }

        private static bool SameHash(string first, string second)
        {
            FileInfo a = new FileInfo(first), b = new FileInfo(second);
            if (a.Length != b.Length) return false;
            using (SHA256 hash = SHA256.Create())
            {
                byte[] left;
                byte[] right;
                using (FileStream stream = File.OpenRead(first)) left = hash.ComputeHash(stream);
                using (FileStream stream = File.OpenRead(second)) right = hash.ComputeHash(stream);
                return left.SequenceEqual(right);
            }
        }

        private static string MakeRelativePath(string root, string path)
        {
            Uri rootUri = new Uri(AppendSeparator(Path.GetFullPath(root)));
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path : path + Path.DirectorySeparatorChar;
        }

        private void OpenFolder(string path) { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        private void OpenFile(string path) { if (File.Exists(path)) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        private void OpenUrl(string url) { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        private void Log(string message) { if (activityLog != null) activityLog.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + message); }

        private static string FormatBytes(long value)
        {
            if (value >= 1024L * 1024L * 1024L) return (value / (1024D * 1024D * 1024D)).ToString("0.##") + " GB";
            if (value >= 1024L * 1024L) return (value / (1024D * 1024D)).ToString("0.##") + " MB";
            if (value >= 1024L) return (value / 1024D).ToString("0.##") + " KB";
            return value + " bytes";
        }

        private Panel Card(int x, int y, int width, int height) { return new Panel { Location = new Point(x, y), Size = new Size(width, height), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; }
        private Label Heading(string text, int x, int y) { return new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Navy }; }
        private Label SectionTitle(string text, Color color, int x, int y) { return new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), ForeColor = color }; }
        private Label StatusLabel(int x, int y) { return new Label { Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F) }; }
        private Label TextLabel(string text, int x, int y, int width) { return TextLabel(text, x, y, width, Navy); }
        private Label TextLabel(string text, int x, int y, int width, Color color) { return new Label { Text = text, Location = new Point(x, y), Width = width, AutoSize = false, Height = 38, ForeColor = color }; }
        private Label WrappingTextLabel(string text, int x, int y, int maximumWidth) { return WrappingTextLabel(text, x, y, maximumWidth, Navy); }
        private Label WrappingTextLabel(string text, int x, int y, int maximumWidth, Color color)
        {
            return new Label {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                MaximumSize = new Size(maximumWidth, 0),
                ForeColor = color,
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
        }
        private Button ActionButton(string text, int x, int y, int width, Color color)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 38), FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand };
        }
        private TextBox SettingsField(Control page, string label, string value, int y)
        {
            page.Controls.Add(new Label { Text = label, Location = new Point(18, y + 4), Width = 160 });
            TextBox box = new TextBox { Text = value, Location = new Point(194, y), Width = 470 };
            page.Controls.Add(box); return box;
        }
    }
}
