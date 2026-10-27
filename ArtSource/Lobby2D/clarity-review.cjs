'use strict';
const fs=require('node:fs'),path=require('node:path'),sharp=require('./tooling.cjs')('sharp');
const root=path.resolve(__dirname,'../..'),evidence=path.join(root,'NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const latest=prefix=>fs.readdirSync(evidence).filter(name=>name.startsWith(prefix)).sort().at(-1);
async function main(){
  const out=path.join(evidence,'clarity_'+new Date().toISOString().replace(/[:.]/g,'-'));fs.mkdirSync(out,{recursive:true});
  const captions=[];
  for(const [file,label,area,old,newer] of [
    ['Night','Màu model ban đêm',{left:430,top:160,width:570,height:710},'captures_20261027_205524/Daylight_21.png',latest('captures_')+'/Daylight_21.png'],
    ['Tooltip','Gợi ý tài nguyên vừa nội dung',{left:750,top:70,width:700,height:140},'ui_refinements_20261027_205552/Resources_Hover.png',latest('ui_refinements_')+'/Resources_Hover.png'],
    ['Battle','Mũi tên kép trong tấm đỏ',{left:1380,top:830,width:510,height:220},'captures_20261027_205524/Lobby_1920.png',latest('captures_')+'/Lobby_1920.png'],
    ['Status','Nền Wi-Fi và giờ',{left:1700,top:0,width:220,height:65},'captures_20261027_205524/Daylight_11.png',latest('captures_')+'/Daylight_11.png']]){
    const inputs=await Promise.all([old,newer].map(file=>sharp(path.join(evidence,file)).extract(area).png().toBuffer()));
    await sharp({create:{width:area.width*2,height:area.height,channels:3,background:'#101927'}}).composite([{input:inputs[0],left:0,top:0},{input:inputs[1],left:area.width,top:0}]).png().toFile(path.join(out,file+'.png'));
    captions.push([file+'.png',label]);
  }
  const keyart=path.join(root,'NghiemThu/WebGame/Clarity_20261028/keyart-before.png');
  const original=path.join(root,'docs/UI/Arena3Q_UI_Handoff/Assets/UI/Backgrounds/keyart_3heroes.png');
  const crops=await Promise.all([keyart,original].map(file=>sharp(file).resize(2400,1350).extract({left:900,top:520,width:500,height:300}).png().toBuffer()));
  await sharp({create:{width:1000,height:300,channels:3,background:'#101927'}}).composite([{input:crops[0],left:0,top:0},{input:crops[1],left:500,top:0}]).png().toFile(path.join(out,'Keyart.png'));captions.push(['Keyart.png','Nền đăng nhập: bản1280 và nguồn1672, cùng vùng khi hiển thị2400']);
  const button=path.join(root,'Assets/Arena3Q/Art/UI/Startup/UI/Sprites/Buttons/btn_gold.png');
  const proposal=path.join(root,'ArtSource/UI/Clarity_20261028/Proposals/btn-gold-ai-review.png');
  const buttons=await Promise.all([button,proposal].map(file=>sharp(file).resize(578,152,{fit:'contain',background:'#101927'}).png().toBuffer()));
  await sharp({create:{width:1156,height:152,channels:3,background:'#101927'}}).composite([{input:buttons[0],left:0,top:0},{input:buttons[1],left:578,top:0}]).png().toFile(path.join(out,'Button-proposal.png'));
  fs.writeFileSync(path.join(out,'review.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Độ nét và hoàn thiện UI</title><style>body{margin:0;background:#101927;color:#e6ecf5;font:17px system-ui}main{max-width:1300px;margin:auto;padding:24px}img{max-width:100%;height:auto}figure{margin:24px 0}figcaption{padding:8px}h1,h2,a{color:#ffd877}</style><main><h1>Độ nét và bốn điểm hoàn thiện</h1><p>Mỗi cặp: trái trước, phải sau. Ảnh Unity cùng góc, cùng khung/pha Idle. Chưa thay cho đo FPS thiết bị.</p>${captions.map(([file,label])=>`<figure><img src="${file}"><figcaption>${label}</figcaption></figure>`).join('')}<h2>Đề xuất raster2×, chưa thay vào game</h2><p>Trái: nút hiện tại phóng theo kích thước hiển thị. Phải: bản phục hồi AI dùng skill imagegen. Bản AI có đổi hoa văn nên chỉ để đối chiếu, không coi là bản phóng giữ nguyên pixel. Các asset2× cần chốt chất lượng trước khi thay; nguồn game vẫn giữ nút cũ.</p><img src="Button-proposal.png"><p><a href="../review.html">Sảnh đầy đủ</a></p></main></html>`);
  fs.writeFileSync(path.join(evidence,'clarity-review.html'),`<!doctype html><meta http-equiv="refresh" content="0;url=${path.basename(out)}/review.html">`);
  console.log(out);
}
main().catch(error=>{console.error(error);process.exitCode=1});
