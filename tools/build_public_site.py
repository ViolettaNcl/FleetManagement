"""Package public HTML separately from the interactive demo, for GitHub Pages only."""
import csv
import html
import json
import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

BASE = "https://violettancl.github.io/FleetManagement/"
ROOT = Path(__file__).resolve().parents[1]
PAGES = json.loads((ROOT / "site/pages.json").read_text(encoding="utf-8"))
def esc(value):
    return html.escape(str(value), quote=True)
def route(path, lang):
    return ("ru/" if lang == "ru" else "") + path
def relative_link(dest, current):
    depth = len(current.strip("/").split("/")) if current else 0
    return "../" * depth + (dest or "./")
def page_link(dest, current):
    return esc(relative_link(dest, current))
def verification():
    result=[]
    for key, name in (("GOOGLE_SITE_VERIFICATION","google-site-verification"),("YANDEX_VERIFICATION","yandex-verification")):
        token=os.getenv(key,"")
        if token:
            if not re.fullmatch(r"[A-Za-z0-9_-]{1,200}",token):
                raise ValueError("Invalid verification value: "+key)
            result.append(f'<meta name="{name}" content="{esc(token)}">')
    return "".join(result)
def generate(root):
    root.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(ROOT/"site/site.css", root/"site.css")
    rows=[]
    for lang in ("en","ru"):
        ru=lang=="ru"
        for path,art,primary_en,secondary_en,primary_ru,secondary_ru,en,rus in PAGES:
            title,h1,desc,h2a,p1,h2b,p2=rus if ru else en
            current=route(path,lang)
            out=root/current
            out.mkdir(parents=True,exist_ok=True)
            href=lambda target:page_link(target,current)
            canonical=BASE+current
            enurl=BASE+route(path,"en"); ruurl=BASE+route(path,"ru")
            app=href("app/")
            image=href("app/assets/neon/"+art+".webp")
            social=BASE+"app/assets/neon/"+art+".webp"
            home="Главная" if ru else "Home"
            features="Возможности" if ru else "Features"
            docs="Руководство" if ru else "Guide"
            open_demo="Открыть демо" if ru else "Open interactive demo"
            more="Возможности проекта" if ru else "Explore the project"
            skip="Перейти к содержанию" if ru else "Skip to content"
            other=href(route(path,"en" if ru else "ru"))
            notice=("Синтетические данные · без живого GPS · изменения остаются в браузере." if ru else "Synthetic data · no live GPS · changes stay in your browser.")
            links=[(home,""),(features,"features/"),(docs,"docs/")]
            nav="".join(f'<a href="{href(route(dest,lang))}"'+(' aria-current="page"' if path==dest else '')+f'>{esc(label)}</a>' for label,dest in links)
            cards=[]
            for item in PAGES:
                d,a,*rest=item
                if d==path or not d.startswith("features/") or d=="features/":continue
                data=item[7] if ru else item[6]
                cards.append(f'<a class="feature" href="{href(route(d,lang))}"><img src="{href("app/assets/neon/"+a+".webp")}" width="960" height="540" loading="lazy" decoding="async" alt=""><strong>{esc(data[1])}</strong><p>{esc(data[2])}</p><span>{"Подробнее" if ru else "Explore feature"} →</span></a>')
            graph=[{"@type":"WebSite" if not path else "WebPage","name":h1,"url":canonical,"description":desc,"inLanguage":lang}]
            trail=[(home,route("",lang))]
            if path.startswith("features/") and path!="features/":
                trail.append((features,route("features/",lang)))
            if path:
                trail.append((h1,current))
                graph.append({"@type":"BreadcrumbList","itemListElement":[{"@type":"ListItem","position":i+1,"name":label,"item":BASE+dest} for i,(label,dest) in enumerate(trail)]})
            else:
                graph.append({"@type":"SoftwareApplication","name":"FleetManagement","applicationCategory":"BusinessApplication","operatingSystem":"Web browser","url":BASE+"app/","description":desc,"isAccessibleForFree":True})
            breadcrumb=" / ".join(f'<a href="{href(dest)}">{esc(label)}</a>' if i<len(trail)-1 else esc(label) for i,(label,dest) in enumerate(trail))
            structured=json.dumps({"@context":"https://schema.org","@graph":graph},ensure_ascii=False).replace("<","\\u003c")
            content=f"""<!doctype html>
<html lang="{lang}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{esc(title)}</title>
<meta name="description" content="{esc(desc)}"><meta name="robots" content="index,follow"><meta name="theme-color" content="#090b12">
<link rel="canonical" href="{canonical}"><link rel="alternate" hreflang="en" href="{enurl}"><link rel="alternate" hreflang="ru" href="{ruurl}"><link rel="alternate" hreflang="x-default" href="{enurl}">
<meta property="og:type" content="website"><meta property="og:title" content="{esc(title)}"><meta property="og:description" content="{esc(desc)}"><meta property="og:url" content="{canonical}"><meta property="og:image" content="{social}"><meta property="og:locale" content="{'ru_RU' if ru else 'en_US'}">
<meta name="twitter:card" content="summary_large_image"><meta name="twitter:title" content="{esc(title)}"><meta name="twitter:description" content="{esc(desc)}"><meta name="twitter:image" content="{social}">{verification()}
<link rel="icon" href="{href('app/favicon.svg')}" type="image/svg+xml"><link rel="stylesheet" href="{href('site.css')}"><script type="application/ld+json">{structured}</script></head>
<body><a class="skip" href="#content">{skip}</a><header><div class="wrap top"><a class="brand" href="{href(route('',lang))}">Fleet<span>Management</span></a><nav aria-label="{esc('Навигация' if ru else 'Main navigation')}">{nav}<a href="{other}" hreflang="{'en' if ru else 'ru'}">{'EN' if ru else 'RU'}</a><a class="cta" href="{app}">{open_demo}</a></nav></div></header>
<main class="wrap" id="content"><p class="crumb">{breadcrumb}</p><section class="hero"><div><span class="eyebrow">FLEET INTELLIGENCE · .NET</span><h1>{esc(h1)}</h1><p>{esc(desc)}</p><div class="actions"><a class="cta" href="{app}">{open_demo}</a><a class="cta secondary" href="{href(route('docs/',lang))}">{docs}</a></div></div><img src="{image}" alt="" width="960" height="600" fetchpriority="high" decoding="async"></section><div class="notice">{notice}</div>
<div class="content"><article><h2>{esc(h2a)}</h2><p>{esc(p1)}</p></article><article><h2>{esc(h2b)}</h2><p>{esc(p2)}</p></article></div>
<section><h2>{more}</h2><div class="features">{''.join(cards)}</div></section></main>
<footer><div class="wrap"><span>FleetManagement · {'Инженерный проект' if ru else 'Engineering portfolio'}</span><a href="{href(route('about/',lang))}">{'О проекте' if ru else 'About'}</a><a href="https://github.com/ViolettaNcl/FleetManagement">GitHub</a></div></footer></body></html>"""
            (out/"index.html").write_text(content,encoding="utf-8")
            rows.append([canonical,lang,primary_ru if ru else primary_en,secondary_ru if ru else secondary_en,"informational / demo evaluation","unverified","Wialon / Geotab public feature terminology; manual draft","2026-11-26",title,desc,h1,h2a+" | "+h2b,BASE+route("features/",lang),BASE+"app/"])
    ns="http://www.sitemaps.org/schemas/sitemap/0.9"
    ET.register_namespace("",ns)
    tree=ET.Element("{"+ns+"}urlset")
    for row in rows:
        ET.SubElement(ET.SubElement(tree,"{"+ns+"}url"),"{"+ns+"}loc").text=row[0]
    ET.ElementTree(tree).write(root/"sitemap.xml",encoding="utf-8",xml_declaration=True)
    (root/"robots.txt").write_text("# Project subpath copy; authoritative rules belong at domain root.\nUser-agent: *\nAllow: /\nSitemap: "+BASE+"sitemap.xml\n",encoding="utf-8")
    (root/"404.html").write_text(f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><meta name="robots" content="noindex"><title>Page not found | FleetManagement</title><link rel="stylesheet" href="/FleetManagement/site.css"></head><body><main class="wrap"><h1>Page not found</h1><p>The requested page is unavailable.</p><a class="cta" href="{BASE}">Return to FleetManagement</a></main></body></html>',encoding="utf-8")
    with (root/"semantic-map.csv").open("w",encoding="utf-8",newline="") as fp:
        w=csv.writer(fp);w.writerow(["url","language","primary","secondary","intent","volume","source","research_date","title","description","h1","h2","feature_hub","demo"]);w.writerows(rows)
def main():
    source,target=map(Path,sys.argv[1:3])
    if not (source/"_framework").is_dir() or target.exists():
        raise SystemExit("Require a published WASM source and a new target directory.")
    target.mkdir(parents=True)
    shutil.copytree(source,target/"app")
    shutil.copyfile(source/"preview-build.json",target/"preview-build.json")
    index=target/"app/index.html"
    text=index.read_text(encoding="utf-8")
    text=text.replace("</head>",'<meta name="robots" content="noindex,follow"></head>')
    index.write_text(text,encoding="utf-8")
    from package_pwa import package
    app_index=target/'app/index.html'
    app_index.write_text(app_index.read_text(encoding='utf-8').replace('</head>','<link rel="stylesheet" href="css/pwa.css"></head>'),encoding='utf-8')
    package(target/'app')
    generate(target)
    (target/".nojekyll").touch()
    print(f"Generated {len(PAGES)*2} static public pages and preserved interactive app.")
if __name__=="__main__":
    main()
