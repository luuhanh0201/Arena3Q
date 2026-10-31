'use strict';
// Pack existing approved cutouts; no repainting or generation.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../../../Admin/node_modules/sharp');
const source=path.resolve(__dirname,'../Assets/Export/Sprites'),out=path.join(__dirname,'Export');
const names=['08_riverbank-1-1','09_bamboo-leaves-1-1','11_grass-clumps-1-1','11_grass-clumps-1-2','12_rice-clumps-1-1','12_rice-clumps-1-2','15_reeds-1-1','15_reeds-1-2','16_damage-decals-1-1','16_damage-decals-1-2','27_fallen-leaves-1-1','27_fallen-leaves-1-2','28_ground-marks-1-1','28_ground-marks-2-1','28_ground-marks-2-2','28_ground-marks-3-1'];
(async()=>{
 fs.mkdirSync(out,{recursive:true});const layers=[],sprites=[];
 for(const [i,name]of names.entries()){
  const file=path.join(source,name+'.png'),input=fs.readFileSync(file);
  const data=await sharp(input).resize(224,224).ensureAlpha().raw().toBuffer();
  // Transparent gutter carries nearest border RGB to keep filtered edges from turning black.
  const cell=Buffer.alloc(256*256*4);
  for(let y=0;y<256;y++)for(let x=0;x<256;x++){
   const src=(Math.max(0,Math.min(223,y-16))*224+Math.max(0,Math.min(223,x-16)))*4,dst=(y*256+x)*4;
   cell[dst]=data[src];cell[dst+1]=data[src+1];cell[dst+2]=data[src+2];cell[dst+3]=x>=16&&x<240&&y>=16&&y<240?data[src+3]:0;
  }
  layers.push({input:await sharp(cell,{raw:{width:256,height:256,channels:4}}).png().toBuffer(),left:(i%4)*256,top:Math.floor(i/4)*256});
  sprites.push({name,sourceHash:crypto.createHash('sha256').update(input).digest('hex'),uv:{x:((i%4)*256+16)/1024,y:1-(Math.floor(i/4)*256+240)/1024,width:224/1024,height:224/1024}});
 }
 const atlas=path.join(out,'chapter-details.png');await sharp({create:{width:1024,height:1024,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(layers).png().toFile(atlas);
 const bytes=fs.statSync(atlas).size;fs.writeFileSync(path.join(out,'details.json'),JSON.stringify({width:1024,height:1024,bytes,sprites},null,2)+'\n');console.log(JSON.stringify({sprites:sprites.length,bytes,atlas}));
})().catch(e=>{console.error(e);process.exitCode=1;});
