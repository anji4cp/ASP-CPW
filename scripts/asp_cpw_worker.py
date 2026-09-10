#!/usr/bin/env python3
"""Consumes allowlisted web requests without granting shell access to the web user."""

import json
import os
import re
import subprocess
from pathlib import Path

STATE = Path(os.environ.get("ASP_CPW_STATE", "/var/lib/asp-cpw-control"))


def main() -> int:
    requests = sorted((STATE / "requests").glob("*.json"))
    if not requests:
        return 0
    request = requests[0]
    claimed = STATE / "processing.json"
    os.replace(request, claimed)
    try:
        data = json.loads(claimed.read_text(encoding="utf-8"))
        action = data.get("action")
        actor = str(data.get("actor", "web"))[:40]
        if not re.fullmatch(r"[A-Za-z0-9_.@ -]{1,40}", actor):
            actor = "web"
        command = ["/usr/local/sbin/asp-cpw-control", action, "--actor", actor]
        if action == "rollback":
            release = str(data.get("release", ""))
            if not re.fullmatch(r"[A-Za-z0-9._-]+", release):
                raise ValueError("Invalid release")
            command += ["--release", release]
        elif action not in ("publish", "verify"):
            raise ValueError("Action not allowed")
        result = subprocess.run(command, check=False).returncode
        snapshot = subprocess.run(["/usr/local/sbin/asp-cpw-control", "status"],
                                  check=False, stdout=subprocess.PIPE, text=True)
        if snapshot.returncode == 0:
            temp = STATE / "snapshot.json.tmp"
            temp.write_text(snapshot.stdout, encoding="utf-8")
            os.replace(temp, STATE / "snapshot.json")
        return result
    finally:
        claimed.unlink(missing_ok=True)


if __name__ == "__main__":
    raise SystemExit(main())
