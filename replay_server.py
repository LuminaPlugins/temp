import os, ssl, json, time, datetime, http.server, socketserver

DYNAMIC_LIST = True                                                

BASE = os.path.dirname(os.path.abspath(__file__))
FLOWS = os.path.join(BASE, "flows")
CERT = os.path.join(BASE, "crl", "leaf-combined.pem")
LOGF = os.path.join(BASE, "replay.log")
CFGDIR = os.path.join(BASE, "config_store")
os.makedirs(CFGDIR, exist_ok=True)
INDEX_F = os.path.join(CFGDIR, "index.json")
DATA_F  = os.path.join(CFGDIR, "data.json")

DROP = {"transfer-encoding", "connection", "content-length", "date", "server",
        "cf-ray", "cf-cache-status", "nel", "report-to", "alt-svc"}

def log(msg):
    line = f"{datetime.datetime.now():%H:%M:%S.%f} {msg}"
    print(line)
    try:
        with open(LOGF, "a", encoding="utf-8") as f: f.write(line + "\n")
    except Exception: pass

def _flow_dirs():
    for d in sorted(os.listdir(FLOWS)):
        fd = os.path.join(FLOWS, d)
        if os.path.isdir(fd) and os.path.exists(os.path.join(fd, "request.txt")):
            yield d, fd

def _path_of(fd):
    first = open(os.path.join(fd, "request.txt"), encoding="utf-8", errors="replace").readline()
    url = first.split(" ", 1)[1].strip() if " " in first else first.strip()
    p = ("/" + url.split("://", 1)[1].split("/", 1)[1]) if "://" in url else url
    return p.split("?")[0]

def _headers(fd):
    rt = open(os.path.join(fd, "response.txt"), encoding="utf-8", errors="replace").read().splitlines()
    status = int(rt[0].split()[1])
    headers = [(l.split(":", 1)[0].strip(), l.split(":", 1)[1].strip())
               for l in rt[2:] if ":" in l and l.split(":", 1)[0].strip().lower() not in DROP]
    return status, headers

def _req_id(reqbody):
    try:
        o = json.loads(reqbody.decode("utf-8", "replace"))
        if isinstance(o.get("id"), int): return o["id"]
        return next((v for v in o.values() if isinstance(v, int)), None)
    except Exception:
        return None

TABLE = {}                                                                                         
CFGDATA_VERBATIM = {}                                                                  
CFGDATA_FALLBACK = None
LIST_TEMPLATE = {"data": "{}", "maxPages": "1", "challenge": "", "id": ""}
DATA_CHALLENGE = ""
SEED_INDEX = {}
SEED_DATA = {}

for name, fd in _flow_dirs():
    path = _path_of(fd)
    status, headers = _headers(fd)
    body = open(os.path.join(fd, "response.body.bin"), "rb").read()
    reqbody = open(os.path.join(fd, "request.body.bin"), "rb").read()
    if "modGetConfigData" in name:
        cid = _req_id(reqbody)
        if cid is not None:
            CFGDATA_VERBATIM[cid] = (status, headers, body)
            try:
                o = json.loads(body.decode("utf-8", "replace"))
                SEED_DATA[str(cid)] = o.get("data", "")
                DATA_CHALLENGE = o.get("challenge", DATA_CHALLENGE)
            except Exception: pass
        CFGDATA_FALLBACK = CFGDATA_FALLBACK or (status, headers, body)
    elif path == "/modGetConfigListNew":
        TABLE[path] = (status, headers, body)
        try:
            outer = json.loads(body.decode("utf-8", "replace"))
            LIST_TEMPLATE = dict(outer)
            SEED_INDEX = json.loads(outer["data"])
        except Exception: pass
    else:
        TABLE[path] = (status, headers, body)

if os.path.exists(INDEX_F) and os.path.exists(DATA_F):
    INDEX = json.load(open(INDEX_F, encoding="utf-8"))
    DATA  = json.load(open(DATA_F, encoding="utf-8"))
else:
    INDEX = dict(SEED_INDEX)
    DATA  = dict(SEED_DATA)
    json.dump(INDEX, open(INDEX_F, "w", encoding="utf-8"))
    json.dump(DATA, open(DATA_F, "w", encoding="utf-8"))

def save_store():
    json.dump(INDEX, open(INDEX_F, "w", encoding="utf-8"))
    json.dump(DATA, open(DATA_F, "w", encoding="utf-8"))

_OWNER_UID, _OWNER_USER, _TIER = 15355, "vm3q", 2
for m in INDEX.values():
    try: _OWNER_UID, _OWNER_USER, _TIER = m[2], m[3], m[7]; break
    except Exception: pass

def _compact(o): return json.dumps(o, separators=(",", ":")).encode("utf-8")
def _new_id():
    import random
    while True:
        i = str(random.randint(1000, 99999))
        if i not in INDEX: return i

CONFIG_EPS = {"/modGetConfigListNew", "/modGetConfigData", "/modCreateConfig",
              "/modEditConfig", "/modDeleteConfig"}

def handle_config(path, reqbody):
    now = datetime.datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
    if path == "/modGetConfigListNew":
        if not DYNAMIC_LIST:
            return TABLE.get(path)
        resp = dict(LIST_TEMPLATE)
        resp["data"] = json.dumps(INDEX, separators=(",", ":"))
        return (200, [("Content-Type", "application/json")], _compact(resp))
    if path == "/modGetConfigData":
        cid = _req_id(reqbody)
        s = str(cid)
        edited = s in DATA and (cid not in CFGDATA_VERBATIM or DATA[s] != SEED_DATA.get(s))
        if edited:                                                          
            return (200, [("Content-Type", "application/json")],
                    _compact({"data": DATA[s], "challenge": DATA_CHALLENGE}))
        if cid in CFGDATA_VERBATIM:                                              
            return CFGDATA_VERBATIM[cid]
        return CFGDATA_FALLBACK
    if path == "/modCreateConfig":
        o = json.loads(reqbody.decode("utf-8", "replace"))
        cid = _new_id()
        INDEX[cid] = [o.get("name", "config"), o.get("desc", ""), _OWNER_UID, _OWNER_USER,
                      0, now, o.get("tags", "[]"), _TIER]
        DATA[cid] = o.get("data", "{}")
        save_store(); log(f"[CONFIG] created id={cid} name={o.get('name')!r} total={len(INDEX)}")
        return TABLE.get(path)
    if path == "/modEditConfig":
        o = json.loads(reqbody.decode("utf-8", "replace"))
        cid = str(o.get("id"))
        if cid in INDEX:
            if "data" in o: DATA[cid] = o["data"]
            if o.get("name"): INDEX[cid][0] = o["name"]
            if o.get("desc"): INDEX[cid][1] = o["desc"]
            INDEX[cid][5] = now
            save_store(); log(f"[CONFIG] edited id={cid}")
        return TABLE.get(path)
    if path == "/modDeleteConfig":
        cid = str(_req_id(reqbody))
        INDEX.pop(cid, None); DATA.pop(cid, None)
        save_store(); log(f"[CONFIG] deleted id={cid} total={len(INDEX)}")
        return TABLE.get(path)
    return None

class H(http.server.BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"
    def _serve(self):
        t0 = time.time()
        clen = int(self.headers.get("Content-Length", 0) or 0)
        reqbody = self.rfile.read(clen) if clen else b""
        path = self.path.split("?")[0]
        entry = None
        if path in CONFIG_EPS:
            try: entry = handle_config(path, reqbody)
            except Exception as e: log(f"[CFG-ERR] {path}: {e}"); entry = TABLE.get(path)
        else:
            entry = TABLE.get(path)
        if entry is None:
            log(f"[MISS] {self.command} {path} reqlen={clen}")
            self.send_response(404); self.send_header("Content-Length", "0"); self.end_headers(); return
        status, headers, body = entry
        self.send_response(status)
        for k, v in headers: self.send_header(k, v)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        try:
            self.wfile.write(body)
            dt = (time.time() - t0) * 1000
            tag = "[CFG]" if path in CONFIG_EPS else "[SERVE]"
            log(f"{tag} {self.command} {path} -> {status} {len(body)}b {dt:.0f}ms")
        except Exception as e:
            log(f"[WERR] {path}: {e}")
    def do_POST(self): self._serve()
    def do_GET(self): self._serve()
    def log_message(self, *a): pass

class Server(socketserver.ThreadingMixIn, http.server.HTTPServer):
    daemon_threads = True

if __name__ == "__main__":
    PORT = int(os.environ.get("REPLAY_PORT", "443"))
    srv = Server(("127.0.0.1", PORT), H)
    ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    ctx.load_cert_chain(CERT)
    srv.socket = ctx.wrap_socket(srv.socket, server_side=True)
    print(f"[*] Replay server on https://127.0.0.1:{PORT}  "
          f"({len(TABLE)} endpoints, {len(INDEX)} configs, DYNAMIC_LIST={DYNAMIC_LIST})")
    srv.serve_forever()
