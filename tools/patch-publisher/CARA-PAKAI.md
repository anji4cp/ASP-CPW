# Cara memakai ASP CPW One-Click Publisher

Paket ini digunakan setelah **ASP CPW Manager** sudah terpasang pada Ubuntu. Tidak ada password yang disimpan.

## Langkah singkat

1. Backup file client yang akan diganti.
2. Kosongkan file patch lama dari folder `PATCH-FILES`, kecuali file petunjuk bawaan.
3. Masukkan file baru dengan struktur yang sama seperti client.
4. Klik `PREVIEW-PATCH.cmd` untuk melihat daftar tanpa mengirim apa pun.
5. Klik `PUBLISH-PATCH.cmd`.
6. Periksa daftar file, lalu ketik `PUBLISH` jika benar.
7. Masukkan password SSH Ubuntu ketika diminta. Masukkan lagi bila `sudo` meminta password.
8. Tunggu sampai muncul `Patch published successfully`.
9. Jalankan launcher pada **salinan client pengujian**.

Setelah publish berhasil, file lokal otomatis dipindahkan dari `PATCH-FILES` ke `PUBLISHED/TANGGAL-WAKTU`.
Dengan demikian file patch lama tidak ikut terkirim lagi, tetapi salinannya tetap tersedia sebagai arsip lokal.

Konfigurasi server default sudah sesuai VM PWKU:

```text
Address : 127.0.0.1
SSH port: 2223
Username: pwadmin
Web     : http://127.0.0.1:8081/admin/patch
```

## Apa yang diletakkan di setiap folder?

### `PATCH-FILES/element`

File yang berada di dalam folder `element` client. Struktur folder wajib dipertahankan.

| File client | Letakkan sebagai |
|---|---|
| `element/data/elements.data` | `PATCH-FILES/element/data/elements.data` |
| `element/data/tasks.data` | `PATCH-FILES/element/data/tasks.data` |
| `element/data/gshop.data` | `PATCH-FILES/element/data/gshop.data` |
| `element/interfaces.pck` | `PATCH-FILES/element/interfaces.pck` |
| `element/surfaces.pck` | `PATCH-FILES/element/surfaces.pck` |

Untuk perubahan UDE yang hanya menghasilkan `elements.data`, buat folder `data` lalu masukkan file tersebut.

### `PATCH-FILES/launcher`

File yang relatif terhadap channel launcher, misalnya `Launcher.exe` atau file pendukung launcher. Jangan mengganti
`Launcher.exe` produksi sebelum RSA key dan update pada client uji dinyatakan berhasil.

### `PATCH-FILES/patcher`

File untuk channel patcher. Pertahankan struktur relatifnya. Gunakan hanya bila memang mengubah komponen patcher.

## File yang tidak boleh dimasukkan

- `gshopsev.data`, `npcgen.data`, `aipolicy.data`, `domain.sev`, dan konfigurasi di `gamed/config`—itu file server.
- `keys.json`, password, konfigurasi database, file `.env`, atau backup pribadi.
- Folder client lengkap jika hanya satu file yang berubah.
- File petunjuk `PLACE-FILES-HERE.txt`; publisher mengabaikannya otomatis.

## Jika staging server belum kosong

Publisher berhenti dan tidak menimpa staging yang sudah ada. Periksa dari PuTTY:

```bash
sudo asp-cpw-control preview
```

Jika file tersebut memang sisa percobaan dan tidak diperlukan, pindahkan dahulu ke folder backup; jangan langsung
menghapusnya tanpa pemeriksaan.

## Setelah publish

Buka [Patch Manager](http://127.0.0.1:8081/admin/patch). Hanya channel yang berisi file yang naik versinya.
Gunakan tombol **Verify current release** untuk pemeriksaan ulang.

Rollback publik tidak menurunkan client yang sudah telanjur update. Untuk client tersebut, perbaiki file dan publish
revision koreksi yang lebih tinggi.
