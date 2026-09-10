# Tutorial instalasi Ubuntu Server dan ASP CPW Manager

**Bahasa Indonesia** | [English](INSTALL-UBUNTU.en.md)

Panduan ini ditulis untuk pengguna yang belum pernah memasang server Ubuntu. Untuk server PW yang
sudah berjalan, langsung mulai dari bagian **Memasang ASP CPW Manager**.

## 1. Kebutuhan

- PC Windows 64-bit dengan VirtualBox.
- Ubuntu Server 22.04 LTS atau 24.04 LTS ISO.
- Minimal 4 core CPU, RAM 8 GB, dan disk virtual 80 GB. RAM 12–16 GB lebih nyaman bila banyak map aktif.
- Jaringan internet saat instalasi paket.
- Folder `ASP-CPW` ini berada pada PC Windows.
- Backup VM atau snapshot sebelum mengubah server yang sudah digunakan.

## 2. Membuat VM VirtualBox

1. Buka VirtualBox, pilih **New**.
2. Name: `PWKU-Ubuntu`; Type: **Linux**; Version: **Ubuntu (64-bit)**.
3. Pilih ISO Ubuntu Server. Bila ada **Unattended Installation**, centang **Skip Unattended Installation**
   agar pilihan instalasi sama dengan tutorial ini.
4. Atur minimal 8192 MB RAM dan 4 CPU. Jangan melewati area hijau VirtualBox.
5. Buat disk VDI dynamically allocated minimal 80 GB.
6. Pada **Settings > Network**, pilih salah satu:
   - **NAT** untuk komputer tunggal. Tambahkan Port Forwarding: host `2223` ke guest `22`.
   - **Bridged Adapter** bila VM perlu alamat IP sendiri di jaringan lokal.
7. Start VM.

## 3. Pilihan pada installer Ubuntu

1. Pilih bahasa **English** dan keyboard yang sesuai; `English (US)` aman bila ragu.
2. Pilih **Ubuntu Server**, bukan minimized, agar troubleshooting lebih mudah.
3. Network: gunakan DHCP untuk awal. Catat alamat IP yang tampil.
4. Proxy: kosongkan bila tidak memakai proxy.
5. Mirror: gunakan mirror otomatis yang berhasil diuji oleh installer.
6. Storage: pilih **Use an entire disk**. Pastikan disk yang dipilih adalah disk virtual VM, bukan disk fisik.
7. Profile setup:
   - Your name: bebas.
   - Server name: `pwku-server`.
   - Username: `pwadmin`.
   - Password: buat sandi kuat dan simpan di password manager. Paket ini tidak menyimpan atau mengetahui sandinya.
8. Ubuntu Pro: pilih **Skip for now**.
9. Pada SSH Setup, **centang Install OpenSSH server**. Jangan impor SSH key bila belum memakainya.
10. Featured server snaps: jangan pilih apa pun; lanjutkan instalasi.
11. Setelah selesai, pilih **Reboot Now**, lepaskan ISO bila diminta, lalu login sebagai `pwadmin`.

## 4. Tes koneksi dari Windows

Untuk NAT dengan port forwarding, buka PowerShell:

```powershell
ssh -p 2223 pwadmin@127.0.0.1
```

Untuk Bridged, ganti `127.0.0.1` dengan IP VM dan biasanya gunakan port `22`. Ketik `yes` saat fingerprint
pertama muncul, lalu masukkan password Ubuntu.

## 5. Memasang ASP CPW Manager

1. Pastikan VM hidup dan SSH dapat tersambung.
2. Di Windows buka folder `ASP-CPW`.
3. Klik dua kali **INSTALL-ASP-CPW.cmd**.
4. Isi alamat server, SSH port, dan **username Ubuntu**. Default proyek ini adalah `127.0.0.1`, `2223`,
   `pwadmin`. Jangan masukkan password pada kolom username.
5. Ketika muncul `pwadmin@127.0.0.1's password:`, ketik password Ubuntu lalu Enter. Karakter password
   memang tidak terlihat saat diketik. Setelah file diunggah, masukkan password yang sama untuk `sudo`.
6. Konfirmasi instalasi dengan Enter atau `Y`.
7. Tunggu hingga muncul `Installation complete`.

Installer aman dijalankan kembali untuk memperbaiki service atau integrasi web. Kunci RSA dan release yang sudah
ada tidak dibuat ulang; password akun database khusus CPW akan diperbarui bersama konfigurasi terlindungnya.

Installer akan memasang MariaDB client/server bila belum ada, membuat database khusus `cpw_patch`,
membuat kunci RSA, membuat baseline patch, memasang worker systemd, serta menghubungkan web PW155 bila
`/opt/pw155-web` ditemukan.

## 6. Verifikasi hasil

Jalankan melalui SSH:

```bash
sudo asp-cpw-control status
sudo systemctl status asp-cpw-control.path --no-pager
sudo systemctl status pw155-web --no-pager
```

Kemudian login ke web sebagai admin dan buka **Admin Panel > Patch Manager**. Status harus `READY` dan
release `baseline` harus terlihat.

## 7. Backup yang wajib

Salin file berikut ke media terenkripsi yang berbeda:

```text
/opt/asp-cpw/config/keys.json
/opt/asp-cpw/config/patcher.conf
/opt/asp-cpw/config/db.cnf
/srv/asp-cpw/backups/
```

`keys.json` berisi private key. Jangan masukkan file itu ke GitHub dan jangan kirim melalui chat publik.
