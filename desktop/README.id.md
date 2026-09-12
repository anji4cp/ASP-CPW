# ASP CPW Desktop Manager

[English](README.md) | **Bahasa Indonesia**

Aplikasi Windows ringan yang menyatukan instalasi server CPW, persiapan client, pengambilan file update,
preview, publish, verifikasi, dan panduan staging. Aplikasi tidak menyimpan password atau menyertakan data game.

Nilai **Ubuntu address**, **SSH port**, **SSH username**, **Public patch URL**, **Game address**, dan **Game port**
diambil otomatis dari tab Settings. Console hanya meminta konfirmasi tindakan dan password. Bila pengaturan
koneksi kosong atau tidak valid, operasi diblokir dan pengguna diarahkan kembali ke Settings.

## Membuka aplikasi

Klik dua kali `ASP-CPW-DESKTOP.cmd` di root repository. Launcher tersebut membangun executable secara otomatis
bila `desktop/bin/ASP-CPW-Desktop.exe` belum tersedia. Windows 10/11 umumnya sudah menyediakan .NET Framework
yang diperlukan; tidak diperlukan .NET SDK atau Electron.

## Alur penggunaan

### Hanya sekali

1. Buka **Dashboard > Install / Update Server** untuk memasang manager ke Ubuntu. Ulangi hanya setelah aplikasi
   server di repository diperbarui.
2. Buka **Prepare Client** untuk client baru. Ulangi hanya jika client, launcher/patcher, URL, atau RSA key berubah.

### Setiap update

1. Buka **Create Update (Repeat)** dan pilih root client.
2. Centang hanya file yang berubah, lalu klik **Add Checked Files**. File custom harus berada di dalam folder
   `element`, `launcher`, atau `patcher` pada client.
3. Buka **Preview & Publish (Repeat)** dan periksa jalur serta ukuran.
4. Klik **Publish Update**, ikuti konfirmasi di console, lalu klik **Verify Release**.
5. Uji update menggunakan salinan client pengujian.

## Perlindungan publish ganda

Aplikasi membandingkan SHA-256 dengan arsip lokal `tools/patch-publisher/PUBLISHED`. File identik diberi status
**Already published locally** dan publish diblokir sampai file tersebut dikeluarkan atau diganti. Bila staging
Ubuntu terisi akibat publish gagal, gunakan **Server Staging**. Terbitkan staging lama hanya setelah semua jalurnya
dipastikan benar, lalu gunakan **Publish Existing Staging**; jangan langsung menghapusnya.

## File client dan server

CPW hanya mengirim file client. Jangan masukkan `gshopsev.data`, `npcgen.data`, `aipolicy.data`, `domain.sev`,
atau file `gamed/config` ke CPW. Saat memilih `gshop.data`, aplikasi mengingatkan untuk memasang dan menguji
pasangan `gshopsev.data` pada server lebih dahulu.

## Build manual

Klik `desktop/BUILD-DESKTOP.cmd`. Source berada di `desktop/src` dan hasil berada di
`desktop/bin/ASP-CPW-Desktop.exe`.
