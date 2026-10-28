const fs=require('node:fs'),path=require('node:path');
const read=f=>JSON.parse(fs.readFileSync(path.join(__dirname,f),'utf8').replace(/^\uFEFF/,''));
const wb=read('workbook-extract.json'),jobs=read('prompts.json');
const rows=wb.find(x=>x.sheet==='Danh sách asset').rows.slice(1).filter(r=>/^(2D|Hybrid)/.test(r.P));
const vfx={36:['Reuse/magic_01.png','Reuse/spark_01.png'],37:['Reuse/smoke_01.png','Reuse/fire_01.png'],50:['Reuse/fire_01.png','Reuse/smoke_07.png']};
const data=rows.map(r=>{
 const id=Number(r.A), matches=jobs.filter(j=>j.rows.includes(id));
 return {id,name:r.C,variantsRequested:r.D,stage:r.E,priority:r.M,category:r.P,scope:r.Q,
 files:matches.map(j=>j.id+'.png').concat(vfx[id]||[]),
 status:id===11?'pending-render-from-approved-3d-model':vfx[id]?'existing-particle-source-only':'drawn-pending-scene-validation',
 note:id===11?'Chưa có model/render chuẩn; không thay impostor bằng ảnh AI.':vfx[id]?'Chưa có animation, prefab, fade hoặc nghiệm thu hiệu ứng.':'Sheet nguồn, chưa cắt sprite/atlas hoặc đối màu với render 3D.'};
});
fs.writeFileSync(path.join(__dirname,'coverage.json'),JSON.stringify(data,null,2));
let md='# Đối chiếu workbook\n\nTrạng thái phần vẽ và nguồn hình, không phải nghiệm thu tích hợp. STT theo sheet Danh sách asset.\n\n| STT | Hạng mục | Hình nguồn | Trạng thái |\n|---|---|---|---|\n';
for(const r of data)md+=`| ${r.id} | ${r.name} | ${r.files.map(f=>`[${f}](${f.replace(/ /g,'%20')})`).join(', ')||'Chờ render'} | ${r.status==='pending-render-from-approved-3d-model'?'Chờ model 3D':r.status==='existing-particle-source-only'?'Dùng nguồn có sẵn, chưa dựng VFX':'Đã vẽ, chờ ghép cảnh'} |\n`;
md+='\nMục 20 và 51 có PNG vẽ riêng và dùng thêm nguồn lửa/khói trong Reuse-VFX.md. Nhóm 46 và 47 dùng chung tấm vũ khí; không thay giá gỗ 3D. Tất cả mục Giữ 3D nằm ngoài phạm vi vẽ này.\n';
fs.writeFileSync(path.join(__dirname,'Coverage.md'),md);
console.log(JSON.stringify({rows:data.length,drawn:data.filter(r=>r.status.startsWith('drawn')).length,reuse:data.filter(r=>r.status.startsWith('existing')).length,pending:data.filter(r=>r.status.startsWith('pending')).length}));
