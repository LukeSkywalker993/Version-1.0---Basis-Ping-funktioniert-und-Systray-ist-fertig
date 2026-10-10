#!/usr/bin/env python3
"""Minimaler Empfänger für Apple-Health-Uploads aus einem iOS-Kurzbefehl.

Start:  HEALTH_TOKEN=geheim python3 server.py
Läuft auf HTTP (Port 8080). HTTPS übernimmt ein Reverse-Proxy (Caddy/nginx),
siehe README.md.
"""
import hmac
import json
import os
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

TOKEN = os.environ.get("HEALTH_TOKEN", "")
DATA_DIR = Path(os.environ.get("HEALTH_DATA_DIR", "data"))
PORT = int(os.environ.get("PORT", "8080"))
MAX_BYTES = 20 * 1024 * 1024


class Handler(BaseHTTPRequestHandler):
    def _reply(self, code, msg):
        body = json.dumps({"status": msg}).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_POST(self):
        if self.path != "/upload":
            return self._reply(404, "not found")
        auth = self.headers.get("Authorization", "")
        if not TOKEN or not hmac.compare_digest(auth, f"Bearer {TOKEN}"):
            return self._reply(401, "unauthorized")
        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0 or length > MAX_BYTES:
            return self._reply(413, "bad size")
        raw = self.rfile.read(length)
        # Kurzbefehle senden JSON oder Formular-/Rohtext; wir speichern roh.
        DATA_DIR.mkdir(parents=True, exist_ok=True)
        name = time.strftime("%Y-%m-%d_%H%M%S") + ".json"
        try:
            json.loads(raw)
        except ValueError:
            name = name.replace(".json", ".txt")
        (DATA_DIR / name).write_bytes(raw)
        self._reply(200, "ok")

    def log_message(self, fmt, *args):
        print(f"{self.address_string()} {fmt % args}")


if __name__ == "__main__":
    if not TOKEN:
        raise SystemExit("HEALTH_TOKEN nicht gesetzt")
    print(f"Lausche auf Port {PORT}, Daten in {DATA_DIR.resolve()}")
    ThreadingHTTPServer(("0.0.0.0", PORT), Handler).serve_forever()
