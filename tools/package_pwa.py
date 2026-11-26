"""Generate a scoped, exact-revision offline cache for public synthetic demo files only."""
import json
import hashlib
import re
import struct
import zlib
from pathlib import Path
from urllib.parse import quote

def icon(path, size):
    # Vector-like geometric fleet mark, no external artwork or font dependency.
    def chunk(name,data):
        return struct.pack(">I",len(data))+name+data+struct.pack(">I",zlib.crc32(name+data)&0xffffffff)
    rows=[]
    for y in range(size):
        row=bytearray([0])
        for x in range(size):
            nx=(x+.5)/size;ny=(y+.5)/size
            rgb=(18,16,34)
            if .19<=nx<=.81 and .19<=ny<=.81: rgb=(105,65,198)
            # Safe central maskable area: truck body, cab and wheels.
            if .30<=nx<=.56 and .37<=ny<=.60: rgb=(244,238,255)
            if .56<=nx<=.70 and .44<=ny<=.60: rgb=(244,238,255)
            if .60<=nx<=.67 and .46<=ny<=.51: rgb=(105,65,198)
            if (nx-.38)**2+(ny-.62)**2<.042**2 or (nx-.63)**2+(ny-.62)**2<.042**2: rgb=(38,27,73)
            row.extend(rgb)
        rows.append(row)
    path.write_bytes(b"\x89PNG\r\n\x1a\n"+chunk(b"IHDR",struct.pack(">IIBBBBB",size,size,8,2,0,0,0))+chunk(b"IDAT",zlib.compress(b"".join(rows),9))+chunk(b"IEND",b""))
def package(app):
    app=Path(app)
    rev=json.loads((app/"preview-build.json").read_text())["revision"]
    if not re.fullmatch(r"[a-f0-9]{40}",rev):raise ValueError("Invalid revision")
    for size in (192,512):icon(app/f"icon-{size}.png",size)
    manifest={"name":"FleetManagement — interactive demo","short_name":"FleetManagement","id":"./","start_url":"./","scope":"./","display":"standalone","background_color":"#090b12","theme_color":"#131722","description":"Synthetic fleet operations demo; no live GPS or connected API.","icons":[{"src":f"icon-{n}.png","sizes":f"{n}x{n}","type":"image/png","purpose":"any maskable"} for n in (192,512)]}
    (app/"manifest.webmanifest").write_text(json.dumps(manifest),encoding="utf-8")
    (app/"pwa.js").write_text("""if('serviceWorker' in navigator&&window.isSecureContext){
  const showUpdate=worker=>{
    if(!worker||document.querySelector('[data-pwa-update]'))return;
    const bar=document.createElement('aside');bar.dataset.pwaUpdate='true';bar.className='pwa-update';
    const text=document.createElement('span');text.textContent='A new version is ready / Доступна новая версия';
    const button=document.createElement('button');button.textContent='Update / Обновить';
    text.setAttribute('role','status');
    button.addEventListener('click',()=>{
      if(!confirm('Reload now? Unsaved form edits will be lost. / Перезагрузить? Несохранённые изменения будут потеряны.'))return;
      button.disabled=true;
      text.textContent='Checking open demo tabs / Проверяем открытые вкладки';
      const channel=new MessageChannel();
      const reload=()=>{clearTimeout(timer);channel.port1.close();location.reload();};
      const fail=message=>{
        clearTimeout(timer);
        navigator.serviceWorker.removeEventListener('controllerchange',reload);
        channel.port1.close();
        text.textContent=message;
        button.disabled=false;
      };
      const timer=setTimeout(()=>fail('Update not confirmed. Retry or reopen the demo. / Обновление не подтверждено. Повторите или откройте демо заново.'),30000);
      navigator.serviceWorker.addEventListener('controllerchange',reload,{once:true});
      channel.port1.onmessage=event=>{
        if(event.data==='BLOCKED_TABS')fail('Close other demo tabs, then retry. / Закройте другие вкладки демо и повторите.');
        else if(event.data==='FAILED')fail('Update failed. Please retry. / Ошибка обновления. Повторите попытку.');
        else if(event.data==='ACCEPTED')text.textContent='Activating update / Активируем обновление';
      };
      try{worker.postMessage('ACTIVATE_UPDATE',[channel.port2]);}
      catch{fail('Update unavailable. Reopen the demo. / Обновление недоступно. Откройте демо заново.');}
    });
    bar.append(text,button);document.body.append(bar);
  };
  navigator.serviceWorker.register('./sw.js',{scope:'./',updateViaCache:'none'}).then(reg=>{
    if(reg.waiting&&navigator.serviceWorker.controller)showUpdate(reg.waiting);
    reg.addEventListener('updatefound',()=>{const worker=reg.installing;if(worker)worker.addEventListener('statechange',()=>{if(worker.state==='installed'&&navigator.serviceWorker.controller)showUpdate(worker);});});
  }).catch(()=>{});
}
""",encoding="utf-8")
    index=app/"index.html"
    index.write_text(index.read_text(encoding="utf-8").replace("</head>",'<link rel="manifest" href="manifest.webmanifest"><link rel="apple-touch-icon" href="icon-192.png"></head>').replace("</body>",'<script src="pwa.js" defer></script></body>'),encoding="utf-8")
    # Published files only: no API, token, browser database, query responses, or connected workspace.
    files=[]
    for path in sorted(app.rglob("*")):
        if path.is_file() and path.suffix not in (".br",".gz",".map") and not path.name.startswith(".") and path.name!="sw.js":
            files.append({"path":"./"+quote(path.relative_to(app).as_posix(),safe="/-._~"),"sha256":hashlib.sha256(path.read_bytes()).hexdigest()})
    worker="""const REV=__REV__,FILES=__FILES__;
const scope=self.registration.scope,prefix='fleet-public-demo:'+scope+':',name=prefix+REV;
const allowed=new Set(FILES.map(file=>new URL(file.path,scope).href));
self.addEventListener('install',event=>event.waitUntil((async()=>{
  const cache=await caches.open(name);
  try{
    // Bounded parallel fetches; exact hashes prevent stale CDN bytes from becoming an offline release.
    for(let i=0;i<FILES.length;i+=6)await Promise.all(FILES.slice(i,i+6).map(async file=>{
      const req=new Request(new URL(file.path,scope),{cache:'reload'});
      const response=await fetch(req);
      if(!response.ok||response.redirected)throw Error('Missing published asset');
      const hash=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await response.clone().arrayBuffer())),b=>b.toString(16).padStart(2,'0')).join('');
      if(hash!==file.sha256)throw Error('Published asset revision mismatch');
      await cache.put(req,response);
    }));
  }catch(error){await caches.delete(name);throw error;}
})()));
self.addEventListener('activate',event=>event.waitUntil((async()=>{for(const key of await caches.keys())if(key.startsWith(prefix)&&key!==name)await caches.delete(key);await self.clients.claim();})()));
self.addEventListener('message',event=>{
  if(event.data!=='ACTIVATE_UPDATE'||!event.source?.url?.startsWith(scope)||!event.ports[0])return;
  const reply=event.ports[0];
  event.waitUntil((async()=>{
    try{
      // Do not knowingly replace the active worker beneath another open demo tab.
      // A newly opened tab can still race activation; this is a best-effort guard.
      const windows=await self.clients.matchAll({type:'window',includeUncontrolled:true});
      if(windows.some(client=>client.id!==event.source.id&&client.url.startsWith(scope))){
        reply.postMessage('BLOCKED_TABS');return;
      }
      reply.postMessage('ACCEPTED');
      await self.skipWaiting();
    }catch{reply.postMessage('FAILED');}
  })());
});
self.addEventListener('fetch',event=>{
  if(event.request.method!=='GET')return;
  const url=new URL(event.request.url);
  if(url.origin!==location.origin||!url.href.startsWith(scope))return;
  if(event.request.mode==='navigate'&&(url.pathname===new URL(scope).pathname||url.pathname===new URL('index.html',scope).pathname)){
    event.respondWith(caches.open(name).then(cache=>cache.match(new URL('index.html',scope).href)).then(response=>response||fetch(event.request)));return;
  }
  if(allowed.has(url.href))event.respondWith(caches.open(name).then(cache=>cache.match(url.href)).then(response=>response||fetch(event.request)));
});
"""
    (app/"sw.js").write_text(worker.replace("__REV__",json.dumps(rev)).replace("__FILES__",json.dumps(files)),encoding="utf-8")
    print(f"PWA cache: {len(files)} static assets; scope app/ only.")
