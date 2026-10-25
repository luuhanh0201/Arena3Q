const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const{createRequire}=require('node:module');const root=path.resolve(__dirname,'../../../..');
const{chromium}=createRequire(path.join(root,'Admin/package.json'))('@playwright/test');
const evidence=path.join(root,'NghiemThu/Battlefield/XemTruoc_ChinhSua_20261025');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.EDGE_PATH??'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1100}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  assert.equal((await page.goto('http://127.0.0.1:4311/app/feedback.html')).status(),200);
  const buttons=page.locator('[data-image]');const count=await buttons.count();assert.equal(count,7);
  for(let i=0;i<count;i++){
   const file=await buttons.nth(i).getAttribute('data-image');await buttons.nth(i).click();
   await page.waitForFunction(file=>{const img=document.querySelector('#map');return img.src.endsWith('/'+file)&&img.complete&&img.naturalHeight===900&&img.naturalWidth===(file.includes('before-after')||file.includes('castle-alive-ruined')?3200:1600);},file);
  }
  await buttons.nth(0).click();await page.waitForFunction(()=>document.querySelector('#map').complete);
  await page.screenshot({path:path.join(evidence,'web-feedback-desktop.png'),fullPage:true});
  await page.setViewportSize({width:412,height:915});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await page.screenshot({path:path.join(evidence,'web-feedback-mobile.png'),fullPage:true});
  assert.equal((await page.request.post('http://127.0.0.1:4311/app/feedback.html')).status(),405);
  assert.equal((await page.request.get('http://127.0.0.1:4311/map-feedback/final-play-results.xml')).status(),404);
  assert.equal((await page.request.head('http://127.0.0.1:4311/test-apk/Arena3Q-dev.apk')).status(),404);
  assert.deepEqual(errors,[]);
  const report={imagesDecoded:count,mobileOverflow:false,pageErrors:errors,readOnly:true};
  fs.writeFileSync(path.join(evidence,'web-feedback-verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
