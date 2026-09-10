# Installing Ubuntu Server and ASP CPW Manager

[Bahasa Indonesia](INSTALL-UBUNTU.md) | **English**

This guide is intended for users who have never installed Ubuntu Server. If the PW server is already running,
continue directly to **Install ASP CPW Manager**.

## 1. Requirements

- A 64-bit Windows PC with VirtualBox.
- An Ubuntu Server 22.04 LTS or 24.04 LTS ISO.
- At least 4 CPU cores, 8 GB RAM, and an 80 GB virtual disk. Use 12–16 GB RAM when running many maps.
- Internet access while installing packages.
- This `ASP-CPW` folder on the Windows PC.
- A VM backup or snapshot before changing an existing server.

## 2. Create the VirtualBox VM

1. Open VirtualBox and select **New**.
2. Name: `PWKU-Ubuntu`; Type: **Linux**; Version: **Ubuntu (64-bit)**.
3. Select the Ubuntu Server ISO. Enable **Skip Unattended Installation**, when offered, so the screens match this guide.
4. Allocate at least 8192 MB RAM and 4 CPUs without exceeding VirtualBox's green area.
5. Create a dynamically allocated VDI disk of at least 80 GB.
6. Under **Settings > Network**, choose one mode:
   - **NAT** for a single PC. Forward host port `2223` to guest port `22`.
   - **Bridged Adapter** when the VM needs its own address on the local network.
7. Start the VM.

## 3. Ubuntu installer choices

1. Select **English** and the appropriate keyboard; `English (US)` is a safe default.
2. Select **Ubuntu Server**, not minimized, to make troubleshooting easier.
3. Initially use DHCP for networking and note the displayed IP address.
4. Leave Proxy empty unless your network requires one.
5. Use the mirror that passes the installer's test.
6. Choose **Use an entire disk** and verify that the selected disk is the VM's virtual disk.
7. Profile setup:
   - Your name: any name.
   - Server name: `pwku-server`.
   - Username: `pwadmin`.
   - Password: create a strong password and store it in a password manager. This package never stores it.
8. Select **Skip for now** for Ubuntu Pro.
9. Under SSH Setup, enable **Install OpenSSH server**. Do not import a key unless you already use one.
10. Do not select any featured server snaps.
11. Select **Reboot Now**, remove the ISO when requested, then log in as `pwadmin`.

## 4. Test the Windows connection

For NAT with the forwarding rule, open PowerShell:

```powershell
ssh -p 2223 pwadmin@127.0.0.1
```

For Bridged mode, replace `127.0.0.1` with the VM IP and normally use port `22`. Type `yes` for the first
fingerprint prompt, then enter the Ubuntu password. Password characters are intentionally hidden.

## 5. Install ASP CPW Manager

1. Confirm that the VM is running and SSH works.
2. Open the `ASP-CPW` folder on Windows.
3. Double-click **INSTALL-ASP-CPW.cmd**.
4. Enter the Ubuntu address, SSH port, and **Ubuntu username**. PWKU defaults are `127.0.0.1`, `2223`, and
   `pwadmin`. Never enter the password in the username field.
5. At `pwadmin@127.0.0.1's password:`, enter the Ubuntu password. Enter it again when `sudo` asks.
6. Confirm installation with Enter or `Y`.
7. Wait for `Installation complete`.

The installer can safely be run again to repair services or web integration. Existing RSA keys and releases are
preserved. It installs MariaDB when required, creates the dedicated `cpw_patch` database, creates the initial
RSA key and baseline, installs the systemd worker, and integrates `/opt/pw155-web` when found.

## 6. Verify the installation

Run over SSH:

```bash
sudo asp-cpw-control status
sudo systemctl status asp-cpw-control.path --no-pager
sudo systemctl status pw155-web --no-pager
```

Log in to the web admin account and open **Admin Panel > Patch Manager**. The state should be `READY` and the
`baseline` release should be visible.

## 7. Required backups

Copy the following to separate encrypted storage:

```text
/opt/asp-cpw/config/keys.json
/opt/asp-cpw/config/patcher.conf
/opt/asp-cpw/config/db.cnf
/srv/asp-cpw/backups/
```

`keys.json` contains the private key. Never upload it to GitHub or send it through public chat.
