import { chromium } from 'playwright';
import assert from 'node:assert/strict';
import { mkdir, readdir } from 'node:fs/promises';
const url='http://127.0.0.1:8080/FleetManagement/';
const browser=await chromium.launch({headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
try{
  await mkdir('browser-results',{recursive:true});
  const page=await browser.newPage({viewport:{width:1440,height:900},reducedMotion:'reduce'});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(url);
  await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
  await page.locator('.scene-stage[data-renderer="webgl"]').waitFor({timeout:20000});
  const hasArchive=(await readdir('design')).some(name=>name.endsWith('.zip'));
  const artwork=await (await page.request.get(url+'assets/neon/manifest.json')).json();
  assert.equal(artwork.mode,hasArchive?'imported':'fallback');
  assert.equal(artwork.files.length,hasArchive?8:0);
  if(hasArchive){
    for(const asset of artwork.files){
      assert.match(asset.name,/^[a-z-]+\.webp$/);
      assert.ok(await page.evaluate(async name=>{const img=new Image();img.src='assets/neon/'+name;await img.decode();return img.naturalWidth>0;},asset.name));
    }
    assert.match(await page.locator('.art-routes').evaluate(el=>getComputedStyle(el).backgroundImage),/routes\.webp/);
    assert.equal(await page.locator('.art-routes .art-glyph').isVisible(),false);
  }
  assert.equal(await page.getByRole('button',{name:'Play 3D motion',exact:true}).count(),1);
  await page.getByRole('button',{name:'Rotate right',exact:true}).click();
  await page.getByRole('button',{name:'Play 3D motion',exact:true}).click();
  await page.getByRole('button',{name:'Pause 3D motion',exact:true}).click();
  await page.screenshot({path:'browser-results/neon-desktop.png',fullPage:true});
  await page.locator('.capability-card').filter({hasText:'Route optimization'}).click();
  await page.getByRole('heading',{name:'Route optimization lab',exact:true}).waitFor();
  assert.equal(await page.locator('.hero-scene').count(),0);
  await page.getByRole('button',{name:'Overview',exact:true}).click();
  await page.locator('.scene-stage[data-renderer="webgl"]').waitFor();
  await page.setViewportSize({width:320,height:760});
  await page.locator('.mobile-nav-toggle').click();
  await page.locator('#workspace-nav').getByRole('button',{name:'Vehicles',exact:true}).click();
  await page.getByRole('heading',{name:'Vehicle register',exact:true}).waitFor();
  assert.equal(await page.locator('.mobile-nav-toggle').getAttribute('aria-expanded'),'false');
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await page.locator('.mobile-nav-toggle').click();
  await page.locator('#workspace-nav').getByRole('button',{name:'Overview',exact:true}).click();
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await page.screenshot({path:'browser-results/neon-mobile-320.png',fullPage:true});
  assert.deepEqual(errors,[]);
  // Force unavailable WebGL before application startup; the SVG must remain visible.
  const fallback=await browser.newPage({viewport:{width:768,height:1024}});
  await fallback.addInitScript(()=>{const original=HTMLCanvasElement.prototype.getContext;HTMLCanvasElement.prototype.getContext=function(type,...args){return type.startsWith('webgl')?null:original.call(this,type,...args);};});
  await fallback.goto(url);
  await fallback.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
  await fallback.locator('.scene-fallback').waitFor({state:'visible'});
  assert.equal(await fallback.locator('.scene-stage[data-renderer="webgl"]').count(),0);
  console.log('PASS: 3D, reduced motion, module cards, 320px navigation, fallback');
}finally{await browser.close();}
