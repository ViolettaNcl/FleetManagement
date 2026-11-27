
/* Reference GFM layout test. GitHub itself controls its production rendering.
   External badges are intentionally not a dependency of this test. */
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import {createRequire} from 'node:module';
import MarkdownIt from 'markdown-it';
import {chromium} from 'playwright';
const require=createRequire(import.meta.url);
const root=path.resolve(process.argv[2]);
const artifacts=path.resolve(process.argv[3]);
await fs.mkdir(artifacts,{recursive:true});
const md=new MarkdownIt({html:true,linkify:true,typographer:false});
const styles=await fs.readFile(require.resolve('github-markdown-css/github-markdown.css'),'utf8');
const pages=['README.md','README.ru.md','docs/engineering/api.md','docs/engineering/api.ru.md','docs/visuals/gallery.md','docs/visuals/gallery.ru.md','docs/visuals/analytics.md','docs/visuals/analytics.ru.md'];
const mime={'.png':'image/png','.webp':'image/webp','.svg':'image/svg+xml','.gif':'image/gif'};
const server=http.createServer(async(req,res)=>{
 try{
  const pathname=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname);
  const target=path.resolve(root,'.'+pathname);
  if(!target.startsWith(root+path.sep)){res.writeHead(403).end();return}
  if(path.extname(target)==='.md'){
   const content=await fs.readFile(target,'utf8');
   const lang=target.endsWith('.ru.md')?'ru':'en';
   res.writeHead(200,{'Content-Type':'text/html; charset=utf-8'});
   res.end('<!doctype html><html lang="'+lang+'"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><style>'+styles+'\nbody{margin:0;background:var(--bgColor-default,#fff)}.markdown-body{box-sizing:border-box;min-width:0;max-width:1012px;margin:0 auto;padding:32px}@media(max-width:767px){.markdown-body{padding:16px}}</style></head><body><main class="markdown-body">'+md.render(content)+'</main></body></html>');
  }else{
   if(!mime[path.extname(target)]){res.writeHead(404).end();return}
   res.writeHead(200,{'Content-Type':mime[path.extname(target)]});res.end(await fs.readFile(target));
  }
 }catch(e){if(!res.headersSent)res.writeHead(404);res.end()}
});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const origin='http://127.0.0.1:'+server.address().port;
const browser=await chromium.launch({headless:true});
let count=0;
try{
 for(const theme of ['light','dark']){
  const context=await browser.newContext({colorScheme:theme});
  await context.route('**/*',route=>route.request().url().startsWith(origin+'/')?route.continue():route.abort());
  const page=await context.newPage();
  for(const name of pages){
   for(const width of [320,390,768,1440]){
    await page.setViewportSize({width,height:900});
    await page.goto(origin+'/'+name);
    const images=await page.locator('img').evaluateAll(async imgs=>{
      const local=imgs.filter(x=>x.src.startsWith(location.origin+'/')&&x.getClientRects().length);
      await Promise.all(local.map(x=>x.decode()));
      return local.map(x=>({src:x.getAttribute('src'),width:x.naturalWidth,shown:x.getBoundingClientRect().width,alt:x.alt}));
    });
    assert.ok(images.length>0,name+' has no visible local images');
    assert.ok(images.every(x=>x.width>0&&x.shown<=width&&x.alt.trim()),name+' image width or alt');
    const size=await page.evaluate(()=>({document:document.documentElement.scrollWidth,viewport:innerWidth,main:document.querySelector('main').scrollWidth,client:document.querySelector('main').clientWidth}));
    assert.ok(size.document<=size.viewport+1,JSON.stringify({name,width,theme,size}));
    const details=page.locator('details').first();
    if(await details.count()){
      await details.locator('summary').click();
      assert.equal(await details.getAttribute('open'),'');
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),name+' expanded block overflow');
      await details.locator('summary').click();
    }
    if(name.startsWith('README')&&(width===390||width===1440)){
      await page.screenshot({path:path.join(artifacts,name.replace('.md','')+'-'+theme+'-'+width+'.png')});
    }
    count++;
   }
  }
  await context.close();
 }
 console.log('PASS: '+count+' reference GFM layouts; 320/390/768/1440 px, light/dark, local images and expandable blocks. Mermaid native rendering and live GitHub UI are outside this harness.');
}finally{await browser.close();await new Promise(r=>server.close(r))}
