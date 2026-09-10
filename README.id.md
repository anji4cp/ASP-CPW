# ASP CPW Manager

[English](README.md) | **Bahasa Indonesia**

Pengelola patch client Perfect World yang ringan untuk server PWKU. Aplikasi ini membungkus engine
[`cpw_pw`](https://github.com/MrBIOSs/cpw_pw) berlisensi MIT dengan staging release yang lebih aman,
pemeriksaan integritas, publikasi atomik, rollback, worker systemd, dan integrasi panel web PW155.

## Isi paket

- Binary CPW Linux x64 siap pakai; server Ubuntu tidak memerlukan Dart.
- Verifikasi SHA-256 sebelum instalasi.
- Database MariaDB khusus bernama `cpw_patch`, bukan database game `pw`.
- Staging terpisah untuk update `element`, `launcher`, dan `patcher`.
- Verifikasi MD5 payload dan manifest RSA/MD5 lama yang dibutuhkan launcher PW bawaan.
- Riwayat release immutable dan symlink `current` yang diganti secara atomik.
- Rollback publikasi tanpa menulis ulang riwayat database CPW.
- Halaman Patch Manager khusus admin pada aplikasi web.
- Installer sekali klik dari Windows ke Ubuntu.
- Tool persiapan client Windows portabel dengan backup transaksional.
- Preview dan publisher patch Windows sekali klik.

## Mulai cepat

1. Baca [instalasi Ubuntu](docs/INSTALL-UBUNTU.md).
2. Di Windows, klik dua kali `INSTALL-ASP-CPW.cmd`.
3. Setelah instalasi, backup `/opt/asp-cpw/config/keys.json` secara aman.
4. Siapkan **salinan client untuk pengujian** dengan [panduan client](docs/CLIENT-SETUP.md).
5. Terbitkan file uji kecil mengikuti [panduan operasi](docs/OPERATIONS.md).

## Struktur repositori

| Folder | Fungsi |
|---|---|
| `installer/` | Script instalasi Ubuntu yang dipanggil `INSTALL-ASP-CPW.cmd` |
| `scripts/`, `systemd/` | Manager release, worker, dan integrasi service terlindung |
| `web-integration/` | Integrasi Patch Manager ke panel admin PWKU |
| `tools/client-setup/` | Persiapan salinan client uji secara portabel |
| `tools/patch-publisher/` | Preview dan publikasi file client yang berubah |
| `vendor/cpw_pw/` | Source upstream CPW berlisensi MIT pada commit tertentu |
| `bin/linux-x64/` | Executable CPW Linux upstream yang sudah diverifikasi |

Repositori ini sengaja tidak memuat data client/server Perfect World, private RSA key, kredensial database,
password, release hasil generate, payload patch, ataupun backup client.

## Tool Windows

Untuk menyiapkan client uji, buka `tools/client-setup/PREPARE-ASP-CLIENT.cmd`. Tool akan meminta lokasi
folder client dan menyimpan backup di folder `backups/` yang diabaikan Git.

Untuk membuat patch, baca `tools/patch-publisher/CARA-PAKAI.md`, letakkan hanya file yang berubah di
`PATCH-FILES`, lakukan preview, lalu jalankan `PUBLISH-PATCH.cmd`. Payload patch diabaikan Git agar file
game berhak cipta tidak ikut ter-commit secara tidak sengaja.

## Sebelum upload ke GitHub

Jalankan `CHECK-BEFORE-GITHUB.cmd` untuk mencari secret khusus mesin, file game, arsip, backup, dan file
yang terlalu besar. Setelah lolos, gunakan alur Git biasa:

```powershell
git init
git add .
git status
git commit -m "Initial ASP CPW Manager release"
```

Periksa `git status` sebelum setiap commit dan jangan gunakan `git add -f` untuk melewati pengamanan.
Jangan pernah menimpa satu-satunya salinan `Launcher.exe` atau `patcher.exe`.

## Lokasi utama di Ubuntu

| Fungsi | Lokasi |
|---|---|
| Program dan kunci terlindung | `/opt/asp-cpw` |
| Input staging | `/srv/asp-cpw/staging` |
| Data kerja CPW | `/srv/asp-cpw/work` |
| Release yang sudah diterbitkan | `/srv/asp-cpw/releases` |
| Release publik saat ini | `/srv/asp-cpw/current` |
| Backup | `/srv/asp-cpw/backups` |
| Antrean/status web | `/var/lib/asp-cpw-control` |

## Lisensi dan asal komponen

Wrapper dan dokumentasi adalah bagian dari ASP Editor Studio. Engine CPW vendored tetap memakai lisensi
MIT upstream yang disimpan di `LICENSES/cpw_pw-MIT.txt`. Source upstream dipatok pada commit
`9a673ac12cff2f79f611f1bcd36438db3efae3c9`; checksum binary tersedia di
`bin/linux-x64/cpw.sha256.upstream`.

Publikasi source tidak otomatis memberikan lisensi atas wrapper asli ASP Editor Studio. Tambahkan lisensi
terpisah setelah menentukan ketentuan distribusi yang diinginkan.
