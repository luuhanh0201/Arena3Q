'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const sharp=require('../Admin/node_modules/sharp'),{chromium}=require('../Admin/node_modules/playwright');
const before=path.resolve('NghiemThu/Chapter01/P1_Runtime_20261028/run_20261031_001816_105'),after=path.resolve(process.argv[2]||'');
if(!process.argv[2])throw Error('Pass final ground/batching capture folder.');
const out=path.resolve('NghiemThu/Chapter01/XemTruoc_P4Details_20261031');fs.mkdirSync(out,{recursive:true});
(async()=>{
 const scenes=[];
 for(const stage of [0,1,2,3])for(const tier of [30,60,120])scenes.push({name:`ground-${stage}-${tier}`,label:['Làng','Đồng lúa','Rừng trúc','Trại lương'][stage]+' / '+tier});
 for(const scene of scenes)for(const [dir,prefix] of [[before,'before'],[after,'after']])fs.copyFileSync(path.join(dir,scene.name+'.png'),path.join(out,prefix+'-'+scene.name+'.png'));
 const batching=JSON.parse(fs.readFileSync(path.join(after,'batching.json')));assert.ok(batching.samples.every(s=>s.drawCalls>0));
 const differences=[];
 for(const mode of ['source','motion']){
  const pixels=[];for(const state of ['off','on']){const name=`batching-${mode}-${state}.png`;fs.copyFileSync(path.join(after,name),path.join(out,name));pixels.push(await sharp(path.join(out,name)).removeAlpha().raw().toBuffer());}
  let delta=0,changed=0;for(let i=0;i<pixels[0].length;i+=3){const d=Math.max(...[0,1,2].map(c=>Math.abs(pixels[0][i+c]-pixels[1][i+c])));delta+=d;if(d>8)changed++;}
  differences.push({mode,meanMaxChannelDelta:delta/(pixels[0].length/3),changedOver8:changed,totalPixels:pixels[0].length/3});
 }
 const webImages=[];
 if(process.argv[3]){
  const run=path.resolve(process.argv[3]),report=JSON.parse(fs.readFileSync(path.join(run,'result.json')));assert.ok(report.passed);assert.equal(report.cases.length,12);assert.equal(report.world.length,5);assert.equal(report.build.contentHash,JSON.parse(fs.readFileSync('Builds/WebGL/Arena3Q/build.json')).contentHash);
  for(const n of report.screenshots.filter(n=>/^12-(paddy|bamboo)|^14-details/.test(n))){fs.copyFileSync(path.join(run,n),path.join(out,'web-'+n));webImages.push('web-'+n);}
 }
 fs.writeFileSync(path.join(out,'index.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>P4b: chi tiết mặt đất</title><style>body{font:16px system-ui;background:#14202b;color:#eef3f6;margin:24px auto;max-width:1760px;padding:0 16px;line-height:1.6}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}img{max-width:100%}figure{margin:0}select{padding:10px;font:inherit}table{border-collapse:collapse}td,th{padding:8px 16px;border:1px solid #68798b}</style><h1>P4b: chi tiết mặt đất và điểm âm thanh</h1><p>Ảnh trước/sau cùng camera, bốn khu và ba preset. Đồng bộ đất/đường/ruộng với nguồn đối màu R2; chi tiết lá, vết xe, vết cháy, lúa, cỏ, sậy, bờ suối dùng hình đã duyệt. Không đổi layout hoặc đường đi.</p><p>Âm thanh hiện mới có điểm gắn và pool4 nguồn, chưa có clip môi trường. Chưa đo hiệu năng điện thoại.</p><select id="scene">${scenes.map(s=>`<option value="${s.name}">${s.label}</option>`).join('')}</select><div id="comparison" class="pair"><figure>Trước<img id="before"></figure><figure>Sau<img id="after"></figure></div><h2>SRP Batcher bật/tắt</h2><p>Camera render vào RT trong Unity batch, không phải FPS thiết bị. Cột Batches không được backend này cung cấp đáng tin cậy; chỉ báo Draw Calls và SetPass.</p><table><tr><th>Vật liệu</th><th>SRP</th><th>Draw calls</th><th>SetPass</th></tr>${batching.samples.map(s=>`<tr><td>${s.mode}</td><td>${s.srp}</td><td>${s.drawCalls}</td><td>${s.setPass}</td></tr>`).join('')}</table>${['source','motion'].map(mode=>`<h3>${mode}</h3><div class="pair"><figure>Tắt<img src="batching-${mode}-off.png"></figure><figure>Bật<img src="batching-${mode}-on.png"></figure></div>`).join('')}<h2>WebGL</h2><div class="pair">${webImages.map(n=>`<figure>${n}<img src="${n}"></figure>`).join('')}</div><script>function show(){for(const id of ['before','after'])document.getElementById(id).src=id+'-'+document.getElementById('scene').value+'.png'}document.getElementById('scene').onchange=show;show();</script></html>`);
 const publish=path.resolve('Builds/WebGL/Arena3Q/chapter-p4-details');fs.mkdirSync(publish,{recursive:true});for(const n of fs.readdirSync(out))if(/\.(png|html)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 const browser=await chromium.launch({channel:'msedge',headless:true,chromiumSandbox:true,args:['--mute-audio']});
 try{
  const page=await browser.newPage({viewport:{width:1760,height:1100}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('http://127.0.0.1:4322/chapter-p4-details/');
  for(const s of scenes){await page.selectOption('#scene',s.name);await page.locator('#before').evaluate(i=>i.decode());await page.locator('#after').evaluate(i=>i.decode());}
  await page.selectOption('#scene','ground-1-60');await page.locator('#after').evaluate(i=>i.decode());await page.locator('#comparison').screenshot({path:path.join(out,'comparison.png')});
  const broken=await page.locator('img').evaluateAll(async ims=>{await Promise.all(ims.map(i=>i.decode().catch(()=>{})));return ims.filter(i=>!i.naturalWidth).map(i=>i.src)});assert.deepEqual(errors,[]);assert.deepEqual(broken,[]);
  fs.writeFileSync(path.join(out,'gallery-check.json'),JSON.stringify({passed:true,before,after,webRun:process.argv[3],combinations:12,errors,broken,batching,differences},null,2));
 }finally{await browser.close();}
 console.log('http://127.0.0.1:4322/chapter-p4-details/');
})().catch(e=>{console.error(e);process.exitCode=1;});
