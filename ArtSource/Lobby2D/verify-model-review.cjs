'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('./tooling.cjs')('playwright');
async function main(){
  const browser=await chromium.launch({channel:'msedge',headless:true});
  try{
    const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[],failed=[];
    page.on('pageerror',error=>errors.push(String(error))); page.on('response',response=>{if(response.status()>=400)failed.push(response.url())});
    await page.goto('http://127.0.0.1:4331/model-review.html',{waitUntil:'networkidle'});
    const images=await page.locator('img').evaluateAll(items=>items.map(item=>({src:item.getAttribute('src'),loaded:item.complete&&item.naturalWidth>0})));
    const result={passed:images.every(item=>item.loaded)&&errors.length===0&&failed.length===0,images,errors,failed};
    const output=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
    fs.writeFileSync(path.join(output,'model-browser-check.json'),JSON.stringify(result,null,2));
    await page.screenshot({path:path.join(output,'model-review-browser.png')});
    console.log(JSON.stringify(result)); if(!result.passed)process.exitCode=1;
  }finally{await browser.close()}
}
main().catch(error=>{console.error(error);process.exitCode=1});
