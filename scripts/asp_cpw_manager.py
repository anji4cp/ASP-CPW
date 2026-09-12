#!/usr/bin/env python3
"""Safe release wrapper around the CPW Perfect World patch engine."""

from __future__ import annotations

import argparse
import base64
import contextlib
import fcntl
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path

TYPES = ("element", "launcher", "patcher")
MANIFEST_MARKER = b"-----BEGIN ELEMENT SIGNATURE-----\n"


class ManagerError(RuntimeError):
    pass


def env_path(name: str, default: str) -> Path:
    return Path(os.environ.get(name, default)).resolve()


HOME = env_path("ASP_CPW_HOME", "/opt/asp-cpw")
DATA = env_path("ASP_CPW_DATA", "/srv/asp-cpw")
STATE = env_path("ASP_CPW_STATE", "/var/lib/asp-cpw-control")
WORK = DATA / "work"
STAGING = DATA / "staging"
RELEASES = DATA / "releases"
BACKUPS = DATA / "backups"
CURRENT = DATA / "current"
CPW = HOME / "cpw"


def utc_now() -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())


def atomic_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def status_update(**changes) -> dict:
    path = STATE / "status.json"
    try:
        status = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError, TypeError):
        status = {}
    status.update(changes)
    status["updated_at"] = utc_now()
    atomic_json(path, status)
    return status


@contextlib.contextmanager
def exclusive_lock():
    STATE.mkdir(parents=True, exist_ok=True)
    with (STATE / "manager.lock").open("w", encoding="utf-8") as handle:
        try:
            fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError as exc:
            raise ManagerError("Another ASP CPW operation is still running") from exc
        yield


def ensure_layout() -> None:
    for path in (WORK / "new", WORK / "CPW", STAGING, RELEASES, BACKUPS, STATE):
        path.mkdir(parents=True, exist_ok=True)
    for kind in TYPES:
        (WORK / "new" / kind).mkdir(parents=True, exist_ok=True)
        (STAGING / kind).mkdir(parents=True, exist_ok=True)
    if not CPW.is_file():
        raise ManagerError(f"CPW executable not found: {CPW}")
    if not (HOME / "config" / "patcher.conf").is_file():
        raise ManagerError("Missing /opt/asp-cpw/config/patcher.conf")


def safe_release(name: str) -> Path:
    if not re.fullmatch(r"[A-Za-z0-9._-]+", name):
        raise ManagerError("Invalid release name")
    path = (RELEASES / name).resolve()
    if path.parent != RELEASES.resolve() or not path.is_dir():
        raise ManagerError(f"Release not found: {name}")
    return path


def staged_files() -> dict[str, list[str]]:
    result = {}
    for kind in TYPES:
        root = STAGING / kind
        result[kind] = sorted(
            str(p.relative_to(root)).replace(os.sep, "/")
            for p in root.rglob("*") if p.is_file() and not p.name.startswith(".")
        )
    return result


def preview() -> dict:
    files = staged_files()
    return {
        "ok": True,
        "action": "preview",
        "files": files,
        "counts": {kind: len(items) for kind, items in files.items()},
        "total": sum(map(len, files.values())),
        "current": CURRENT.resolve().name if CURRENT.is_symlink() else None,
    }


def run_checked(args: list[str], *, cwd: Path | None = None, stdout=None) -> None:
    process = subprocess.run(args, cwd=cwd, stdout=stdout, stderr=subprocess.PIPE, text=stdout is None)
    if process.returncode:
        detail = (process.stderr or "").strip() if isinstance(process.stderr, str) else ""
        raise ManagerError(f"Command failed ({args[0]}): {detail[-800:]}")


def database_backup(target: Path) -> None:
    defaults = HOME / "config" / "db.cnf"
    if not defaults.is_file():
        raise ManagerError("Missing protected database client config: config/db.cnf")
    dump_command = shutil.which("mariadb-dump") or shutil.which("mysqldump")
    if not dump_command:
        raise ManagerError(
            "MariaDB dump utility is missing. Install it with: "
            "sudo apt-get update && sudo apt-get install -y mariadb-client"
        )
    with target.open("wb") as handle:
        proc = subprocess.run(
            [dump_command, f"--defaults-extra-file={defaults}", "--single-transaction",
             "--no-create-db", "--skip-add-locks", "cpw_patch"],
            stdout=handle, stderr=subprocess.PIPE,
        )
    if proc.returncode:
        raise ManagerError("Database backup failed: " + proc.stderr.decode("utf-8", "replace")[-800:])


def database_restore(source: Path) -> None:
    defaults = HOME / "config" / "db.cnf"
    with source.open("rb") as handle:
        proc = subprocess.run(
            ["mariadb", f"--defaults-extra-file={defaults}", "cpw_patch"],
            stdin=handle, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        )
    if proc.returncode:
        raise ManagerError("Database restore failed: " + proc.stderr.decode("utf-8", "replace")[-800:])


def database_sql(sql: str) -> str:
    """Run SQL with the restricted CPW account; never expose its password."""
    defaults = HOME / "config" / "db.cnf"
    proc = subprocess.run(
        ["mariadb", f"--defaults-extra-file={defaults}", "--batch", "--skip-column-names", "cpw_patch"],
        input=sql.encode("utf-8"), stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    )
    if proc.returncode:
        raise ManagerError("Database maintenance failed: " + proc.stderr.decode("utf-8", "replace")[-800:])
    return proc.stdout.decode("utf-8", "replace").strip()


def reconcile_database_with_output(root: Path) -> None:
    """Make CPW database metadata match the last known-good patch output.

    Older CPW schemas omitted the unique path index even though the writer uses
    ON DUPLICATE KEY UPDATE. That creates duplicate manifest rows after a file is
    updated and makes the manifest checksum disagree with the overwritten
    payload. Reconcile from the immutable/previously verified output, remove the
    duplicates, then install the missing uniqueness constraint.
    """
    expected: list[tuple[str, str, str, str, int, int]] = []
    for kind in TYPES:
        manifest = root / kind / "files.md5"
        version_file = root / kind / "version"
        if not manifest.is_file() or not version_file.is_file():
            raise ManagerError(f"Cannot reconcile database: missing {kind} manifest/version")
        version = int(version_file.read_text(encoding="ascii").strip())
        body = manifest.read_bytes().split(MANIFEST_MARKER, 1)[0]
        for raw in body.splitlines()[1:]:
            if not raw.strip():
                continue
            digest, encoded_path = raw.decode("ascii", "strict").split(" ", 1)
            parts = encoded_path.split("/")
            folder64 = "/".join(parts[:-1])
            file64 = parts[-1]
            payload = root / kind / kind / encoded_path
            if not payload.is_file():
                raise ManagerError(f"Cannot reconcile database: missing payload {kind}/{encoded_path}")
            expected.append((kind, folder64, file64, digest, payload.stat().st_size, version))

    table_collation = database_sql(
        "SELECT table_collation FROM information_schema.tables "
        "WHERE table_schema=DATABASE() AND table_name='files' LIMIT 1;\n"
    )
    if not re.fullmatch(r"[A-Za-z0-9_]+", table_collation):
        raise ManagerError("Cannot determine a safe collation for the CPW files table")
    table_charset = table_collation.split("_", 1)[0]

    statements = [
        "DROP TEMPORARY TABLE IF EXISTS asp_cpw_expected",
        "CREATE TEMPORARY TABLE asp_cpw_expected ("
        "type VARCHAR(16) NOT NULL, folder_base64 VARCHAR(700) NOT NULL, "
        "file_base64 VARCHAR(350) NOT NULL, md5 CHAR(32) NOT NULL, "
        "size BIGINT NOT NULL, revision INT NOT NULL) "
        f"DEFAULT CHARACTER SET {table_charset} COLLATE {table_collation}",
    ]
    for kind, folder64, file64, digest, size, version in expected:
        # All string values come from strict ASCII manifest fields and fixed type names.
        statements.append(
            "INSERT INTO asp_cpw_expected VALUES "
            f"('{kind}','{folder64}','{file64}','{digest}',{size},{version})"
        )
    statements.extend([
        "DELETE f FROM files f LEFT JOIN asp_cpw_expected e "
        "ON e.type=f.type AND e.folder_base64=f.folder_base64 AND e.file_base64=f.file_base64 "
        "WHERE e.type IS NULL",
        "UPDATE files f JOIN asp_cpw_expected e "
        "ON e.type=f.type AND e.folder_base64=f.folder_base64 AND e.file_base64=f.file_base64 "
        "SET f.md5=e.md5, f.size=e.size, f.revision=e.revision",
        "UPDATE files keep_row JOIN (SELECT MAX(id) keep_id, MIN(added) first_added "
        "FROM files GROUP BY type, folder, file HAVING COUNT(*) > 1) d "
        "ON keep_row.id=d.keep_id SET keep_row.added=d.first_added",
        "DELETE stale FROM files stale JOIN files newer "
        "ON stale.type=newer.type AND stale.folder=newer.folder AND stale.file=newer.file "
        "AND stale.id<newer.id",
    ])
    database_sql(";\n".join(statements) + ";\n")

    index_exists = database_sql(
        "SELECT EXISTS(SELECT 1 FROM information_schema.statistics "
        "WHERE table_schema=DATABASE() AND table_name='files' "
        "AND index_name='uq_files_path');\n"
    )
    if index_exists != "1":
        database_sql("ALTER TABLE files ADD UNIQUE KEY uq_files_path (type, folder, file);\n")


def copy_staging_to_input(files: dict[str, list[str]]) -> None:
    for kind, names in files.items():
        source_root, target_root = STAGING / kind, WORK / "new" / kind
        for name in names:
            source = (source_root / name).resolve()
            target = (target_root / name).resolve()
            if source_root.resolve() not in source.parents or target_root.resolve() not in target.parents:
                raise ManagerError(f"Unsafe staged path: {name}")
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)


def clear_work_input() -> None:
    """Clear only the three fixed CPW input roots."""
    for kind in TYPES:
        root = WORK / "new" / kind
        if root.is_dir():
            for child in root.iterdir():
                if child.is_dir() and not child.is_symlink():
                    shutil.rmtree(child)
                else:
                    child.unlink(missing_ok=True)
        else:
            root.mkdir(parents=True, exist_ok=True)


def release_name() -> str:
    versions = []
    for kind in TYPES:
        path = WORK / "CPW" / kind / "version"
        versions.append(path.read_text(encoding="ascii").strip() if path.is_file() else "0")
    return time.strftime("%Y%m%d-%H%M%S", time.gmtime()) + "-" + "-".join(versions)


def clone_tree(source: Path, target: Path) -> None:
    # Do not hard-link releases to mutable CPW work files: CPW overwrites payloads in place.
    shutil.copytree(source, target, copy_function=shutil.copy2)


def make_release_readable(release: Path) -> None:
    """Patch artifacts contain no secrets and must be readable by the web service."""
    release.chmod(0o755)
    for path in release.rglob("*"):
        if path.is_dir():
            path.chmod(0o755)
        elif path.is_file():
            path.chmod(0o644)


def switch_current(release: Path) -> None:
    temp_link = DATA / ".current.tmp"
    with contextlib.suppress(FileNotFoundError):
        temp_link.unlink()
    temp_link.symlink_to(release, target_is_directory=True)
    os.replace(temp_link, CURRENT)


def verify_manifest(kind: str, root: Path) -> dict:
    manifest = root / kind / "files.md5"
    version_file = root / kind / "version"
    if not manifest.is_file() or not version_file.is_file():
        raise ManagerError(f"Missing {kind} version or files.md5")
    version = int(version_file.read_text(encoding="ascii").strip())
    data = manifest.read_bytes()
    if MANIFEST_MARKER not in data:
        raise ManagerError(f"Missing RSA signature in {kind}/files.md5")
    body, signature_text = data.split(MANIFEST_MARKER, 1)
    first = body.splitlines()[0].decode("ascii", "strict")
    if first != f"# {version}":
        raise ManagerError(f"{kind} manifest version does not match version file")
    checked = 0
    for raw in body.splitlines()[1:]:
        if not raw.strip():
            continue
        digest, encoded_path = raw.decode("ascii", "strict").split(" ", 1)
        if not re.fullmatch(r"[0-9a-f]{32}", digest):
            raise ManagerError(f"Invalid MD5 record in {kind}")
        payload = root / kind / kind / encoded_path
        if not payload.is_file():
            raise ManagerError(f"Missing payload: {kind}/{encoded_path}")
        actual = hashlib.md5(payload.read_bytes()).hexdigest()
        if actual != digest:
            raise ManagerError(f"Checksum mismatch: {kind}/{encoded_path}")
        checked += 1
    keys_path = HOME / "config" / "keys.json"
    if not keys_path.is_file():
        raise ManagerError("keys.json is missing; RSA signature cannot be verified")
    if not shutil.which("openssl"):
        raise ManagerError("OpenSSL is missing; RSA signature cannot be verified")
    keys = json.loads(keys_path.read_text(encoding="utf-8"))
    signature = base64.b64decode(b"".join(signature_text.splitlines()), validate=True)
    with tempfile.TemporaryDirectory() as temp:
        temp_path = Path(temp)
        (temp_path / "body").write_bytes(body)
        (temp_path / "signature").write_bytes(signature)
        (temp_path / "public.pem").write_text(keys["publicKeyPem"] + "\n", encoding="ascii")
        proc = subprocess.run(
            ["openssl", "dgst", "-md5", "-verify", str(temp_path / "public.pem"),
             "-signature", str(temp_path / "signature"), str(temp_path / "body")],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        )
        if proc.returncode:
            raise ManagerError(f"RSA signature verification failed for {kind}")
    return {"version": version, "files": checked}


def verify(root: Path | None = None) -> dict:
    root = root or (CURRENT.resolve() if CURRENT.is_symlink() else WORK / "CPW")
    details = {kind: verify_manifest(kind, root) for kind in TYPES}
    return {"ok": True, "action": "verify", "root": str(root), "types": details}


def publish(actor: str) -> dict:
    ensure_layout()
    files = staged_files()
    if not any(files.values()):
        raise ManagerError("Staging is empty; nothing to publish")
    stamp = time.strftime("%Y%m%d-%H%M%S", time.gmtime()) + f"-{time.time_ns() % 1_000_000:06d}"
    backup_dir = BACKUPS / stamp
    backup_dir.mkdir(parents=True, exist_ok=False)
    database_backup(backup_dir / "cpw_patch.sql")
    if (WORK / "CPW").is_dir():
        shutil.copytree(WORK / "CPW", backup_dir / "CPW", copy_function=shutil.copy2)
    shutil.copy2(HOME / "config" / "patcher.conf", backup_dir / "patcher.conf")
    release = None
    archive = backup_dir / "published-input"
    try:
        reconcile_database_with_output(WORK / "CPW")
        clear_work_input()
        copy_staging_to_input(files)
        run_checked([str(CPW), "new"], cwd=HOME)
        checked = verify(WORK / "CPW")
        name = release_name()
        release = RELEASES / name
        clone_tree(WORK / "CPW", release)
        for kind, names in files.items():
            for name in names:
                source = STAGING / kind / name
                target = archive / kind / name
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.move(source, target)
        clear_work_input()
        metadata = {"release": name, "actor": actor, "published_at": utc_now(), "files": files,
                    "verification": checked["types"]}
        atomic_json(release / "release.json", metadata)
        make_release_readable(release)
        switch_current(release)
    except Exception as exc:
        recovery_errors = []
        try:
            database_restore(backup_dir / "cpw_patch.sql")
        except Exception as recovery:
            recovery_errors.append(str(recovery))
        try:
            if (WORK / "CPW").exists():
                shutil.rmtree(WORK / "CPW")
            if (backup_dir / "CPW").is_dir():
                shutil.copytree(backup_dir / "CPW", WORK / "CPW", copy_function=shutil.copy2)
            clear_work_input()
            if release and release.is_dir():
                shutil.rmtree(release)
            if archive.is_dir():
                for source in sorted(archive.rglob("*")):
                    if source.is_file():
                        target = STAGING / source.relative_to(archive)
                        target.parent.mkdir(parents=True, exist_ok=True)
                        if not target.exists():
                            shutil.move(source, target)
        except Exception as recovery:
            recovery_errors.append(str(recovery))
        suffix = "; recovery errors: " + " | ".join(recovery_errors) if recovery_errors else "; previous state restored"
        raise ManagerError(str(exc) + suffix) from exc
    keep = max(2, int(os.environ.get("ASP_CPW_KEEP_RELEASES", "10")))
    all_releases = sorted(p for p in RELEASES.iterdir() if p.is_dir())
    protected = CURRENT.resolve() if CURRENT.is_symlink() else None
    for old in all_releases[:-keep]:
        if old.resolve() != protected:
            shutil.rmtree(old)
    return {"ok": True, "action": "publish", **metadata}


def bootstrap(actor: str) -> dict:
    """Create keys/schema/baseline once during installation."""
    ensure_layout()
    if CURRENT.is_symlink() or any(RELEASES.iterdir()):
        raise ManagerError("ASP CPW is already initialized")
    run_checked([str(CPW), "install"], cwd=HOME)
    run_checked([str(CPW), "initial"], cwd=HOME)
    checked = verify(WORK / "CPW")
    name = release_name() + "-baseline"
    release = RELEASES / name
    clone_tree(WORK / "CPW", release)
    metadata = {"release": name, "actor": actor, "published_at": utc_now(),
                "files": {kind: [] for kind in TYPES}, "verification": checked["types"],
                "baseline": True}
    atomic_json(release / "release.json", metadata)
    make_release_readable(release)
    switch_current(release)
    return {"ok": True, "action": "bootstrap", **metadata}


def rollback(name: str, actor: str) -> dict:
    release = safe_release(name)
    verify(release)
    switch_current(release)
    return {"ok": True, "action": "rollback", "release": name, "actor": actor,
            "rolled_back_at": utc_now(),
            "note": "Publication pointer changed; CPW database history was not rewritten."}


def status() -> dict:
    releases = []
    if RELEASES.is_dir():
        releases = [p.name for p in sorted(RELEASES.iterdir(), reverse=True) if p.is_dir()]
    result = preview()
    result.update({"action": "status", "releases": releases})
    try:
        result["verification"] = verify()["types"]
    except (ManagerError, OSError, ValueError) as exc:
        result["verification_error"] = str(exc)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="ASP CPW release manager")
    parser.add_argument("action", choices=("bootstrap", "preview", "publish", "verify", "rollback", "status"))
    parser.add_argument("--release", default="")
    parser.add_argument("--actor", default=os.environ.get("SUDO_USER", os.environ.get("USER", "system")))
    args = parser.parse_args()
    mutating = args.action in ("bootstrap", "publish", "rollback")
    tracked = mutating or args.action == "verify"
    try:
        with exclusive_lock() if mutating else contextlib.nullcontext():
            if tracked:
                status_update(busy=True, action=args.action, actor=args.actor, started_at=utc_now(), error=None)
            result = {"bootstrap": lambda: bootstrap(args.actor), "preview": preview,
                      "publish": lambda: publish(args.actor),
                      "verify": verify, "rollback": lambda: rollback(args.release, args.actor),
                      "status": status}[args.action]()
            if tracked:
                status_update(busy=False, last_result=result, error=None)
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return 0
    except (ManagerError, OSError, ValueError, KeyError, json.JSONDecodeError) as exc:
        if tracked:
            status_update(busy=False, error=str(exc), failed_at=utc_now())
        print(json.dumps({"ok": False, "action": args.action, "error": str(exc)}), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
