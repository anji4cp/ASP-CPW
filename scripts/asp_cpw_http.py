#!/usr/bin/env python3
"""Read-only standalone HTTP endpoint for ASP CPW patch files."""

import os
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit

ROOT = Path(os.environ.get("ASP_CPW_PUBLIC_ROOT", "/srv/asp-cpw/current")).resolve()
HOST = os.environ.get("ASP_CPW_HTTP_HOST", "0.0.0.0")
PORT = int(os.environ.get("ASP_CPW_HTTP_PORT", "8082"))
ALLOWED_TOP = {"info", "element", "launcher", "patcher"}

def resolve_patch(path):
    value = PurePosixPath(unquote(path))
    if value.is_absolute() or ".." in value.parts or not value.parts:
        return None
    if value.parts[0] not in ALLOWED_TOP:
        return None
    candidate = (ROOT / Path(*value.parts)).resolve()
    try:
        candidate.relative_to(ROOT)
    except ValueError:
        return None
    return candidate if candidate.is_file() and not candidate.is_symlink() else None

class Handler(BaseHTTPRequestHandler):
    server_version = "ASP-CPW/1.0"
    def do_GET(self):
        path = urlsplit(self.path).path
        if path in ("/", "/health"):
            body = b"ASP CPW patch service is online\n"
            self.send_response(HTTPStatus.OK); self.send_header("Content-Type", "text/plain; charset=utf-8")
            self.send_header("Content-Length", str(len(body))); self.end_headers(); self.wfile.write(body); return
        if not path.startswith("/patch/"):
            self.send_error(HTTPStatus.NOT_FOUND); return
        source = resolve_patch(path[7:])
        if source is None:
            self.send_error(HTTPStatus.NOT_FOUND); return
        self.send_response(HTTPStatus.OK); self.send_header("Content-Type", "application/octet-stream")
        self.send_header("Content-Length", str(source.stat().st_size)); self.send_header("Cache-Control", "no-store" if source.name in {"pid", "version", "files.md5"} else "public, max-age=31536000, immutable")
        self.end_headers()
        try:
            with source.open("rb") as stream:
                while True:
                    chunk = stream.read(1024 * 1024)
                    if not chunk: break
                    self.wfile.write(chunk)
        except (BrokenPipeError, ConnectionResetError): pass
    def log_message(self, fmt, *args): return

if __name__ == "__main__":
    ThreadingHTTPServer((HOST, PORT), Handler).serve_forever()
