# ASP CPW — Perfect World Client Patch Manager

**English** | [Bahasa Indonesia](README.id.md)

ASP CPW is a lightweight tool for creating, publishing, and managing **Perfect World game client updates**. It turns changed client files into versioned CPW revisions, publishes them to the Ubuntu patch server, and lets a compatible Perfect World launcher download the update.

It uses the MIT-licensed [`cpw_pw`](https://github.com/MrBIOSs/cpw_pw) engine and adds a Windows desktop workflow, Ubuntu installation, integrity verification, release history, recovery, rollback, and PW155 web-panel integration.

## What ASP CPW does

- Creates updates for files below the client `element`, `launcher`, and `patcher` folders.
- Generates the revision data and signed manifest required by a compatible PW launcher/patcher.
- Uploads changed files from Windows to the Ubuntu patch server over SSH.
- Keeps release history and verifies file checksums before a release becomes public.
- Provides staging inspection, safe recovery, verification, and rollback.
- Adds manual database backup in the Admin Panel, stored on the VM/VPS and downloadable to the administrator's PC.
- Prevents game payloads, passwords, private keys, and generated releases from being committed to Git.

ASP CPW does **not** install the Perfect World game server and does not automatically convert client files into their server-side equivalents. Files such as `gshopsev.data`, `npcgen.data`, and `domain.sev` must be deployed through the game-server deployment workflow.

## Requirements

- Windows 10 or 11 with the OpenSSH `ssh.exe` and `scp.exe` clients.
- An Ubuntu PW server reachable through SSH.
- The Ubuntu SSH address, port, and username.
- A public patch URL, for example `http://127.0.0.1:8081/patch/` for local testing.
- A compatible Perfect World Launcher and patcher.
- A separate copy of the game client for the first test.

The installer installs the required Ubuntu packages, including MariaDB, Python 3, OpenSSL, CA certificates, and curl.

## First-time setup

1. Download or clone this repository on Windows.
2. Start the Ubuntu VM/server and confirm that SSH is available.
3. Double-click [`ASP-CPW-DESKTOP.cmd`](ASP-CPW-DESKTOP.cmd).
4. Open **Settings** and enter:
   - Ubuntu address
   - SSH port
   - SSH username
   - Public patch URL
   - Game address and game port
5. Click **Save Settings**, then **Test SSH / Status**. Password input remains hidden and is never saved.
6. On the Dashboard, click **Install / Update Server**. Do this once for a new server, or again after this repository has been updated.
7. Back up `/opt/asp-cpw/config/keys.json` securely. The RSA private key must not be uploaded to GitHub.
8. Click **Prepare Client** and select a test copy of the Perfect World client. Do this once per client, or repeat it only after changing the patch URL, executable, or RSA key.
9. Start the prepared launcher and confirm that it can read the patch-server information.

For a manual installation and VirtualBox details, see [Ubuntu installation](docs/INSTALL-UBUNTU.en.md) and [client preparation](docs/CLIENT-SETUP.en.md).

## Creating an update

Use this workflow every time client files change:

1. Finish editing the required files in your Perfect World client.
2. Close the game, Launcher, patcher, and editors that may still be writing those files.
3. Open `ASP-CPW-DESKTOP.cmd` and select **Create Update**.
4. Choose the client folder.
5. Check only the files that changed, then click **Add Checked Files**. Use **Add Custom Client File** for another file located inside the client.
6. Open **Preview & Publish** and click **Refresh Preview**.
7. Review every client path and status. Do not continue if an unexpected file is listed.
8. Click **Publish Update**, type `PUBLISH` when requested, and enter the SSH/sudo passwords.
9. Click **Verify Release** after publication succeeds.
10. Run the Launcher on a test client and verify the update before distributing it to players.

The same update file should not be published twice. An identical file already present in the local `PUBLISHED` archive is marked as a duplicate.

## Important client/server file pairs

Some changes require both a client file and a compatible server file. For example, publish `element/data/gshop.data` to clients only after deploying and testing the matching `gshopsev.data` on the game server. A mismatched pair can make the boutique reject an item or can stop `gs01` during map loading.

ASP CPW distributes only the client side. Deploy and verify server-side files with the server deployment and rollback tools before releasing their client-side counterparts.

## If publication was interrupted

1. Open **Preview & Publish**.
2. Click **Server Staging**.
3. If staging shows exactly the files from the interrupted update, use **Publish Existing Staging (Recovery)**.
4. If staging is empty, use the normal **Publish Update** button instead.

Never use recovery without reviewing the remote staging paths first. More operational and rollback details are available in [Operations](docs/OPERATIONS.en.md), [Rollback](docs/ROLLBACK.en.md), and [Troubleshooting](docs/TROUBLESHOOTING.en.md).

## Main repository files

| Path | Purpose |
|---|---|
| `ASP-CPW-DESKTOP.cmd` | Opens the recommended Windows desktop manager |
| `INSTALL-ASP-CPW.cmd` | Manual one-click Windows-to-Ubuntu installer |
| `desktop/` | Desktop application source and versioned executable |
| `tools/client-setup/` | Manual client-preparation tool |
| `tools/patch-publisher/` | Manual preview and patch-publishing tools |
| `scripts/`, `systemd/` | Ubuntu release manager and worker |
| `web-integration/` | PW155 admin-panel Patch Manager integration |
| `vendor/cpw_pw/` | Pinned upstream CPW source and license |

## Security and repository safety

- Passwords are requested by SSH/sudo and are never stored by the desktop application.
- Do not commit Perfect World client/server files, generated patch payloads, backups, database credentials, or RSA private keys.
- Run `CHECK-BEFORE-GITHUB.cmd` before committing repository changes.
- Do not use `git add -f` to bypass the included safeguards.

After **Install / Update Server**, the Admin Panel provides a **Backup & download database** section. Confirm the action and click **Create backup now**, refresh the page, then click **Download** when the archive is ready. Server copies are retained for 14 days; keep important copies on the administrator's device.

The ASP wrapper and documentation are part of ASP Editor Studio. The bundled `cpw_pw` engine retains its upstream MIT license in [`LICENSES/cpw_pw-MIT.txt`](LICENSES/cpw_pw-MIT.txt).
