# Preparing the client launcher

[Bahasa Indonesia](CLIENT-SETUP.md) | **English**

Use a dedicated test copy of the client. Do not modify the players' primary client directly.

## Patch address

The standalone ASP CPW service exposes `/patch/` on guest port `8082`. Example for VirtualBox NAT using the same host port:

```text
http://127.0.0.1:8082/patch/
```

Add a TCP NAT rule from host port `8082` to guest port `8082`. With a Bridged Adapter, use
`http://VM-IP:8082/patch/`. ASP PWPanel is not required to serve patches.

In the client, `patcher/server/updateserver.txt` must use the same format and UTF-16 LE encoding as the
original file. Always end the URL with `/`. The portable tool handles this automatically.

## PID and baseline versions

- Set `patcher/server/pid.ini` to PID `101` when using the standard CPW configuration.
- Keep each channel's `version.sw` and set its baseline version to `1`.
- Remove other cached `.sw` files only from the **test client copy**.

## Embedding the public key

`Launcher.exe` and `patcher.exe` must contain the public key paired with
`/opt/asp-cpw/config/keys.json`. Repeat this only when preparing a launcher build or intentionally rotating keys.

1. Back up both executables.
2. Upload copies to an Ubuntu work directory; never patch the only client copy.
3. Run:

```bash
sudo /opt/asp-cpw/cpw x /path/to/work/Launcher.exe
sudo /opt/asp-cpw/cpw x /path/to/work/patcher.exe
```

4. Download the patched copies into the test client.
5. Confirm that the launcher reads the baseline before publishing a large update.

`marker not found` or `serialized key exceeds placeholder` means the executable has no compatible CPW
placeholder. Do not force a manual hex replacement.

RSA-1024 with MD5withRSA is retained only for compatibility with the legacy PW launcher. Do not change the
algorithm without changing and testing the launcher code as well.

## Launcher website and news links

The **Launcher Links** tab in ASP CPW Desktop configures the embedded News
page and the Register, Arc/Website, Support, and Forum buttons. Settings are
stored locally without passwords and written to `patcher/skin/mainuni.xml`
while preserving its original UTF-16 LE format. A direct update creates a
backup first. Publish this file through the `patcher` channel for existing
player clients.
