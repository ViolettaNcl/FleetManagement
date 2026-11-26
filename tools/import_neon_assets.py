"""Validate the ten committed owner WebPs in the published output; write provenance."""
import argparse
import hashlib
import json
from pathlib import Path

ASSETS = {
    "hero": "4fa8bde1e2bc9c28fa500f4b48780d3ce5dc1ecf",
    "routes": "8f1e3d0270a16ada3bb52e44d6b6f40cd934d9db",
    "twin": "9690ac89a07c660cc6450571686e61987e02f3ae",
    "maintenance": "1cb85a8d502ca6fc860c8d96cf38c2aea0410cba",
    "assistant": "52a22d1524196c8fbe5eb7bbdf3bc58a7bc12fc8",
    "analytics": "e3bb28dcee838312db95c024710652b7c0677a19",
    "abstract": "e014294b0b1d0408a757eeca1d13c9ee5ef7c611",
    "command-center": "c92df09e1b63bc7a8e2780f3b0c0634cd2018b26",
    "ui-mockup": "f7177c8ef8d4f3fb2c13cb3ee0e59a7cd8986e3e",
    "style-board": "0e2ee84da036b87cdaeedb39254b7372d724ddbe"
}
MAX_IMAGE = 300 * 1024

def install(output: Path):
    if not (output / "index.html").is_file() or not (output / "css/neon-assets.css").is_file():
        raise ValueError("Expected published app with committed artwork stylesheet")
    target = output / "assets/neon"
    records = []
    for slot, expected in ASSETS.items():
        name = slot + ".webp"
        path = target / name
        if not path.is_file() or not 16 <= path.stat().st_size <= MAX_IMAGE:
            raise ValueError("Missing or invalid asset " + name)
        data = path.read_bytes()
        if data[:4] != b"RIFF" or data[8:12] != b"WEBP":
            raise ValueError("Invalid WebP " + name)
        sha = hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()
        if sha != expected:
            raise ValueError("Artwork integrity mismatch " + name)
        records.append({"name": name, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()})
    css = (output / "css/neon-assets.css").read_text(encoding="utf-8")
    if any(f"../assets/neon/{slot}.webp" not in css for slot in ASSETS):
        raise ValueError("Artwork stylesheet is incomplete")
    result = {"schema": 2, "mode": "committed", "archiveSha256": None, "files": records}
    (target / "manifest.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    result = install(args.output)
    print(f'Verified artwork: {len(result["files"])} committed images.')
