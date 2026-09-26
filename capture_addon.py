import os
from mitmproxy import http, tls

OUTDIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "flows")
os.makedirs(OUTDIR, exist_ok=True)
_counter = {"n": 0}

def _safe(p: str) -> str:
    return "".join(c if c.isalnum() or c in "-._" else "_" for c in p)[:80] or "root"

def tls_established_client(data: tls.TlsData):
    print(f"[TLS] client handshake OK  (ALPN={data.conn.alpn})")

def tls_failed_client(data: tls.TlsData):
    print(f"[TLS-FAIL client] {getattr(data.conn, 'error', None)}")

def request(flow: http.HTTPFlow):
    r = flow.request
    print(f"[REQ] {r.method} {r.pretty_url}  ({len(r.raw_content or b'')} body bytes)")

def response(flow: http.HTTPFlow):
    _counter["n"] += 1
    n = _counter["n"]
    req, resp = flow.request, flow.response
    path = _safe(req.path.split("?")[0].strip("/"))
    d = os.path.join(OUTDIR, f"{n:02d}_{req.method}_{path}")
    os.makedirs(d, exist_ok=True)
    with open(os.path.join(d, "request.txt"), "w", encoding="utf-8", errors="replace") as f:
        f.write(f"{req.method} {req.url}\n\n")
        for k, v in req.headers.items():
            f.write(f"{k}: {v}\n")
    with open(os.path.join(d, "request.body.bin"), "wb") as f:
        f.write(req.raw_content or b"")
    with open(os.path.join(d, "response.txt"), "w", encoding="utf-8", errors="replace") as f:
        f.write(f"HTTP {resp.status_code}\n\n")
        for k, v in resp.headers.items():
            f.write(f"{k}: {v}\n")
    with open(os.path.join(d, "response.body.bin"), "wb") as f:
        f.write(resp.raw_content or b"")
    ctype = resp.headers.get("content-type", "")
    blen = len(resp.raw_content or b"")
    print(f"[CAP {n:02d}] {req.method} {req.path.split('?')[0]}  ->  {resp.status_code}  {ctype}  {blen} bytes")

def error(flow):
    try:
        print(f"[ERR] {flow.request.method} {flow.request.path}  ->  {flow.error}")
    except Exception:
        print(f"[ERR] {getattr(flow, 'error', 'unknown')}")
