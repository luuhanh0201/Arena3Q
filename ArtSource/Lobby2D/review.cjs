'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
const project=path.resolve(__dirname,'../..');
const delivery=JSON.parse(fs.readFileSync(path.join(__dirname,'delivery.json'),'utf8'));
const out=path.join(project,'NghiemThu/UI/XemTruoc_Lobby2D_20261026');fs.mkdirSync(out,{recursive:true});
function label(text,width,height=28){
  return new Resvg(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><rect width="100%" height="100%" fill="#101018"/><text x="8" y="20" font-size="15" font-family="Arial" fill="#ffd166">${text}</text></svg>`).render().asPng();
}
async function sheet(name,assets,w,h,columns){
  const items=[];
  for(let i=0;i<assets.length;i++){
    const a=assets[i],left=(i%columns)*w,top=Math.floor(i/columns)*(h+28);
    const data=await sharp(path.join(project,a.path)).resize(w,h,{fit:'contain',background:'#232b3b'}).flatten({background:'#232b3b'}).png().toBuffer();
    items.push({input:data,left,top},{input:label(a.id,w),left,top:top+h});
  }
  await sharp({create:{width:w*columns,height:(h+28)*Math.ceil(assets.length/columns),channels:4,background:'#17243a'}}).composite(items).png().toFile(path.join(out,name+'.png'));
}
async function main(){
  await sheet('UI_Icons',delivery.assets.filter(a=>a.role==='icon'),180,180,6);
  await sheet('VIP_01_12',delivery.assets.filter(a=>a.role==='vip'),300,130,4);
  await sheet('LuBu_Portraits_KeyPoses',delivery.assets.filter(a=>['portrait','key-pose'].includes(a.role)),310,310,4);
  await sheet('Environment_Layers',delivery.assets.filter(a=>a.role.startsWith('environment')||a.role==='sky'),400,225,3);
  await sheet('Buttons_Banners_Frames',delivery.assets.filter(a=>['button','effect','frame','banner'].includes(a.role)),350,160,4);
  const baseline=path.join(__dirname,'Generated/LuBu_FullBody_v01.png');
  await sheet('FullBody_Framing_Before_After',[
    {id:'BEFORE - clipped silhouette',path:path.relative(project,baseline)},
    delivery.assets.find(a=>a.id==='LuBu_FullBody')
  ],620,620,2);
  console.log(out);
}
main().catch(e=>{console.error(e);process.exitCode=1;});
