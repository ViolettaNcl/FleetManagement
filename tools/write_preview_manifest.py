"""Write public non-sensitive build provenance to the static output only."""
import json
import os
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
sha = os.environ["GITHUB_SHA"]
if not re.fullmatch(r"[0-9a-f]{40}", sha):
    raise SystemExit("Exact source revision required")
if not (root / "index.html").is_file() or not (root / "_framework").is_dir():
    raise SystemExit("Not a published WebAssembly directory")
(root / "preview-build.json").write_text(json.dumps({
    "schema": 1, "revision": sha, "mode": "synthetic-browser-demo"
}) + "\n", encoding="utf-8")
