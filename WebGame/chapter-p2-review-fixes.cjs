'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const sharp=require('../Admin/node_modules/sharp');
const {chromium}=require('../Admin/node_modules/playwright');
const root=path.resolve('NghiemThu/Chapter01'),out=path.join(root,'XemTruoc_P2Review_20261030');
const before=path.join(root,'P1_Runtime_20261028/run_20261030_180450_134');
const after=path.join(root,'P1_Runtime_20261028/run_20261030_180608_418');
const sample=path.resolve('AgentComms/codex/inbox/tu-claude_20261028-1930_UI-map-mau-chu-thich.png');
fs.mkdirSync(out,{recursive:true});
const names=fs.readdirSync(after).filter(n=>/^ui-.*\.png$/.test(n));assert.equal(names.length,13);
for(const [folder,prefix]of [[before,'before-'],[after,'after-']])for(const n of names)fs.copyFileSync(path.join(folder,n),path.join(out,prefix+n));
fs.copyFileSync(sample,path.join(out,'sample.png'));
const webRun=process.argv[2];if(!webRun)throw Error('Pass the completed WebGL result directory.');
const web=JSON.parse(fs.readFileSync(path.join(webRun,'result.json')));
assert.ok(web.passed);assert.equal(web.cases.length,12);assert.equal(web.world.length,4);
assert.equal(web.build.contentHash,JSON.parse(fs.readFileSync('Builds/WebGL/Arena3Q/build.json')).contentHash);
for(const n of web.screenshots)fs.copyFileSync(path.join(webRun,n),path.join(out,'web-'+n));
const pair=(file,label)=>`<section><h2>${label}</h2><div class="pair"><figure><figcaption>Trước</figcaption><img src="before-${file}"></figure><figure><figcaption>Sau</figcaption><img src="after-${file}"></figure></div></section>`;
fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>P2: sửa UI bản đồ theo review</title><style>body{background:#14202b;color:#eef1f4;font:16px system-ui;max-width:1760px;margin:24px auto;padding:0 16px}p{line-height:1.6}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}figure{margin:0}img{max-width:100%;height:auto}figcaption{padding:8px}select{font:inherit;margin:8px;padding:8px}section{margin:30px 0}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style>
<h1>P2: sửa UI bản đồ theo review 17:55</h1><p>Băng đỏ viền vàng cho tế đàn; sao rương dừng tại mốc; viền/bóng tối cho lời dẫn và tên boss; thông tin thưởng từ dữ liệu; panel chụp sau fade; nút Đóng dùng GameButton và vùng chạm ít nhất 48 dp. Nguồn thưởng chưa biên soạn nên hiện “Phần thưởng sẽ được công bố sau.”</p>
<section><h2>Ảnh mẫu và bản chạy, cùng khung 1672 × 941</h2><p>Đối chiếu thành phần UI; ảnh mẫu là bản vẽ, bản chạy dùng cảnh 3D hiện có.</p><div class="pair"><figure><figcaption>Mẫu được giao</figcaption><img src="sample.png"></figure><figure><figcaption>Unity: UI đã sửa</figcaption><img src="after-ui-introduction-trees-60.png"></figure></div></section>
${pair('ui-altar-60.png','Tế đàn: băng đỏ, tên boss và mốc rương')}${pair('ui-stage-panel-ready.png','Bảng ải đã hiện đủ: thưởng và nút Đóng')}
<section><h2>Lời dẫn trên ba nền, ba preset</h2><label>Nền<select id="background"><option value="cloud">Mây</option><option value="trees">Cây</option><option value="ground">Đất</option></select></label><label>Preset<select id="tier"><option>30</option><option selected>60</option><option>120</option></select></label><div class="pair"><figure><figcaption>Trước</figcaption><img id="before" src="before-ui-introduction-cloud-60.png"></figure><figure><figcaption>Sau</figcaption><img id="after" src="after-ui-introduction-cloud-60.png"></figure></div></section>
<h2>WebGL thực tế</h2><p>12 tổ hợp màn hình/preset, 4 hồ sơ tiến độ, ${web.screenshots.length} ảnh; chưa đo FPS/nhiệt hoặc nén ASTC trên điện thoại. Các lệch nhỏ ngoài sáu mục chặn và nút quay lại vẫn theo quyết định review riêng.</p><div class="pair">${web.screenshots.map(n=>`<figure><figcaption>${n}</figcaption><img loading="lazy" src="web-${n}"></figure>`).join('')}</div>
<script>function update(){for(const side of ['before','after'])document.getElementById(side).src=side+'-ui-introduction-'+document.getElementById('background').value+'-'+document.getElementById('tier').value+'.png'}document.getElementById('background').onchange=update;document.getElementById('tier').onchange=update;</script></html>`);
(async()=>{
 const strips=[];let row=0;
 for(const tier of [30,60,120])for(const background of ['cloud','trees','ground']){
  for(const [column,prefix]of ['before-','after-'].entries())strips.push({input:await sharp(path.join(out,prefix+`ui-introduction-${background}-${tier}.png`)).extract({left:20,top:78,width:550,height:80}).png().toBuffer(),left:column*550,top:row*80});
  row++;
 }
 await sharp({create:{width:1100,height:720,channels:3,background:'#14202b'}}).composite(strips).png().toFile(path.join(out,'text-presets-before-after.png'));
 for(const n of ['ui-altar-60.png','ui-stage-panel-ready.png','ui-introduction-cloud-60.png']){
  const a=await sharp(path.join(out,'before-'+n)).metadata(),b=await sharp(path.join(out,'after-'+n)).metadata();assert.equal(a.width,b.width);assert.equal(a.height,b.height);
  await sharp({create:{width:a.width*2,height:a.height,channels:3,background:'#14202b'}}).composite([{input:path.join(out,'before-'+n),left:0,top:0},{input:path.join(out,'after-'+n),left:a.width,top:0}]).png().toFile(path.join(out,'compare-'+n));
 }
 const publish=path.resolve('Builds/WebGL/Arena3Q/chapter-p2-review');fs.mkdirSync(publish,{recursive:true});for(const n of fs.readdirSync(out))if(/\.(png|html)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{const page=await browser.newPage({viewport:{width:1760,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('http://127.0.0.1:4322/chapter-p2-review/');
  const broken=await page.locator('img').evaluateAll(async ims=>{await Promise.all(ims.map(i=>{i.loading='eager';return i.decode().catch(()=>{})}));return ims.filter(i=>!i.naturalWidth).map(i=>i.src)});assert.deepEqual(broken,[]);assert.deepEqual(errors,[]);
  for(const background of ['cloud','trees','ground'])for(const tier of ['30','60','120']){await page.selectOption('#background',background);await page.selectOption('#tier',tier);for(const side of ['before','after'])await page.locator('#'+side).evaluate(i=>i.decode());}
  await page.locator('section').nth(1).screenshot({path:path.join(out,'comparison.png')});
  fs.writeFileSync(path.join(out,'gallery-check.json'),JSON.stringify({passed:true,errors,broken,before,after,webRun,combinations:9,webImages:web.screenshots.length},null,2));
 }finally{await browser.close();}
 console.log('http://127.0.0.1:4322/chapter-p2-review/');
})().catch(e=>{console.error(e);process.exitCode=1;});
