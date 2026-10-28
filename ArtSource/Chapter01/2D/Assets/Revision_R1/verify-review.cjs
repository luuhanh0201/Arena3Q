'use strict';
const fs=require('node:fs'),path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require('../../../../../Admin/node_modules/@playwright/test');
const {review}=require('./process-assets.cjs');
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true});try{
 const page=await browser.newPage({viewport:{width:1400,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.join(review,'review.html')).href);
 await page.waitForFunction(()=>document.querySelectorAll('figure img').length===29&&[...document.querySelectorAll('figure img')].every(i=>i.complete&&i.naturalWidth>0));
 const count=await page.locator('figure').count();await page.screenshot({path:path.join(review,'review-browser.png')});
 const first=await page.locator('figure img').first().getAttribute('src');await page.getByRole('button',{name:'Cập nhật ảnh'}).click();await page.waitForFunction(old=>document.querySelector('figure img').getAttribute('src')!==old,first);
 const result={count,allLoaded:count===29,refreshWorks:true,errors};fs.writeFileSync(path.join(__dirname,'review-check.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));if(errors.length)throw Error('Browser errors');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
