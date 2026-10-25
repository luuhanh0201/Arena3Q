const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const{createRequire}=require('node:module');const root=path.resolve(__dirname,'../../..');
const{chromium}=createRequire(path.join(root,'Admin/package.json'))('@playwright/test');
const evidence=path.join(root,'NghiemThu/Icons/Mobile_20261025');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.EDGE_PATH??'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1100}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  assert.equal((await page.goto('http://127.0.0.1:4311/app/icon-mobile.html')).status(),200);
  await page.waitForFunction(()=>document.querySelectorAll('#skill option').length===42);
  const ids=await page.locator('#skill option').evaluateAll(items=>items.map(i=>i.value));
  for(const id of ids){await page.selectOption('#skill',id);await page.waitForFunction(id=>['before','after'].every(key=>{const image=document.querySelector('#'+key);return image.src.endsWith('/'+id+'.png')&&image.complete&&image.naturalWidth===1254&&image.naturalHeight===1254;}),id);}
  await page.selectOption('#skill','lubu_phuong_thien_tram');await page.waitForFunction(()=>document.querySelector('#after').complete);
  await page.screenshot({path:path.join(evidence,'web-desktop.png'),fullPage:true});
  await page.setViewportSize({width:412,height:915});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await page.screenshot({path:path.join(evidence,'web-mobile.png'),fullPage:true});
  const head=await page.request.head('http://127.0.0.1:4311/test-apk/Arena3Q-Mobile-20261025.apk');assert.equal(head.status(),200);
  assert.equal(Number(head.headers()['content-length']),fs.statSync(path.join(root,'Builds/Android/Arena3Q-Mobile-20261025.apk')).size);
  assert.equal((await page.request.post('http://127.0.0.1:4311/app/icon-mobile.html')).status(),405);assert.deepEqual(errors,[]);
  const report={pairsDecoded:ids.length,imagesDecoded:ids.length*2,nativeWidth:1254,mobileOverflow:false,pageErrors:errors,downloadHead:200};
  fs.writeFileSync(path.join(evidence,'web-verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
