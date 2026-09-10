# Operasi harian ASP CPW Manager

**Bahasa Indonesia** | [English](OPERATIONS.en.md)

## Struktur staging

Letakkan hanya file baru atau file yang berubah. Jalur di dalam staging harus sama dengan jalur client.

```text
/srv/asp-cpw/staging/
├── element/
│   ├── data/elements.data
│   ├── data/tasks.data
│   └── interfaces/...
├── launcher/
└── patcher/
```

Contoh mengunggah `elements.data` dari Windows:

```powershell
scp -P 2223 "F:\Path\element\data\elements.data" pwadmin@127.0.0.1:/tmp/elements.data
ssh -p 2223 pwadmin@127.0.0.1 "sudo install -D -m 0644 /tmp/elements.data /srv/asp-cpw/staging/element/data/elements.data"
```

## Preview

```bash
sudo asp-cpw-control preview
```

Periksa jumlah dan nama file. Jangan publish bila ada file rahasia, backup, atau jalur yang salah.

## Publish

Melalui CLI:

```bash
sudo asp-cpw-control publish --actor pwadmin
```

Atau buka **Admin Panel > Patch Manager**, centang konfirmasi, lalu pilih **Publish staged update**.

Sebelum CPW dijalankan, manager menyimpan dump database dan snapshot output. Setelah CPW selesai, manager
memverifikasi checksum setiap payload dan signature manifest, membuat release immutable, kemudian mengganti
symlink `current` secara atomik. Staging dipindahkan ke backup hanya setelah sukses.
Jika proses atau verifikasi gagal, manager otomatis mengembalikan database dan output kerja dari snapshot
pra-publish; file staging tetap tersedia untuk diperiksa dan dicoba kembali.

## Verify

```bash
sudo asp-cpw-control verify
```

Verify memeriksa versi, manifest, seluruh MD5 payload, dan RSA signature bila OpenSSL serta `keys.json` tersedia.

## Melihat log

```bash
sudo journalctl -u asp-cpw-control.service -n 100 --no-pager
sudo tail -n 100 /opt/asp-cpw/log/errors.log
```

## Batasan CPW upstream

- Gunakan MariaDB/MySQL; adapter PostgreSQL upstream belum diimplementasikan.
- Format incremental mendukung file baru dan file berubah. Penghapusan file client tidak memiliki tombstone
  yang jelas; untuk menghapus file, perlakukan sebagai perubahan khusus dan uji launcher terlebih dahulu.
- Jangan memakai database game `pw`; database manager selalu `cpw_patch`.
