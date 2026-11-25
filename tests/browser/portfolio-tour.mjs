import { chromium } from "playwright";
import { mkdir, rename } from "node:fs/promises";
import assert from "node:assert/strict";

// Actual application recording. Fresh browser context; only synthetic records.
// Delays are deliberate reading time for the silent portfolio video, not tests.
const browser=await chromium.launch({headless:true});
await mkdir("portfolio-video",{recursive:true});
const context=await browser.newContext({viewport:{width:1280,height:800},recordVideo:{dir:"portfolio-video",size:{width:1280,height:800}}});
const page=await context.newPage();
const video=page.video();
const errors=[];
page.on("pageerror",error=>errors.push(error.message));
const hold=ms=>page.waitForTimeout(ms);
async function nav(name,heading){
    await page.getByRole("button",{name,exact:true}).click();
    await page.getByRole("heading",{name:heading,exact:true}).waitFor();
    await page.evaluate(()=>window.scrollTo(0,0));
    await hold(1800);
}
async function show(heading){
    await page.getByRole("heading",{name:heading,exact:true}).scrollIntoViewIfNeeded();
    await hold(2600);
}
try{
    await page.goto("http://127.0.0.1:8080/FleetManagement/");
    await page.getByRole("heading",{name:"Operations overview",exact:true}).waitFor({timeout:90000});
    await hold(2800);
    await page.getByRole("button",{name:"Dark theme",exact:true}).click();await hold(1800);
    await page.getByRole("button",{name:"Dark theme",exact:true}).click();
    await nav("Vehicles","Vehicle register");
    await page.getByRole("row").filter({hasText:"FM-102"}).getByRole("button",{name:"Passport",exact:true}).click();
    await page.getByRole("heading",{name:"Digital vehicle passport",exact:true}).waitFor();await hold(2400);
    await nav("Calendar","Fleet calendar");await page.getByRole("button",{name:"Month",exact:true}).click();await hold(2600);
    await nav("Dispatch","Intelligent dispatch planner");
    await page.getByRole("button",{name:"Suggest assignment"}).click();await hold(2000);
    await nav("Optimization","Route optimization lab");
    await page.getByRole("button",{name:"Calculate routes"}).click();
    await page.getByRole("button",{name:"2-opt",exact:true}).click();await hold(2500);
    await nav("Intelligence","Fleet Intelligence Lab");
    await page.getByRole("button",{name:"Train model"}).click();
    await show("Held-out evaluation");await show("Feature contributions");
    await nav("Model Drift","Model drift laboratory");
    await page.getByRole("button",{name:"Compare drift windows"}).click();await show("Population stability index (PSI)");
    await page.getByRole("button",{name:"Run retraining review"}).click();await show("Independent candidate evaluation");
    await page.getByRole("button",{name:"Digital Twin",exact:true}).click();
    await page.getByRole("button",{name:"Initialize scenario"}).click();
    await show("Fleet movement");
    for(let i=0;i<3;i++){await page.getByRole("button",{name:"+ 5 minutes",exact:true}).click();await hold(500);}
    await show("Scenario event log");
    await page.getByRole("button",{name:"Overview",exact:true}).click();
    await page.evaluate(()=>window.scrollTo(0,0));await hold(1800);
    assert.deepEqual(errors,[]);
}finally{
    await context.close();await browser.close();
}
const original=await video.path();
await rename(original,"portfolio-video/fleetmanagement-v3-tour.webm");
console.log("PASS: actual offline browser tour recorded (silent, synthetic data)");
