// Drives the actual WebGL player with pointer/keyboard input. No game methods are replaced.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '..');
const { chromium } = createRequire(path.join(root, 'Admin/package.json'))('@playwright/test');
const settings = require('./web-settings.json');
const touch = process.argv.includes('--touch');
const introOnly = process.argv.includes('--intro-only');
const out = path.join(root, 'NghiemThu/WebGame/20261025', introOnly ? 'intro' : touch ? 'flow-touch' : 'flow');
fs.mkdirSync(out, { recursive: true });

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.EDGE_PATH || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--enable-webgl', '--ignore-gpu-blocklist'] });
  const context = await browser.newContext({ viewport: { width: 1600, height: 1050 }, hasTouch: touch, isMobile: touch });
  const page = await context.newPage();
  await page.addInitScript(() => {
    window.__arenaQaVideos = [];
    const create = document.createElement;
    document.createElement = function(...args) {
      const element = create.apply(this, args);
      if (String(args[0]).toLowerCase() === 'video') window.__arenaQaVideos.push(element);
      return element;
    };
  });
  const report = { touch, pageErrors: [], consoleErrors: [], screenshots: [], videoRequests: [], profile: null };
  fs.writeFileSync(path.join(out, 'console.log'), '');
  const persist = () => fs.writeFileSync(path.join(out, 'result.json'), JSON.stringify(report, null, 2));
  page.on('pageerror', e => { report.pageErrors.push(e.message); persist(); });
  page.on('console', m => {
    fs.appendFileSync(path.join(out, 'console.log'), `[${m.type()}] ${m.text()}\n`);
    if (m.type() === 'error') { report.consoleErrors.push(m.text()); persist(); }
  });
  page.on('response', r => { if (r.url().includes(settings.introFile)) { report.videoRequests.push({ status: r.status(), url: r.url() }); persist(); } });
  async function capture(name) {
    const filename = name + '.png';
    await page.screenshot({ path: path.join(out, filename) });
    report.screenshots.push(filename); persist(); console.log('Captured ' + name);
  }
  async function clickDesign(x, y) {
    const box = await page.locator('#unity-canvas').boundingBox();
    const px = box.x + x / 1280 * box.width, py = box.y + y / 720 * box.height;
    if (touch) await page.touchscreen.tap(px, py);
    else await page.mouse.click(px, py);
    await page.waitForTimeout(1300);
  }
  try {
    await page.goto(`http://127.0.0.1:${settings.port}`);
    await page.waitForSelector('#modes button[aria-pressed="true"]');
    await page.click('[data-mode="pc"]');
    await page.click('#start');
    await page.waitForSelector('#game-frame[data-ready="true"]', { timeout: 240000 });
    await capture('01-intro');
    await page.waitForTimeout(3500);
    report.videoPlayback = await page.evaluate(() => window.__arenaQaVideos.map(v => ({ time: v.currentTime, width: v.videoWidth, height: v.videoHeight, frames: v.getVideoPlaybackQuality?.().totalVideoFrames, paused: v.paused })));
    await capture('01b-intro-playing');
    assert.ok(report.videoPlayback.some(v => v.time > 0 && v.width > 0 && v.frames > 0), 'Browser must actually decode and play intro frames');
    if (introOnly) { console.log(JSON.stringify({ passed: true, videoPlayback: report.videoPlayback })); return; }
    await page.waitForTimeout(5000);
    await capture('02-login-pc');
    for (const mode of ['phone', 'ipad', 'pc']) {
      await page.click(`[data-mode="${mode}"]`);
      await page.waitForTimeout(700);
      await capture('03-login-' + mode);
    }
    await clickDesign(640, 550); // Chơi ngay, mobile login panel.
    await capture('04-server');
    await clickDesign(640, 638); // Vào game, server start screen.
    await capture('05-lord-select');
    // Legacy select/name/story screens retain the 1920x1080 canvas design below.
    await clickDesign(960, 667); // CHỌN.
    await capture('06-confirm-lord');
    await clickDesign(773, 507); // Đồng ý.
    await capture('07-name');
    // The name input already has a generated valid name; enter a known name to test web input.
    if (!touch) {
      await clickDesign(734, 347);
      await page.keyboard.press('Control+A');
      await page.keyboard.type('WebAnhHung');
    }
    await capture('08-name-input');
    await clickDesign(794, 493); // BẮT ĐẦU.
    await capture('09-story');
    await clickDesign(1180, 40); // Bỏ qua.
    await page.waitForTimeout(7000);
    await capture('10-battle');
    await clickDesign(462, 645); // Summon the lord through its actual card.
    await capture('11-battle-summon');
    for (const mode of ['phone', 'ipad', 'pc']) {
      await page.click(`[data-mode="${mode}"]`);
      await page.waitForTimeout(700);
      await capture('12-battle-' + mode);
    }
    report.profile = await page.evaluate(async () => {
      for (const info of await indexedDB.databases()) {
        if (!info.name?.startsWith('/idbfs')) continue;
        const database = await new Promise((resolve, reject) => { const request = indexedDB.open(info.name); request.onsuccess = () => resolve(request.result); request.onerror = () => reject(request.error); });
        try {
          if (!database.objectStoreNames.contains('FILE_DATA')) continue;
          const store = database.transaction('FILE_DATA').objectStore('FILE_DATA');
          const [keys, values] = await Promise.all(['getAllKeys', 'getAll'].map(method => new Promise((resolve, reject) => { const r = store[method](); r.onsuccess = () => resolve(r.result); r.onerror = () => reject(r.error); })));
          const index = keys.findIndex(k => String(k).endsWith('/profile.json'));
          if (index >= 0) return JSON.parse(new TextDecoder().decode(values[index].contents));
        } finally { database.close(); }
      }
      return null;
    });
    persist();
    assert.ok(report.profile, 'Real Unity profile must reach IndexedDB');
    assert.equal(report.profile.introSeen, true);
    const character = touch ? report.profile.characters?.find(c => c.name) : report.profile.characters?.find(c => c.name === 'WebAnhHung');
    assert.ok(character, 'Mouse and keyboard flow must save the selected lord and entered name');
    assert.ok(character.lordHeroId);
    assert.equal(character.storySeen, true);
    assert.ok(report.videoRequests.some(r => r.status === 200 || r.status === 206), 'Intro must load its actual video');
    assert.deepEqual(report.pageErrors, []);
    assert.deepEqual(report.consoleErrors, []);
    console.log(JSON.stringify({ passed: true, screenshots: report.screenshots.length, profileSaved: true, selectedLord: character.lordHeroId }));
  } finally { persist(); await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
