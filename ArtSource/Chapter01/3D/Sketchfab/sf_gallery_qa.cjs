'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('../../../../Admin/node_modules/playwright');
const out=path.resolve(__dirname,'../../../../NghiemThu/Chapter01/Sketchfab_20261029');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  const response=await page.goto('http://127.0.0.1:4322/sketchfab-review/');
  if(response.status()!==200)throw Error('Gallery HTTP '+response.status());
  const imageCheck=await page.locator('img').evaluateAll(async images=>{
   await Promise.all(images.map(i=>{i.loading='eager';return i.decode().catch(()=>{});}));
   return {count:images.length,broken:images.filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src)};
  });
  if(imageCheck.count!==120||imageCheck.broken.length||errors.length)throw Error(JSON.stringify({imageCheck,errors}));
  await page.locator('#SF04').scrollIntoViewIfNeeded();
  await page.locator('#SF04 article').first().screenshot({path:path.join(out,'browser-SF04.png')});
  await page.setViewportSize({width:390,height:844});
  if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1))throw Error('Mobile overflow');
  await page.goto('http://127.0.0.1:4322/sketchfab-review/#SF21');
  await page.screenshot({path:path.join(out,'browser-mobile-SF21.png')});
  fs.writeFileSync(path.join(out,'browser-checks.json'),JSON.stringify({passed:true,status:200,images:imageCheck.count,broken:0,pageErrors:errors,mobileOverflow:false},null,2));
  console.log('GALLERY_QA_PASS '+imageCheck.count+' images; desktop/mobile');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
