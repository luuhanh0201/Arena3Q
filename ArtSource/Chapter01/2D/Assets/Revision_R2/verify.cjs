'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../../../../Admin/node_modules/sharp');
const {chromium}=require('../../../../../Admin/node_modules/@playwright/test');
const {pathToFileURL}=require('node:url');
const base=path.resolve(__dirname,'..'),review=path.resolve(base,'../../../../NghiemThu/Chapter01/XemTruoc_DoiMau_20261028');
(async()=>{
 const checks=[];
 const manifest=JSON.parse(fs.readFileSync(path.join(base,'images.json'),'utf8').replace(/^\uFEFF/,''));
 for(const id of ['01_ground-earth','02_ground-path','04_paddy-ground']){
  const entry=manifest.find(x=>x.file===id+'.png'),file=path.join(base,entry.file);
  if(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')!==entry.sha256)throw Error('Manifest hash '+id);
  for(const size of [512,1024]){
   const {data,info}=await sharp(path.join(base,'Export',String(size),entry.file)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
   if(info.width!==size||info.height!==size)throw Error('Dimensions '+id);
   let edge=0;for(let y=0;y<size;y++)for(let x=0;x<size;x++){const i=(y*size+x)*4;if(data[i+3]!==255)throw Error('Opacity '+id);if(!x)for(let c=0;c<3;c++)edge=Math.max(edge,Math.abs(data[i+c]-data[(y*size+size-1)*4+c]));if(!y)for(let c=0;c<3;c++)edge=Math.max(edge,Math.abs(data[i+c]-data[((size-1)*size+x)*4+c]));}
   if(edge>1)throw Error('Tile edge '+id);checks.push({id,size,opaque:true,maxEdgeDelta:edge});
  }
 }
 const atlas=JSON.parse(fs.readFileSync(path.join(base,'Export/atlas.json')));
 for(const [file,hash] of Object.entries(atlas.sourceHashes))if(crypto.createHash('sha256').update(fs.readFileSync(path.join(base,file))).digest('hex')!==hash)throw Error('Stale sprite atlas '+file);
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true});
 let errors=[],count=0;try{
  const page=await browser.newPage({viewport:{width:1200,height:1000}});page.on('pageerror',e=>errors.push(e.message));
  await page.goto(pathToFileURL(path.join(review,'review.html')).href);
  await page.waitForFunction(()=>document.images.length===3&&[...document.images].every(i=>i.complete&&i.naturalWidth===1024));
  count=await page.locator('figure').count();await page.screenshot({path:path.join(review,'browser-check.png'),fullPage:true});
 }finally{await browser.close();}
 if(errors.length)throw Error(errors.join('\n'));
 const report={checks,atlasSourceHashesValid:true,browser:{images:count,errors},scope:'Offline export/browser only; no Unity lighting, compressed mip or device FPS verification'};
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exitCode=1;});
