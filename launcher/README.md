# ASP Launcher

ASP Launcher is a lightweight companion for ASP CPW. It displays the configured
news page independently from the legacy Perfect World patcher's embedded browser.
It reads the client folder and website URLs saved by **ASP CPW Desktop**.

1. Configure the client folder and Launcher Links in ASP CPW Desktop.
2. Run `INSTALL-ASP-LAUNCHER-TO-CLIENT.cmd` once to integrate it into the client.
3. Afterwards, start `launcher/Launcher.exe` or `patcher/patcher.exe` normally.
4. ASP Launcher starts the original patcher and places ASP News over its blank browser area.
5. The News panel uses native Windows controls instead of the legacy embedded
   browser, preventing a white surface when the patcher is moved or restored.

The overlay reads `SkinBrowser` and its `MapColor` from `mainuni.xml`, then finds
that colored region in `main-map.png`. This makes its position follow the active
launcher skin instead of relying on fixed desktop coordinates.

The installer makes recoverable changes:

- The original `patcher.exe` is retained as `patcher-core.exe`.
- ASP Launcher is installed as both `patcher.exe` and `ASP-Launcher.exe`.
- An additional backup is created under `patcher/asp-launcher-backup/<date>/`.
- ASP Launcher requests Administrator permission through UAC automatically.

Run `restore-original-patcher.ps1` to restore the original patcher. If a CPW
patcher-channel update replaces `patcher.exe`, run the ASP Launcher installer again
after the update. Use `launcher/bin/ASP-Launcher.exe --manager` only for the standalone window.

The installer does not change game data; it only integrates the patcher executable entry point.
