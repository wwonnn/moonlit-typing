from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import os
root=Path(__file__).resolve().parents[1]/'docs'
os.chdir(root)
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.unityweb':'application/octet-stream'}
    def end_headers(self):
        self.send_header('Cache-Control','no-cache')
        super().end_headers()
print('Moonlit Press: http://127.0.0.1:8797',flush=True)
ThreadingHTTPServer(('127.0.0.1',8797),Handler).serve_forever()
