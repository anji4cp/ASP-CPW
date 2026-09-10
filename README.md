# ASP CPW Manager

Lightweight Perfect World client patch management for the PWKU server. It wraps the MIT-licensed
[`cpw_pw`](https://github.com/MrBIOSs/cpw_pw) engine with safer release staging, integrity checks,
atomic publication, rollback, a systemd worker, and integration for the existing PW155 web panel.

## What is included

- Precompiled Linux x64 CPW binary; Dart is not required on the Ubuntu server.
- SHA-256 verification before installation.
- Dedicated MariaDB database named `cpw_patch` (never the game database `pw`).
- Staging folders for `element`, `launcher`, and `patcher` updates.
- MD5 payload and legacy RSA/MD5 manifest verification required by the stock PW launcher.
- Immutable release history and atomic `current` symlink.
- Publication rollback without rewriting CPW database history.
- Admin-only English Patch Manager page in the existing web application.
- Windows-to-Ubuntu one-click installer.
- Portable Windows client preparation tool with transactional backup.
- One-click Windows patch preview and publisher.

## Quick start

1. Read [Ubuntu installation](docs/INSTALL-UBUNTU.md).
2. On Windows, double-click `INSTALL-ASP-CPW.cmd`.
3. Securely back up `/opt/asp-cpw/config/keys.json` after installation.
4. Configure a **test copy** of the client using [Client setup](docs/CLIENT-SETUP.md).
5. Publish a small test file using [Operations](docs/OPERATIONS.md).

## Repository layout

| Folder | Purpose |
|---|---|
| `installer/` | Ubuntu installation scripts called by `INSTALL-ASP-CPW.cmd` |
| `scripts/`, `systemd/` | Release manager, worker, and protected service integration |
| `web-integration/` | Patch Manager integration for the PWKU admin panel |
| `tools/client-setup/` | Portable one-click preparation of a test client |
| `tools/patch-publisher/` | One-click preview and publication of changed client files |
| `vendor/cpw_pw/` | Pinned MIT-licensed upstream CPW source |
| `bin/linux-x64/` | Verified upstream Linux CPW executable |

The repository intentionally contains no Perfect World client/server data, private RSA keys,
database credentials, passwords, generated releases, or client backups.

## Windows tools

Prepare a test client by opening `tools/client-setup/PREPARE-ASP-CLIENT.cmd`. The tool asks for the
full client folder and stores backups below its own ignored `backups/` directory.

Create a patch by reading `tools/patch-publisher/CARA-PAKAI.md`, putting only changed client files
below `PATCH-FILES`, previewing them, and then running `PUBLISH-PATCH.cmd`. Patch payloads are ignored
by Git so copyrighted game files cannot be committed accidentally.

## Before uploading to GitHub

Run `CHECK-BEFORE-GITHUB.cmd`. It checks for machine-specific secrets, game payloads, generated
archives, backups, and unexpectedly large files. Then create the repository normally:

```powershell
git init
git add .
git status
git commit -m "Initial ASP CPW Manager release"
```

Review `git status` before every commit. Do not use `git add -f` to bypass the safeguards.

Do not overwrite the only copy of `Launcher.exe` or `patcher.exe`. The installer deliberately does
not patch or replace Windows client executables automatically.

## Main paths on Ubuntu

| Purpose | Path |
|---|---|
| Program and protected keys | `/opt/asp-cpw` |
| Staging input | `/srv/asp-cpw/staging` |
| CPW working data | `/srv/asp-cpw/work` |
| Published releases | `/srv/asp-cpw/releases` |
| Current public release | `/srv/asp-cpw/current` |
| Backups | `/srv/asp-cpw/backups` |
| Web queue/status | `/var/lib/asp-cpw-control` |

## Licensing and provenance

The wrapper and documentation are part of ASP Editor Studio. The vendored CPW engine remains under
its upstream MIT license, preserved in `LICENSES/cpw_pw-MIT.txt`. The vendored source is pinned from
upstream commit `9a673ac12cff2f79f611f1bcd36438db3efae3c9`; the bundled release binary checksum is recorded in
`bin/linux-x64/cpw.sha256.upstream`.

No license for the original ASP Editor Studio wrapper is granted merely by publishing the source.
Add a separate license only after choosing the distribution terms you want.
