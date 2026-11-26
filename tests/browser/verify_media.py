"""Verify actual Playwright deliverables, not a substitute for visual editorial review."""
import hashlib
import json
from pathlib import Path
import struct
import subprocess

out = Path("portfolio-video")
screens = Path("browser-results")
required = ("overview.png", "overview-dark.png", "mobile.png", "assistant-ru.png",
            "digital-twin.png", "model-drift.png", "vehicle-passport.png")
report = {"schema": 1, "videos": [], "screenshots": [], "manualVisualReviewRequired": True}
for name in ("fleetmanagement-v3-tour.webm", "fleetmanagement-v3-tour.mp4"):
    path = out / name
    metadata = json.loads(subprocess.check_output(
        ["ffprobe", "-v", "error", "-show_format", "-show_streams", "-of", "json", str(path)]))
    videos = [s for s in metadata["streams"] if s["codec_type"] == "video"]
    assert len(videos) == 1, name
    width, height = videos[0]["width"], videos[0]["height"]
    duration = float(metadata["format"]["duration"])
    assert width == 1280 and height == 800 and 25 <= duration <= 300, (name, width, height, duration)
    assert path.stat().st_size > 100_000, name
    subprocess.run(["ffmpeg", "-v", "error", "-xerror", "-i", str(path), "-map", "0:v:0", "-f", "null", "-"], check=True)
    report["videos"].append({"file": name, "durationSeconds": duration, "width": width, "height": height,
                             "bytes": path.stat().st_size, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
for name in required:
    path = screens / name
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n" and data[12:16] == b"IHDR", name
    width, height = struct.unpack(">II", data[16:24])
    assert width >= 390 and height >= 300 and len(data) > 2000, name
    if name == "mobile.png":
        assert width == 390, (name, width)
    report["screenshots"].append({"file": name, "width": width, "height": height,
                                  "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()})
(out / "media-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("PASS: required screenshots, video dimensions/duration and full video decoding")
