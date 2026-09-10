# Rollback and recovery

[Bahasa Indonesia](ROLLBACK.md) | **English**

## Publication rollback

List available releases:

```bash
sudo asp-cpw-control status
```

Point the public endpoint to an older release:

```bash
sudo asp-cpw-control rollback --release RELEASE_NAME --actor pwadmin
```

The web panel provides the same action. The manager verifies the target release before replacing the symlink.
This is fast and does not rewrite CPW database history. The next publication continues from the latest version,
so the version history does not branch.

## Full recovery

Every publication backup is stored under `/srv/asp-cpw/backups/TIMESTAMP/` and contains `cpw_patch.sql`,
the previous CPW output, configuration, and published input. A full database restore is destructive and is
therefore intentionally unavailable as a web button.

Before a full restore:

1. Stop new publications.
2. Create a VM snapshot.
3. Confirm that the backup matches the release to restore.
4. Restore the database manually as an administrator, restore the working output, run `cpw listgen`, then verify.

When unsure, use publication rollback and preserve all logs for diagnosis.
