// Real-app regression for the V6.3 Enterprise design, not a mock screenshot.
import {chromium} from 'playwright';
import assert from 'node:assert/strict';
import {mkdir,readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
const base='http://127.0.0.1:8080/FleetManagement/';
const manifest=JSON.parse(await readFile(new URL('../../docs/fleet-approved-assets.json',import.meta.url),'utf8'));
const modules={Vehicles:'vehicle-management',Drivers:'drivers','Vehicle Passport':'vehicle-passport',Dispatch:'dispatch','Batch Dispatch':'dispatch',Calendar:'calendar','Resource Planning':'resource-planning',Maintenance:'maintenance',Tracking:'tracking','Geo Map':'tracking',Analytics:'analytics','Cost Intelligence':'cost-intelligence',Assistant:'assistant',Optimization:'routes','Multi Dispatch':'dispatch','Digital Twin':'twin','What-if':'twin',Intelligence:'machine-learning',Predictions:'predictions','Demand Forecast':'demand-forecast','Model Registry':'model-registry','Data Quality':'model-registry','Model Drift':'model-drift',Notifications:'notifications'};
const browser=await chromium.launch({args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
try{
 const page=await browser.newPage({viewport:{width:1440,height:900},reducedMotion:'reduce'});
 const errors=[],bad=[];
 page.on('pageerror',e=>errors.push(e.message));
 page.on('response',r=>{if(r.url().startsWith(base)&&r.status()>=400)bad.push([r.status(),r.url()]);});
 await page.addInitScript(()=>{if(!localStorage.getItem('fleetmanagement.preferences.navigation.v6'))localStorage.setItem('fleetmanagement.preferences.navigation.v6','all');});
 await page.goto(base);
 await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
 const content=page.locator('#fleet-content');
 const go=async(name)=>{
   if(!(await page.locator('#workspace-nav').isVisible()))await page.locator('.mobile-nav-toggle').click();
   await page.locator('#workspace-nav').getByRole('button',{name,exact:true}).click();
   await page.waitForFunction(n=>document.querySelector('#fleet-content')?.dataset.module===n,name,{timeout:30000});
 };
 const fit=async(label)=>assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`Horizontal page overflow: ${label}`);
 const art=async(name,key)=>{
   const banner=content.locator('.enterprise-module');
   await banner.waitFor();assert.equal(await banner.getAttribute('data-art'),key,name);
   const img=banner.locator('img');
   await img.scrollIntoViewIfNeeded();
   assert.equal(await img.getAttribute('src'),manifest[key].path,name);
   assert.ok(await img.evaluate(async el=>{await el.decode();return el.naturalWidth>0;}),name);
 };
 await mkdir('browser-results',{recursive:true});
 assert.equal(Object.keys(manifest).length,36);
 for(const [name,asset] of Object.entries(manifest)){
   const r=await page.request.get(base+asset.path);assert.ok(r.ok(),name);
   assert.equal(createHash('sha256').update(await r.body()).digest('hex'),asset.sha256,name);
 }
 await fit('Overview desktop');
 await page.screenshot({path:'browser-results/v63-overview-dark.png',fullPage:true});
 for(const [name,key] of Object.entries(modules)){await go(name);await art(name,key);await fit(name+' desktop');}
 await go('Vehicles');
 await page.getByRole('button',{name:'Card view',exact:true}).click();
 const cards=content.locator('.vehicle-record');
 assert.ok(await cards.count()>0);
 const sources=await cards.locator('.approved-vehicle-image').evaluateAll(imgs=>imgs.map(i=>i.getAttribute('src')));
 assert.equal(sources.length,await cards.count());
 for(const src of sources)assert.match(src,/^assets\/vehicles\/vehicle-(mercedes-actros|volvo-fh|man-tgx|ford-transit|default)\.webp$/);
 await page.screenshot({path:'browser-results/v63-vehicle-cards.png',fullPage:true});
 await go('Help & settings');
 await page.getByLabel('Appearance',{exact:true}).selectOption('light');
 await go('Overview'); await fit('Overview light');
 assert.equal(await page.locator('.demo-shell.dark-theme').count(),0);
 await page.screenshot({path:'browser-results/v63-overview-light.png',fullPage:true});
 for(const width of [768,390,320]){
   await page.setViewportSize({width,height:844});
   for(const name of ['Overview','Vehicles','Calendar','Tracking','Intelligence','Assistant','Analytics','Notifications']){
      await go(name);if(modules[name])await art(name,modules[name]);await fit(`${name} ${width}`);
   }
 }
 await go('Overview');await page.screenshot({path:'browser-results/v63-overview-mobile.png',fullPage:true});
 await page.locator('.demo-language select').selectOption('ru');
 await page.waitForFunction(()=>document.querySelector('main.workspace-layout')?.lang==='ru');
 await fit('Russian mobile');
 await page.screenshot({path:'browser-results/v63-overview-ru.png',fullPage:true});
 assert.deepEqual(errors,[]);assert.deepEqual(bad,[]);
 console.log('PASS: 36 exact assets, 24 module headers, desktop/tablet/mobile, both themes, RU language switch, HTTP and runtime errors');
}finally{await browser.close();}
