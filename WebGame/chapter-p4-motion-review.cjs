'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const sharp=require('../Admin/node_modules/sharp');
const {chromium}=require('../Admin/node_modules/playwright');
const root=path.resolve('NghiemThu/Chapter01'),out=path.join(root,'XemTruoc_P4Motion_20261030');
const before=path.join(root,'P1_Runtime_20261028/run_20261030_233948_315');
const after=path.resolve(process.argv[2]||'');
if(!process.argv[2])throw Error('Pass the completed after capture folder.');
fs.mkdirSync(out,{recursive:true});
const names=fs.readdirSync(after).filter(n=>/^environment-(village|bamboo|water)-(30|60|120)-[0-4]\.png$/.test(n));assert.equal(names.length,45);
for(const [folder,prefix]of [[before,'before-'],[after,'after-']])for(const n of names)fs.copyFileSync(path.join(folder,n),path.join(out,prefix+n));
fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>P4: gió, tre và nước</title><style>body{background:#14202b;color:#eef1f4;font:16px system-ui;max-width:1760px;margin:24px auto;padding:0 16px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}figure{margin:0}img{max-width:100%}figcaption{padding:8px}button,select,input{font:inherit;margin:8px;padding:8px}p{line-height:1.6}</style><h1>P4a: gió, tre và nước</h1><p>Cùng camera và thời điểm. Cây/cỏ dùng hệ gió chiến trường ở35%; thấp còn45% cường độ. Tre lay ngọn tối đa10cm, thấp5cm; gốc cố định, pha khác nhau theo vị trí. Suối trôi0,06 ô/s và lớp hai0,03 ô/s; thấp một lớp. Không đổi luật hoặc tiến độ chương.</p><p>Đây là chuỗi5 mẫu cách nhau0,55s để xem thay đổi, không phải phép đo FPS hay video chuyển động60Hz. Chưa đại diện toàn bộ P4; lá, bụi, chim, cờ, VFX, decal và âm thanh được bàn giao riêng.</p><label>Góc<select id="view"><option value="village">Làng</option><option value="bamboo" selected>Rừng trúc</option><option value="water">Suối và đồng</option></select></label><label>Preset<select id="tier"><option>30</option><option selected>60</option><option>120</option></select></label><input type="range" id="frame" min="0" max="4" value="0"><button id="play">Chạy / dừng</button><output id="reading"></output><div class="pair"><figure><figcaption>Trước</figcaption><img id="before" src="before-environment-bamboo-60-0.png"></figure><figure><figcaption>Sau</figcaption><img id="after" src="after-environment-bamboo-60-0.png"></figure></div><script>function update(){for(const s of ['before','after'])document.getElementById(s).src=s+'-environment-'+document.getElementById('view').value+'-'+document.getElementById('tier').value+'-'+document.getElementById('frame').value+'.png';document.getElementById('reading').textContent=(Number(document.getElementById('frame').value)*.55).toFixed(2)+'s'}for(const s of ['view','tier','frame'])document.getElementById(s).oninput=update;let timer;document.getElementById('play').onclick=()=>{if(timer){clearInterval(timer);timer=null}else timer=setInterval(()=>{const f=document.getElementById('frame');f.value=(Number(f.value)+1)%5;update()},550)};update();</script></html>`);
(async()=>{
 let webRun;
 if(process.argv[3]){
  webRun=process.argv[3];const web=JSON.parse(fs.readFileSync(path.join(webRun,'result.json')));
  assert.ok(web.passed);assert.equal(web.cases.length,12);assert.equal(web.world.length,4);
  assert.equal(web.build.contentHash,JSON.parse(fs.readFileSync('Builds/WebGL/Arena3Q/build.json')).contentHash);
  const frames=web.screenshots.filter(n=>n.startsWith('14-environment-'));assert.equal(frames.length,15);
  for(const n of frames)fs.copyFileSync(path.join(webRun,n),path.join(out,'web-'+n));
  fs.appendFileSync(path.join(out,'index.html'),`<h2>WebGL thật: rừng trúc và suối, ba preset</h2><p>12 tổ hợp màn hình/preset và4 hồ sơ tiến độ qua; không lỗi trình duyệt. Các ảnh dưới là mẫu cách nhau ít nhất250ms, có thời gian chụp xen giữa; không dùng để đo FPS.</p><div class="pair">${frames.map(n=>`<figure><figcaption>${n}</figcaption><img src="web-${n}"></figure>`).join('')}</div>`);
 }
 const comparisons=[];
 for(const view of ['village','bamboo','water'])for(const tier of [30,60,120]){
  const files=[0,4].map(f=>path.join(after,`environment-${view}-${tier}-${f}.png`));
  const images=await Promise.all(files.map(f=>sharp(f).removeAlpha().raw().toBuffer()));let changed=0,total=0;
  // Exclude HUD and markers: inspect a fixed lower central region in the scene.
  for(let y=410;y<800;y++)for(let x=400;x<1000;x++){const i=(y*1672+x)*3;const delta=Math.max(...[0,1,2].map(c=>Math.abs(images[0][i+c]-images[1][i+c])));if(delta>8)changed++;total++;}
  comparisons.push({view,tier,changedPixels:changed,total});
 }
 const publish=path.resolve('Builds/WebGL/Arena3Q/chapter-p4-motion');fs.mkdirSync(publish,{recursive:true});for(const n of fs.readdirSync(out))if(/\.(png|html)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{const page=await browser.newPage({viewport:{width:1760,height:1100}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('http://127.0.0.1:4322/chapter-p4-motion/');
  for(const view of ['village','bamboo','water'])for(const tier of ['30','60','120'])for(let f=0;f<5;f++){await page.selectOption('#view',view);await page.selectOption('#tier',tier);await page.locator('#frame').fill(String(f));await page.locator('#frame').dispatchEvent('input');for(const side of ['before','after'])await page.locator('#'+side).evaluate(i=>i.decode());}
  await page.selectOption('#view','bamboo');await page.selectOption('#tier','60');await page.locator('#frame').fill('0');await page.locator('#frame').dispatchEvent('input');await page.locator('#after').evaluate(i=>i.decode());await page.locator('.pair').first().screenshot({path:path.join(out,'comparison.png')});
  const broken=await page.locator('img').evaluateAll(async ims=>{await Promise.all(ims.map(i=>i.decode().catch(()=>{})));return ims.filter(i=>!i.naturalWidth).map(i=>i.src)});
  assert.deepEqual(errors,[]);assert.deepEqual(broken,[]);fs.writeFileSync(path.join(out,'gallery-check.json'),JSON.stringify({passed:true,before,after,webRun,combinations:45,errors,broken,comparisons},null,2));
 }finally{await browser.close();}
 console.log('http://127.0.0.1:4322/chapter-p4-motion/');
})().catch(e=>{console.error(e);process.exitCode=1;});
