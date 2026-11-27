import { chromium } from 'playwright';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';

const url=process.env.FLEET_PREVIEW_URL;
const revision=process.env.GITHUB_SHA;
assert.ok(['http://127.0.0.1:8080/FleetManagement/','https://violettancl.github.io/FleetManagement/'].includes(url),'Only the intended local or public preview is supported');
assert.match(revision,/^[0-9a-f]{40}$/);
const browser=await chromium.launch({headless:true});
try {
  const context=await browser.newContext({viewport:{width:1366,height:900}});
  // Allow bounded CDN propagation, never accept stale or unknown revision.
  let verified=false;
  for(let attempt=0;attempt<18;attempt++){
    try{
      const response=await context.request.get(url+'preview-build.json?rev='+revision+'&attempt='+attempt,{timeout:10000,maxRedirects:0});
      if(response.ok()){
        const data=await response.json();
        verified=data.schema===1&&data.revision===revision&&data.mode==='synthetic-browser-demo'&&data.artworkMode==='committed';
        if(verified)break;
      }
    }catch{}
    if(attempt<17)await new Promise(resolve=>setTimeout(resolve,5000));
  }
  assert.ok(verified,'Public build marker missing or stale');
  const appUrl=url+'app/';
  const page=await context.newPage();
  await page.addInitScript(()=>{if(!localStorage.getItem('fleetmanagement.preferences.navigation.v6'))localStorage.setItem('fleetmanagement.preferences.navigation.v6','all');});
  const errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  const response=await page.goto(appUrl,{waitUntil:'domcontentloaded',timeout:60000});
  assert.ok(response?.ok(),'Preview HTTP response failed');
  assert.equal(new URL(page.url()).origin,new URL(url).origin);
  await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
  await page.getByTestId('build-revision').waitFor();
  assert.equal(await page.getByTestId('build-revision').innerText(),revision.slice(0,7));
  assert.equal(await page.locator('.dashboard-kpis .metric-action').count(),4);
  await page.locator('.workload-donut').waitFor();
  await page.locator('.simulation-showcase').waitFor();
  {
    const artwork=await (await context.request.get(appUrl+'assets/neon/manifest.json?rev='+revision)).json();
    assert.equal(artwork.mode,'committed');
    assert.equal(artwork.files.length,10);
    for(const asset of artwork.files){
      assert.match(asset.name,/^[a-z-]+\.webp$/);
      const actual=await context.request.get(appUrl+'assets/neon/'+asset.name+'?rev='+revision);
      assert.ok(actual.ok());
      assert.equal(createHash('sha256').update(await actual.body()).digest('hex'),asset.sha256);
      assert.ok(await page.evaluate(async name=>{const image=new Image();image.src='assets/neon/'+name;await image.decode();return image.naturalWidth>0;},asset.name));
    }
  }
  // Validate the newly approved artwork in the *packaged* and deployed app,
  // not just the legacy ten-image neon manifest.
  const approved=JSON.parse(await readFile(new URL('../../docs/fleet-approved-assets.json',import.meta.url),'utf8'));
  assert.equal(Object.keys(approved).length,36);
  for(const [name,asset] of Object.entries(approved)){
    const actual=await context.request.get(appUrl+asset.path+'?rev='+revision);
    assert.ok(actual.ok(),'Approved image HTTP: '+name);
    assert.equal(createHash('sha256').update(await actual.body()).digest('hex'),asset.sha256,'Approved image SHA-256: '+name);
  }
  await page.getByRole('button',{name:'Vehicles',exact:true}).click();
  const vehicleImage=page.locator('.approved-vehicle-image').first();
  await vehicleImage.waitFor();
  assert.ok(await vehicleImage.evaluate(async img=>{await img.decode();return img.naturalWidth>0;}));
  await page.getByRole('button',{name:'Optimization',exact:true}).click();
  await page.getByRole('button',{name:'Calculate routes',exact:true}).click();
  await page.getByRole('button',{name:'Multi-start 2-opt',exact:true}).waitFor();
  await page.getByRole('button',{name:'Multi-start 2-opt',exact:true}).click();
  await page.getByRole('button',{name:'Overview',exact:true}).click();
  await page.getByLabel('Language',{exact:true}).selectOption('ru');
  await page.getByRole('heading',{name:'Обзор операций',exact:true}).waitFor();
  assert.deepEqual(errors,[]);
  console.log('PASS: actual preview UI and exact deployed revision '+revision);
}finally{await browser.close();}
