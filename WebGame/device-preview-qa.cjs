'use strict';
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const { chromium } = require('../Admin/node_modules/playwright');
const config = JSON.parse(fs.readFileSync(path.join(__dirname, 'web-settings.json')));
const out = 'NghiemThu/Chapter01/P2_DeviceControls_20261028';
(async () => {
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--mute-audio'] });
  const page = await browser.newPage({ viewport: { width: 1680, height: 1000 } });
  const errors = [], cases = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.addInitScript(() => {
    window.deviceMessages = [];
    // Test wrapper controls only; no Unity payload is loaded in this fixture.
    HTMLCanvasElement.prototype.getContext = () => ({ getExtension: () => null });
  });
  await page.route('http://127.0.0.1:4398/**', async route => {
    const pathname = new URL(route.request().url()).pathname;
    if (pathname === '/build.json') return route.fulfill({ json: { loaderUrl: '/fixture-loader.js' } });
    if (pathname === '/fixture-loader.js') return route.fulfill({ contentType: 'text/javascript', body:
      'window.createUnityInstance=async()=>({Module:{},SendMessage:(target,method,json)=>window.deviceMessages.push({target,method,payload:JSON.parse(json)})});' });
    const filename = pathname === '/' ? 'index.html' : path.basename(pathname);
    if (pathname.startsWith('/art/')) return route.fulfill({ status: 200, body: '' });
    const file = path.join(__dirname, filename);
    if (!fs.existsSync(file)) return route.fulfill({ status: 404, body: '' });
    return route.fulfill({ body: fs.readFileSync(file), contentType: filename.endsWith('.html') ? 'text/html' : filename.endsWith('.css') ? 'text/css' : filename.endsWith('.json') ? 'application/json' : 'text/javascript' });
  });
  try {
    await page.goto('http://127.0.0.1:4398/');
    await page.getByRole('button', { name: 'Điện thoại', exact: true }).click();
    await page.locator('#start').click();
    await page.waitForFunction(() => document.getElementById('game-frame').dataset.ready === 'true');
    for (const device of config.phoneDevices) for (const preset of config.displayPresets) {
      await page.selectOption('#phone-device', device.id);
      await page.selectOption('#display-preset', String(preset.frameRate));
      const actual = await page.evaluate(() => {
        const box = document.getElementById('game-frame').getBoundingClientRect();
        return { message: window.deviceMessages.at(-1), aspect: box.width/box.height,
          pageOverflow: document.documentElement.scrollWidth > innerWidth };
      });
      assert.equal(actual.message.target, 'WebDisplayPreview'); assert.equal(actual.message.method, 'Apply');
      assert.equal(actual.message.payload.width, device.width); assert.equal(actual.message.payload.frameRate, preset.frameRate);
      assert.equal(actual.message.payload.left, device.safeInsets.left/device.width);
      assert.ok(Math.abs(actual.aspect-device.width/device.height)<.001);
      assert.equal(actual.pageOverflow, false);
      cases.push({device:device.id,frameRate:preset.frameRate,...actual});
    }
    await page.getByRole('button', { name: 'PC', exact: true }).click();
    assert.equal(await page.locator('#phone-control').isHidden(), true);
    const pc = await page.evaluate(() => window.deviceMessages.at(-1).payload);
    assert.equal(pc.dpi, 0); assert.equal(pc.left, 0); assert.equal(pc.right, 0);
    assert.deepEqual(errors, []);
    fs.writeFileSync(out+'/wrapper-results.json',JSON.stringify({passed:true,scope:'Wrapper with fake Unity only',cases,pageErrors:errors},null,2));
    console.log('PASS: 12 phone/preset cases, PC reset, 0 page errors. Unity runtime not tested here.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode=1; });
