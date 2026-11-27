"""Verify bilingual documentation structure, links, visual evidence and brand identity."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import unicodedata
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
VIS = ROOT / "docs/visuals"

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def content_without_fences(s):
    out, fence = [], None
    for line in s.splitlines():
        match = re.match(r"^\s{0,3}([\x60~]{3,})", line)
        if match:
            marker = match[1]
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
        elif fence is None:
            out.append(line)
    if fence is not None:
        raise ValueError("Unclosed fenced code")
    return "\n".join(out)

def anchors(s):
    counts, ids = {}, set()
    for title in re.findall(r"^#{1,6}\s+(.+)$", s, re.M):
        base = "".join(c for c in title.lower() if c in "_ -" or unicodedata.category(c)[0] in "LNM").replace(" ", "-")
        n = counts.get(base, 0)
        counts[base] = n+1
        ids.add(base if n==0 else f"{base}-{n}")
    return ids

def main():
    pages = [ROOT / x for x in ("README.md","README.ru.md","SECURITY.md","SECURITY.ru.md")]
    pages += sorted((ROOT/"docs/engineering").glob("*.md")) + sorted(VIS.glob("*.md"))
    errors, checked = [], 0
    for p in pages:
        s = p.read_text(encoding="utf-8")
        try:
            body = content_without_fences(s)
        except ValueError as exc:
            errors.append(f"{p.relative_to(ROOT)}: {exc}")
            continue
        def fail(msg):
            errors.append(f"{p.relative_to(ROOT)}: {msg}")
        if len(re.findall(r"^# ",body,re.M))+len(re.findall(r"<h1>",body))!=1:
            fail("Expected one main title")
        if body.count("<details>")!=body.count("</details>"):
            fail("Unbalanced details")
        if body.count("<summary>")!=body.count("</summary>"):
            fail("Unbalanced summary")
        if re.search(r"<(?:script|style|iframe)\b",body,re.I):
            fail("Unsupported active markup")
        if re.search(r"!\[\]\(",body):
            fail("Missing image alternative text")
        other = p.with_name(p.name[:-6]+".md" if p.name.endswith(".ru.md") else p.stem+".ru.md")
        if not other.is_file() or not re.search(r"(?:\]\(|href=\x22)"+re.escape(other.name)+r"(?:\)|\x22)",body):
            fail("Missing direct language companion link")
        for dest in re.findall(r"\]\(([^)\s]+)\)",body)+re.findall(r'(?:href|src)="([^"]+)"',body):
            if re.match(r"^(https?:|mailto:)",dest):
                continue
            path,sep,frag=dest.partition("#")
            target=(p.parent/unquote(path)).resolve() if path else p
            checked+=1
            if not target.is_relative_to(ROOT) or not target.exists():
                fail(f"Missing local target {dest}")
            elif not path and frag and unquote(frag) not in anchors(body):
                fail(f"Missing heading anchor {frag}")
    manifest=json.loads((VIS/"manifest.json").read_text(encoding="utf-8"))
    actual={p.relative_to(VIS).as_posix() for p in VIS.rglob("*") if p.is_file() and p!=VIS/"manifest.json"}
    if actual!=set(manifest["files"]):
        errors.append("Visual inventory differs from manifest")
    for name,info in manifest["files"].items():
        p=VIS/name
        if not p.is_file() or p.stat().st_size!=info["bytes"] or digest(p)!=info["sha256"]:
            errors.append("Visual manifest mismatch: "+name)
    for folder,pattern,n in [("screenshots","*.png",23),("screenshots","*.webp",23),("diagrams","*.svg",7),("analytics","*.svg",10),("media","*.gif",1),("media","*.mp4",1)]:
        if len(list((VIS/folder).glob(pattern)))!=n:
            errors.append(f"Unexpected {folder}/{pattern} count")
    for suffix in ("",".ru"):
        gallery=(VIS/f"gallery{suffix}.md").read_text(encoding="utf-8")
        for screenshot in (VIS/"screenshots").glob("*.png"):
            if f"](screenshots/{screenshot.name})" not in gallery:
                errors.append(f"Missing screenshot {screenshot.name} in gallery{suffix}")
    banner="docs/assets/brand/fleet-readme-banner.webp"
    gitsha=subprocess.check_output(["git","hash-object",banner],cwd=ROOT,text=True).strip()
    if gitsha!="3460b43b4394505b9a3b306a31ebafc3e49daa74":
        errors.append("Approved banner changed")
    for name in ("README.md","README.ru.md"):
        if f'src="{banner}"' not in (ROOT/name).read_text():
            errors.append("Banner missing from "+name)
    if errors:
        raise SystemExit("\n".join(errors))
    print(f"PASS: {len(pages)} bilingual pages, {checked} local links, {len(actual)} visual manifest entries, approved banner")

if __name__=="__main__":
    main()
