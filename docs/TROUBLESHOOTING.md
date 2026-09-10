# Troubleshooting

## Panel menampilkan Not available

```bash
sudo asp-cpw-control status
sudo systemctl restart asp-cpw-control.path pw155-web
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
curl.exe http://127.0.0.1:8081/patch/info/pid
curl.exe http://127.0.0.1:8081/patch/element/version
```

Periksa firewall, port forwarding VirtualBox, URL `updateserver.txt`, PID, versi `.sw`, dan kesesuaian RSA key.

## Publish gagal di tengah proses

Input staging tidak dihapus sebelum publish sukses. Baca journal dan log CPW. Jangan menggunakan `cpw new --force`
tanpa memastikan penyebab kegagalan, ruang disk, database, dan backup.
