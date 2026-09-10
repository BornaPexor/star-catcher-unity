import hashlib
import json
import os
import sqlite3
import threading
from datetime import UTC, datetime
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

DATABASE = "/data/star_catcher.sqlite3"
DATABASE_URL = os.environ.get("DATABASE_URL", "")
SALT = os.environ.get("IP_HASH_SALT", "change-this-before-public-release")
ALLOWED_ORIGIN = os.environ.get("ALLOWED_ORIGIN", "https://bornapexor.github.io")
LOCK = threading.Lock()
POSTGRES = bool(DATABASE_URL)

if POSTGRES:
    import psycopg
    from psycopg.rows import dict_row


def now():
    return datetime.now(UTC).isoformat()


def connection():
    if POSTGRES:
        return psycopg.connect(DATABASE_URL, row_factory=dict_row)
    db = sqlite3.connect(DATABASE)
    db.row_factory = sqlite3.Row
    return db


def execute(db, statement, parameters=()):
    if POSTGRES:
        statement = statement.replace("?", "%s")
    return db.execute(statement, parameters)


def initialise():
    with LOCK, connection() as db:
        execute(db, """CREATE TABLE IF NOT EXISTS sessions (
            id TEXT PRIMARY KEY, visitor_id TEXT NOT NULL, started_at TEXT NOT NULL,
            ended_at TEXT, duration_seconds INTEGER NOT NULL DEFAULT 0,
            score INTEGER NOT NULL DEFAULT 0, device TEXT NOT NULL,
            ip_masked TEXT NOT NULL, ip_hash TEXT NOT NULL
        )""")


def mask_ip(value):
    if ":" in value:
        return ":".join(value.split(":")[:3]) + "::/48"
    parts = value.split(".")
    return ".".join(parts[:3] + ["0"]) if len(parts) == 4 else "unknown"


def request_ip(headers, fallback):
    forwarded = headers.get("X-Forwarded-For", "")
    return forwarded.split(",")[0].strip() if forwarded else fallback


class Handler(BaseHTTPRequestHandler):
    server_version = "StarCatcherAnalytics/1.0"

    def log_message(self, fmt, *args):
        return

    def send_json(self, status, value):
        body = json.dumps(value, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        origin = self.headers.get("Origin")
        if origin == ALLOWED_ORIGIN:
            self.send_header("Access-Control-Allow-Origin", origin)
            self.send_header("Vary", "Origin")
        self.end_headers()
        self.wfile.write(body)

    def do_OPTIONS(self):
        origin = self.headers.get("Origin")
        if origin != ALLOWED_ORIGIN:
            return self.send_json(HTTPStatus.FORBIDDEN, {"error": "origin not allowed"})
        self.send_response(HTTPStatus.NO_CONTENT)
        self.send_header("Access-Control-Allow-Origin", origin)
        self.send_header("Access-Control-Allow-Methods", "POST, PATCH, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.send_header("Access-Control-Max-Age", "86400")
        self.send_header("Vary", "Origin")
        self.end_headers()

    def read_json(self):
        length = int(self.headers.get("Content-Length", "0"))
        if length < 1 or length > 4096:
            raise ValueError("Invalid request size")
        return json.loads(self.rfile.read(length).decode("utf-8"))

    def do_GET(self):
        if self.path == "/health":
            return self.send_json(HTTPStatus.OK, {"status": "ok"})
        if self.path == "/sessions":
            with LOCK, connection() as db:
                rows = execute(db, """SELECT started_at, ended_at, duration_seconds, score,
                    device, ip_masked FROM sessions ORDER BY started_at DESC LIMIT 200""").fetchall()
                totals = execute(db, "SELECT COUNT(*) AS rounds, COUNT(DISTINCT visitor_id) AS players, COALESCE(MAX(score),0) AS high_score FROM sessions").fetchone()
            return self.send_json(HTTPStatus.OK, {"sessions": [dict(row) for row in rows], "summary": dict(totals)})
        if self.path == "/admin":
            return self.send_html()
        return self.send_json(HTTPStatus.NOT_FOUND, {"error": "not found"})

    def do_POST(self):
        if self.path != "/sessions":
            return self.send_json(HTTPStatus.NOT_FOUND, {"error": "not found"})
        try:
            data = self.read_json()
            session_id = str(data["sessionId"])[:64]
            visitor_id = str(data["visitorId"])[:64]
            device = str(data.get("device", "Unknown browser"))[:160]
            if not session_id or not visitor_id:
                raise ValueError("Missing identifiers")
        except (ValueError, KeyError, TypeError, json.JSONDecodeError):
            return self.send_json(HTTPStatus.BAD_REQUEST, {"error": "invalid session"})
        ip = request_ip(self.headers, self.client_address[0])
        masked = mask_ip(ip)
        digest = hashlib.sha256((SALT + ip).encode("utf-8")).hexdigest()
        with LOCK, connection() as db:
            if POSTGRES:
                execute(db, "INSERT INTO sessions (id, visitor_id, started_at, device, ip_masked, ip_hash) VALUES (?, ?, ?, ?, ?, ?) ON CONFLICT (id) DO NOTHING", (session_id, visitor_id, now(), device, masked, digest))
            else:
                execute(db, "INSERT OR IGNORE INTO sessions (id, visitor_id, started_at, device, ip_masked, ip_hash) VALUES (?, ?, ?, ?, ?, ?)", (session_id, visitor_id, now(), device, masked, digest))
        return self.send_json(HTTPStatus.CREATED, {"sessionId": session_id})

    def do_PATCH(self):
        if not self.path.startswith("/sessions/"):
            return self.send_json(HTTPStatus.NOT_FOUND, {"error": "not found"})
        try:
            data = self.read_json()
            score = max(0, min(int(data.get("score", 0)), 1000000))
            duration = max(0, min(int(data.get("durationSeconds", 0)), 3600))
        except (ValueError, TypeError, json.JSONDecodeError):
            return self.send_json(HTTPStatus.BAD_REQUEST, {"error": "invalid result"})
        session_id = self.path.rsplit("/", 1)[-1][:64]
        with LOCK, connection() as db:
            result = execute(db, "UPDATE sessions SET ended_at = ?, score = ?, duration_seconds = ? WHERE id = ?", (now(), score, duration, session_id))
        return self.send_json(HTTPStatus.OK, {"updated": result.rowcount == 1})

    def send_html(self):
        page = """<!doctype html><html lang='fa' dir='rtl'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>آمار Star Catcher</title><style>body{margin:0;background:#091019;color:#eaf1f6;font:15px Tahoma,Arial}main{max-width:1100px;margin:auto;padding:28px}h1{color:#d5fa76}.cards{display:flex;gap:12px;flex-wrap:wrap}.card{background:#142332;padding:16px;border-radius:12px;min-width:150px}strong{display:block;font-size:27px;margin-top:7px}table{width:100%;border-collapse:collapse;margin-top:25px;background:#101e2b}th,td{padding:12px;text-align:right;border-bottom:1px solid #2d4150}th{color:#9fb2c1}@media(max-width:650px){table{font-size:12px}th,td{padding:8px}}</style><main><h1>آمار بازیکن‌ها</h1><div class='cards' id='cards'></div><table><thead><tr><th>شروع</th><th>دستگاه</th><th>IP ماسک‌شده</th><th>مدت</th><th>امتیاز</th></tr></thead><tbody id='rows'></tbody></table></main><script>const fa=n=>Number(n).toLocaleString('fa-IR');fetch('/sessions').then(r=>r.json()).then(d=>{document.querySelector('#cards').innerHTML=`<div class='card'>دورهای ثبت‌شده<strong>${fa(d.summary.rounds)}</strong></div><div class='card'>بازیکن یکتا<strong>${fa(d.summary.players)}</strong></div><div class='card'>بیشترین امتیاز<strong>${fa(d.summary.high_score)}</strong></div>`;document.querySelector('#rows').innerHTML=d.sessions.map(s=>`<tr><td>${new Date(s.started_at).toLocaleString('fa-IR')}</td><td>${s.device}</td><td>${s.ip_masked}</td><td>${fa(s.duration_seconds)} ثانیه</td><td>${fa(s.score)}</td></tr>`).join('')})</script>"""
        body = page.encode("utf-8")
        self.send_response(HTTPStatus.OK)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        origin = self.headers.get("Origin")
        if origin == ALLOWED_ORIGIN:
            self.send_header("Access-Control-Allow-Origin", origin)
            self.send_header("Vary", "Origin")
        self.end_headers()
        self.wfile.write(body)


initialise()
ThreadingHTTPServer(("0.0.0.0", int(os.environ.get("PORT", "8090"))), Handler).serve_forever()
