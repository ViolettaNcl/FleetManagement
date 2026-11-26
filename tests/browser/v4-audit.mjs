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
   const layout=await page.evaluate(()=>({viewport:innerWidth,document:document.documentElement.scrollWidth}));
   result.push({module,layout,findings});
 }
 await writeFile('browser-results/v4-accessibility.json',JSON.stringify({scope:'Automated checks; full manual WCAG evaluation not claimed',modules:result,errors},null,2));
 assert.deepEqual(errors,[]);
 const overflow=result.filter(x=>x.layout.document>x.layout.viewport);
 assert.deepEqual(overflow.map(x=>({module:x.module,layout:x.layout})),[],'Module mobile overflow');
 console.log('PASS: screenshot and mobile-width audit for '+modules.length+' modules; accessibility findings recorded.');
}finally{await browser.close();}
