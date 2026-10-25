const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const{createRequire}=require('node:module');const root=path.resolve(__dirname,'../../../..');
const{chromium}=createRequire(path.join(root,'Admin/package.json'))('@playwright/test');
const evidence=path.join(root,'NghiemThu/Battlefield/XemTruoc_Map_20261025');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.EDGE_PATH??'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1100}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  assert.equal((await page.goto('http://127.0.0.1:4311/app/map.html')).status(),200);
  const buttons=page.locator('[data-image]');const count=await buttons.count();assert.equal(count,10);
  for(let i=0;i<count;i++){await buttons.nth(i).click();await page.waitForFunction(()=>{const img=document.querySelector('#map');return img.complete&&img.naturalWidth===1600&&img.naturalHeight===900;});}
  await buttons.nth(0).click();await page.waitForFunction(()=>document.querySelector('#map').complete);
  const video=page.locator('video');await video.scrollIntoViewIfNeeded();await video.evaluate(v=>v.play());await page.waitForFunction(()=>document.querySelector('video').currentTime>.25);assert.equal(await video.evaluate(v=>v.error),null);await video.evaluate(v=>v.pause());
  await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(evidence,'web-map-desktop.png'),fullPage:true});
  await page.setViewportSize({width:412,height:915});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await page.screenshot({path:path.join(evidence,'web-map-mobile.png'),fullPage:true});
  const media=await page.request.head('http://127.0.0.1:4311/map-evidence/environment-motion.mp4');assert.match(media.headers()['content-type'],/video\/mp4/);
  assert.equal((await page.request.post('http://127.0.0.1:4311/app/map.html')).status(),405);
  assert.equal((await page.request.get('http://127.0.0.1:4311/map-evidence/..%2f..%2fAdmin/apps/api/data/db.json')).status(),404);
  assert.equal((await page.request.get('http://127.0.0.1:4311/map-evidence/play-delivery-tests.log')).status(),404);
  assert.deepEqual(errors,[]);const report={imagesDecoded:count,videoPlayback:true,mobileOverflow:false,pageErrors:errors,readOnly:true};fs.writeFileSync(path.join(evidence,'web-verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
