# ASP Perfect World Client Preparation

Tool ini menyiapkan base client agar mempercayai ASP CPW Manager yang sudah terpasang di Ubuntu.

## Yang dikerjakan

- Mengirim salinan `Launcher.exe` dan `patcher.exe` ke Ubuntu.
- Menanam public RSA key aktif menggunakan executable CPW di server.
- Mengunduh hasil dan memverifikasinya terhadap signature manifest aktif.
- Membuat backup file client sebelum perubahan diterapkan.
- Mengatur URL update ke `http://127.0.0.1:8081/patch/`.
- Mengatur PID ke `101`.
- Mengatur server game ke `127.0.0.1:29001`.
- Mengatur versi awal element, launcher, dan patcher ke `1`.
- Memindahkan cache `.sw` lama ke backup.

## Cara menjalankan

1. Pastikan VM hidup dan ASP CPW sudah berhasil dipasang.
2. Tutup Launcher, patcher, elementclient, dan UDE.
3. Klik `PREPARE-ASP-CLIENT.cmd`.
4. Masukkan lokasi lengkap folder client ketika diminta.
5. Periksa lokasi client yang ditampilkan, lalu ketik `PREPARE`.
6. Tekan Enter untuk memakai koneksi server default dan masukkan password Ubuntu ketika diminta.
7. Tunggu pesan `Client preparation completed successfully`.
8. Jalankan `launcher/Launcher.exe` dari salinan client ini.

Jika berhenti pada `Uploading executable copies`, tekan `Ctrl+C`, buka console VirtualBox, lalu jalankan
`sudo systemctl restart ssh`. Sesudah itu ulangi installer. Uploader memiliki timeout agar koneksi SSH yang
tidak merespons tidak menggantung tanpa batas.

Backup disimpan di folder `backups\TANGGAL-WAKTU` di samping tool ini. Folder backup diabaikan oleh Git.
Password tidak pernah disimpan.
