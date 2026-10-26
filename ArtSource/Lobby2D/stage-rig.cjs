'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const project=path.resolve(__dirname,'../..'),target=path.join(project,'Assets/Arena3Q/Lobby2D/Characters/LuBu/RigSource');
fs.mkdirSync(target,{recursive:true});
async function main(){
  const locations=JSON.parse(fs.readFileSync(path.join(__dirname,'rig-source-locations.json'),'utf8')),result={};
  for(const [id,file]of Object.entries(locations)){
    const raw=fs.readFileSync(file),archive=path.join(__dirname,'Generated','Rig_'+id+'_source.png');
    fs.writeFileSync(archive,raw);const destination=path.join(target,id+'.png');fs.writeFileSync(destination,raw);
    const {data,info}=await sharp(raw).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    let x0=info.width,y0=info.height,x1=0,y1=0;
    for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>8){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
    result[id]={path:path.relative(project,destination).replaceAll('\\','/'),width:info.width,height:info.height,bounds:[x0,y0,x1,y1]};
    locations[id]=path.relative(project,archive).replaceAll('\\','/');
  }
  fs.writeFileSync(path.join(__dirname,'rig-textures.json'),JSON.stringify(result,null,2)+'\n');
  fs.writeFileSync(path.join(__dirname,'rig-source-locations.json'),JSON.stringify(locations,null,2)+'\n');
  console.log('RIG_SOURCES '+Object.keys(result).length);
}
main().catch(e=>{console.error(e);process.exitCode=1;});
