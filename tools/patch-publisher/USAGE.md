# Using ASP CPW One-Click Publisher

[Bahasa Indonesia](CARA-PAKAI.md) | **English**

Use this package after installing **ASP CPW Manager** on Ubuntu. It never stores passwords.

## Quick procedure

1. Back up each client file that will be replaced.
2. Remove old payloads from `PATCH-FILES`, but keep the instruction files.
3. Add new files with the same relative layout as the client.
4. Run `PREVIEW-PATCH.cmd`; it only displays the list and sends nothing.
5. Run `PUBLISH-PATCH.cmd`.
6. Review the list and type `PUBLISH` only when it is correct.
7. Enter the Ubuntu SSH password and enter it again if `sudo` asks.
8. Wait for `Patch published successfully`.
9. Test the launcher using a **dedicated test client copy**.

After success, local payloads move from `PATCH-FILES` to `PUBLISHED/DATE-TIME`. This prevents old files from
being sent again while retaining a local archive. `PUBLISHED` is ignored by Git.

PWKU VM defaults:

```text
Address : 127.0.0.1
SSH port: 2223
Username: pwadmin
Patch   : http://127.0.0.1:8082/patch/
```

## What belongs in each folder?

### `PATCH-FILES/element`

Files from the client's `element` folder. Preserve the complete relative path.

| Client file | Publisher location |
|---|---|
| `element/data/elements.data` | `PATCH-FILES/element/data/elements.data` |
| `element/data/tasks.data` | `PATCH-FILES/element/data/tasks.data` |
| `element/data/gshop.data` | `PATCH-FILES/element/data/gshop.data` |
| `element/interfaces.pck` | `PATCH-FILES/element/interfaces.pck` |
| `element/surfaces.pck` | `PATCH-FILES/element/surfaces.pck` |

For a UDE change that only produces `elements.data`, create `data` and place the file inside it.

### `PATCH-FILES/launcher`

Launcher-channel files such as `Launcher.exe` and its support files. Never replace the production launcher until
the RSA key and update flow pass testing.

### `PATCH-FILES/patcher`

Patcher-channel files. Preserve their relative layout and use this channel only when changing patcher components.

## Files that must not be added

- `gshopsev.data`, `npcgen.data`, `aipolicy.data`, `domain.sev`, or anything from server `gamed/config`.
- `keys.json`, passwords, database configuration, `.env` files, or private backups.
- An entire client when only one file changed.
- `PLACE-FILES-HERE.txt`; the publisher ignores its own instruction files.

## When server staging is not empty

The publisher stops instead of overwriting existing staging. Inspect it through PuTTY:

```bash
sudo asp-cpw-control preview
```

If it is abandoned test input, move it to a backup after inspection. Do not delete it blindly.

## After publishing

Select **Verify Release** in ASP CPW Desktop. Only channels containing files increase their versions.

A publication rollback does not downgrade clients that already updated. Publish a corrective revision with a
higher version for those clients.
