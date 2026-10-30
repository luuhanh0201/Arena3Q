'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('../Admin/node_modules/playwright');
const root=path.resolve('NghiemThu/Chapter01'),out=path.join(root,'P3_Fixes_20261029');fs.mkdirSync(out,{recursive:true});
const previous=path.join(root,'P3_Web_2026-10-29T03-07-34-178Z');
const run=fs.readdirSync(root).filter(n=>n.startsWith('P3_Web_')).sort().reverse().map(n=>path.join(root,n)).find(p=>{
 if(p===previous||!fs.existsSync(path.join(p,'result.json')))return false;return JSON.parse(fs.readFileSync(path.join(p,'result.json'))).passed;
});
if(!run)throw Error('Need passed WebGL review run');
const report=JSON.parse(fs.readFileSync(path.join(run,'result.json')));
const captures=path.join(root,'P1_Runtime_20261028/run_20261029_105045_038');
const building=path.join(root,'P1_Runtime_20261028/run_20261029_105527_261');
for(const [folder,prefix]of [[previous,'before-'],[run,'after-'],[captures,'unity-'],[building,'unity-']])
 for(const name of fs.readdirSync(folder).filter(n=>n.endsWith('.png')))fs.copyFileSync(path.join(folder,name),path.join(out,prefix+name));
const compare=(name,title)=>`<section><h2>${title}</h2><div class="pair"><figure><figcaption>Trước review</figcaption><img src="before-${name}"></figure><figure><figcaption>Sau sửa</figcaption><img src="after-${name}"></figure></div></section>`;
fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Chương1 sửa review P3</title><style>body{background:#14202b;color:#e6e9ed;font:16px system-ui;max-width:1600px;margin:24px auto;padding:0 16px}p{line-height:1.6}img{max-width:100%;height:auto}figure{margin:0}figcaption{padding:10px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}section{margin:28px 0}button,select,input{font:inherit;padding:8px;margin:6px}a{color:#f2ce8c}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style>
<h1>Chương1: sửa review P3 ngày29/10</h1><p>Dấu ải mờ khi đến gần HUD, biến mất và ngừng nhận chạm nếu giao với khung HUD. Chữ nhiệm vụ chừa khoảng cho mũi tên xoay. Sương dùng noise tuần hoàn, tan theo ngưỡng nhiễu/khoảng cách; dải biên nằm ở phía khu khóa.</p><p>PlayMode14/14 + kiểm riêng chuyển trạng thái1/1, EditMode7/7. WebGL12tổ hợp màn hình/preset +3hồ sơ tiến độ, kiểm giao HUD bằng tọa độ runtime. Chưa đoFPS/ASTC/nhiệt trên thiết bị. Đây là bản gửi Claude review lại.</p>
${compare('03-map-redmi-30.png','HUD Redmi,30FPS')}${compare('11-world-7-cleared.png','Bảy ải: dấu không chồng chữ và nhiệm vụ dài')}${compare('11-world-4-cleared.png','Trại Lương và vùng kế tiếp')}
<h2>Chuỗi10khung tan sương, cùng góc</h2><p>Khung được lấy tại10mức hiển thị0→1; preset30dùng2lớp,60/120dùng3lớp. Đây là capture Unity dùng fixture render cô lập, không sửa tiến độ của người chơi.</p><label>Preset<select id="tier"><option>30</option><option selected>60</option><option>120</option></select></label><label>Khung<input id="frame" type="range" min="0" max="9" value="4"></label><button id="play">Chạy / dừng</button><output id="reading"></output><img id="fog" src="unity-fog-60-04.png">
<h2>Cận sương và ranh Trại Lương</h2><div class="pair">${[30,60,120].map(n=>`<figure><figcaption>${n}FPS</figcaption><img src="unity-fog-close-${n}.png"></figure>`).join('')}<figure><figcaption>Trại Lương mở, khu kế tiếp đangtan50%</figcaption><img src="unity-fog-granary-open-border.png"></figure></div>
<h2>State_Before/After: kiểm khung giữa</h2><p>Hai khu hiện có art trạng thái (1và4): trước,50%,sau. Ở các asset hiện có chưa thấy mái vẽ xuyên tường tại frame50%; giữ fade trong suốt hiện tại. Các khu chưa có State_After vẫn thiếuart, không thêm nhóm rỗng để giả hoàn tất.</p>${[1,4].map(n=>`<section><h3>Khu${n}</h3>${[0,50,100].map(a=>`<details ${a===50?'open':''}><summary>${a}%</summary><img src="unity-building-state-${n}-${a}.png"></details>`).join('')}</section>`).join('')}
<h2>Toàn bộ ảnh WebGL lượt cuối</h2><div class="pair">${report.screenshots.map(n=>`<figure><figcaption>${n}</figcaption><img loading="lazy" src="after-${n}"></figure>`).join('')}</div>
<script>const tier=document.querySelector('#tier'),frame=document.querySelector('#frame'),fog=document.querySelector('#fog');function update(){fog.src='unity-fog-'+tier.value+'-'+String(frame.value).padStart(2,'0')+'.png';document.querySelector('#reading').textContent=frame.value+'/9'}tier.onchange=frame.oninput=update;let timer;document.querySelector('#play').onclick=()=>{if(timer){clearInterval(timer);timer=null}else timer=setInterval(()=>{frame.value=(Number(frame.value)+1)%10;update()},310)};update();</script></html>`);
(async()=>{
 const publish=path.resolve('Builds/WebGL/Arena3Q/chapter-p3-fixes');fs.mkdirSync(publish,{recursive:true});
 for(const n of fs.readdirSync(out))if(/\.(png|html)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{const page=await browser.newPage({viewport:{width:1660,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('http://127.0.0.1:4322/chapter-p3-fixes/');
 const broken=await page.locator('img').evaluateAll(async ims=>{await Promise.all(ims.map(i=>{i.loading='eager';return i.decode().catch(()=>{})}));return ims.filter(i=>!i.naturalWidth).map(i=>i.src)});if(broken.length||errors.length)throw Error(JSON.stringify({broken,errors}));
 await page.locator('section').first().screenshot({path:path.join(out,'comparison.png')});
 for(const tier of ['30','60','120']){await page.selectOption('#tier',tier);await page.locator('#frame').fill('9');await page.locator('#frame').dispatchEvent('input');await page.locator('#fog').evaluate(i=>i.decode());}
 fs.writeFileSync(path.join(out,'gallery-check.json'),JSON.stringify({passed:true,broken,errors,run,frames:30},null,2));
 }finally{await browser.close();}console.log('http://127.0.0.1:4322/chapter-p3-fixes/');
})().catch(e=>{console.error(e);process.exitCode=1});
