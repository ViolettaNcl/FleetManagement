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
artwork = json.loads((root / "assets" / "neon" / "manifest.json").read_text(encoding="utf-8"))
(root / "preview-build.json").write_text(json.dumps({
    "schema": 1, "revision": sha, "mode": "synthetic-browser-demo", "artworkMode": artwork["mode"], "artworkArchive": artwork["archiveSha256"]
}) + "\n", encoding="utf-8")

# Match the visible workspace build identifier to the actual Pages artifact.
(root / "js" / "build-info.js").write_text("window.fleetBuildRevision=()=>" + json.dumps(sha) + ";\n", encoding="utf-8")
