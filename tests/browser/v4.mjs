import {chromium} from 'playwright';
import assert from 'node:assert/strict';
import {mkdir} from 'node:fs/promises';
const browser=await chromium.launch();
try{
  const page=await browser.newPage({viewport:{width:1440,height:900},reducedMotion:'reduce'});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8080/FleetManagement/');
  await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
  const availability=await page.getByTestId('available-now').innerText();
  const counts=availability.match(/(\d+)\s*\/\s*(\d+)/);
  assert.ok(counts&&Number(counts[1])<=Number(counts[2]));
  await page.getByRole('button',{name:'Collapse sidebar',exact:true}).click();
  assert.equal(await page.locator('.sidebar-collapsed').count(),1);
  await page.getByRole('button',{name:'Expand sidebar',exact:true}).click();
  await page.locator('.v4-context-actions').getByRole('button',{name:'Quick actions',exact:false}).click();
  await page.locator('.v4-context-actions').getByRole('button',{name:'Register vehicle',exact:true}).click();
  await page.getByRole('heading',{name:'New record',exact:true}).waitFor();
  await page.getByLabel('Registration',{exact:true}).fill('DRAFT-QA');
  await page.locator('.v4-context-actions').getByRole('button',{name:'Quick actions',exact:false}).click();
  page.once('dialog',d=>d.dismiss());
  await page.locator('.v4-context-actions').getByRole('button',{name:'+ New trip',exact:true}).click();
  assert.equal(await page.getByLabel('Registration',{exact:true}).inputValue(),'DRAFT-QA');
  await page.locator('.v4-context-actions').getByRole('button',{name:'Quick actions',exact:false}).click();
  page.once('dialog',d=>d.accept());
  await page.locator('.v4-context-actions').getByRole('button',{name:'+ New trip',exact:true}).click();
  await page.getByRole('heading',{name:'Intelligent dispatch planner',exact:true}).waitFor();
  await page.waitForURL(/module=Dispatch/);
  await page.reload();
  await page.getByRole('heading',{name:'Intelligent dispatch planner',exact:true}).waitFor({timeout:90000});
  await page.goBack();
  await page.getByRole('heading',{name:'Vehicle register',exact:true}).waitFor();
  await page.getByLabel('Rows per page',{exact:true}).selectOption('5');
  assert.equal(await page.getByRole('button',{name:'Previous page',exact:true}).isDisabled(),true);
  await page.locator('.v4-context-actions').getByRole('button',{name:'Quick actions',exact:false}).click();
  await page.locator('.v4-context-actions').getByRole('button',{name:'Help & settings',exact:true}).click();
  await page.getByRole('heading',{name:'Help & settings',exact:true}).waitFor();
  await page.getByLabel('Appearance',{exact:true}).selectOption('system');
  assert.equal(await page.getByLabel('Appearance',{exact:true}).inputValue(),'system');
  await page.emulateMedia({colorScheme:'light'});
  await page.locator('.demo-shell:not(.dark-theme)').waitFor();
  await page.emulateMedia({colorScheme:'dark'});
  await page.locator('.demo-shell.dark-theme').waitFor();
  await page.getByRole('button',{name:'Overview',exact:true}).click();
  await mkdir('browser-results',{recursive:true});
  for(const width of [320,360,375,390,414,480,768,820,1024,1280,1440,1920,2560]){
    await page.setViewportSize({width,height:900});
    assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Overview overflow '+width);
    if([320,768,1440].includes(width))await page.screenshot({path:'browser-results/v4-overview-'+width+'.png',fullPage:true});
  }
  await page.setViewportSize({width:390,height:844});
  await page.getByRole('button',{name:'Mobile More',exact:true}).click();
  await page.locator('#workspace-nav').getByRole('button',{name:'Calendar',exact:true}).click();
  await page.getByRole('heading',{name:'Fleet calendar',exact:true}).waitFor();
  for(const width of [320,390,768,1440]){
    await page.setViewportSize({width,height:844});
    for(const mode of ['Day','Week','Month']){
      await page.getByRole('button',{name:mode,exact:true}).click();
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Calendar '+mode+' overflow '+width);
      if(mode!=='Day')await page.locator('.calendar-day').first().click();
      assert.ok(await page.locator('.calendar-agenda').isVisible());
    }
  }
  await page.setViewportSize({width:390,height:844});
  await page.getByRole('button',{name:'Mobile Fleet',exact:true}).click();
  await page.locator('.register-cards .register-record').first().waitFor();
  assert.equal(await page.locator('.register-table').isVisible(),false);
  assert.ok(await page.locator('.register-cards .register-record').first().getByRole('button',{name:'Passport',exact:true}).isVisible());
  await page.getByRole('button',{name:'Mobile Trips',exact:true}).click();
  await page.getByRole('heading',{name:'Intelligent dispatch planner',exact:true}).waitFor();
  await page.locator('.trip-cards .register-record').first().waitFor();
  assert.equal(await page.locator('.trip-table').isVisible(),false);
  await page.getByRole('button',{name:'Mobile Alerts',exact:true}).click();
  await page.getByRole('heading',{name:'Notification center',exact:true}).waitFor();
  await page.getByLabel('Search alerts',{exact:true}).fill('not-an-alert-qa');
  await page.getByText('No matching alerts. This does not certify vehicle safety.',{exact:true}).waitFor();
  await page.getByRole('button',{name:'Clear filters',exact:true}).click();
  await page.getByRole('button',{name:'Mobile More',exact:true}).click();
  await page.locator('#workspace-nav').getByRole('button',{name:'Optimization',exact:true}).click();
  await page.getByRole('heading',{name:'Route optimization lab',exact:true}).waitFor();
  await page.getByRole('button',{name:'Mobile Home',exact:true}).click();
  await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor();
  assert.deepEqual(errors,[]);
  console.log('PASS: quick actions, deep links, back, system theme, 13 viewport widths and mobile navigation');
}finally{await browser.close();}
