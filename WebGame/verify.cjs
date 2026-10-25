const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '..');
const { chromium } = createRequire(path.join(root, 'Admin/package.json'))('@playwright/test');
const settings = require('./web-settings.json');
const evidence = path.join(root, 'NghiemThu/WebGame/20261025');
const base = `http://127.0.0.1:${settings.port}`;
const uiOnly = process.argv.includes('--ui');
fs.mkdirSync(evidence, { recursive: true });

(async () => {
  const browser = await chromium.launch({
    executablePath: process.env.EDGE_PATH || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
    headless: true,
    args: ['--enable-webgl', '--ignore-gpu-blocklist']
  });
  const report = { uiOnly, layouts: [], pageErrors: [], consoleErrors: [], requestsFailed: [] };
  try {
    const context = await browser.newContext({ viewport: { width: 1600, height: 1050 } });
    const page = await context.newPage();
    page.on('pageerror', e => report.pageErrors.push(e.message));
    page.on('console', m => { if (m.type() === 'error') report.consoleErrors.push(m.text()); });
    page.on('requestfailed', r => report.requestsFailed.push({ url: r.url(), error: r.failure() }));
    let loaderRequests = 0;
    page.on('request', r => { if (r.url().includes('.loader.js')) loaderRequests++; });
    assert.equal((await page.goto(base)).status(), 200);
    await page.waitForSelector('#modes button[aria-pressed="true"]');
    await page.evaluate(() => document.fonts.ready);
    assert.equal(await page.locator('#modes button').count(), 3);
    assert.equal(await page.locator('#game-frame').getAttribute('data-mode'), 'pc');
    assert.ok(await page.evaluate(() => [...document.images].every(i => i.complete && i.naturalWidth > 0)));
    await page.screenshot({ path: path.join(evidence, 'welcome-desktop.png') });
    if (!uiOnly) {
      await page.click('#start');
      await page.waitForSelector('#game-frame[data-ready="true"]', { timeout: 240000 });
      await page.waitForTimeout(5000);
      assert.equal(await page.locator('#cover').isHidden(), true);
    }
    for (const viewport of [{ width: 1600, height: 1050 }, { width: 1024, height: 768 }, { width: 915, height: 412 }, { width: 412, height: 915 }, { width: 320, height: 568 }]) {
      await page.setViewportSize(viewport);
      for (const mode of settings.modes) {
        await page.click(`[data-mode="${mode.id}"]`);
        await page.waitForTimeout(uiOnly ? 40 : 1200);
        const dimensions = await page.evaluate(() => {
          const c = document.getElementById('unity-canvas');
          const f = document.getElementById('game-frame').getBoundingClientRect();
          return { width: c.width, height: c.height, x: f.x, y: f.y, w: f.width, h: f.height, overflow: document.documentElement.scrollWidth > innerWidth || document.documentElement.scrollHeight > innerHeight };
        });
        assert.equal(dimensions.width, mode.width);
        assert.equal(dimensions.height, mode.height);
        assert.ok(!dimensions.overflow, `Overflow at ${viewport.width} ${mode.id}`);
        assert.ok(dimensions.x >= -1 && dimensions.y >= 0 && dimensions.x + dimensions.w <= viewport.width + 1 && dimensions.y + dimensions.h <= viewport.height + 1);
        assert.ok(Math.abs(dimensions.w / dimensions.h - mode.width / mode.height) < .025);
        if (!uiOnly) assert.equal(await page.locator('#game-frame').getAttribute('data-ready'), 'true');
        const filename = `${uiOnly ? 'ui' : 'game'}-${viewport.width}-${mode.id}.png`;
        await page.screenshot({ path: path.join(evidence, filename) });
        report.layouts.push({ viewport, mode: mode.id, ...dimensions, screenshot: filename });
      }
    }
    assert.equal(loaderRequests, uiOnly ? 0 : 1, 'Switching display must keep the Unity instance');
    report.loaderRequests = loaderRequests;
    await page.reload();
    await page.waitForSelector('#modes button[aria-pressed="true"]');
    assert.equal(await page.locator('#game-frame').getAttribute('data-mode'), 'pc');
    await page.click('[data-mode="ipad"]');
    await page.reload();
    await page.waitForSelector('#modes button[aria-pressed="true"]');
    assert.equal(await page.locator('#game-frame').getAttribute('data-mode'), 'ipad');
    report.persistence = true;

    const video = '/Player/StreamingAssets/' + settings.introFile;
    if (!uiOnly) {
      const range = await page.request.get(base + video, { headers: { Range: 'bytes=0-1023' } });
      assert.equal(range.status(), 206); assert.equal((await range.body()).length, 1024);
      report.videoRange = 206;
      const manifest = await (await page.request.get(base + '/build.json')).json();
      const payload = await page.request.head(base + '/' + manifest.codeUrl);
      assert.equal(payload.status(), 200);
      report.wasmBytes = Number(payload.headers()['content-length']);
    }
    assert.equal((await page.request.get(base + '/../../CLAUDE.md')).status(), 404);
    assert.equal((await page.request.post(base)).status(), 405);
    assert.deepEqual(report.pageErrors, []);
    assert.deepEqual(report.consoleErrors, []);
    fs.writeFileSync(path.join(evidence, uiOnly ? 'ui-verification.json' : 'game-verification.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify({ layouts: report.layouts.length, uiOnly, pageErrors: report.pageErrors, consoleErrors: report.consoleErrors, loaderRequests, persistence: true }));
  } finally {
    fs.writeFileSync(path.join(evidence, uiOnly ? 'ui-latest.json' : 'game-latest.json'), JSON.stringify(report, null, 2));
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
