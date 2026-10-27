'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const root=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
async function main(){
  const latest=prefix=>fs.readdirSync(root).filter(name=>name.startsWith(prefix)).sort().at(-1);
  const captures=latest('captures_'),refinements=latest('ui_refinements_');
  const out=path.join(root,'name-layout_'+new Date().toISOString().replace(/[:.]/g,'-'));
  fs.mkdirSync(out,{recursive:true});
  await sharp(path.join(root,'name-layout-sources/reference.png')).extract({left:55,top:28,width:580,height:307}).png().toFile(path.join(out,'reference.png'));
  const images=[];
  for(const width of [1920,2160,2340,2400]){
    const file='Player_'+width+'.png';
    await sharp(path.join(root,captures,'Lobby_'+width+'.png')).extract({left:width===1920?0:60,top:0,width:600,height:310}).png().toFile(path.join(out,file));
    images.push([file,'Bố cục thật '+width+' × 1080']);
  }
  await sharp(path.join(root,captures,'Lobby_1920.png')).extract({left:245,top:390,width:210,height:100}).resize(630,300).png().toFile(path.join(out,'Lord.png'));
  images.push(['Lord.png','Nhãn Chủ Tướng, phóng 300%']);
  for(const tier of [1,12]){
    const file='Vip_'+tier+'.png';
    await sharp(path.join(root,refinements,file)).extract({left:0,top:0,width:600,height:310}).png().toFile(path.join(out,file));
    images.push([file,'VIP '+tier+', EXP đầy']);
  }
  const before=path.join(root,'captures_20261027_184722/Lobby_1920.png');
  const pair=await Promise.all([before,path.join(root,captures,'Lobby_1920.png')].map(file=>sharp(file).extract({left:0,top:0,width:600,height:310}).png().toBuffer()));
  await sharp({create:{width:1200,height:310,channels:3,background:'#0b101b'}}).composite([{input:pair[0],left:0,top:0},{input:pair[1],left:600,top:0}]).png().toFile(path.join(out,'before-after.png'));
  fs.writeFileSync(path.join(out,'review.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Bố cục người chơi đã áp dụng</title><style>body{margin:0;background:#0b101b;color:#e6ecf5;font:17px system-ui}main{max-width:1250px;margin:auto;padding:24px}h1,h2,a{color:#ffd877}img{max-width:100%;height:auto}figure{margin:18px 0}.grid{display:flex;gap:20px;flex-wrap:wrap}.grid figure{max-width:600px}figcaption{padding:8px 0}</style><main><h1>Bố cục theo ảnh bạn chọn</h1><p>Ảnh runtime Unity: avatar bên trái, tên trong lòng khung, Lv/EXP và VIP ở hàng riêng, Chiến Lực phía dưới. Nhãn cạnh nhân vật chỉ còn Chủ Tướng. Nhạc nền tắt khi chạy thử trong Editor.</p><figure><img src="reference.png"><figcaption>Ảnh bạn gửi làm mẫu</figcaption></figure><figure><img src="before-after.png"><figcaption>Trái: trước. Phải: đã áp dụng, cùng khung 1920 × 1080.</figcaption></figure><div class="grid">${images.map(([file,label])=>`<figure><a href="${file}"><img src="${file}"></a><figcaption>${label}</figcaption></figure>`).join('')}</div><p><a href="../review.html">Xem toàn bộ sảnh và các tỉ lệ</a></p></main></html>`);
  fs.writeFileSync(path.join(out,'layout.json'),JSON.stringify({appliedToGame:true,captures,refinements,vipBodyChecked:12,musicMutedInEditor:true},null,2));
  fs.writeFileSync(path.join(root,'name-design.html'),`<!doctype html><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=${path.basename(out)}/review.html"><a href="${path.basename(out)}/review.html">Xem bố cục đã áp dụng</a>`);
  console.log(out);
}
main().catch(error=>{console.error(error);process.exitCode=1});
