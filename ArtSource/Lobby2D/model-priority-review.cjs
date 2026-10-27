'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const out=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const label=(text,w)=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="42"><rect width="100%" height="100%" fill="#101927"/><text x="16" y="28" font-family="Arial" font-size="19" fill="#ffd877">${text}</text></svg>`);
async function pair(a,b,file,textA,textB,size){
  const w=size[0],h=size[1];
  const images=await Promise.all([a,b].map(image=>sharp(image).resize(w,h,{fit:'fill'}).png().toBuffer()));
  await sharp({create:{width:w*2,height:h+42,channels:3,background:'#101927'}}).composite([
    {input:label(textA,w),left:0,top:0},{input:label(textB,w),left:w,top:0},
    {input:images[0],left:0,top:42},{input:images[1],left:w,top:42}]).png().toFile(path.join(out,file));
}
async function main(){
  const regions={Full:{left:430,top:20,width:650,height:800},Face:{left:620,top:190,width:270,height:190},Robe:{left:565,top:350,width:380,height:440}};
  for(let frame=0;frame<3;frame++)for(const [name,bounds] of Object.entries(regions)){
    const before=await sharp(path.join(out,'attempt1/Idle_'+frame+'.png')).extract(bounds).png().toBuffer();
    // The saved baseline predates the ground-anchor correction: register only the crop, never reconstruct the old render.
    const after=await sharp(path.join(out,'Idle_'+frame+'.png')).extract({...bounds,top:bounds.top+86}).png().toBuffer();
    await pair(before,after,`Model_${name}_${frame}_before-after.png`,'Saved baseline 08:52 / Idle '+frame,'Revised lighting + Portrait / Idle '+frame,name==='Face'?[540,380]:name==='Full'?[650,800]:[570,660]);
  }
  await pair(path.join(out,'ActualBattleSurface.png'),path.join(out,'LordSurface.png'),'Model_battle-vs-lobby.png','Actual battle renderer / Battle LOD','Lobby / existing Portrait LOD',[768,768]);
  const available=['Model_battle-vs-lobby.png','LordSurfaceAlpha.png',...Object.keys(regions).flatMap(name=>[0,1,2].map(frame=>`Model_${name}_${frame}_before-after.png`))];
  fs.writeFileSync(path.join(out,'model-review.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Arena3Q - Kiểm model sảnh</title><style>body{background:#0b101b;color:#e6ecf5;font:17px system-ui;margin:0}main{max-width:1600px;padding:24px;margin:auto}h1,h2,a{color:#ffd877}img{max-width:100%;height:auto;display:block}figure{margin:24px 0}figcaption{padding:10px}nav{display:flex;gap:14px;flex-wrap:wrap}</style><main><h1>Ưu tiên kiểm model sảnh</h1><p>Ảnh trước giữ từ lượt 08:52; ảnh sau từ Unity hiện tại. Crop sau dịch xuống 86 px để căn cùng vùng tướng, vì bước chỉnh bệ trước đó đã đổi neo chân. Không dựng lại ảnh trước, không sửa mesh hoặc texture nguồn.</p><nav><a href="IdleSurfaceAndEditorTiming.json">Audit da + thời gian Editor</a><a href="SurfaceAlpha.json">Kênh alpha</a><a href="ModelMaterialSources.txt">Nguồn vật liệu</a><a href="Lobby_1920.png">Sảnh thật</a></nav><p>Ảnh Battle lấy từ model đã triệu hồi trong trận; camera cùng góc chính diện theo trục model. Hai LOD dùng chung clip nhưng khác mật độ mesh và bản bake, vì vậy không khẳng định trùng từng pixel.</p>${available.map(file=>`<figure><img src="${file}"><figcaption>${file}</figcaption></figure>`).join('')}</main></html>`);
  fs.writeFileSync(path.join(out,'model-review-manifest.json'),JSON.stringify({baseline:'attempt1/Idle_[0..2].png',baselineTime:'08:52',cropRegistrationY:86,sourceEdited:false,images:available},null,2));
}
main().catch(error=>{console.error(error);process.exitCode=1});
