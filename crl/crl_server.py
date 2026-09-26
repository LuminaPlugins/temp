import http.server, os

CRL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "mitm.crl")

class H(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        try:
            with open(CRL, "rb") as f:
                data = f.read()
            self.send_response(200)
            self.send_header("Content-Type", "application/pkix-crl")
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "max-age=86400")
            self.end_headers()
            self.wfile.write(data)
            print(f"[CRL] served {self.path} ({len(data)} bytes)")
        except Exception as e:
            self.send_error(500, str(e))
    def log_message(self, *a):
        pass

if __name__ == "__main__":
    http.server.HTTPServer(("127.0.0.1", 8081), H).serve_forever()
