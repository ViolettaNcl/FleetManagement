"""Protocol tests with a fake local Ollama server; not model inference."""
import importlib.util
import json
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
import threading
import unittest
import urllib.error

spec = importlib.util.spec_from_file_location("local_ai", Path(__file__).resolve().parents[2] / "tools/local_ai.py")
adapter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(adapter)

class Server(BaseHTTPRequestHandler):
    mode = "ok"
    def do_POST(self):
        data = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        self.server.payload = data
        if self.mode == "redirect":
            self.send_response(302)
            self.send_header("Location", "http://127.0.0.1:1/never")
            self.end_headers()
            return
        body = json.dumps({"message": {"content": "Synthetic demonstration only."}}).encode()
        if self.mode == "oversize":
            body = b"x" * (adapter.MAX_RESPONSE + 1)
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)
    def log_message(self, *args):
        pass

class LocalAI(unittest.TestCase):
    def setUp(self):
        self.server = HTTPServer(("127.0.0.1", 0), Server)
        self.original = adapter.URL
        adapter.URL = "http://127.0.0.1:" + str(self.server.server_port) + "/api/chat"
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        Server.mode = "ok"
    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()
        adapter.URL = self.original
    def test_request(self):
        self.assertEqual(adapter.explain("test:local", "overview", "ru"), "Synthetic demonstration only.")
        payload = self.server.payload
        self.assertEqual(payload["model"], "test:local")
        self.assertFalse(payload["stream"])
        self.assertIn("Russian", payload["messages"][0]["content"])
        self.assertIn('"synthetic": true', payload["messages"][1]["content"])
        self.assertNotIn("api_key", payload)
    def test_redirect(self):
        Server.mode = "redirect"
        with self.assertRaises(urllib.error.HTTPError):
            adapter.explain("test", "overview", "en")
    def test_oversize(self):
        Server.mode = "oversize"
        with self.assertRaises(ValueError):
            adapter.explain("test", "overview", "en")
    def test_invalid_model(self):
        with self.assertRaises(ValueError):
            adapter.explain("invalid model\n", "overview", "en")

if __name__ == "__main__":
    unittest.main()
