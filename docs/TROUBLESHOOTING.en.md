# Troubleshooting

[Bahasa Indonesia](TROUBLESHOOTING.md) | **English**

## The panel shows Not available

```bash
sudo asp-cpw-control status
sudo systemctl restart asp-cpw-control.path pw155-web
sudo ls -la /var/lib/asp-cpw-control
```

Confirm that `pwweb` can write the `requests` directory and read `status.json` and `snapshot.json`.

## Database connection failed

```bash
sudo mariadb --defaults-extra-file=/opt/asp-cpw/config/db.cnf -e "SELECT 1" cpw_patch
sudo systemctl status mariadb --no-pager
```

Never change `db-name` to the game database `pw`.

## Signature verification failed

The public/private keys may not match, the manifest may belong to another CPW installation, or `keys.json` may
have been replaced. Restore the matching key backup. Do not generate a new production key without embedding its
public key into both launcher executables.

## The launcher does not download patches

Test the endpoints from Windows:

```powershell
curl.exe http://127.0.0.1:8081/patch/info/pid
curl.exe http://127.0.0.1:8081/patch/element/version
```

Check the firewall, VirtualBox port forwarding, `updateserver.txt` URL and UTF-16 LE encoding, PID, `.sw`
versions, and RSA key pairing.

## Publication stops midway

Staging input is retained until publication succeeds. Read the system journal and CPW log. Do not use
`cpw new --force` before checking the failure, disk space, database, and backup.

### `Checksum mismatch` followed by `Access denied ... cpw_patch`

`Clients can now update to this revision` is printed by the CPW engine before ASP's final safety verification.
When the final result contains `"ok": false`, the release was **not** activated and clients still use the previous
release.

An early ASP CPW package could keep multiple database rows for the same patch path. The current package:

1. reconciles database metadata with the last verified output;
2. removes duplicate path rows without deleting source game files;
3. installs a unique path index so the problem cannot recur; and
4. creates dumps without database-creation statements, allowing the restricted `asp_cpw` account to restore them.

Reconciliation uses a temporary table. The installer grants the dedicated `CREATE TEMPORARY TABLES` privilege
to `asp_cpw`, including when upgrading an existing installation. The temporary table automatically inherits
the legacy CPW table's character set and collation, supporting both `utf8mb4_general_ci` and
`utf8mb4_unicode_ci` installations.

Reinstall the current package with `INSTALL-ASP-CPW.cmd`. Existing configuration, keys, releases, and staging are
preserved. When installation finishes, review staging and publish once again.
