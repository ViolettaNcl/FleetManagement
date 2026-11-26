import {chromium} from 'playwright';
import assert from 'node:assert/strict';
import {mkdir,writeFile} from 'node:fs/promises';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url);
const axePath=require.resolve('axe-core/axe.min.js');
const root='http://127.0.0.1:8080/FleetManagement/';
const browser=await chromium.launch();
const modules=['Overview','Vehicles','Drivers','Vehicle Passport','Dispatch','Batch Dispatch','Calendar','Resource Planning','Maintenance','Tracking','Geo Map','Analytics','Cost Intelligence','Assistant','Optimization','Multi Dispatch','Digital Twin','What-if','Intelligence','Predictions','Demand Forecast','Model Registry','Data Quality','Model Drift','Notifications','Help & settings'];
const result=[];
await mkdir('browser-results/v4-modules',{recursive:true});
try{
 const page=await browser.newPage({viewport:{width:1440,height:900},reducedMotion:'reduce'});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(root+'app/');
 await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
 await page.addScriptTag({path:axePath});
 for(const module of modules){
   await page.setViewportSize({width:1440,height:900});
   await page.locator('#workspace-nav').getByRole('button',{name:module,exact:true}).click();
   await page.locator('#workspace-nav').getByRole('button',{name:module,exact:true}).waitFor();
   await page.locator('.fleet-content h2').first().waitFor();
   const slug=module.toLowerCase().replace(/[^a-z0-9]+/g,'-');
   await page.evaluate(()=>window.scrollTo(0,0));
   await page.screenshot({path:'browser-results/v4-modules/'+slug+'-desktop.png'});
   // Record all automated WCAG findings; do not claim manual AA conformance.
   const findings=await page.evaluate(async()=>{const r=await axe.run({runOnly:{type:'tag',values:['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa']}});return r.violations.map(v=>({id:v.id,impact:v.impact,description:v.description,nodes:v.nodes.map(n=>({target:n.target,summary:n.failureSummary}))}));});
   await page.setViewportSize({width:390,height:844});await page.evaluate(()=>window.scrollTo(0,0));
   await page.screenshot({path:'browser-results/v4-modules/'+slug+'-mobile.png'});
   const layout=await page.evaluate(()=>({viewport:innerWidth,document:document.documentElement.scrollWidth,offenders:[...document.querySelectorAll("body *")].filter(el=>{const r=el.getBoundingClientRect();return r.width>0&&r.right>innerWidth+1&&getComputedStyle(el).position!=="fixed";}).slice(0,15).map(el=>({tag:el.tagName,className:String(el.className).slice(0,120),right:Math.round(el.getBoundingClientRect().right)}))}));
   const layouts=[layout];
   for(const width of [320,480,768,1024,1280,1920,2560]){
     await page.setViewportSize({width,height:844});
     layouts.push(await page.evaluate(()=>({viewport:innerWidth,document:document.documentElement.scrollWidth})));
     if(width===768)await page.screenshot({path:'browser-results/v4-modules/'+slug+'-tablet.png'});
   }
   result.push({module,layout,layouts,findings});
 }
 await writeFile('browser-results/v4-accessibility.json',JSON.stringify({scope:'Automated checks; full manual WCAG evaluation not claimed',modules:result,errors},null,2));
 assert.deepEqual(errors,[]);
 const overflow=result.flatMap(x=>x.layouts.filter(l=>l.document>l.viewport).map(layout=>({module:x.module,layout})));
 assert.deepEqual(overflow,[],'Module responsive overflow');
 assert.deepEqual(result.flatMap(x=>x.findings.map(f=>({module:x.module,rule:f.id,nodes:f.nodes.map(n=>n.target)}))),[],'Automated accessibility violations on audited module states');
 console.log('PASS: screenshot, mobile-width and automated accessibility audit for '+modules.length+' modules.');
}finally{await browser.close();}
