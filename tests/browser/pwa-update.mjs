// Test the production service-worker template against three independently packaged fixture revisions.
// The production WASM offline path is exercised separately by seo-pwa.mjs.
import {chromium} from 'playwright';
import {mkdtemp,readFile,writeFile,mkdir,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join,resolve} from 'node:path';
import {spawnSync} from 'node:child_process';
import {createServer} from 'node:http';
import assert from 'node:assert/strict';
const folder=await mkdtemp(join(tmpdir(),'fleet-pwa-update-'));
const revisions=['a'.repeat(40),'b'.repeat(40),'c'.repeat(40)];
const versions=[];
let active=0,broken=false;
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.webmanifest':'application/manifest+json','.png':'image/png'};
for(const [i,revision] of revisions.entries()){
  const dir=join(folder,String(i));await mkdir(dir);
  await writeFile(join(dir,'preview-build.json'),JSON.stringify({revision}));
  await writeFile(join(dir,'index.html'),'<html lang="en"><head><meta charset="utf-8"><title>Fixture</title></head><body><h1>Revision '+i+'</h1><script src="fixture.js"></script></body></html>');
  await writeFile(join(dir,'fixture.js'),"window.fixtureVersion="+i+";");
  const process=spawnSync('python3',['-c','import sys;sys.path.insert(0,"tools");from package_pwa import package;package(sys.argv[1])',dir],{encoding:'utf8'});
  assert.equal(process.status,0,process.stderr);versions.push(dir);
}
const server=createServer(async(req,res)=>{
  const path=new URL(req.url,'http://localhost').pathname;
  if(!path.startsWith('/app/')){res.writeHead(404);res.end();return;}
  const file=path.slice(5)||'index.html';
  if(!/^[a-zA-Z0-9._-]+$/.test(file)){res.writeHead(404);res.end();return;}
  try{
    const body=broken&&file==='fixture.js'?Buffer.from('stale fixture bytes'):await readFile(join(versions[active],file));
    res.writeHead(200,{'Content-Type':types[file.slice(file.lastIndexOf('.'))]||'application/octet-stream','Cache-Control':'no-store'});
    res.end(body);
  }catch{res.writeHead(404);res.end();}
});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const url='http://127.0.0.1:'+server.address().port+'/app/';
const browser=await chromium.launch();
try{
  const context=await browser.newContext({serviceWorkers:'allow'});
  const page=await context.newPage();
  await page.goto(url);
  await page.waitForFunction(()=>!!navigator.serviceWorker.controller,null,{timeout:30000});
  await page.evaluate(()=>localStorage.setItem('fleet-fixture-record','saved-record'));
  active=1;
  await page.evaluate(async()=>{const reg=await navigator.serviceWorker.getRegistration();await reg.update();});
  const update=page.locator('[data-pwa-update] button');
  await update.waitFor({timeout:30000});
  page.once('dialog',d=>d.dismiss());await update.click();
  assert.equal(await page.locator('h1').innerText(),'Revision 0');
  const other=await context.newPage();
  await other.goto(url);
  await other.waitForFunction(()=>!!navigator.serviceWorker.controller);
  await other.getByRole('heading',{name:'Revision 0'}).waitFor();
  page.once('dialog',d=>d.accept());await update.click();
  await page.getByRole('status').filter({hasText:'Close other demo tabs'}).waitFor({timeout:30000});
  assert.equal(await page.locator('h1').innerText(),'Revision 0');
  assert.equal(await other.locator('h1').innerText(),'Revision 0');
  assert.equal(await update.isEnabled(),true);
  await other.close();
  page.once('dialog',d=>d.accept());await update.click();
  await page.getByRole('heading',{name:'Revision 1'}).waitFor({timeout:30000});
  assert.equal(await page.evaluate(()=>localStorage.getItem('fleet-fixture-record')),'saved-record');
  await context.setOffline(true);await page.reload();
  await page.getByRole('heading',{name:'Revision 1'}).waitFor({timeout:30000});
  await context.setOffline(false);
  active=2;broken=true;
  const rejected=await page.evaluate(async()=>{
    const reg=await navigator.serviceWorker.getRegistration();
    return new Promise(async(resolve,reject)=>{
      const timer=setTimeout(()=>reject(new Error('Bad revision was not rejected')),30000);
      const observe=worker=>{if(!worker)return;const check=()=>{if(worker.state==='redundant'){clearTimeout(timer);resolve(true);}else if(worker.state==='installed'||worker.state==='activated'){clearTimeout(timer);resolve(false);}};worker.addEventListener('statechange',check);check();};
      reg.addEventListener('updatefound',()=>observe(reg.installing),{once:true});
      await reg.update();observe(reg.installing);
    });
  });
  assert.equal(rejected,true);
  await page.reload();await page.getByRole('heading',{name:'Revision 1'}).waitFor();
  assert.equal(await page.evaluate(()=>localStorage.getItem('fleet-fixture-record')),'saved-record');
  console.log('PASS: production PWA worker fixture revision update, dismiss/accept, multi-tab guard, retry, saved data, offline and stale-byte recovery');
}finally{await browser.close();await new Promise(r=>server.close(r));await rm(folder,{recursive:true,force:true});}
