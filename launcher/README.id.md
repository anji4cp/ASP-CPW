# ASP Launcher

ASP Launcher adalah pendamping ringan ASP CPW. Halaman berita ditampilkan secara
mandiri sehingga tidak bergantung pada browser bawaan patcher Perfect World lama.
Aplikasi membaca folder client dan URL website yang tersimpan di **ASP CPW Desktop**.

1. Atur folder client dan Launcher Links melalui ASP CPW Desktop.
2. Jalankan `INSTALL-ASP-LAUNCHER-TO-CLIENT.cmd` sekali untuk memasangnya ke client.
3. Setelah terpasang, jalankan `launcher/Launcher.exe` atau `patcher/patcher.exe` seperti biasa.
4. ASP Launcher membuka patcher asli dan menempatkan ASP News di atas area browser yang putih.
5. Panel News memakai kontrol native Windows, bukan browser lama, agar tidak
   berubah putih ketika patcher digeser atau dikembalikan dari minimize.

Overlay membaca `SkinBrowser` dan `MapColor` dari `mainuni.xml`, lalu mencari
area berwarna tersebut pada `main-map.png`. Posisi mengikuti skin launcher aktif,
bukan memakai koordinat layar desktop yang tetap.

Installer melakukan perubahan yang dapat dipulihkan:

- `patcher.exe` asli disimpan sebagai `patcher-core.exe`.
- ASP Launcher dipasang sebagai `patcher.exe` dan `ASP-Launcher.exe`.
- Backup tambahan dibuat di `patcher/asp-launcher-backup/<tanggal>/`.
- Executable ASP Launcher meminta hak Administrator melalui UAC secara otomatis.

Jalankan `restore-original-patcher.ps1` untuk memulihkan patcher asli. Jika channel
patcher CPW mengganti `patcher.exe`, jalankan installer ASP Launcher lagi setelah
update selesai. Gunakan `launcher/bin/ASP-Launcher.exe --manager` hanya untuk membuka jendela mandiri.

Installer tidak mengubah data game; hanya mengintegrasikan entry executable patcher.
