'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('./tooling.cjs')('playwright');
const out=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_20261026/Web');fs.mkdirSync(out,{recursive:true});
let browser;
async function main(){
  browser=await chromium.launch({channel:'msedge',headless:true});
  const page=await browser.newPage({viewport:{width:1600,height:1000},deviceScaleFactor:1});
  const errors=[],missing=[],checks=[];
  page.on('pageerror',e=>{errors.push(String(e));console.error(String(e))});page.on('response',r=>{if(r.status()>=400){missing.push(r.url());console.error('HTTP '+r.status()+' '+r.url())}});
  const check=(name,passed,details)=>{checks.push({name,passed,...details});if(!passed)throw Error(name)};
  await page.goto('http://127.0.0.1:4330',{waitUntil:'networkidle'});
  for(let i=0;i<5;i++){
    await page.locator('#times button').nth(i).click();await page.locator('#lobby-image').evaluate(im=>im.decode());
    check('Lobby time '+i,await page.locator('#lobby-image').evaluate(im=>im.naturalWidth===1920&&im.naturalHeight===1080));
    await page.screenshot({path:path.join(out,'lobby_'+i+'.png'),fullPage:true});
  }
  for(const width of [2160,2340,2400]){
    await page.locator('[data-width="'+width+'"]').click();await page.locator('#lobby-image').evaluate(im=>im.decode());
    check('Safe area '+width,await page.locator('#lobby-image').evaluate((im,w)=>im.naturalWidth===w,width));
    await page.screenshot({path:path.join(out,'safe_'+width+'.png'),fullPage:true});
  }
  await page.locator('[data-tab="animations"]').click();
  for(const clip of ['Idle','Run','Attack']){
    await page.locator('[data-clip="'+clip+'"]').click();await page.waitForFunction(()=>window.artReview.loaded===24);
    check('24 frames '+clip,(await page.locator('#frame-strip img').count())===24);
    for(const frame of [0,6,13,17,23]){
      await page.locator('#frame-slider').evaluate((input,value)=>{input.value=value;input.dispatchEvent(new Event('input',{bubbles:true}))},String(frame));
      await page.locator('#frame-image').evaluate(im=>im.decode());
      await page.screenshot({path:path.join(out,clip+'_'+String(frame).padStart(3,'0')+'.png'),fullPage:true});
    }
    await page.locator('#frame-slider').evaluate(input=>{input.value='0';input.dispatchEvent(new Event('input',{bubbles:true}))});
    await page.locator('#play').click();await page.waitForTimeout(clip==='Idle'?2300:1400);
    const state=await page.evaluate(()=>({playing:window.artReview.playing,frame:window.artReview.frame}));
    check('Playback '+clip,clip==='Attack'?!state.playing&&state.frame===23:state.playing,state);
    if(state.playing)await page.locator('#play').click();
    await page.locator('#speed').selectOption('0.25');
    await page.locator('#frame-slider').evaluate(input=>{input.value='0';input.dispatchEvent(new Event('input',{bubbles:true}))});
    await page.locator('#play').click();await page.waitForTimeout(500);
    check('Slow playback '+clip,(await page.evaluate(()=>window.artReview.frame))<8);
    await page.locator('#play').click();await page.locator('#speed').selectOption('1');
  }
  await page.locator('[data-tab="characters"]').click();
  check('Portraits',(await page.locator('#character-grid .asset').count())===5);
  check('Layer groups',(await page.locator('#layer-grid .asset').count())===26);
  await page.locator('#layer-grid .asset').first().click();await page.locator('#asset-image').evaluate(im=>im.decode());
  check('Original layer 2048px',await page.locator('#asset-image').evaluate(im=>im.naturalWidth===2048));
  await page.screenshot({path:path.join(out,'layer-original.png'),fullPage:true});await page.locator('#close-dialog').click();
  await page.locator('[data-tab="ui"]').click();await page.locator('[data-role="vip"]').click();
  check('Twelve VIP designs',(await page.locator('#ui-grid .asset').count())===12);
  await page.screenshot({path:path.join(out,'vip.png'),fullPage:true});
  await page.locator('[data-role="icon"]').click();check('Dedicated icons',(await page.locator('#ui-grid .asset').count())===17);
  await page.screenshot({path:path.join(out,'icons.png'),fullPage:true});
  await page.locator('#micro-toggle').uncheck();check('Independent micro effects toggle',await page.locator('.micro-demos').evaluate(el=>el.classList.contains('paused')));
  await page.locator('#micro-toggle').check();
  await page.setViewportSize({width:740,height:400});await page.locator('[data-tab="animations"]').click();
  check('Landscape review fits',await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await page.screenshot({path:path.join(out,'landscape-mobile.png'),fullPage:true});
  check('No page errors',errors.length===0,{errors});check('No missing files',missing.length===0,{missing});
  const report={passed:checks.every(c=>c.passed),checks,errors,missing,validationScope:'web artwork review only; no APK/player build and no device FPS claim'};
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));await browser.close();
}
main().catch(e=>{console.error(e);process.exitCode=1;}).finally(async()=>{if(browser)await browser.close()});
