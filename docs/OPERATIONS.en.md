# Daily ASP CPW Manager operations

[Bahasa Indonesia](OPERATIONS.md) | **English**

## Staging layout

Place only new or changed files in staging. Paths below staging must match their client paths.

```text
/srv/asp-cpw/staging/
├── element/
│   ├── data/elements.data
│   ├── data/tasks.data
│   └── interfaces/...
├── launcher/
└── patcher/
```

Example upload of `elements.data` from Windows:

```powershell
scp -P 2223 "F:\Path\element\data\elements.data" pwadmin@127.0.0.1:/tmp/elements.data
ssh -p 2223 pwadmin@127.0.0.1 "sudo install -D -m 0644 /tmp/elements.data /srv/asp-cpw/staging/element/data/elements.data"
```

## Preview

```bash
sudo asp-cpw-control preview
```

Check every path and the file count. Do not publish secrets, backups, or files with incorrect paths.

## Publish

From the CLI:

```bash
sudo asp-cpw-control publish --actor pwadmin
```

Alternatively, use **Preview & Publish** in ASP CPW Desktop and select **Publish Existing Staging (Recovery)** after reviewing every path.

Before CPW runs, the manager saves a database dump and an output snapshot. After CPW finishes, it verifies every
payload checksum and manifest signature, creates an immutable release, and atomically switches `current`.
Staging is moved to backup only after success. On failure, the manager restores the pre-publication database and
working output automatically while retaining staging for inspection and retry.

## Verify

```bash
sudo asp-cpw-control verify
```

Verification checks versions, manifests, all payload MD5 values, and RSA signatures when OpenSSL and `keys.json`
are available.

## Read logs

```bash
sudo journalctl -u asp-cpw-control.service -n 100 --no-pager
sudo tail -n 100 /opt/asp-cpw/log/errors.log
```

## Upstream CPW limitations

- Use MariaDB/MySQL; the upstream PostgreSQL adapter is not implemented.
- Incremental patches support new and changed files. Client deletions have no clear tombstone behavior; treat a
  deletion as a special migration and test the launcher first.
- Never use the game database `pw`; the manager database is always `cpw_patch`.
