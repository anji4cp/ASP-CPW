# Troubleshooting

**Bahasa Indonesia** | [English](TROUBLESHOOTING.en.md)

## Panel menampilkan Not available

```bash
sudo asp-cpw-control status
sudo systemctl restart asp-cpw-control.path asp-cpw-http
sudo ls -la /var/lib/asp-cpw-control
```

Pastikan `pwweb` dapat menulis folder `requests` dan membaca `status.json` serta `snapshot.json`.

## Database connection failed

```bash
sudo mariadb --defaults-extra-file=/opt/asp-cpw/config/db.cnf -e "SELECT 1" cpw_patch
sudo systemctl status mariadb --no-pager
```

Jangan mengganti `db-name` menjadi `pw`.

## Signature verification failed

Kemungkinan public/private key tidak sepasang, manifest berasal dari instalasi CPW lain, atau `keys.json` pernah
diganti. Kembalikan backup kunci yang cocok; jangan membuat key baru pada server produksi tanpa menanam public
key baru ke kedua executable launcher.

## Launcher tidak mengunduh patch

Tes URL dari Windows:

```powershell
curl.exe http://127.0.0.1:8082/patch/info/pid
curl.exe http://127.0.0.1:8082/patch/element/version
```

Periksa firewall, port forwarding VirtualBox, URL `updateserver.txt`, PID, versi `.sw`, dan kesesuaian RSA key.

## Publish gagal di tengah proses

Input staging tidak dihapus sebelum publish sukses. Baca journal dan log CPW. Jangan menggunakan `cpw new --force`
tanpa memastikan penyebab kegagalan, ruang disk, database, dan backup.

## Installer tidak menemukan `/tmp/.../install-asp-cpw.sh`

Installer terbaru mengambil nama folder paket secara otomatis. Folder repository boleh bernama `ASP-CPW`,
`ASP-CPW-GitHub`, atau nama aman lain yang hanya memakai huruf, angka, titik, garis bawah, dan tanda hubung.

### `Checksum mismatch` diikuti `Access denied ... cpw_patch`

Pesan `Clients can now update to this revision` berasal dari mesin CPW sebelum pemeriksaan keselamatan ASP selesai.
Jika hasil akhir berisi `"ok": false`, rilis **belum** diaktifkan dan klien masih memakai rilis lama.

Versi awal paket ASP CPW dapat menyimpan lebih dari satu catatan untuk jalur patch yang sama. Paket terbaru akan:

1. mencocokkan metadata database dengan output terakhir yang sudah terverifikasi;
2. menghapus catatan jalur ganda tanpa menghapus file game sumber;
3. memasang indeks jalur unik agar masalah tidak berulang; dan
4. membuat dump tanpa perintah pembuatan database, sehingga akun terbatas `asp_cpw` dapat memulihkannya.

Rekonsiliasi memakai tabel sementara. Installer memberikan izin khusus `CREATE TEMPORARY TABLES` kepada
`asp_cpw`; izin ini diterapkan juga ketika memperbarui instalasi lama. Tabel sementara otomatis mengikuti
charset dan collation tabel CPW lama agar instalasi `utf8mb4_general_ci` maupun `utf8mb4_unicode_ci` didukung.

Pasang ulang paket terbaru menggunakan `INSTALL-ASP-CPW.cmd`. Konfigurasi, kunci, rilis, dan staging yang ada tetap
dipertahankan. Setelah instalasi selesai, periksa staging lalu publish kembali satu kali.
