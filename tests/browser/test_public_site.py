"""Offline unit tests for deterministic static SEO and PWA packaging."""
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urljoin

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/"tools"))
from build_public_site import BASE,PAGES,generate
from package_pwa import package

class Inspector(HTMLParser):
    def __init__(self):
        super().__init__();self.h1=0;self.meta={};self.links=[];self.anchors=[]
    def handle_starttag(self,tag,attributes):
        a=dict(attributes)
        if tag=="h1":self.h1+=1
        if tag=="meta" and "name" in a:self.meta[a["name"]]=a.get("content")
        if tag=="link":self.links.append(a)
        if tag=="a" and "href" in a:self.anchors.append(a["href"])

class SiteTest(unittest.TestCase):
    def test_static_site(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            generate(root)
            urls=[e.text for e in ET.parse(root/"sitemap.xml").getroot().findall(".//{http://www.sitemaps.org/schemas/sitemap/0.9}loc")]
            self.assertEqual(len(urls),20);self.assertEqual(len(set(urls)),20)
            descriptions=set()
            for url in urls:
                page=root/url.removeprefix(BASE)/"index.html"
                inspector=Inspector();inspector.feed(page.read_text(encoding="utf-8"))
                self.assertEqual(inspector.h1,1)
                self.assertNotIn(inspector.meta["description"],descriptions);descriptions.add(inspector.meta["description"])
                self.assertEqual([x["href"] for x in inspector.links if x.get("rel")=="canonical"],[url])
                self.assertEqual(inspector.meta["robots"],"index,follow")
                for link in inspector.anchors:
                    target=urljoin(url,link)
                    lang="ru" if url.startswith(BASE+"ru/") else "en"
                    if target.startswith(BASE) and "#" not in target:self.assertIn(target,urls+[BASE+"app/?lang="+lang])
                self.assertIn(BASE+"app/?lang="+("ru" if url.startswith(BASE+"ru/") else "en"),[urljoin(url,link) for link in inspector.anchors])
                for alt in [x for x in inspector.links if x.get("hreflang")]:
                    self.assertIn(alt["href"],urls)
    def test_scoped_pwa(self):
        with tempfile.TemporaryDirectory() as tmp:
            app=Path(tmp)
            (app/"index.html").write_text("<html><head></head><body></body></html>")
            (app/"preview-build.json").write_text(json.dumps({"revision":"a"*40}))
            (app/"example.wasm").write_bytes(b"fixture")
            (app/"example.wasm.br").write_bytes(b"compressed")
            package(app)
            manifest=json.loads((app/"manifest.webmanifest").read_text())
            self.assertEqual(manifest["scope"],"./")
            self.assertEqual(manifest["start_url"],"./")
            sw=(app/"sw.js").read_text()
            self.assertIn(hashlib.sha256(b"fixture").hexdigest(),sw)
            self.assertNotIn("example.wasm.br",sw)
            self.assertIn(hashlib.sha256((app/"index.html").read_bytes()).hexdigest(),sw)
            for size in (192,512):
                raw=(app/f"icon-{size}.png").read_bytes()
                self.assertEqual(raw[:8],b"\x89PNG\r\n\x1a\n")
                self.assertEqual(int.from_bytes(raw[16:20],"big"),size)
            subprocess.run(["node","--check",str(app/"sw.js")],check=True)
            subprocess.run(["node","--check",str(app/"pwa.js")],check=True)

if __name__=="__main__":unittest.main()
