"""Reproduce the V6.3 documentation evidence from pinned CI artifacts."""
import argparse, hashlib, json, shutil, subprocess
from pathlib import Path
from PIL import Image
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

REV="f2cea2bfe5334fed0e056feb142e56db8fc470c0"
RUN="https://github.com/ViolettaNcl/FleetManagement/actions/runs/"
ROWS="""dashboard-dark|v63-overview-dark.png|0bbb64b1a8fe0163b22a3b62a4cf9ab3d1fc897c2b20d02ef553953709eeab55|Overview — dark theme|Обзор — тёмная тема
dashboard-light|v63-overview-light.png|f7d8bd90967ad7ba2508ecdf7ec874c8bdda4cc2e3c3e5acb6225583e9987e41|Overview — light theme|Обзор — светлая тема
dashboard-ru|v63-overview-ru.png|30bb9552f248ac5b9864b92eb500da2e009ce1730eceeeed5ab6be3213b9b98c|Russian workspace|Русский интерфейс
vehicles-cards|v63-vehicle-cards.png|c0c80fff92df84cee65e6e8572714f0bd107dc4184dc637e84578ad7ae062d04|Vehicle register|Реестр транспорта
vehicle-passport|vehicle-passport.png|35c5557a96e38318bcec6a40e80668287288698eb8ec68ba4405581cd28b2f8e|Vehicle passport|Паспорт автомобиля
drivers|v4-modules/drivers-desktop.png|ce3575d42c6fd2289637451075f5e0ec4c3ce5d6e2bb829b9527354949c6ec97|Driver register|Реестр водителей
calendar|calendar.png|2bca25af37854373c73e7b15ea11361b54b8eab419b616aab1c93882c700156d|Operations calendar|Операционный календарь
dispatch|dispatch.png|de7bc6354f0c13ddbb9d12014a17a453c2dd2b1dc0a90d7633170261917716ff|Dispatch|Диспетчеризация
batch-dispatch|batch-dispatch.png|d79cab3d8cb69eb5f829a153883031b903c855f6d280617f46ffa23908a90df6|Batch dispatch|Пакетная диспетчеризация
route-optimization|optimization.png|6c180ee75edec8a692c0f0248c4ac5176552434e7b04c8281fe0e1179c8c2b31|Route optimization|Оптимизация маршрута
digital-twin|digital-twin.png|7b0f55c43a8034bef05dcb3118c3c0dc51f4971cd4ba09a8ac6cc1336b6308d4|Digital twin simulation|Симуляция цифрового двойника
analytics|analytics.png|7981281062d60dc53faa69cd3911646d3ffcca8d4d9902ac38f1ee89d2f9bd70|Analytics and reporting|Аналитика и отчётность
cost-intelligence|cost-intelligence.png|106122d8a08735d725dd9ffed545269ffaabaa1352a86972d6670fc1ff4690e8|Cost intelligence|Аналитика затрат
machine-learning|intelligence.png|2a8b0b84099bdbd970801d9ff3c23eaea05639c684d8346b66bee7759ff50add|Machine learning lab|Лаборатория ML
maintenance-ensemble|v5-ensemble.png|bf6ee87c8b47534ba8aed0d665731a33ee278d161bbb5c51ceb82445912db275|Maintenance ensemble|Ансамбль моделей ТО
demand-forecast|demand-forecast.png|04b12aab4ee528495eec4257dc52c6d0602d0485ca2f3080517282033fe56169|Demand forecast|Прогноз спроса
model-drift|model-drift.png|e6675280c4b540f49a8e52821ec1c3ea4c081f14a4375840a9ae544ddc9420b0|Model drift|Дрейф модели
model-registry|model-registry.png|148724d6c0067875595b4b4793c6f545094251c332cdea2f9778e493dba9e92f|Local model registry|Локальный реестр моделей
assistant|assistant.png|1de3479c3ac1f0351e4ef8b13eaaa907b9af785daece0ecb8eec984e9c7123af|Rule-based assistant|Детерминированный помощник
maintenance|maintenance.png|0324de77f2f93a938c929d725ee7636c899c88ad6bc8dc43f67a9a8f2f998a4f|Maintenance workflow|Техническое обслуживание
predictions|predictions.png|9e00b3cb00787d9b78fdb616907a031a5346c2d030810b11722090d90021053c|Prediction experiments|Эксперименты прогнозирования
geographic-map|geographic-map.png|4e06982bb6d47c50a1a3daff783789124c38437cc99724792b180e91638ae5b4|Simulated geographic map|Симуляционная карта
mobile-dashboard|v63-overview-mobile.png|06ef16bc206a0a9784576e8b6a80d946960a31312b3193b6013bb6855a9027c8|Mobile overview|Мобильный обзор"""
GRAPHS={
"architecture":("System boundaries",r'''subgraph cluster_public {label="Public Pages / static only"; site[label="Public site"]; web[label="Blazor WASM demo"]; store[label="Browser localStorage",shape=cylinder]; site->web; web->store;} subgraph cluster_local {label="Independent local backend"; console[label="Connected console"]; api[label="ASP.NET Core API"]; ef[label="EF Core / FleetStore"]; db[label="SQLite or SQL Server",shape=cylinder]; hub[label="SignalR"]; console->api[label="same-origin"];api->ef;ef->db;api->hub;hub->console[label="notification"]; } core[label="Shared C# Core",fillcolor="#e9ddff"]; trainer[label="ML.NET CLI"]; registry[label="Local model registry",shape=cylinder]; web->core;api->core;trainer->core;trainer->registry;'''),
"data-model":("Logical persistence relationships",r'''vehicle[label="Vehicle\nId / Plate unique"]; driver[label="Driver\nId / License unique"]; trip[label="Trip\nVehicleId / DriverId\nStart / End"]; work[label="MaintenanceEntry\nVehicleId"]; reserve[label="VehicleReservation\nVehicleId / interval"]; shift[label="DriverShift\nDriverId / interval"]; audit[label="AuditEntry\nHistorical actor/entity references"]; receipt[label="BatchReceipt\nActorId + Key / payload hash"]; vehicle->trip[label="1 : N"]; driver->trip[label="1 : N"];vehicle->work[label="1 : N"];vehicle->reserve[label="1 : N"];driver->shift[label="1 : N"]; audit->receipt[style=invis];'''),
"write-transaction":("Confirmed fleet write",r'''a[label="Authorize + receive UUID"]; b[label="Acquire process semaphore\n+ provider transaction lock"];c[label="Lookup actor/key receipt",shape=diamond];d[label="Replay original response"];e[label="Reject conflict"];f[label="Validate current fleet state",shape=diamond];g[label="Rollback / reject"];h[label="Save records + audit + receipt"];i[label="Commit"];j[label="Notify after commit"];a->b->c;c->d[label="same fingerprint"];c->e[label="different fingerprint"];c->f[label="new key"];f->g[label="invalid"];f->h[label="valid"];h->i->j;'''),
"ml-pipeline":("Local ML experiment lifecycle",r'''a[label="Synthetic dataset\nseed and fingerprints"];b[label="Independent train / validation / test\nvehicle-disjoint where applicable"];c[label="Train-only fitting"];d[label="Validation / threshold / calibration"];e[label="Held-out evaluation + baseline"];f[label="Save model + SHA-256"];g[label="Reload and verify"];h[label="Local versioned registry\nNo automatic production promotion",fillcolor="#e9ddff"];a->b->c->d->e->f->g->h;'''),
"release-pipeline":("GitHub Pages release evidence",r'''a[label="Reviewed feature revision"];b[label="Fast-forward preview branch"];c[label="Build + package + browser smoke"];d[label="Eight CI workflows\nExact SHA + branch\nCodeQL validated SARIF"];e[label="Evidence gate",shape=diamond];f[label="Block release\nFix failure",fillcolor="#fee2e2"];g[label="Pages deploy"];h[label="Live verify: SHA, 36 hashes, UI",fillcolor="#dcfce7"];a->b;b->c;b->d;c->e;d->e;e->f[label="fail"];e->g[label="pass"];g->h;'''),
"route-algorithms":("Euclidean closed-tour optimization",r'''a[label="2–100 unique stops\nfirst stop = depot"];b[label="Original order baseline"];c[label="Nearest neighbor\nID tie-break"];d[label="2-opt\nat most 100 passes"];e[label="Multi-start 2-opt\nat most 8 additional starts"];f[label="Held–Karp exact\nat most 12 stops",fillcolor="#e9ddff"];g[label="Compare closed-tour lengths\nNo real-road routing"];a->b;a->c;b->d;c->d;d->e;a->f[label="small n"];b->g;c->g;d->g;e->g;f->g;'''),
"pwa-update":("PWA revision activation",r'''a[label="New static revision"];b[label="Verify packaged asset hashes",shape=diamond];c[label="Keep working cache",fillcolor="#fee2e2"];d[label="Waiting service worker"];e[label="User confirms reload\nOther tabs closed?",shape=diamond];f[label="Retain previous active worker"];g[label="Activate verified revision\nSaved localStorage preserved",fillcolor="#dcfce7"];a->b;b->c[label="fail"];b->d[label="pass"];d->e;e->f[label="cancel / other tabs"];e->g[label="accept"];''')}
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
def verified(p,digest):
    if sha(p)!=digest: raise ValueError(f"Hash mismatch: {p}")
    return p
def write(p,s):
    p.write_text(s,encoding="utf-8")
def append_once(p,marker,s):
    old=p.read_text(encoding="utf-8")
    if marker not in old: write(p,old.rstrip()+"\n\n"+s+"\n")
def insert_once(p,anchor,marker,s):
    old=p.read_text(encoding="utf-8")
    if marker in old:return
    if old.count(anchor)!=1: raise ValueError(f"Missing unique anchor: {p}")
    write(p,old.replace(anchor,s+"\n\n"+anchor,1))
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for n in ("screenshots","ml","tour"):parser.add_argument("--"+n,type=Path,required=True)
    args=parser.parse_args()
    root=Path("docs/visuals")
    S,D,A,V=[root/n for n in ("screenshots","diagrams","analytics","media")]
    for p in (S,D,A,V):p.mkdir(parents=True,exist_ok=True)
    rows=[line.split("|") for line in ROWS.splitlines()]
    meta={}
    for name,src,digest,en,ru in rows:
        target=S/(name+".png");shutil.copyfile(verified(args.screenshots/src,digest),target)
        with Image.open(target) as im:
            im.load();w,h=im.size
            im.convert("RGB").crop((0,0,w,min(h,900 if w>500 else 844))).save(S/(name+"-viewport.webp"),lossless=True,method=0)
        meta[name]=dict(source=src,source_sha256=digest,width=w,height=h,viewport_sha256=sha(S/(name+"-viewport.webp")))
    write(root/"screenshots-manifest.json",json.dumps(dict(revision=REV,workflow_run=46058985349,artifact_id=15227644469,screenshots=meta),indent=2))
    candidates=list(args.ml.rglob("manifest.json"))
    if len(candidates)!=1:raise ValueError("Expected one ML registry manifest")
    source=verified(candidates[0],"724042ea524669988b355a791b06567147ca88333f93787f126dd4c8373f0ff8")
    m=json.loads(source.read_text())
    assert m["sourceRevision"]==REV and m["seed"]==42 and m["synthetic"] is True
    for task in ("maintenance","eta"):
        for key in ("artifact","fastForestArtifact"):
            if key in m[task]:
                x=m[task][key];verified(source.parent/x["file"],x["sha256"])
    shutil.copyfile(source,A/"source-manifest.json")
    plt.rcParams.update({"font.family":"DejaVu Sans","font.size":11,"axes.spines.top":False,"axes.spines.right":False,"axes.titleweight":"bold","text.color":"#142039","svg.fonttype":"none"})
    colors=["#94a3b8","#6d28d9","#0284c7","#0f766e"]
    def finish(fig,name,note):
        fig.text(.02,.015,"Synthetic evaluation / CI f2cea2b / seed 42 / "+note,fontsize=9,color="#64748b")
        fig.tight_layout(rect=(0,.06,1,.96))
        for fmt in ("svg","png"):fig.savefig(A/(name+"."+fmt),bbox_inches="tight",dpi=160)
        plt.close(fig)
    keys=["baseline","logistic","fastTree","fastForest"]
    for metric in ("F1","Brier","Recall"):
        fig,ax=plt.subplots(figsize=(8,4.4))
        bars=ax.bar(["Baseline","Logistic","FastTree","FastForest"],[m["maintenance"][k][metric] for k in keys],color=colors,width=.6)
        ax.bar_label(bars,fmt="%.3f",padding=4);ax.set_ylim(0,1 if metric!="Brier" else .24)
        ax.set_title("Maintenance — "+metric,loc="left");ax.grid(axis="y",alpha=.15);ax.set_axisbelow(True)
        ax.set_ylabel("Lower is better" if metric=="Brier" else "Higher is better")
        finish(fig,"maintenance-"+metric.lower(),"720 held-out observations; threshold 0.5")
    for k in keys[1:]:
        a=m["maintenance"][k];mat=np.array([[a["TN"],a["FP"]],[a["FN"],a["TP"]]])
        assert mat.sum()==720
        fig,ax=plt.subplots(figsize=(5.4,4.8));ax.imshow(mat,cmap="Purples",vmin=0,vmax=534)
        ax.set_xticks([0,1],["Negative","Positive"]);ax.set_yticks([0,1],["Negative","Positive"])
        ax.set_xlabel("Predicted label");ax.set_ylabel("Actual label");ax.set_title("Confusion matrix — "+k)
        for i in range(2):
            for j in range(2):ax.text(j,i,str(mat[i,j]),ha="center",va="center",fontsize=23,color="white" if mat[i,j]>270 else "#142039",weight="bold")
        finish(fig,"confusion-"+k.lower(),"720 held-out observations")
    for metric in ("Mae","Rmse"):
        fig,ax=plt.subplots(figsize=(8,4.4))
        bars=ax.bar(["Free-flow baseline","Linear regression","FastTree"],[m["eta"][k][metric] for k in ("baseline","linear","fastTree")],color=colors[:3],width=.6)
        ax.bar_label(bars,fmt="%.2f",padding=4);ax.set_ylim(0,max(b.get_height() for b in bars)*1.19)
        ax.set_ylabel("Minutes (lower is better)");ax.set_title("ETA — "+metric.upper(),loc="left");ax.grid(axis="y",alpha=.15);ax.set_axisbelow(True)
        finish(fig,"eta-"+metric.lower(),"400 held-out journeys")
    for task in ("maintenance","eta"):
        d=m[task]["permutationImportance"];f=d["features"]
        values=np.array([i["meanLossIncrease"] for i in f]);lo=np.array([i["minLossIncrease"] for i in f]);hi=np.array([i["maxLossIncrease"] for i in f])
        fig,ax=plt.subplots(figsize=(8.5,4.6))
        ax.barh([i["feature"] for i in f],values,color="#6d28d9",xerr=[values-lo,hi-values],capsize=3)
        ax.invert_yaxis();ax.set_xlabel("Increase in "+d["metric"]);ax.set_title("FastTree permutation importance — "+task,loc="left");ax.grid(axis="x",alpha=.15)
        finish(fig,task+"-permutation","3 shuffles; min/max, not confidence intervals")
    for n,(title,body) in GRAPHS.items():
        dot='digraph G { graph [rankdir=TB,bgcolor="white",pad="0.25",nodesep="0.38",ranksep="0.48",fontname="DejaVu Sans",fontsize=22,label="'+title+'",labelloc=t,fontcolor="#142039"]; node [shape=box,style="rounded,filled",fillcolor="#f1f5ff",color="#8c9fc6",fontname="DejaVu Sans",fontsize=12,fontcolor="#142039",margin="0.18,0.12"]; edge [color="#64748b",fontname="DejaVu Sans",fontsize=10,fontcolor="#334155"]; '+body+' }'
        path=D/(n+".dot");write(path,dot+"\n")
        for fmt in ("svg","png"):subprocess.run(["dot","-T"+fmt,str(path),"-o",str(D/(n+"."+fmt))],check=True)
    video=verified(args.tour/"fleetmanagement-v3-tour.mp4","8bf609dfe457037b3a4ca8803cbd472de5242b2c212dc004def669d1ba812f45")
    shutil.copyfile(video,V/"application-tour.mp4")
    subprocess.run(["ffmpeg","-v","error","-y","-ss","6","-t","8","-i",str(video),"-vf","fps=8,scale=800:-1:flags=lanczos","-loop","0",str(V/"application-tour.gif")],check=True)
    with Image.open(V/"application-tour.gif") as im:
        assert im.n_frames>=16 and im.size[0]==800
    assert len(list(S.glob("*.png")))==23 and len(list(D.glob("*.svg")))==7 and len(list(A.glob("*.svg")))==10
    guides(root,rows,meta,m)
    inventory={str(p.relative_to(root)):dict(bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(root.rglob("*")) if p.is_file() and p.name!="manifest.json"}
    write(root/"manifest.json",json.dumps(dict(revision=REV,files=inventory),indent=2))
    print("Verified 23 exact PNG screenshots, seven diagrams, ten plots, MP4 and GIF.")
def guides(root,rows,meta,m):
    sections=[("Operations","Операции",["dashboard-dark","dashboard-light","dashboard-ru","vehicles-cards","vehicle-passport","drivers","calendar","dispatch","batch-dispatch","maintenance"]),("Simulation and optimization","Симуляция и оптимизация",["route-optimization","digital-twin","geographic-map"]),("Analytics and experiments","Аналитика и эксперименты",["analytics","cost-intelligence","machine-learning","maintenance-ensemble","demand-forecast","model-drift","model-registry","assistant","predictions"]),("Mobile","Мобильная версия",["mobile-dashboard"])]
    captions={x[0]:x[3:] for x in rows}
    notes=[("Public demo is independent of the connected API and ML trainer.","Демо независимо от подключённого API и ML Trainer."),("Logical relationships, not a physical migration.","Логические связи, не физическая миграция."),("Replay, validation and commit paths under the lock.","Повтор, проверка и commit под блокировкой."),("Synthetic training and evaluation, no production deployment.","Синтетическое обучение без промышленного развёртывания."),("Exact branch/SHA evidence before Pages publication.","Подтверждение точной ветки/SHA перед Pages."),("Euclidean tours, not road-network routing.","Евклидовы туры, не дорожная сеть."),("Integrity-verified worker activation.","Активация worker после проверки хешей.")]
    for ru in (False,True):
        s=".ru" if ru else "";o="" if ru else ".ru";idx=int(ru)
        nav=f"[{'English' if ru else 'Русский'}](README{o}.md) · [Engineering handbook](../engineering/README{s}.md) · [Project README](../../README{s}.md)"
        title="Визуальная документация V6.3" if ru else "V6.3 visual engineering evidence"
        intro="23 отдельных скриншота CI, 7 диаграмм, 10 аналитических графиков. Данные синтетические; обрезки viewport обозначены явно." if ru else "23 individual CI screenshots, seven diagrams and ten analytical plots. Synthetic data; viewport crops explicitly labeled."
        write(root/f"README{s}.md",f"# {title}\n\n{nav}\n\n{intro}\n\n**Revision:** {REV}\n\n[Browser CI]({RUN}46058985349) · [ML registry]({RUN}46058985367) · [Pages evidence]({RUN}46058985410)\n\n[{'Галерея' if ru else 'Gallery'}](gallery{s}.md) · [{'Диаграммы' if ru else 'Diagrams'}](diagrams{s}.md) · [{'Результаты ML' if ru else 'ML evidence'}](analytics{s}.md)\n\n![Actual overview](screenshots/dashboard-dark-viewport.webp)\n\n## {'Видеодемонстрация' if ru else 'Recorded walkthrough'}\n\n[MP4 — 48.88 s](media/application-tour.mp4) · [GIF — 8 s excerpt](media/application-tour.gif)\n\n[Manifest](manifest.json) · [Screenshot hashes](screenshots-manifest.json) · [ML manifest](analytics/source-manifest.json)\n")
        g=["# "+("Галерея реальных скриншотов" if ru else "Actual application screenshots"),nav,"Нажмите на отдельный кадр для просмотра полного оригинального PNG. Синтетические данные." if ru else "Click each individual viewport image for its full byte-exact PNG. Synthetic data."]
        for en,rr,names in sections:
            g.append("## "+(rr if ru else en))
            for n in names:
                cap=captions[n][idx];v=meta[n]
                g.append(f"### {cap}\n\n[![{cap}](screenshots/{n}-viewport.webp)](screenshots/{n}.png)\n\n{'Оригинал' if ru else 'Original'}: {v['width']} × {v['height']} px · {v['source']}.")
        write(root/f"gallery{s}.md","\n\n".join(g)+"\n")
        d=["# "+("Инженерные диаграммы" if ru else "Engineering diagrams"),nav,"Редактируемые исходники Graphviz; подписи на английском." if ru else "Editable Graphviz DOT sources."]
        for (n,(title,_)),note in zip(GRAPHS.items(),notes):
            d.append(f"## {title}\n\n{note[idx]}\n\n![{title}](diagrams/{n}.svg)\n\n[PNG](diagrams/{n}.png) · [DOT](diagrams/{n}.dot)")
        write(root/f"diagrams{s}.md","\n\n".join(d)+"\n")
        a=["# "+("Аналитика и результаты ML" if ru else "Analytics and ML evidence"),nav,
            "Синтетический эксперимент, seed 42; 720 наблюдений ТО, 400 поездок ETA. Реальная эффективность не подтверждена." if ru else "Synthetic seed-42 experiment: 720 held-out maintenance observations, 400 ETA journeys. No real-world effectiveness claim.",
            "## Maintenance\n\n| Model | F1 ↑ | Recall ↑ | Brier ↓ | TP | FP | FN | TN |\n| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
        for k in ("baseline","logistic","fastTree","fastForest"):
            q=m["maintenance"][k];a[-1]+=f"\n| {k} | {q['F1']:.3f} | {q['Recall']:.3f} | {q['Brier']:.3f} | {q['TP']} | {q['FP']} | {q['FN']} | {q['TN']} |"
        a.append("Logistic имеет максимальные F1 и recall при пороге 0,5; FastForest — минимальный Brier. Автоматического выбора победителя нет." if ru else "Logistic has the highest F1 and recall at threshold 0.5; FastForest the lowest Brier. No automatic winner selection.")
        for n in ("maintenance-f1","maintenance-brier","maintenance-recall","confusion-logistic","confusion-fasttree","confusion-fastforest","maintenance-permutation"):
            a.append(f"### {n.replace('-',' ').title()}\n\n![{n}](analytics/{n}.svg)\n\n[PNG](analytics/{n}.png)")
        a.append("## ETA\n\n| Model | MAE (min) ↓ | RMSE (min) ↓ | R² |\n| --- | ---: | ---: | ---: |")
        for k in ("baseline","linear","fastTree"):
            q=m["eta"][k];a[-1]+=f"\n| {k} | {q['Mae']:.2f} | {q['Rmse']:.2f} | {q['R2']:.4f} |"
        a.append("Линейная модель лучше FastTree по MAE/RMSE в этом запуске; перенос на реальные дороги не проверен." if ru else "Linear regression outperforms FastTree on MAE/RMSE in this synthetic run; real-road generalization is untested.")
        for n in ("eta-mae","eta-rmse","eta-permutation"):
            a.append(f"### {n.replace('-',' ').title()}\n\n![{n}](analytics/{n}.svg)\n\n[PNG](analytics/{n}.png)")
        a.append("Permutation importance: разброс трёх перестановок, не доверительные интервалы и не причинность. ROC/PR/calibration не построены: нет индивидуальных прогнозов. [Манифест](analytics/source-manifest.json)." if ru else "Permutation importance: range of three shuffles, not confidence intervals or causal attribution. ROC/PR/calibration omitted because individual predictions are unavailable. [Manifest](analytics/source-manifest.json).")
        write(root/f"analytics{s}.md","\n\n".join(a)+"\n")
        heading="## Визуальный обзор приложения" if ru else "## Application in action"
        block=f"""{heading}

[![Verified release](https://img.shields.io/badge/Verified_release-f2cea2b-16803c?style=flat-square)]({RUN}46058985410)
[![Screenshots](https://img.shields.io/badge/Screenshots-23-6d28d9?style=flat-square)](docs/visuals/gallery{s}.md)
[![Diagrams](https://img.shields.io/badge/Diagrams-7-0284c7?style=flat-square)](docs/visuals/diagrams{s}.md)
[![ML charts](https://img.shields.io/badge/ML_charts-10-0f766e?style=flat-square)](docs/visuals/analytics{s}.md)

[![Overview](docs/visuals/screenshots/dashboard-dark-viewport.webp)](docs/visuals/screenshots/dashboard-dark.png)

[![Vehicle register](docs/visuals/screenshots/vehicles-cards-viewport.webp)](docs/visuals/screenshots/vehicles-cards.png)

{'Реальные кадры CI; нажмите для просмотра оригинала. Синтетические данные.' if ru else 'Actual CI captures; click for full original. Synthetic data.'}

<details>
<summary>{'Анимированный фрагмент реальной записи' if ru else 'Animated excerpt of the actual recording'}</summary>

![Actual recorded application excerpt](docs/visuals/media/application-tour.gif)

[{'Полная видеозапись' if ru else 'Full recording'}](docs/visuals/media/application-tour.mp4)

</details>

**[{'Полная визуальная документация' if ru else 'Complete visual evidence'}](docs/visuals/README{s}.md)** · [{'Галерея' if ru else 'Gallery'}](docs/visuals/gallery{s}.md) · [{'Диаграммы' if ru else 'Diagrams'}](docs/visuals/diagrams{s}.md) · [{'Результаты ML' if ru else 'ML evidence'}](docs/visuals/analytics{s}.md)"""
        insert_once(Path(f"README{s}.md"),"## Возможности" if ru else "## Scope",heading,block)
        for name,asset in (("architecture","architecture"),("domain","data-model"),("algorithms","route-algorithms"),("api","write-transaction"),("intelligence","ml-pipeline"),("operations","release-pipeline"),("frontend","pwa-update"),("quality","release-pipeline")):
            head="## Визуальные материалы" if ru else "## Visual evidence"
            block2=f"{head}\n\n![{asset}](../visuals/diagrams/{asset}.svg)\n\n[{'Все диаграммы' if ru else 'All diagrams'}](../visuals/diagrams{s}.md) · [{'Галерея' if ru else 'Gallery'}](../visuals/gallery{s}.md)"
            if name=="intelligence":block2+=f"\n\n![F1 score](../visuals/analytics/maintenance-f1.svg)\n\n[{'Результаты ML' if ru else 'ML evidence'}](../visuals/analytics{s}.md)"
            append_once(Path(f"docs/engineering/{name}{s}.md"),head,block2)
        append_once(Path(f"docs/engineering/README{s}.md"),"../visuals/README",f"## {'Визуальное сопровождение' if ru else 'Visual companion'}\n\n[{'Полная визуальная документация' if ru else 'Complete visual evidence'}](../visuals/README{s}.md).")
    insert_once(Path("CHANGELOG.md"),"## Documentation refinement — V6.3 Enterprise","## Visual documentation — V6.3 Enterprise","## Visual documentation — V6.3 Enterprise\n\n- Add 23 original CI screenshots and individual viewport crops, seven editable technical diagrams, ten evidence-based ML charts, and an actual recorded tour with animated excerpt.\n- Link bilingual visual galleries throughout the engineering handbook. Preserve the approved README banner; no runtime changes.")
if __name__=="__main__":main()
