# Rollback dan pemulihan

**Bahasa Indonesia** | [English](ROLLBACK.en.md)

## Publication rollback

Lihat nama release:

```bash
sudo asp-cpw-control status
```

Pindahkan publikasi ke release lama:

```bash
sudo asp-cpw-control rollback --release NAMA_RELEASE --actor pwadmin
```

Panel web menyediakan tombol yang sama. Manager memverifikasi release tujuan sebelum mengganti symlink.
Operasi ini cepat dan tidak menulis ulang database CPW. Publish berikutnya tetap melanjutkan nomor versi terbaru,
sehingga tidak membuat sejarah versi bercabang.

## Pemulihan penuh

Backup setiap publish berada di `/srv/asp-cpw/backups/TIMESTAMP/` dan berisi `cpw_patch.sql`, output CPW lama,
konfigurasi, dan input yang sudah dipublikasikan. Pemulihan database penuh bersifat destruktif dan sengaja tidak
disediakan sebagai tombol web.

Sebelum restore penuh:

1. Matikan publikasi baru.
2. Buat snapshot VM.
3. Pastikan backup sesuai release yang ingin dipulihkan.
4. Restore database secara manual sebagai administrator, salin output kerja, jalankan `cpw listgen`, lalu verify.

Jika ragu, gunakan publication rollback saja dan simpan seluruh log untuk diagnosis.
