# Menyiapkan launcher client

**Bahasa Indonesia** | [English](CLIENT-SETUP.en.md)

Gunakan salinan client khusus pengujian. Jangan langsung mengubah client utama pemain.

## Alamat patch

Layanan mandiri ASP CPW menyediakan endpoint `/patch/` pada port guest `8082`. Contoh untuk VirtualBox NAT dengan host port yang sama:

```text
http://127.0.0.1:8082/patch/
```

Tambahkan aturan NAT TCP host `8082` ke guest `8082`. Bila memakai Bridged Adapter, gunakan
`http://IP-VM:8082/patch/`. ASP PWPanel tidak diperlukan untuk menyajikan patch.

Pada client, buka `patcher/server/updateserver.txt`, hapus alamat resmi yang tidak dipakai, lalu masukkan
alamat tersebut sesuai format yang sudah digunakan file asli. Pastikan URL diakhiri `/`.

## PID dan versi awal

- Isi `patcher/server/pid.ini` dengan PID `101` bila client CPW ini memakai nilai standar.
- Di folder konfigurasi channel client, pertahankan `version.sw` dan set ke versi baseline `1`.
- Hapus file `.sw` cache lain hanya pada **salinan client pengujian**.

## Menanam public key ke executable

`Launcher.exe` dan `patcher.exe` harus memakai public key yang sama dengan `/opt/asp-cpw/config/keys.json`.
Proses ini hanya dilakukan saat menyiapkan build launcher atau saat kunci sengaja diganti.

1. Backup kedua executable.
2. Upload salinannya ke folder kerja Ubuntu, bukan langsung menimpa client.
3. Jalankan:

```bash
cd /opt/asp-cpw
sudo ./cpw x /path/kerja/Launcher.exe
sudo ./cpw x /path/kerja/patcher.exe
```

4. Unduh kembali hasilnya dan tempatkan pada client uji.
5. Jalankan launcher uji dan pastikan baseline dapat dibaca sebelum menerbitkan patch besar.

Jika muncul `marker not found` atau `serialized key exceeds placeholder`, executable tersebut tidak memiliki
placeholder CPW yang kompatibel. Jangan paksa atau melakukan hex replacement manual.

Catatan: RSA-1024 dan MD5withRSA adalah format lama yang dipertahankan untuk kompatibilitas launcher PW.
Jangan mengganti algoritma sepihak tanpa mengubah dan menguji kode launcher.
