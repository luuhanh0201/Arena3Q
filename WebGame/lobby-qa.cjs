// Browser smoke test using an isolated QA profile in the real WebGL save store.
'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {createRequire}=require('node:module');
const root=path.resolve(__dirname,'..');
const {chromium}=createRequire(path.join(root,'Admin/package.json'))('@playwright/test');
const settings=require('./web-settings.json');
const show=process.argv.includes('--show');
const out=path.join(root,'NghiemThu/WebGame/Lobby_20261027');
const profileDirectory=path.join(out,'browser-profile');
fs.mkdirSync(out,{recursive:true});
const savedQa=fs.existsSync(path.join(out,'lobby-browser.json'))
  ?JSON.parse(fs.readFileSync(path.join(out,'lobby-browser.json'),'utf8')).seed:null;
async function main(){
  const context=await chromium.launchPersistentContext(profileDirectory,{
    channel:'msedge',headless:!show,chromiumSandbox:true,
    viewport:show?null:{width:1600,height:1050},
    args:['--enable-webgl','--ignore-gpu-blocklist','--mute-audio','--start-maximized']
  });
  const page=context.pages()[0]||await context.newPage();
  const report={passed:false,qaProfileOnly:true,mutedBrowser:true,pageErrors:[],consoleErrors:[],screenshots:[],modes:[]};
  const persist=()=>fs.writeFileSync(path.join(out,'lobby-browser.json'),JSON.stringify(report,null,2));
  fs.writeFileSync(path.join(out,'browser-console.log'),'');
  page.on('pageerror',error=>{report.pageErrors.push(error.message);persist()});
  page.on('console',message=>{
    fs.appendFileSync(path.join(out,'browser-console.log'),`[${message.type()}] ${message.text()}\n`);
    if(message.type()==='error'){report.consoleErrors.push(message.text());persist()}
  });
  const base=`http://127.0.0.1:${settings.port}`;
  async function start(){
    await page.goto(base,{waitUntil:'networkidle'});
    await page.locator('#modes button[data-mode="pc"]').click();
    await page.locator('#start').click();
    await page.waitForSelector('#game-frame[data-ready="true"]',{timeout:300000});
    await page.waitForTimeout(10000);
  }
  async function capture(name){
    await page.screenshot({path:path.join(out,name+'.png')});
    report.screenshots.push(name+'.png');persist();console.log('Captured '+name);
  }
  async function click(x,y){
    const box=await page.locator('#unity-canvas').boundingBox();
    await page.mouse.click(box.x+x/1280*box.width,box.y+y/720*box.height);
    await page.waitForTimeout(1800);
  }
  try{
    report.build=await(await page.request.get(base+'/build.json')).json();
    await start();
    await capture('01-startup');
    report.seed=savedQa||await page.evaluate(async()=>{
      const profile={version:1,accountId:'guest-lobby-web-preview',accountKind:1,accountLabel:'Khách',introSeen:true,lastServerId:'s3',characters:[{
        serverId:'s3',lordHeroId:'lord_strategist',name:'Lâm Tử Yên',lordLevel:15,lordStars:5,lordExperience:65,vip:12,combatPower:1245680,
        wallet:[{id:'Banknotes',amount:12680,capacity:0},{id:'Silver',amount:5320000,capacity:0},{id:'Stamina',amount:180,capacity:180}],
        lordSkillLevels:[0,0,0,0],storySeen:true,tutorialCleared:true,heroes:[{heroId:'guardian',level:1,stars:3,shards:0}]
      }]};
      for(const info of await indexedDB.databases()){
        if(!info.name?.startsWith('/idbfs'))continue;
        const db=await new Promise((resolve,reject)=>{const r=indexedDB.open(info.name);r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error)});
        try{
          if(!db.objectStoreNames.contains('FILE_DATA'))continue;
          const tx=db.transaction('FILE_DATA','readwrite'),store=tx.objectStore('FILE_DATA');
          const keys=await new Promise((resolve,reject)=>{const r=store.getAllKeys();r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error)});
          const key=keys.find(key=>String(key).endsWith('/profile.json'));
          if(!key)continue;
          const previous=await new Promise((resolve,reject)=>{const r=store.get(key);r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error)});
          store.put({...previous,timestamp:new Date(),contents:new TextEncoder().encode(JSON.stringify(profile))},key);
          await new Promise((resolve,reject)=>{tx.oncomplete=resolve;tx.onerror=()=>reject(tx.error)});
          return {database:info.name,key,character:profile.characters[0].name};
        }finally{db.close()}
      }
      throw Error('The real WebGL profile.json was not saved');
    });
    // Reload the player so its own profile loader reads the seeded QA save.
    if(!savedQa)await start();
    await capture('02-returning-player');
    await click(640,638);
    await page.waitForTimeout(6000);
    await capture('03-lobby-pc');
    for(const mode of ['phone','ipad','pc']){
      await page.locator(`#modes button[data-mode="${mode}"]`).click();
      await page.waitForTimeout(2000);
      const actual=await page.locator('#unity-canvas').evaluate(canvas=>{const box=canvas.getBoundingClientRect();return {width:canvas.width,height:canvas.height,cssWidth:box.width,cssHeight:box.height,dpr:devicePixelRatio}});
      const ratio=Math.min(actual.dpr,settings.renderQuality.maxPixelRatio,settings.renderQuality.maxLongEdge/Math.max(actual.cssWidth,actual.cssHeight),Math.sqrt(settings.renderQuality.maxPixels/(actual.cssWidth*actual.cssHeight)));
      assert.ok(Math.abs(actual.width-Math.round(actual.cssWidth*ratio))<=2);assert.ok(Math.abs(actual.height-Math.round(actual.cssHeight*ratio))<=2);
      report.modes.push({mode,...actual});
      await capture('04-lobby-'+mode);
    }
    report.viewport=await page.evaluate(()=>({innerWidth,innerHeight,outerWidth,outerHeight}));
    if(show)assert.ok(Math.abs(report.viewport.outerWidth-report.viewport.innerWidth)<100,'Visible preview uses the real browser viewport');
    assert.deepEqual(report.pageErrors,[]);assert.deepEqual(report.consoleErrors,[]);
    report.passed=true;persist();
    console.log(JSON.stringify({passed:true,modes:report.modes,profileDirectory,screenshots:report.screenshots.length}));
  }finally{persist();if(!show)await context.close()}
}
main().catch(error=>{console.error(error);process.exitCode=1});
