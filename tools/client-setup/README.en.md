# ASP Perfect World Client Preparation

[Bahasa Indonesia](README.md) | **English**

This tool prepares a base client to trust an ASP CPW Manager installation on Ubuntu.

## What it does

- Uploads copies of `Launcher.exe` and `patcher.exe` to Ubuntu.
- Embeds the active RSA public key using the server's CPW executable.
- Downloads the results and verifies them against the active signed manifest.
- Creates a backup before committing any client changes.
- Sets the standalone update URL (default `http://127.0.0.1:8082/patch/`), PID `101`, game server address, and baseline channel versions.
- Uses the standalone ASP CPW patch URL, defaulting to `http://127.0.0.1:8082/patch/`.
- Removes obsolete `.sw` cache files only after validation succeeds.
- Writes `updateserver.txt` and `serverlist.txt` as UTF-16 LE, as required by the stock launcher.

## How to run it

1. Start the VM and confirm that ASP CPW is installed.
2. Close Launcher, patcher, elementclient, and UDE.
3. Double-click `PREPARE-ASP-CLIENT.cmd`.
4. Enter the full path to the test client folder.
5. Confirm the displayed path by typing `PREPARE`.
6. Press Enter to accept the default server values and enter the Ubuntu password when prompted.
7. Wait for `Client preparation completed successfully`.
8. Run `launcher/Launcher.exe` from the test client.

If the tool remains at `Uploading executable copies`, press `Ctrl+C`, open the VirtualBox console, run
`sudo systemctl restart ssh`, and retry. Network operations include timeouts to prevent indefinite waiting.

Backups are stored under `backups\DATE-TIME` beside this tool and are ignored by Git. Passwords are never stored.
