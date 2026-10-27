'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const root=path.resolve(__dirname,'../..');
async function main(){
  const frames=[];
  for(let tier=1;tier<=12;tier++){
    const file=path.join(root,'Assets/Arena3Q/Lobby2D/UI/VIP/VIP_'+String(tier).padStart(2,'0')+'.png');
    const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    let start=-1,best=[0,0];
    const left=Math.floor(info.width*.35),right=Math.ceil(info.width*.65);
    for(let y=Math.floor(info.height*.25);y<=Math.floor(info.height*.85);y++){
      let red=0;
      for(let x=left;x<right;x++){const p=(y*info.width+x)*4,r=data[p],g=data[p+1],b=data[p+2];
        const nameRed=r>50&&r>g*1.65&&r>b*1.3;
        const nameSilver=Math.min(r,g,b)>85&&Math.max(r,g,b)-Math.min(r,g,b)<45;
        if(data[p+3]>240&&(nameRed||nameSilver))red++;}
      const active=red/(right-left)>.6;
      if(active&&start<0)start=y;
      if(!active&&start>=0){if(y-start>best[1]-best[0])best=[start,y];start=-1;}
    }
    if(best[1]-best[0]<24)throw Error('No continuous name band for VIP '+tier);
    frames.push({tier,bodyAnchorY:Number(((best[0]+best[1])/(2*info.height)).toFixed(6)),bodyHeightY:Number(((best[1]-best[0])/info.height).toFixed(6)),sourceNameBand:best,sourceHeight:info.height});
  }
  fs.writeFileSync(path.join(__dirname,'vip-layout-metrics.json'),JSON.stringify({frames},null,2));
  console.log(JSON.stringify(frames));
}
main().catch(error=>{console.error(error);process.exitCode=1});
