const fs = require('node:fs');
const path = require('node:path');
const {pathToFileURL} = require('node:url');
const {createRequire} = require('node:module');
const {chromium} = createRequire(path.resolve(__dirname, '../../../Admin/package.json'))('@playwright/test');
(async () => {
  const browser = await chromium.launch({channel:'msedge', headless:true, chromiumSandbox:true});
  try {
    const page = await browser.newPage({viewport:{width:1400,height:1050}});
    await page.goto(pathToFileURL(path.join(__dirname,'gallery.html')).href);
    await page.waitForFunction(() => document.querySelectorAll('article').length >= 9);
    await page.waitForTimeout(1000);
    const files = await page.locator('article img').evaluateAll(xs => xs.map(x => ({src:x.getAttribute('src'), loaded:x.complete && x.naturalWidth>0})));
    const reeds = page.locator('article').filter({has:page.locator('img[src="15_reeds.png"]')});
    if (await reeds.count()) {
      await page.evaluate(() => document.documentElement.style.setProperty('--matte','#ded8c5'));
      await reeds.screenshot({path:path.join(__dirname,'qa-reeds-light.png')});
      await page.evaluate(() => document.documentElement.style.setProperty('--matte','#25332e'));
      await reeds.screenshot({path:path.join(__dirname,'qa-reeds-dark.png')});
    }
    await page.screenshot({path:path.join(__dirname,'qa-gallery.png')});
    fs.writeFileSync(path.join(__dirname,'gallery-check.json'),JSON.stringify({files, allLoaded:files.every(x=>x.loaded)},null,2));
    console.log(JSON.stringify({count:files.length,allLoaded:files.every(x=>x.loaded)}));
  } finally { await browser.close(); }
})().catch(e => {console.error(e);process.exitCode=1;});
