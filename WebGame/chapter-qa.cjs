'use strict';
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const { chromium } = require('../Admin/node_modules/playwright');
const { seed, lobbyProfile } = require('./qa-profile.cjs');
const settings = require('./web-settings.json');
const baseline = process.argv.includes('--baseline');
const worldQa = process.argv.includes('--world');
const uiReview = process.argv.includes('--ui-review');
const detailsReview = process.argv.includes('--details-review');
const motionReview = process.argv.includes('--motion-review') || detailsReview;
const out = path.resolve('NghiemThu/Chapter01/' + (worldQa ? 'P3_Web_' : 'P2_Web_') + (baseline ? 'before_' : '') + new Date().toISOString().replace(/[:.]/g, '-'));
const base = `http://127.0.0.1:${settings.port}`;
fs.mkdirSync(out, { recursive: true });
const report = { passed: false, mutedBrowser: true, isolatedProfile: true, errors: [], cases: [], screenshots: [] };
const save = () => fs.writeFileSync(path.join(out, 'result.json'), JSON.stringify(report, null, 2));
save();
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true, chromiumSandbox: true,
    args: ['--enable-webgl', '--ignore-gpu-blocklist', '--mute-audio'] });
  const context = await browser.newContext({ viewport: { width: 1680, height: 1000 }, hasTouch: true, deviceScaleFactor: 1 });
  const page = await context.newPage(); let reading = null, readingId = 0, readingParts = [];
  page.on('pageerror', error => { report.errors.push(error.message); save(); });
  page.on('console', message => {
    const text = message.text(); fs.appendFileSync(path.join(out, 'console.log'), `[${message.type()}] ${text}\n`);
    const chunk = /\[ChapterMapQA:(\d+):(\d+)\]([\s\S]*)/.exec(text);
    if (chunk) {
      const part = Number(chunk[1]), count = Number(chunk[2]);
      if (part === 0) readingParts = [];
      assert.equal(part, readingParts.length, 'UI telemetry chunks must arrive in order.');
      readingParts.push(chunk[3].replace(/\r?\n$/, ''));
      if (readingParts.length === count) { reading = JSON.parse(readingParts.join('')); readingId++; }
    }
    if (message.type() === 'error') { report.errors.push(text); save(); }
  });
  await page.route('**/*.loader.js*', async route => {
    const response = await route.fetch();
    const hook = ';{const create=window.createUnityInstance;window.createUnityInstance=(...args)=>{const promise=create(...args);promise.then(instance=>window.__arenaQaInstance=instance);return promise};}';
    await route.fulfill({ response, body: await response.text() + hook });
  });
  async function capture(name) {
    if(!baseline&&name!=='FAILED'&&reading?.modal){
      const ready=await waitState(s=>s.modalReady&&s.modalAlpha>=.999);
      const close=ready.buttons.find(b=>b.name==='Close');
      assert.ok(close?.active,'Close must be interactable after modal fade.');
      assert.ok(close.rect.height*160/ready.dpi>=48-.5,'Close target must be at least 48 dp.');
      await page.waitForTimeout(100); // Let GameButton leave the disabled visual state after UiReveal.
    }
    await page.screenshot({ path: path.join(out, name + '.png') }); report.screenshots.push(name + '.png'); save();
  }
  async function clickDesign(x, y) {
    const box = await page.locator('#unity-canvas').boundingBox();
    await page.touchscreen.tap(box.x + x / 1280 * box.width, box.y + y / 720 * box.height);
    await page.waitForTimeout(1600);
  }
  async function start() {
    await page.goto(base); await page.locator('#modes button[data-mode="pc"]').click(); await page.locator('#start').click();
    await page.waitForSelector('#game-frame[data-ready="true"]', { timeout: 300000 }); await page.waitForTimeout(9000);
  }
  async function inspect() {
    const previous = readingId;
    await page.evaluate(() => window.__arenaQaInstance.SendMessage('CH01_MapSystems', 'InspectUi'));
    const end = Date.now() + 10000;
    while (readingId === previous && Date.now() < end) await page.waitForTimeout(100);
    assert.ok(readingId > previous, 'The real chapter runtime must answer the read-only inspection.');
    if(!baseline){
      for(const label of reading.labels.filter(x=>x.name==='Stars'&&/^\d+\/\d+$/.test(x.text))){const [current,limit]=label.text.split('/').map(Number);assert.ok(current<=limit,'Chest count must stop at its milestone.');}
      assert.ok(Array.isArray(reading.occluders),'HUD exclusion telemetry must be present.');
      const overlap=(a,b)=>a.x<b.x+b.width&&a.x+a.width>b.x&&a.y<b.y+b.height&&a.y+a.height>b.y;
      const quest=reading.labels.find(x=>x.name==='Text');
      for(const arrow of reading.directions||[])if(arrow.active&&quest)
        assert.ok(!overlap(quest.rect,arrow.rect),'Quest text intersects the rotated direction arrow.');
      for(const marker of reading.markers)for(const hud of reading.occluders.filter(x=>x.active))
        if(overlap(marker.visualRect,hud.rect)){
          assert.ok(marker.alpha<=.001,marker.name+' visible over '+hud.name);
          assert.ok(!marker.active,marker.name+' clickable over '+hud.name);
        }
    }
    return reading;
  }
  async function tapRect(rect, state) {
    const box = await page.locator('#unity-canvas').boundingBox();
    await page.touchscreen.tap(box.x + (rect.x + rect.width / 2) / state.width * box.width,
      box.y + (1 - (rect.y + rect.height / 2) / state.height) * box.height);
    await page.waitForTimeout(500);
  }
  async function waitState(predicate, timeout = 20000) {
    const end = Date.now() + timeout;
    while (Date.now() < end) { const state = await inspect(); if (predicate(state)) return state; await page.waitForTimeout(300); }
    throw new Error('Chapter interaction timed out: ' + JSON.stringify(reading));
  }
  try {
    report.build = await (await page.request.get(base + '/build.json')).json();
    await start(); await seed(page, lobbyProfile()); await start();
    await clickDesign(640, 550); await clickDesign(640, 638); await page.waitForTimeout(3500);
    await capture('01-lobby'); await clickDesign(1080, 630); await page.waitForTimeout(6000);
    if (baseline) { await capture('02-map-pc'); report.scope = 'Screenshot baseline only; no runtime assertions'; save(); console.log(out); return; }
    let state = await inspect(); assert.equal(state.unlocked, 1); await capture('02-map-pc');
    await page.locator('#modes button[data-mode="phone"]').click();
    for (const device of settings.phoneDevices) for (const preset of settings.displayPresets) {
      await page.selectOption('#phone-device', device.id); await page.selectOption('#display-preset', String(preset.frameRate));
      await page.waitForTimeout(1300); state = await inspect();
      assert.equal(state.previewFrameRate, preset.frameRate); assert.ok(state.frameRate <= preset.frameRate && state.frameRate > 0);
      assert.ok(Math.abs(state.width / state.height - device.width / device.height) < .005);
      assert.ok(Math.abs(state.safeArea.x / state.width - device.safeInsets.left / device.width) < .0001);
      assert.ok(Math.abs(state.dpi - device.dpi * state.width / device.width) < .1);
      for (const button of state.buttons.filter(x => ['Back','Information','Add'].includes(x.name))) {
        const b = button.rect, s = state.safeArea;
        assert.ok(b.x >= s.x - 1 && b.y >= s.y - 1 && b.x+b.width <= s.x+s.width+1 && b.y+b.height <= s.y+s.height+1, button.name + ' outside safe area');
      }
      const quest = state.markers.find(marker => marker.stage === 0 && marker.target === 0 && marker.active);
      assert.ok(quest, 'The first unlocked quest marker must remain visible.');
      assert.ok(quest.rect.width >= 56 * state.dpi / 160 - 1, 'Marker must retain a 56 dp touch target.');
      assert.deepEqual(state.labels.filter(label => label.text && label.overflow).map(label => label.name), [], 'Text must not be clipped.');
      report.cases.push({ device: device.id, preset: preset.frameRate, state });
      await capture(`03-map-${device.id}-${preset.frameRate}`);
    }
    await page.selectOption('#phone-device', 'redmi'); await page.selectOption('#display-preset', '60'); await page.waitForTimeout(800);
    state = await inspect(); await tapRect(state.buttons.find(x => x.name === 'Information').rect, state);
    state = await waitState(s => s.modal); await capture('04-chapter-info');
    await tapRect(state.buttons.find(x => x.name === 'Close').rect, state); await waitState(s => !s.modal);
    state = await inspect(); await tapRect(state.markers.find(x => x.stage === 0 && x.target === 0 && x.active).rect, state);
    state = await waitState(s => s.buttons.some(x => x.name === 'AcceptQuest')); await capture('05-quest-dialogue');
    await tapRect(state.buttons.find(x => x.name === 'AcceptQuest').rect, state);
    state = await waitState(s => !s.modal && s.markers.some(x => x.stage === 0 && x.target === 1 && x.active));
    await capture('06-quest-accepted');
    await tapRect(state.markers.find(x => x.stage === 0 && x.target === 1 && x.active).rect, state);
    state = await waitState(s => s.modalReady&&s.buttons.some(x => x.name === 'Deploy'&&x.active));
    assert.ok(state.labels.some(x=>x.name==='Rewards'&&x.text.includes('Phần thưởng')),'Stage panel must show reward information.');
    await capture('07-stage-panel');
    await tapRect(state.buttons.find(x => x.name === 'Deploy').rect, state);
    state = await waitState(s => s.message === 'Chức năng đang phát triển'); assert.equal(state.unlocked, 1);
    await capture('08-missing-battle-does-not-unlock');
    await tapRect(state.buttons.find(x => x.name === 'Close').rect, state); await waitState(s => !s.modal);
    state = await inspect(); await tapRect(state.buttons.find(x => x.name === 'Back').rect, state); await page.waitForTimeout(4000);
    await capture('09-return-lobby');
    if (worldQa) {
      report.world = [];
      for (const checkpoint of (detailsReview?[1,4,7,'paddy','bamboo']:motionReview?[1,4,7,'bamboo']:uiReview?[1,4,7,'altar']:[1,4,7])) {
        const cleared=typeof checkpoint==='string'?7:checkpoint;
        const profile = lobbyProfile();
        profile.characters[0].chapters = [{ chapterId: 'chapter01', revealedStages: cleared,
          stages: Array.from({length:cleared},(_,i)=>({stageId:`ch01_${String(i+1).padStart(2,'0')}`,questAccepted:true,stars:3})),
          claimedRewards:[],pendingBattleId:'',pendingStageId:'' }];
        if(typeof checkpoint==='string'){
          const config=fs.readFileSync('Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Data/CH01_MapConfig.asset','utf8');
          const altar=config.split(checkpoint==='altar'?'  - id: ch01_07':checkpoint==='paddy'?'  - id: ch01_02':'  - id: ch01_03')[1].match(/center: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/);
          Object.assign(profile.characters[0].chapters[0],{hasReturnPose:true,playerX:-50,playerY:0,playerZ:-34,cameraX:Number(altar[1]),cameraY:Number(altar[2]),cameraZ:Number(altar[3]),cameraSize:22});
        }
        await seed(page, profile); await start();
        await clickDesign(640,550); await clickDesign(640,638); await page.waitForTimeout(3500);
        await clickDesign(1080,630);
        state = await waitState(s => s.unlocked === Math.min(7,cleared+1));
        if(cleared<7) {
          assert.ok(state.revealing,'A newly unlocked region must play its reveal.');
          assert.ok(!state.markers.some(m=>m.stage===cleared&&m.active),'New markers wait for the reveal.');
          await capture(`10-world-${cleared+1}-during`);
          state=await waitState(s=>!s.revealing);
          assert.equal(state.revealed,cleared+1);
        } else assert.ok(!state.revealing,'Fully revealed progress must restore directly.');
        assert.deepEqual(state.fogVisibility,Array.from({length:7},(_,i)=>i<Math.min(7,cleared+1)?1:0));
        // Reveal completion starts the marker fade; inspect its final visible state before taking the after image.
        await page.waitForTimeout(600); state=await inspect();
        if(cleared<7)assert.ok(state.markers.some(m=>m.stage===cleared&&m.target===-1&&m.active),'The newly revealed stage marker must become interactive.');
        await capture(typeof checkpoint==='string'?`12-${checkpoint}-unlocked`:`11-world-${cleared}-cleared`);report.world.push({cleared,checkpoint,state});save();
        if(checkpoint==='bamboo'||checkpoint==='paddy'){
          for(const preset of settings.displayPresets){
            await page.selectOption('#display-preset',String(preset.frameRate));await page.waitForTimeout(900);
            state=await inspect();assert.equal(state.previewFrameRate,preset.frameRate);assert.equal(state.unlocked,7);
            for(let frame=0;frame<5;frame++){await capture(detailsReview?`14-details-${checkpoint}-${preset.frameRate}-${frame}`:`14-environment-${preset.frameRate}-${frame}`);await page.waitForTimeout(250);}
          }
        }
        if(checkpoint==='altar'){
          assert.ok(state.markers.some(m=>m.stage===6&&m.target===-1&&m.active),'Unlocked altar marker must be visible and clickable.');
          await tapRect(state.markers.find(m=>m.stage===6&&m.target===-1).rect,state);
          state=await waitState(s=>s.modalReady);await capture('13-altar-stage-panel');
          await tapRect(state.buttons.find(x=>x.name==='Close').rect,state);state=await waitState(s=>!s.modal);
        }
        await tapRect(state.buttons.find(x=>x.name==='Back').rect,state);await page.waitForTimeout(4000);
      }
    }
    assert.deepEqual(report.errors, []);
    report.passed = true; save(); console.log('PASS: real WebGL chapter, 12 phone/preset cases, touch quest/battle/back; audio muted. ' + out);
  } catch (error) { report.failure = error.stack; save(); await capture('FAILED'); throw error; }
  finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
