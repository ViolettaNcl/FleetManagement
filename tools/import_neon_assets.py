"""Import a verified owner-supplied design ZIP into a published static app."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

REPO = Path(__file__).resolve().parents[1]
ORIGINAL = "77befc845438cbfe343f996449438749e7eb9791a64b33443bdfb7b76717c2a2"
OPTIMIZED = "74c583f8ff38a2ac4ad240f40b7220f20109b370ffdf53ba42341953dc9b741c"
SLOTS = {"hero":"01-hero-background","routes":"02-route-optimization","twin":"03-digital-twin",
         "maintenance":"04-predictive-maintenance","assistant":"05-ai-assistant","analytics":"06-analytics-background",
         "abstract":"07-abstract-background","command-center":"09-bonus-command-center"}
MAX_ARCHIVE = 24 * 1024 * 1024
MAX_IMAGE = 300 * 1024

def install(output: Path, source_dir: Path):
    if not (output / "index.html").is_file() or not (output / "css").is_dir():
        raise ValueError("Expected published static app with index.html and css/")
    sources = sorted(source_dir.glob("*.zip"))
    if len(sources) > 1:
        raise ValueError("Keep exactly one verified design archive")
    target = output / "assets" / "neon"
    target.mkdir(parents=True, exist_ok=True)
    css_path = output / "css" / "neon-assets.css"
    manifest = {"schema":1,"mode":"fallback","archiveSha256":None,"files":[]}
    css = "/* No owner archive present: procedural fallback. */\n"
    if sources:
        source = sources[0]
        if source.stat().st_size > MAX_ARCHIVE:
            raise ValueError("Design ZIP exceeds 24 MiB")
        digest = hashlib.sha256(source.read_bytes()).hexdigest()
        if digest not in (ORIGINAL, OPTIMIZED):
            raise ValueError("Unrecognized design ZIP; use the exact supplied or optimized archive")
        verified = {}
        with zipfile.ZipFile(source) as archive:
            for slot, stem in SLOTS.items():
                member = ("webp/" + stem + ".webp") if digest == ORIGINAL else ("src/FleetManagement.Web/wwwroot/assets/neon/" + slot + ".webp")
                if archive.namelist().count(member) != 1:
                    raise ValueError("Missing or duplicate required asset " + slot)
                info = archive.getinfo(member)
                if not 16 <= info.file_size <= MAX_IMAGE:
                    raise ValueError("Unexpected asset size " + slot)
                with archive.open(info) as stream:
                    data = stream.read(MAX_IMAGE + 1)
                if len(data) != info.file_size or data[:4] != b"RIFF" or data[8:12] != b"WEBP":
                    raise ValueError("Invalid WebP " + slot)
                verified[slot] = data
        for slot, data in verified.items():
            name = slot + ".webp"
            (target / name).write_bytes(data)
            manifest["files"].append({"name":name,"bytes":len(data),"sha256":hashlib.sha256(data).hexdigest()})
        manifest["mode"] = "imported"
        manifest["archiveSha256"] = digest
        css = ":root{" + ";".join(f'--asset-{slot}:url("../assets/neon/{slot}.webp")' for slot in verified) + ";}\n"
        css += ".art-routes .art-glyph,.art-twin .art-glyph,.art-maintenance .art-glyph,.art-assistant .art-glyph,.module-watermark{display:none}\n"
    else:
        for slot in SLOTS:
            (target / (slot + ".webp")).unlink(missing_ok=True)
    css_path.write_text(css,encoding="utf-8")
    (target / "manifest.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
    return manifest

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output",type=Path)
    args = parser.parse_args()
    result = install(args.output, REPO / "design")
    print(f'Artwork mode: {result["mode"]}; {len(result["files"])} images.')
