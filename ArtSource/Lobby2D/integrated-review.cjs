'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const evidence=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const captures=fs.readdirSync(evidence).filter(name=>name.startsWith('captures_')).sort().at(-1);
if(!captures)throw Error('Run the lobby capture tests first.');
const out=path.join(evidence,'review_'+new Date().toISOString().replace(/[:.]/g,'-'));fs.mkdirSync(out,{recursive:true});fs.mkdirSync(path.join(out,'before'),{recursive:true});
for(const name of fs.readdirSync(path.join(evidence,captures)).filter(name=>/\.(png|json)$/.test(name)))fs.copyFileSync(path.join(evidence,captures,name),path.join(out,name));
for(const name of ['reference.png','LordSurface.png','LordSurfaceAlpha.png','SurfaceAlpha.json','LordPortrait.png','ModelMaterialSources.txt'])fs.copyFileSync(path.join(evidence,name),path.join(out,name));
const refinement=fs.readdirSync(evidence).filter(name=>name.startsWith('ui_refinements_')).sort().at(-1);
const refinementFiles=refinement?fs.readdirSync(path.join(evidence,refinement)).filter(name=>name.endsWith('.png')):[];
for(const name of refinementFiles)fs.copyFileSync(path.join(evidence,refinement,name),path.join(out,name));
const modelDetails=fs.readdirSync(evidence).filter(name=>name.startsWith('model-details_')).sort().at(-1);
const expFix=fs.readdirSync(evidence).filter(name=>name.startsWith('exp-fix_')).sort().at(-1);
const nameLayout=fs.readdirSync(evidence).filter(name=>name.startsWith('name-layout_')).sort().at(-1);
const nameDesign=nameLayout||fs.readdirSync(evidence).filter(name=>name.startsWith('name-design_')).sort().at(-1);
fs.copyFileSync(path.join(evidence,'before/LobbyAndroidStartup.png'),path.join(out,'before/LobbyAndroidStartup.png'));
const svgLabel=(text,w=960)=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="42"><rect width="100%" height="100%" fill="#101927"/><text x="20" y="28" font-size="20" fill="#FFD877" font-family="Arial">${text}</text></svg>`);
async function pair(a,b,file,left,right){
  const images=await Promise.all([a,b].map(image=>sharp(image).resize(960,540,{fit:'fill'}).png().toBuffer()));
  await sharp({create:{width:1920,height:582,channels:3,background:'#101927'}}).composite([
    {input:svgLabel(left),left:0,top:0},{input:svgLabel(right),left:960,top:0},
    {input:images[0],left:0,top:42},{input:images[1],left:960,top:42}]).png().toFile(path.join(out,file));
}
async function main(){
  const sample=await sharp(path.join(out,'reference.png')).extract({left:0,top:0,width:1672,height:815}).resize(1920,1080,{fit:'fill'}).png().toBuffer();
  fs.writeFileSync(path.join(out,'reference-game.png'),sample);
  await pair(sample,path.join(out,'Lobby_1920.png'),'sample-vs-revision.png','Reference game area / normalized 16:9','Integrated lobby / 1920 x 1080');
  await pair(path.join(out,'before/LobbyAndroidStartup.png'),path.join(out,'Lobby_1920.png'),'before-after.png','Before / separate 2D test lobby','After / integrated frontend and chosen lord');
  const baseline=path.join(evidence,'captures_20261027_121525/Idle_0.png');
  if(fs.existsSync(baseline))await pair(baseline,path.join(out,'Idle_0.png'),'refinements-before-after.png','Before latest refinements / same Idle frame 0','VIP frame, wallet, EXP and solid icons / frame 0');
  const areas=[['A1_Player',{left:0,top:0,width:600,height:310}],['A2_Resources',{left:735,top:8,width:720,height:85}],
    ['A3_Status',{left:1510,top:0,width:410,height:115}],['A4_Left',{left:10,top:290,width:150,height:610}],
    ['A5_Lord',{left:245,top:390,width:210,height:100}],['A6_Banners',{left:1350,top:110,width:535,height:690}],
    ['A7_Navigation',{left:40,top:895,width:1100,height:150}],['A8_Battle',{left:1320,top:805,width:590,height:245}]];
  const actual=path.join(out,'Lobby_1920.png');
  for(const [name,rect] of areas) await sharp(actual).extract(rect).png().toFile(path.join(out,name+'.png'));
  for(const hour of [5,11,15,18,21]) {
    const image=path.join(out,'Daylight_'+hour+'.png');
    if(fs.existsSync(image)) await sharp(image).extract(areas[4][1]).resize(420,200,{kernel:'nearest'}).png().toFile(path.join(out,'LordHeading_'+hour+'_200.png'));
  }
  const ids=areas.map(([id])=>id);
  fs.writeFileSync(path.join(out,'review.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Arena3Q - Sảnh tích hợp</title>
<style>body{margin:0;background:#0b101b;color:#e6ecf5;font:16px system-ui}main{max-width:1700px;margin:auto;padding:24px}h1,h2{color:#ffd877}a{color:#ffd877}button{background:#142940;border:1px solid #e2b85c;color:#ffd877;padding:9px 15px;cursor:pointer}figure{margin:18px 0}img{display:block;max-width:100%;height:auto}figcaption{padding:8px;color:#b7c6dc}.grid{display:flex;gap:18px;flex-wrap:wrap}.grid figure{max-width:48%}.closeups{align-items:start}.closeups img{max-height:650px;max-width:100%;object-fit:contain}.hero{position:relative}.overlay{position:absolute;inset:0;pointer-events:none}.marker{position:absolute;border:2px solid #54e1ea;background:#0b101b99;color:#fff;font:bold 13px system-ui;padding:3px}nav{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}</style>
<main><h1>Sảnh tích hợp Frontend</h1><p>Ảnh thật từ Unity. Nguồn mẫu giữ nguyên; ảnh so sánh bỏ dải chú thích dưới mẫu và chuẩn hoá phần game về 16:9. Đây là bản gửi review, chưa phải nghiệm thu cuối.</p>
${nameDesign?`<p><a href="../${nameDesign}/review.html">${nameLayout?'Bố cục ô tên và nhãn Chủ Tướng đã áp dụng theo ảnh mới':'Thiết kế ô tên và nhãn Chủ Tướng đang chờ người dùng duyệt'}</a></p>`:''}
${expFix?`<h2>Sửa EXP tràn khung theo ảnh người dùng</h2><p><a href="../${expFix}/review.html">Xem 0%, 50%, 100%, cấp tối đa và VIP 1/12 ở bốn tỉ lệ</a></p><img src="../${expFix}/Experience_1_100_before-after.png">`:''}
<p><a href="reference.png">Mẫu gốc</a> · <a href="IdleSurfaceAndEditorTiming.json">Audit Idle và số đo Editor</a></p>
<figure><img src="sample-vs-revision.png"><figcaption>Mẫu và bản sửa cùng khung 16:9</figcaption></figure>
<figure><img src="before-after.png"><figcaption>Trước và sau vòng sửa</figcaption></figure>
<figure><img src="refinements-before-after.png"><figcaption>Trước và sau góp ý 12:25, 12:30; cùng khung Idle 0</figcaption></figure>
<h2>Tài nguyên, EXP, VIP và icon</h2><div class="grid">${refinementFiles.map(name=>`<figure><a href="${name}"><img src="${name}"></a><figcaption>${name.replace('.png','')}</figcaption></figure>`).join('')}</div>
<h2>Bố cục A1 đến A10</h2><nav>${[1920,2160,2340,2400].map(w=>`<button data-size="${w}">${w} × 1080</button>`).join('')}</nav>
<figure class="hero"><img id="lobby" src="Lobby_1920.png"><div class="overlay">${[
 ['A1',1,1],['A2',39,1],['A3',82,2],['A4',2,19],['A5',14,39],['A6',71,14],['A7',6,88],['A8',74,83],['A9 giờ máy',44,19],['A10 UI chung',48,91]
].map(([text,x,y])=>`<span class="marker" style="left:${x}%;top:${y}%">${text}</span>`).join('')}</div></figure>
<h2>Chi tiết</h2><div class="grid closeups">${ids.map(id=>`<figure><img src="${id}.png"><figcaption>${id}</figcaption></figure>`).join('')}</div>
<h2>Chu kỳ đứng thở</h2><nav>${[0,1,2].map(i=>`<button data-idle="${i}">Khung ${i}</button>`).join('')}</nav><img id="idle" src="Idle_0.png">
<h2>Năm thời điểm</h2><nav>${[5,11,15,18,21].map(h=>`<button data-hour="${h}">${h} giờ</button>`).join('')}</nav><img id="daylight" src="Daylight_5.png">
<h2>Nhãn Chủ Tướng gọn ở năm thời điểm</h2><div class="grid closeups">${[5,11,15,18,21].map(h=>`<figure><img src="LordHeading_${h}_200.png"><figcaption>${h} giờ, phóng 200%</figcaption></figure>`).join('')}</div>
<h2>Model và vật liệu</h2><div class="grid">${['LordPortrait','LordSurface','ActualBattleSurface','LordSurfaceAlpha'].map(id=>`<figure><img src="${id}.png"><figcaption>${id}</figcaption></figure>`).join('')}</div></main>
${modelDetails?`<main><h2>So sánh phản xạ trước và sau</h2><p><a href="../${modelDetails}/review.html">Mở ảnh cận và số đo model</a></p>${['Crown','Fan','Face','Robe','Hem'].map(id=>`<figure><img src="../${modelDetails}/${id}_before-after.png"><figcaption>${id}, cùng camera và pha Idle</figcaption></figure>`).join('')}</main>`:''}
<script>document.querySelectorAll('[data-size]').forEach(b=>b.onclick=()=>document.querySelector('#lobby').src='Lobby_'+b.dataset.size+'.png');document.querySelectorAll('[data-idle]').forEach(b=>b.onclick=()=>document.querySelector('#idle').src='Idle_'+b.dataset.idle+'.png');document.querySelectorAll('[data-hour]').forEach(b=>b.onclick=()=>document.querySelector('#daylight').src='Daylight_'+b.dataset.hour+'.png');</script></html>`);
}
main().then(()=>{
  const latest=path.basename(out)+'/review.html';
  fs.writeFileSync(path.join(evidence,'review.html'),`<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=${latest}"><a href="${latest}">Xem ảnh sảnh mới nhất</a>`);
  console.log(out);
}).catch(error=>{console.error(error);process.exitCode=1});
