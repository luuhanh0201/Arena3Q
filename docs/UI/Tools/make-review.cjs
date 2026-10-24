const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../../..');
const sharp=require(path.join(root,'AgentComms/codex/staging/ui-tools/node_modules/sharp'));
const review=path.join(root,'NghiemThu/UI',process.argv[2] || 'XemTruoc_Startup_Review2_20261024');
const compare=path.join(review,'comparisons');fs.mkdirSync(compare,{recursive:true});
function title(text){return Buffer.from(`<svg width="1280" height="40"><rect width="1280" height="40" fill="#0a1424"/><text x="20" y="27" fill="#ffd877" font-size="20" font-family="Arial">${text}</text></svg>`)}
async function pair(left,right,name,leftTitle='MOCKUP',rightTitle='UNITY'){
 const buffers=await Promise.all([left,right].map(file=>sharp(path.join(review,file)).resize(1280,720,{fit:'fill'}).png().toBuffer()));
 await sharp({create:{width:2560,height:760,channels:4,background:'#0a1424'}}).composite([
  {input:title(leftTitle),left:0,top:0},{input:title(rightTitle),left:1280,top:0},
  {input:buffers[0],left:0,top:40},{input:buffers[1],left:1280,top:40}
 ]).png().toFile(path.join(compare,name+'.png'));
}
async function main(){
 for(const [label,shot] of [['Login','03_login'],['Register','04_register'],['Servers','06_servers'],['Bind','07_bind'],['LoadingStall','02_loading_Stalled'],['Start','05_start'],['Wide','10_start_ultrawide']])
  await pair('before/'+shot+'.png','after/'+shot+'.png','BeforeAfter_'+label,'BEFORE bf2eb28','AFTER');
 for(const [ref,shot] of [['Login','03_login'],['Register','04_register'],['Start','05_start'],['ServerSelect','06_servers'],['BindAccount','07_bind'],['Loading','02_loading_Good'],['LoadingWeak','02_loading_Weak'],['LoadingLost','02_loading_Lost'],['LoadingStall','02_loading_Stalled'],['LoadingTimeout','02_loading_ServerTimeout']])
  await pair('reference/'+ref+'.png','after/'+shot+'.png','Reference_'+ref);
 const images=fs.readdirSync(path.join(review,'after')).filter(x=>x.endsWith('.png'));
 const width=1920,cellWidth=640,cellHeight=400;
 const tiles=[];for(let i=0;i<images.length;i++){const input=await sharp(path.join(review,'after',images[i])).resize(640,360,{fit:'contain',background:'#0a1424'}).png().toBuffer();tiles.push({input,left:i%3*640,top:Math.floor(i/3)*cellHeight+40});const label=await sharp(title(images[i])).resize(640,40).png().toBuffer();tiles.push({input:label,left:i%3*640,top:Math.floor(i/3)*cellHeight});}
 await sharp({create:{width,height:Math.ceil(images.length/3)*cellHeight,channels:4,background:'#0a1424'}}).composite(tiles).png().toFile(path.join(review,'Overview.png'));
 let html='<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Arena3Q - Review UI</title><style>body{margin:24px;background:#0a1424;color:#e6ecf5;font:16px system-ui}a{color:#ffd877}img{display:block;max-width:100%;margin:16px 0 32px}nav{display:flex;gap:16px;flex-wrap:wrap}section{margin-top:36px}h1,h2{color:#ffd877}</style><h1>Arena3Q: UI khởi động</h1><p>Bản gửi Claude review. Trái = mockup hoặc trước sửa, phải = Unity. Chưa có backend thật hoặc kiểm thiết bị.</p><nav><a href="#screens">Màn hình</a><a href="#compare">Đối chiếu</a><a href="README.md">Ghi chú QA</a></nav><img src="Overview.png"><section id="screens"><h2>Ảnh Unity</h2>';
 for(const file of images)html+=`<p>${file}</p><img loading="lazy" src="after/${file}">`;
 html+='</section><section id="compare"><h2>Đối chiếu</h2>';for(const file of fs.readdirSync(compare).filter(x=>x.endsWith('.png')))html+=`<p>${file}</p><img loading="lazy" src="comparisons/${file}">`;
 html+='</section></html>';fs.writeFileSync(path.join(review,'xem_ui.html'),html);
 console.log(images.length+' Unity screenshots, '+fs.readdirSync(compare).length+' comparisons.');
}
main().catch(e=>{console.error(e);process.exitCode=1});
