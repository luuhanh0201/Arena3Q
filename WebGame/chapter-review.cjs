'use strict';
const fs = require('node:fs'), path = require('node:path');
const { chromium } = require('../Admin/node_modules/playwright');
const root = path.resolve('NghiemThu/Chapter01');
const runs = fs.readdirSync(root).filter(name => name.startsWith('P2_Web_') && !name.includes('before'))
  .sort().reverse().map(name => path.join(root, name));
const run = runs.find(dir => fs.existsSync(path.join(dir, 'result.json')) && JSON.parse(fs.readFileSync(path.join(dir, 'result.json'))).passed);
if (!run) throw new Error('A successful real WebGL acceptance run is required.');
const report = JSON.parse(fs.readFileSync(path.join(run, 'result.json')));
const out = path.join(root, 'P2_Review_20261029'); fs.mkdirSync(out, { recursive: true });
const baseline = path.join(root, 'P2_Web_before_2026-10-28T16-41-47-327Z/02-map-pc.png');
for (const [source, target] of [[baseline, 'before.png'], [path.join(run, '02-map-pc.png'), 'after.png'],
  [path.resolve('ArtSource/Chapter01/Docs/Bản đồ Chương 1 Loạn Khăn Vàng.png'), 'reference.png']]) fs.copyFileSync(source, path.join(out, target));
for (const name of report.screenshots) fs.copyFileSync(path.join(run, name), path.join(out, name));
const paired = '<div class="pair"><figure><figcaption>Trước P2, PC 16:9</figcaption><img src="before.png"></figure><figure><figcaption>Sau P2, PC 16:9</figcaption><img src="after.png"></figure></div>';
const gallery = report.screenshots.map(name => `<figure><figcaption>${name}</figcaption><a href="${name}"><img loading="lazy" src="${name}"></a></figure>`).join('');
fs.writeFileSync(path.join(out, 'index.html'), `<!doctype html><html lang="vi"><meta charset="utf-8"><title>Chương 1: kiểm P2</title>
<style>body{margin:24px;background:#111b25;color:#f3d098;font:16px system-ui}h1{font-size:24px}p{max-width:1000px;line-height:1.6}.pair,.gallery{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0 0 20px}figcaption{padding:8px}img{width:100%;height:auto}a{color:inherit}</style>
<h1>Chương 1: UI và thao tác chạm trên WebGL</h1><p>Ảnh từ bản game thật, hồ sơ kiểm thử riêng, âm thanh tắt. Luồng kiểm: vào map, thông tin chương, chạm NPC từ xa, nhận nhiệm vụ, chọn điểm đánh, thông báo trận chưa có nội dung, trở về Sảnh. Bốn tỉ lệ điện thoại × ba preset.</p>
${paired}<h2>Ảnh mẫu và phạm vi còn thiếu</h2><p>Bố cục HUD dùng bộ nút, khung, icon và thanh tài nguyên chung của game. Mốc thưởng là 7/14/21 theo chương bảy ải. Mây khóa, chuyển trạng thái thế giới và chuyển động môi trường chưa được nghiệm thu trong P2; NPC đang là hình tạm. Lề Redmi là cấu hình giả lập. Chưa đo FPS/ASTC/nhiệt trên máy thật.</p>
<img src="reference.png"><h2>Bằng chứng theo thiết bị và thao tác</h2><div class="gallery">${gallery}</div></html>`);
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true, args: ['--mute-audio'] });
  try {
    const page = await browser.newPage({ viewport: { width: 1680, height: 1170 } });
    await page.goto(require('node:url').pathToFileURL(path.join(out, 'index.html')).href);
    await page.locator('.pair img').evaluateAll(images => Promise.all(images.map(img => img.decode())));
    await page.locator('.pair').screenshot({ path: path.join(out, 'comparison.png') });
  } finally { await browser.close(); }
  const packaged = path.resolve('Builds/WebGL/Arena3Q/chapter-review'); fs.mkdirSync(packaged, { recursive: true });
  for (const name of fs.readdirSync(out)) if (/\.(png|html)$/.test(name)) fs.copyFileSync(path.join(out, name), path.join(packaged, name));
  console.log(out + '\nhttp://127.0.0.1:4322/chapter-review/');
})().catch(error => { console.error(error); process.exitCode = 1; });
