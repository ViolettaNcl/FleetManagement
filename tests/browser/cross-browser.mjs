import { firefox, webkit } from 'playwright';
import assert from 'node:assert/strict';
for (const engine of [firefox, webkit]) {
  const browser=await engine.launch({headless:true});
  try {
    const page=await browser.newPage({viewport:{width:768,height:1024}});
    const errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    await page.goto('http://127.0.0.1:8080/FleetManagement/');
    await page.getByRole('heading',{name:'Operations overview',exact:true}).waitFor({timeout:90000});
    await page.getByLabel('Language',{exact:true}).selectOption('ru');
    await page.locator('.mobile-nav-toggle').click();
    await page.getByRole('button',{name:'Отслеживание',exact:true}).click();
    await page.getByRole('heading',{name:'Отслеживание автопарка',exact:true}).waitFor();
    await page.getByRole('button',{name:'+ 15 минут',exact:true}).click();
    await page.locator('.mobile-nav-toggle').click();
    await page.getByRole('button',{name:'Обзор',exact:true}).click();
    await page.getByRole('heading',{name:'Загрузка рейсами',exact:true}).waitFor();
    assert.deepEqual(errors,[]);
    await page.screenshot({path:'browser-results/tablet-'+engine.name()+'.png',fullPage:true});
    console.log('PASS: '+engine.name()+' tablet smoke');
  } finally { await browser.close(); }
}
