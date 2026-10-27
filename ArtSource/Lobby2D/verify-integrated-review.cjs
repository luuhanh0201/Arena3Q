'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('./tooling.cjs')('playwright');
async function main(){
  const browser=await chromium.launch({channel:'msedge',headless:true});
  try{
    const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[],failed=[],controls=[];
    page.on('pageerror',error=>errors.push(String(error)));
    page.on('response',response=>{if(response.status()>=400)failed.push(response.url())});
    await page.goto('http://127.0.0.1:4331/review.html',{waitUntil:'networkidle'});
    const loaded=()=>page.waitForFunction(()=>Array.from(document.images).every(image=>image.complete&&image.naturalWidth>0));
    await loaded();
    for(const width of [1920,2160,2340,2400]){
      await page.locator(`[data-size="${width}"]`).click(); await loaded();
      controls.push(await page.locator('#lobby').evaluate((image,width)=>({kind:'size',width,passed:image.naturalWidth===width}),width));
    }
    for(const hour of [5,11,15,18,21]){
      await page.locator(`[data-hour="${hour}"]`).click(); await loaded();
      controls.push(await page.locator('#daylight').evaluate((image,hour)=>({kind:'hour',hour,passed:image.src.endsWith('Daylight_'+hour+'.png')}),hour));
    }
    for(const frame of [0,1,2]){
      await page.locator(`[data-idle="${frame}"]`).click(); await loaded();
      controls.push(await page.locator('#idle').evaluate((image,frame)=>({kind:'idle',frame,passed:image.src.endsWith('Idle_'+frame+'.png')}),frame));
    }
    const images=await page.locator('img').evaluateAll(items=>items.map(image=>({src:image.getAttribute('src'),loaded:image.complete&&image.naturalWidth>0})));
    const mainUrl=page.url();
    await page.locator('h1').scrollIntoViewIfNeeded();
    const output=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
    await page.screenshot({path:path.join(output,'integrated-review-browser.png')});
    await page.goto('http://127.0.0.1:4331/exp-review.html',{waitUntil:'networkidle'}); await loaded();
    const expImages=await page.locator('img').evaluateAll(items=>items.map(image=>({src:image.getAttribute('src'),loaded:image.complete&&image.naturalWidth>0})));
    const expUrl=page.url();
    await page.screenshot({path:path.join(output,'exp-review-browser.png')});
    await page.goto('http://127.0.0.1:4331/name-design.html',{waitUntil:'networkidle'}); await loaded();
    const designImages=await page.locator('img').evaluateAll(items=>items.map(image=>({src:image.getAttribute('src'),loaded:image.complete&&image.naturalWidth>0})));
    const mediaElements=await page.locator('audio,video').count();
    const result={passed:errors.length===0&&failed.length===0&&[...images,...expImages,...designImages].every(image=>image.loaded)&&controls.every(control=>control.passed)&&mediaElements===0,url:mainUrl,expUrl,designUrl:page.url(),images,expImages,designImages,mediaElements,controls,errors,failed};
    fs.writeFileSync(path.join(output,'integrated-browser-check.json'),JSON.stringify(result,null,2));
    await page.screenshot({path:path.join(output,'name-design-browser.png')});
    console.log(JSON.stringify({passed:result.passed,images:images.length,expImages:expImages.length,designImages:designImages.length,controls:controls.length,mediaElements,errors,failed}));
    if(!result.passed)process.exitCode=1;
  }finally{await browser.close()}
}
main().catch(error=>{console.error(error);process.exitCode=1});
