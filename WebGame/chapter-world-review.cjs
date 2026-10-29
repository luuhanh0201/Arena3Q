'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('../Admin/node_modules/playwright');
const root=path.resolve('NghiemThu/Chapter01');
const run=fs.readdirSync(root).filter(n=>n.startsWith('P3_Web_')).sort().reverse()
  .map(n=>path.join(root,n)).find(p=>fs.existsSync(path.join(p,'result.json'))&&JSON.parse(fs.readFileSync(path.join(p,'result.json'))).passed);
if(!run)throw Error('A passed P3 WebGL run is required.');
const report=JSON.parse(fs.readFileSync(path.join(run,'result.json')));
const out=path.join(root,'P3_Review_20261029');fs.mkdirSync(out,{recursive:true});
for(const name of report.screenshots)fs.copyFileSync(path.join(run,name),path.join(out,name));
fs.copyFileSync(path.join(root,'P2_Web_2026-10-28T19-39-43-217Z/02-map-pc.png'),path.join(out,'before.png'));
const unity=path.join(root,'P1_Runtime_20261028/run_20261029_025056_922');
for(const name of fs.readdirSync(unity).filter(n=>/^world-.*\.png$/.test(n)))fs.copyFileSync(path.join(unity,name),path.join(out,name));
const pair=(a,b,label)=>`<section><h2>${label}</h2><div class="pair"><figure><figcaption>Trước</figcaption><img src="${a}"></figure><figure><figcaption>Sau</figcaption><img src="${b}"></figure></div></section>`;
fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Chương 1: sương và trạng thái</title>
<style>body{margin:24px;background:#111b25;color:#f3d098;font:16px system-ui}h1{font-size:24px}h2{font-size:20px}p{max-width:1100px;line-height:1.6}.pair,.gallery{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0 0 20px}figcaption{padding:8px}img{width:100%;height:auto}a{color:inherit}</style>
<h1>Chương 1: sương mù, mở khu và trạng thái sau thắng</h1>
<p>WebGL thật: 12 tổ hợp màn hình/preset, thao tác chạm và ba hồ sơ QA cô lập (đã thắng 1, 4, 7 ải). Không gắn battle giả vào nội dung phát hành. Unity PlayMode: 12/12, mở tuần tự cả bảy khu và tải lại đúng trạng thái. Mây lấy sprite Kenney CC0 có sẵn, vùng khóa khử màu/tối, không che HUD. NPC vùng khóa ẩn; dấu tương tác chờ hết chuyển tiếp.</p>
${pair('before.png','02-map-pc.png','WebGL, cùng camera lần đầu')}
<p>Các cặp dưới là capture Unity cùng góc trước/sau mở sương. Ảnh chụp ngay lúc chuyển tiếp hoàn tất, dấu UI còn đang fade. Màu capture batch có giới hạn SRP đã ghi trong nhật ký; ảnh web phía trên dùng để đối chiếu màu player.</p>
${Array.from({length:6},(_,i)=>pair('world-'+(i+2)+'-before.png','world-'+(i+2)+'-after.png','Mở khu '+(i+2))).join('')}
<h2>WebGL theo màn hình và thao tác</h2><div class="gallery">${report.screenshots.map(n=>`<figure><figcaption>${n}</figcaption><a href="${n}"><img loading="lazy" src="${n}"></a></figure>`).join('')}</div>
<p>Chưa hoàn tất P4: chuyển động môi trường, decal, điểm âm thanh. NPC vẫn tạm; mô hình State_After mới có ở khu 1 và 4. Bảy battle và phần thưởng chương chưa được soạn. Preset 30 dùng hai lớp mây, 60/120 ba lớp; thông tin chơi và tiến độ giống nhau. Web chưa chứng minh FPS, ASTC hoặc nhiệt trên điện thoại.</p></html>`);
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{const page=await browser.newPage({viewport:{width:1680,height:1120}});await page.goto(require('node:url').pathToFileURL(path.join(out,'index.html')).href);
 await page.locator('section').first().locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
 await page.locator('section').first().screenshot({path:path.join(out,'comparison.png')});}
 finally{await browser.close();}
 const publish=path.resolve('Builds/WebGL/Arena3Q/chapter-world-review');fs.mkdirSync(publish,{recursive:true});
 for(const n of fs.readdirSync(out))if(/\.(png|html)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 console.log('http://127.0.0.1:4322/chapter-world-review/');
})().catch(e=>{console.error(e);process.exitCode=1;});
