'use strict';
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('../Admin/node_modules/playwright');
const root=path.resolve('NghiemThu/Chapter01'),out=path.join(root,'P3_BorderFix_20261030');
const before=path.join(root,'P1_Runtime_20261028/run_20261029_105045_038');
const beforeMask=path.join(root,'P1_Runtime_20261028/run_20261030_171349_351');
const after=path.resolve(process.argv[2]||'');
if(!process.argv[2]||!fs.existsSync(path.join(after,'fog-pixel-continuity.tsv')))throw Error('Pass the completed Unity capture directory.');
fs.mkdirSync(out,{recursive:true});
function metrics(folder){const rows=fs.readFileSync(path.join(folder,'fog-pixel-continuity.tsv'),'utf8').trim().split(/\r?\n/).slice(1).map(s=>s.split('\t').map(Number));return{samples:rows.length,temporal:Math.max(...rows.map(r=>r[2])),adjacent:Math.max(...rows.map(r=>r[3])),endpoint:Math.max(...rows.filter(r=>r[1]===1||r[1]===120).map(r=>r[2]))};}
const old=metrics(beforeMask),current=metrics(after);
if(current.samples!==363||current.temporal>64||current.endpoint>4||current.adjacent>32)throw Error('Pixel continuity gate has not passed.');
for(const [folder,prefix]of [[before,'before-'],[beforeMask,'before-'],[after,'after-']])for(const n of fs.readdirSync(folder).filter(n=>/^fog-.*\.(png|tsv)$/.test(n)))fs.copyFileSync(path.join(folder,n),path.join(out,prefix+n));
const webRun=fs.readdirSync(root).filter(n=>n.startsWith('P3_Web_')).sort().reverse().find(n=>fs.existsSync(path.join(root,n,'result.json'))&&JSON.parse(fs.readFileSync(path.join(root,n,'result.json'))).passed);
if(!webRun)throw Error('Need passed WebGL check');
const web=JSON.parse(fs.readFileSync(path.join(root,webRun,'result.json')));for(const n of web.screenshots)fs.copyFileSync(path.join(root,webRun,n),path.join(out,'web-'+n));
if(web.build.contentHash!==JSON.parse(fs.readFileSync('Builds/WebGL/Arena3Q/build.json')).contentHash)throw Error('Passed WebGL run must match the current player.');
const pair=(file,label)=>`<section><h3>${label}</h3><div class="pair"><figure><figcaption>Trước</figcaption><img src="before-${file}"></figure><figure><figcaption>Sau</figcaption><img src="after-${file}"></figure></div></section>`;
fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Sửa ranh sương P3</title><style>body{background:#14202b;color:#eef1f4;font:16px system-ui;max-width:1600px;margin:24px auto;padding:0 16px}p{line-height:1.6}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}figure{margin:0}img{max-width:100%;height:auto}figcaption,td,th{padding:8px}td,th{border:1px solid #607080}table{border-collapse:collapse}select,input,button{font:inherit;margin:8px;padding:8px}section{margin:24px 0}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style>
<h1>P3 vòng 3: ranh sương chuyển mềm</h1><p>Trộn trường sương trước và sau mở bằng cùng tiến độ/nhiễu của khu đang tan. Ranh với khu khóa mềm dần, không đổi đột ngột khi tiến độ đạt 1. Khu đã mở vẫn không bị phủ.</p>
<p>Đo trực tiếp shader trên nền trắng với sương đen để tách cạnh cây, mái và HUD. 121 mức tiến độ mỗi preset, tổng ${current.samples} ảnh đo. Giá trị dưới đây là chênh lệch lớn nhất trên kênh sáng 8-bit (0–255), không phải số đo FPS thiết bị.</p>
<table><tr><th>Phép đo</th><th>Trước</th><th>Sau</th><th>Ngưỡng test</th></tr>${[['temporal','Hai mức tiến độ kế nhau',64],['endpoint','Bước đầu/cuối',4],['adjacent','Hai pixel liền nhau',32]].map(([k,label,limit])=>`<tr><td>${label}</td><td>${old[k]}</td><td>${current[k]}</td><td>≤ ${limit}</td></tr>`).join('')}</table>
<h2>30 khung cảnh thật, cùng góc</h2><label>Preset<select id="tier"><option>30</option><option selected>60</option><option>120</option></select></label><label>Khung<input id="frame" type="range" min="0" max="9" value="8"></label><button id="play">Chạy / dừng</button><output id="reading"></output><div class="pair"><figure><figcaption>Trước</figcaption><img id="before" src="before-fog-60-08.png"></figure><figure><figcaption>Sau</figcaption><img id="after" src="after-fog-60-08.png"></figure></div>
${pair('fog-60-07.png','Ranh khi tan 7/9')}${pair('fog-60-08.png','Ranh trước khung cuối')}${pair('fog-60-09.png','Ranh khi hoàn tất')}${pair('fog-granary-open-border.png','Trại Lương đã mở')}
${pair('fog-mask-60-119.png','Mask trước điểm kết thúc (119/120)')}${pair('fog-mask-60-120.png','Mask kết thúc (120/120)')}
<h2>WebGL cuối</h2><p>12 tổ hợp màn hình/preset, 3 hồ sơ tiến độ, ${web.screenshots.length} ảnh; không lỗi trình duyệt. Chưa đo hiệu năng điện thoại.</p><div class="pair">${web.screenshots.map(n=>`<figure><figcaption>${n}</figcaption><img loading="lazy" src="web-${n}"></figure>`).join('')}</div>
<script>const tier=document.querySelector('#tier'),frame=document.querySelector('#frame');function update(){for(const p of ['before','after'])document.getElementById(p).src=p+'-fog-'+tier.value+'-'+String(frame.value).padStart(2,'0')+'.png';document.querySelector('#reading').textContent=frame.value+'/9'}tier.onchange=frame.oninput=update;let timer;document.querySelector('#play').onclick=()=>{if(timer){clearInterval(timer);timer=null}else timer=setInterval(()=>{frame.value=(Number(frame.value)+1)%10;update()},310)};update();</script></html>`);
(async()=>{const publish=path.resolve('Builds/WebGL/Arena3Q/chapter-p3-border-fix');fs.mkdirSync(publish,{recursive:true});for(const n of fs.readdirSync(out))if(/\.(png|html)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('http://127.0.0.1:4322/chapter-p3-border-fix/');
 const broken=await page.locator('img').evaluateAll(async ims=>{await Promise.all(ims.map(i=>{i.loading='eager';return i.decode().catch(()=>{})}));return ims.filter(i=>!i.naturalWidth).map(i=>i.src)});if(broken.length||errors.length)throw Error(JSON.stringify({broken,errors}));
 for(const tier of ['30','60','120'])for(let frame=0;frame<10;frame++){await page.selectOption('#tier',tier);await page.locator('#frame').fill(String(frame));await page.locator('#frame').dispatchEvent('input');await page.locator('#after').evaluate(i=>i.decode());await page.locator('#before').evaluate(i=>i.decode());}
 await page.locator('section').nth(1).screenshot({path:path.join(out,'comparison.png')});
 fs.writeFileSync(path.join(out,'gallery-check.json'),JSON.stringify({passed:true,errors,broken,old,current,after,webRun,frames:30},null,2));
 }finally{await browser.close();}console.log('http://127.0.0.1:4322/chapter-p3-border-fix/');})().catch(e=>{console.error(e);process.exitCode=1});
