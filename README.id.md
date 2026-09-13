# ASP CPW — Pengelola Patch Client Perfect World

[English](README.md) | **Bahasa Indonesia**

ASP CPW adalah aplikasi ringan untuk membuat, menerbitkan, dan mengelola **update client game Perfect World**. File client yang berubah akan dibuat menjadi revisi CPW, diterbitkan ke patch server Ubuntu, lalu dapat diunduh oleh Launcher Perfect World yang kompatibel.

ASP CPW menggunakan engine [`cpw_pw`](https://github.com/MrBIOSs/cpw_pw) berlisensi MIT dan menambahkan aplikasi desktop Windows, installer Ubuntu, layanan patch mandiri read-only, pemeriksaan integritas, riwayat release, recovery, dan rollback.

## Fungsi ASP CPW

- Membuat update untuk file di dalam folder client `element`, `launcher`, dan `patcher`.
- Membuat data revisi dan manifest bertanda tangan yang dibutuhkan Launcher/patcher PW yang kompatibel.
- Mengunggah file yang berubah dari Windows ke patch server Ubuntu melalui SSH.
- Menyimpan riwayat release dan memeriksa checksum sebelum release dipublikasikan.
- Menyediakan pemeriksaan staging, recovery aman, verifikasi, dan rollback.
- Menyajikan file patch secara mandiri pada port TCP `8082`; ASP PWPanel tidak dibutuhkan.
- Mengatur URL Register, Arc/Website, Support, Forum, dan News tanpa hex edit.
- Menyediakan ASP Launcher yang menampilkan News tanpa bergantung pada browser patcher lama.
- Mencegah payload game, password, private key, dan release hasil generate masuk ke Git.

ASP CPW **bukan** installer server game Perfect World dan tidak mengubah file client menjadi pasangan file server secara otomatis. File seperti `gshopsev.data`, `npcgen.data`, dan `domain.sev` harus dipasang melalui alur deployment server game.

## Yang dibutuhkan

- Windows 10 atau 11 dengan OpenSSH client `ssh.exe` dan `scp.exe`.
- Server/VM Ubuntu PW yang dapat dihubungi melalui SSH.
- Alamat Ubuntu, port SSH, dan username SSH.
- URL patch publik, misalnya `http://127.0.0.1:8082/patch/` untuk pengujian lokal.
- Launcher dan patcher Perfect World yang kompatibel.
- Salinan client game khusus untuk pengujian pertama.

Installer akan memasang paket Ubuntu yang diperlukan, termasuk MariaDB, Python 3, OpenSSL, CA certificate, dan curl.

## Instalasi pertama

1. Download atau clone repositori ini di Windows.
2. Hidupkan VM/server Ubuntu dan pastikan SSH sudah aktif.
3. Klik dua kali [`ASP-CPW-DESKTOP.cmd`](ASP-CPW-DESKTOP.cmd).
4. Buka menu **Settings**, kemudian isi:
   - Ubuntu address
   - SSH port
   - SSH username
   - Public patch URL
   - Game address dan game port
5. Klik **Save Settings**, lalu **Test SSH / Status**. Password yang diketik tidak terlihat dan tidak pernah disimpan.
6. Pada Dashboard, klik **Install / Update Server**. Lakukan satu kali untuk server baru, atau jalankan lagi setelah repositori ASP CPW diperbarui.
7. Simpan backup `/opt/asp-cpw/config/keys.json` di tempat aman. RSA private key tidak boleh diunggah ke GitHub.
8. Klik **Prepare Client**, lalu pilih salinan client Perfect World untuk pengujian. Lakukan satu kali untuk setiap client. Ulangi hanya jika URL patch, executable, atau RSA key berubah.
9. Jalankan Launcher yang telah disiapkan dan pastikan informasi patch server dapat dibaca.

### Link website pada Launcher

Buka **Launcher Links** di ASP CPW Desktop, lalu masukkan URL lengkap yang
diawali `http://` atau `https://`. Gunakan endpoint `/launcher-news` dari ASP
PWPanel untuk **News panel URL**. Kolom lainnya mengatur tombol Register, Arc/Website, Support, dan
Forum. Pilih **Apply to Selected Client** untuk client lama yang sudah disiapkan,
atau jalankan **Prepare Client** agar link diterapkan bersama proses persiapan.
Untuk pemain yang sudah memiliki Launcher, publish file
`patcher/skin/mainuni.xml` melalui channel `patcher`.

Sebagian patcher PW lama yang ditandatangani berhenti menjalankan browser internal
setelah RSA key diubah. Jalankan [`INSTALL-ASP-LAUNCHER-TO-CLIENT.cmd`](INSTALL-ASP-LAUNCHER-TO-CLIENT.cmd)
satu kali. Installer menyimpan patcher asli sebagai `patcher-core.exe`, lalu memasang
ASP Launcher berhak Administrator sebagai `patcher.exe`. Dengan demikian, News tampil
baik melalui `Launcher.exe` bawaan maupun ketika `patcher.exe` diklik langsung.

Untuk instalasi manual dan pengaturan VirtualBox, baca [instalasi Ubuntu](docs/INSTALL-UBUNTU.md) dan [persiapan client](docs/CLIENT-SETUP.md).

## Cara membuat update

Lakukan urutan berikut setiap kali ada file client yang berubah:

1. Selesaikan perubahan file pada client Perfect World.
2. Tutup game, Launcher, patcher, dan editor yang mungkin masih menulis file tersebut.
3. Buka `ASP-CPW-DESKTOP.cmd`, lalu pilih **Create Update**.
4. Pilih folder client.
5. Centang hanya file yang berubah, lalu klik **Add Checked Files**. Gunakan **Add Custom Client File** untuk file lain yang masih berada di dalam folder client.
6. Buka **Preview & Publish**, lalu klik **Refresh Preview**.
7. Periksa seluruh path dan status file. Jangan lanjutkan jika ada file yang tidak seharusnya ikut.
8. Klik **Publish Update**, ketik `PUBLISH` ketika diminta, kemudian masukkan password SSH/sudo.
9. Setelah publikasi berhasil, klik **Verify Release**.
10. Jalankan Launcher pada client uji dan periksa hasil update sebelum dibagikan kepada pemain.

File update yang sama tidak perlu diterbitkan dua kali. File identik yang sudah ada di arsip lokal `PUBLISHED` akan ditandai sebagai duplikat.

## Pasangan file client dan server

Beberapa perubahan membutuhkan file client dan file server yang kompatibel. Contohnya, `element/data/gshop.data` hanya boleh diterbitkan ke client setelah pasangan `gshopsev.data` dipasang dan berhasil diuji pada server game. Pasangan yang tidak cocok dapat membuat Boutique menolak item atau menyebabkan `gs01` berhenti saat memuat map.

ASP CPW hanya mendistribusikan bagian client. Pasang dan periksa file sisi server menggunakan fitur deployment dan rollback server sebelum menerbitkan pasangan file client-nya.

## Jika proses publish terputus

1. Buka **Preview & Publish**.
2. Klik **Server Staging**.
3. Jika staging berisi persis file dari update yang terputus, gunakan **Publish Existing Staging (Recovery)**.
4. Jika staging kosong, gunakan tombol **Publish Update** seperti biasa.

Jangan menggunakan recovery sebelum memeriksa seluruh path pada staging server. Penjelasan lanjutan tersedia di [Operasi](docs/OPERATIONS.md), [Rollback](docs/ROLLBACK.md), dan [Pemecahan masalah](docs/TROUBLESHOOTING.md).

## Berkas utama repositori

| Lokasi | Fungsi |
|---|---|
| `ASP-CPW-DESKTOP.cmd` | Membuka aplikasi desktop Windows yang direkomendasikan |
| `ASP-LAUNCHER.cmd` | Membuka ASP Launcher ringan dengan panel News mandiri |
| `INSTALL-ASP-LAUNCHER-TO-CLIENT.cmd` | Mengintegrasikan ASP Launcher sebagai `patcher.exe` client secara aman |
| `RESTORE-ORIGINAL-PATCHER.cmd` | Memulihkan patcher asli pada client |
| `INSTALL-ASP-CPW.cmd` | Installer manual sekali klik dari Windows ke Ubuntu |
| `desktop/` | Source dan executable aplikasi desktop berdasarkan versi |
| `launcher/` | Source, panduan dua bahasa, dan executable ASP Launcher |
| `tools/client-setup/` | Tool manual untuk menyiapkan client |
| `tools/patch-publisher/` | Tool manual untuk preview dan menerbitkan patch |
| `scripts/`, `systemd/` | Release manager, worker, dan layanan patch mandiri Ubuntu |
| `vendor/cpw_pw/` | Source dan lisensi engine CPW upstream yang dipatok |

## Keamanan dan GitHub

- Password diminta langsung oleh SSH/sudo dan tidak disimpan oleh aplikasi desktop.
- Jangan commit file client/server Perfect World, payload patch, backup, kredensial database, atau RSA private key.
- Jalankan `CHECK-BEFORE-GITHUB.cmd` sebelum melakukan commit perubahan repositori.
- Jangan menggunakan `git add -f` untuk melewati perlindungan yang tersedia.

ASP CPW dan ASP PWPanel sengaja dibuat sebagai produk terpisah. Memasang atau memperbarui repositori ini tidak akan memasang, memperbarui, atau menghapus ASP PWPanel.

Wrapper ASP dan dokumentasinya adalah bagian dari ASP Editor Studio. Engine `cpw_pw` tetap menggunakan lisensi MIT upstream yang tersimpan di [`LICENSES/cpw_pw-MIT.txt`](LICENSES/cpw_pw-MIT.txt).
