'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {createRequire}=require('node:module'),root=path.resolve(__dirname,'..');
const {chromium}=createRequire(path.join(root,'Admin/package.json'))('@playwright/test');
const {seed,lobbyProfile}=require('./qa-profile.cjs'),settings=require('./web-settings.json');
const phase=process.argv.includes('--before')?'before':process.argv.includes('--wrapper-only')?'wrapper':'after';
const out=path.join(root,'NghiemThu/WebGame/Clarity_20261028',phase+'_'+new Date().toISOString().replace(/[:.]/g,'-'));
fs.mkdirSync(out,{recursive:true});
async function main(){
  const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--enable-webgl','--ignore-gpu-blocklist','--mute-audio']});
  const report={phase,passed:false,screenshots:[],measurements:[],errors:[],consoleErrors:[],warnings:[],frames:[]};
  const save=()=>fs.writeFileSync(path.join(out,'result.json'),JSON.stringify(report,null,2));
  async function scenario(ratio){
  const context=await browser.newContext({viewport:{width:1600,height:1050},deviceScaleFactor:ratio}),page=await context.newPage();
  page.on('pageerror',error=>{report.errors.push(error.message);save()});
  page.on('console',message=>{if(message.type()==='error')report.consoleErrors.push(message.text());if(message.type()==='warning')report.warnings.push(message.text())});
  const base=`http://127.0.0.1:${settings.port}`;
  if(phase==='before'){
    const baseline=path.join(root,'NghiemThu/WebGame/Clarity_20261028');
    await page.route('**/player.mjs',route=>route.fulfill({contentType:'text/javascript',body:fs.readFileSync(path.join(baseline,'baseline-player.mjs'),'utf8')}));
    await page.route('**/web-settings.json',route=>route.fulfill({contentType:'application/json',body:fs.readFileSync(path.join(baseline,'baseline-web-settings.json'),'utf8')}));
    report.baselineWrapperRef='4b23c57';
  }
  async function shot(name){await page.screenshot({path:path.join(out,name+'.png')});report.screenshots.push(name+'.png');save()}
  async function mode(id){await page.locator(`#modes button[data-mode="${id}"]`).click();await page.waitForTimeout(650)}
  async function click(x,y){const box=await page.locator('#unity-canvas').boundingBox();await page.mouse.click(box.x+x/1280*box.width,box.y+y/720*box.height);await page.waitForTimeout(1200)}
  async function matrix(screen){
      for(const id of ['phone','ipad','pc']){
        await mode(id);
        if(phase!=='before'){
          await page.waitForFunction(quality=>{const canvas=document.getElementById('unity-canvas'),box=canvas.getBoundingClientRect();const density=Math.min(devicePixelRatio,quality.maxPixelRatio,quality.maxLongEdge/Math.max(box.width,box.height),Math.sqrt(quality.maxPixels/(box.width*box.height)));return Math.abs(canvas.width-Math.round(box.width*density))<=2&&Math.abs(canvas.height-Math.round(box.height*density))<=2},settings.renderQuality,{timeout:15000}).catch(async error=>{await shot('FAILED_'+screen+'_'+id+'_DPR'+ratio);report.failedDimensions=await page.evaluate(()=>{const canvas=document.getElementById('unity-canvas'),box=canvas.getBoundingClientRect();return {width:canvas.width,height:canvas.height,cssWidth:box.width,cssHeight:box.height,dpr:devicePixelRatio}});save();throw error});
        }
        const measured=await page.evaluate(()=>{const canvas=document.getElementById('unity-canvas'),box=canvas.getBoundingClientRect();return {width:canvas.width,height:canvas.height,cssWidth:box.width,cssHeight:box.height,dpr:devicePixelRatio,overflow:document.documentElement.scrollWidth>innerWidth}});
        report.measurements.push({screen,mode:id,...measured});save();
        assert.ok(Math.abs(measured.dpr-ratio)<.01,'The browser DPR must match this case');
        assert.ok(!measured.overflow);assert.ok(Math.abs(measured.width/measured.height-measured.cssWidth/measured.cssHeight)<.01);
        if(phase!=='before'){
          assert.ok(measured.width>=Math.round(measured.cssWidth*Math.min(ratio,settings.renderQuality.maxPixelRatio))-2,'Backing buffer must follow physical pixels: '+JSON.stringify({ratio,mode:id,...measured}));
          assert.ok(Math.max(measured.width,measured.height)<=settings.renderQuality.maxLongEdge+2);
        }
        if(screen==='Lobby'){
          const timing=await page.evaluate(()=>new Promise(resolve=>{const intervals=[];let previous=performance.now(),start=previous;function frame(now){intervals.push(now-previous);previous=now;if(now-start<3000)requestAnimationFrame(frame);else{intervals.sort((a,b)=>a-b);resolve({method:'browser requestAnimationFrame cadence',frames:intervals.length,seconds:(now-start)/1000,fps:intervals.length*1000/(now-start),p95:intervals[Math.floor(intervals.length*.95)]})}}requestAnimationFrame(frame)}));
          report.frames.push({mode:id,dpr:ratio,...timing});
        }
        await shot(screen+'_'+id+'_DPR'+ratio);
      }
    await mode('pc');console.log('Captured '+screen+' / DPR '+ratio+' / 3 modes');
  }
  async function start(){await page.goto(base);await page.locator('#modes button[data-mode="pc"]').click();await page.locator('#start').click();await page.waitForSelector('#game-frame[data-ready="true"]',{timeout:300000})}
  try{
    report.build=await(await page.request.get(base+'/build.json')).json();
    await start();await shot('Splash_DPR'+ratio);await page.waitForTimeout(2600);await shot('Loading_DPR'+ratio);await page.waitForTimeout(6000);
    await matrix('Login');
    if(process.argv.includes('--quick'))return;
    await click(640,550);await matrix('Server');await click(640,638);await matrix('LordSelect');
    await click(960,667);await click(773,507);await matrix('Name');await click(794,493);await matrix('Story');
    await click(1180,40);await page.waitForTimeout(6000);await matrix('Battle');
    report.qaProfile=await seed(page,lobbyProfile());await start();await page.waitForTimeout(9000);await click(640,638);await page.waitForTimeout(4000);await matrix('Lobby');
  }finally{save();await context.close()}
  }
  try{
    for(const ratio of [1,1.25,2])await scenario(ratio);
    assert.deepEqual(report.errors,[]);assert.deepEqual(report.consoleErrors,[]);report.passed=true;save();console.log(JSON.stringify({passed:true,phase,out,screenshots:report.screenshots.length,measurements:report.measurements.length}));
  }finally{save();await browser.close()}
}
main().catch(error=>{console.error(error);process.exitCode=1});
