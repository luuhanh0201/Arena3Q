'use strict';
// Build a local, auditable gallery from real Blender / Unity captures, and preserve source attribution.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../../../Admin/node_modules/sharp');
const root=path.resolve(__dirname,'../../../..');
const qa=path.join(root,'ArtSource/Chapter01/3D/Renders/Sketchfab');
const out=path.join(root,'NghiemThu/Chapter01/Sketchfab_20261029');
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
async function main(){
 const manifests=JSON.parse(fs.readFileSync(path.join(qa,'normalization.json')));
 if(manifests.length!==22)throw Error('Expected 22 sources');
 fs.mkdirSync(out,{recursive:true});
 let credits='# Ghi công 22 nguồn Sketchfab Chương 1\n\nBản dẫn xuất: đổi đơn vị sang mét, tách mảnh, bỏ nền/đạo cụ ngoài phạm vi, chỉnh pivot, giảm mesh SF08/SF21, texture tối đa1024, chuyển vật liệu URP. Nguồn gốc giữ nguyên trên máy. Ghi công cả22nguồn theo license.txt CC BY4.0 đi kèm bản tải. Riêng SF13 tiêu đề có CC0 nhưng file giấy phép ghi CC BY4.0; giữ ghi công theo file này.\n\n';
 let sections='',pieces=0,triangles=0,degenerateRemoved=0,unityFiles=[];
 const unity=JSON.parse(fs.readFileSync(path.join(qa,'Unity/index.json')));
 for(const m of manifests){
  const source=path.join(root,'ArtSource/Chapter01/3D/Sources/Sketchfab',m.code);
  const license=fs.readFileSync(path.join(source,'license.txt'),'utf8');
  credits+='## '+m.code+'\n\n'+license.trim()+'\n\n';
  if(crypto.createHash('sha256').update(fs.readFileSync(path.join(source,'scene.gltf'))).digest('hex')!==m.sourceSha256)throw Error('Source changed '+m.code);
  for(const t of m.textures){const metadata=await sharp(path.join(root,t.path)).metadata();if(Math.max(metadata.width,metadata.height)>1024)throw Error('Oversize '+t.path);}
  const sourceName=m.code+'_source.png'; fs.copyFileSync(path.join(qa,'Source',m.code+'.png'),path.join(out,sourceName));
  const note=m.code==='SF20'?'Đảo ruộng bậc thang, không phù hợp thay ruộng phẳng Ải 2.':m.code==='SF26'?'Hành lang có mái, không phải cầu vượt suối.':m.code==='SF13'?'Ba tòa nhà lớn còn 10–29 nghìn tam giác; chưa có LOD, chưa duyệt đưa vào map.':'';
  sections+=`<section id="${m.code}"><h2>${m.code} · ${m.pieces.length} mảnh</h2><p>${esc(note)}</p><details><summary>Nguồn nguyên bản, EEVEE bốn góc</summary><img loading="lazy" src="${sourceName}"></details>`;
  for(const p of m.pieces){
   pieces++;triangles+=p.triangles;degenerateRemoved+=p.removedDegenerate||0;
   if(p.degenerate||!p.finite)throw Error('Invalid mesh '+p.name);
   const name=p.name+'.png';fs.copyFileSync(path.join(qa,'Comparison',name),path.join(out,name));
   const imported=unity.find(u=>u.label===p.name);if(!imported||!imported.file)throw Error('Missing Unity '+p.name);
   const unityName='unity_'+imported.file;fs.copyFileSync(path.join(qa,'Unity',imported.file),path.join(out,unityName));unityFiles.push({file:unityName,label:p.name});
   sections+=`<article><h3>${esc(p.name)}</h3><p>${p.trianglesBefore.toLocaleString()} → ${p.triangles.toLocaleString()} tam giác; ${p.size.map(v=>v.toFixed(2)).join(' × ')} m (X/Y/Z Blender); pivot ${p.pivot}; ${p.cutout?'có lá/chi tiết alpha clip':'opaque'}.</p><p>Mỗi cặp: trái trước, phải sau; bốn góc cùng camera. Hình người xanh cao1,8m. Before đã tách mảnh, chưa giảm lưới/texture; riêng SF04 Before còn nền.</p><a href="${name}"><img loading="lazy" src="${name}"></a><details open><summary>Unity URP dưới ánh sáng trận</summary><a href="${unityName}"><img class="unity" loading="lazy" src="${unityName}"></a></details></article>`;
  }sections+='</section>';
 }
 const comparators=unity.filter(u=>u.label.startsWith('Game '));let comparisons='';
 for(const u of comparators){const name='unity_'+u.file;fs.copyFileSync(path.join(qa,'Unity',u.file),path.join(out,name));comparisons+=`<figure><figcaption>${esc(u.group+' · '+u.label)}</figcaption><img src="${name}"></figure>`;}
 const html=`<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>22 nguồn Sketchfab Chương1</title><style>body{background:#17202a;color:#e4e8eb;font:16px system-ui;margin:24px auto;max-width:1440px;padding:0 16px}a{color:#f0c87b}nav{display:flex;gap:12px;flex-wrap:wrap}p{line-height:1.6}article{background:#202d3a;padding:16px;margin-bottom:24px}img{max-width:100%;height:auto}.unity{max-width:720px;width:100%}.refs{display:flex;flex-wrap:wrap}.refs figure{width:320px;margin:8px}summary{cursor:pointer;padding:12px}h2{margin-top:40px}</style><h1>22 nguồn Sketchfab → ${pieces} mảnh chuẩn hóa</h1><p>Bộ ứng viên để duyệt; chưa thay model trong map. Hình dưới là render Blender EEVEE và Unity thật. ${triangles.toLocaleString()} tam giác nếu đặt mỗi mảnh một lần, không phải ngân sách một cảnh. Tỉ lệ chiều cao là đề xuất. Chưa đo FPS thiết bị, chưa có LOD cho nhà lớn SF13.</p><p>SF20 không phải ruộng phẳng; SF26 không phải cầu suối. Các vật liệu truyền sáng hoặc alpha blend được đơn giản hóa sang URP opaque/cutout; kiểm hình trước khi chọn vào map.</p><nav>${manifests.map(m=>`<a href="#${m.code}">${m.code}</a>`).join('')}</nav><h2>Mẫu trong game để đối chiếu</h2><p>Unity dùng cùng cỡ khung cho các ứng viên trong một nhóm.</p><div class="refs">${comparisons}</div>${sections}</html>`;
 fs.writeFileSync(path.join(out,'index.html'),html);
 fs.writeFileSync(path.join(root,'ArtSource/Chapter01/3D/Catalog/SKETCHFAB_CREDITS.md'),credits.trimEnd()+'\n');
 const report={sources:22,pieces,triangles,degenerateRemoved,texturesAtMost1024:true,sourceGltfHashesUnchanged:true,unityImages:unity.length};
 fs.writeFileSync(path.join(out,'checks.json'),JSON.stringify(report,null,2));
 // Contact sheets are only a browsing aid; full images above are the visual acceptance evidence.
 for(let start=0;start<unityFiles.length;start+=12){
  const batch=unityFiles.slice(start,start+12),panels=[];
  for(let i=0;i<batch.length;i++){
   const b=await sharp(path.join(out,batch[i].file)).resize(300,300).png().toBuffer();
   panels.push({input:b,left:(i%4)*300,top:Math.floor(i/4)*330});
   const label=Buffer.from(`<svg width="300" height="30"><rect width="300" height="30" fill="#17202a"/><text x="8" y="20" fill="white" font-size="12">${esc(batch[i].label)}</text></svg>`);
   panels.push({input:label,left:(i%4)*300,top:Math.floor(i/4)*330+300});
  }
  await sharp({create:{width:1200,height:Math.ceil(batch.length/4)*330,channels:4,background:'#17202a'}}).composite(panels).png().toFile(path.join(out,'unity-sheet-'+(start/12+1)+'.png'));
 }
 const publish=path.join(root,'Builds/WebGL/Arena3Q/sketchfab-review');fs.mkdirSync(publish,{recursive:true});
 for(const n of fs.readdirSync(out))if(/\.(html|png|json)$/.test(n))fs.copyFileSync(path.join(out,n),path.join(publish,n));
 console.log(JSON.stringify(report));console.log('http://127.0.0.1:4322/sketchfab-review/');
}
main().catch(e=>{console.error(e);process.exitCode=1});
