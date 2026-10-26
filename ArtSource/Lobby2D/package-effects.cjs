'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const folder=path.resolve(__dirname,'../../Assets/Arena3Q/Lobby2D/Effects/Characters/LuBu_Attack'),out=path.join(__dirname,'WebFrames/VFX');fs.mkdirSync(out,{recursive:true});
async function main(){
  const frames=[],pages=[];
  for(let i=0;i<24;i++){
    const name='LuBu_Attack_VFX_'+String(i).padStart(3,'0')+'.png',file=path.join(folder,name);
    const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});let pixels=0,edge=0;
    for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){const a=data[(y*info.width+x)*4+3];if(a>8){pixels++;if(!x||!y||x===info.width-1||y===info.height-1)edge++;}}
    if((i<11||i>17)&&pixels)throw Error('VFX exists outside its impact/follow-through window');if(edge)throw Error('VFX cropped at '+i);
    const preview=await sharp(file).resize(512,512).png().toBuffer();fs.writeFileSync(path.join(out,name),preview);
    pages.push(await sharp(preview).ensureAlpha().raw().toBuffer());frames.push({index:i,file:name,time:i*1.1/24,visiblePixels:pixels,pivot:[.5,.2]});
  }
  if(!frames[13].visiblePixels)throw Error('No impact VFX');
  await sharp(Buffer.concat(pages),{raw:{width:512,height:512*24,channels:4,pageHeight:512}}).gif({delay:Array.from({length:24},(_,i)=>(Math.round((i+1)*110/24)-Math.round(i*110/24))*10),loop:1,effort:7}).toFile(path.join(folder,'preview.gif'));
  fs.writeFileSync(path.join(folder,'frames.json'),JSON.stringify({name:'LuBu_Attack_VFX',duration:1.1,loop:false,source:'Kenney Particle Pack 1.1 slash_03, CC0',frames,damageSource:false},null,2)+'\n');
  console.log('VFX_PACKAGED active='+frames.filter(f=>f.visiblePixels).map(f=>f.index).join(',')+' no-clipping independent=true');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
