'use strict';
// Pack approved cutouts plus the three separately generated review supplements.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../../../Admin/node_modules/sharp');
const source=path.resolve(__dirname,'../Assets/Export/Sprites'),out=path.join(__dirname,'Export');
const names=['08_riverbank-1-1','09_bamboo-leaves-1-1','11_grass-clumps-1-1','11_grass-clumps-1-2','12_rice-clumps-1-1','12_rice-clumps-1-2','15_reeds-1-1','15_reeds-1-2','16_damage-decals-1-1','16_damage-decals-1-2','27_fallen-leaves-1-1','27_fallen-leaves-1-2','28_ground-marks-1-1','28_ground-marks-2-1','28_ground-marks-2-2','28_ground-marks-3-1'];
names.push('wildflowers','fallen-cloth','village-litter');
const columns=5,cellSize=204,content=176,gutter=14,atlasSize=1024;
(async()=>{
 fs.mkdirSync(out,{recursive:true});const layers=[],sprites=[];
 for(const [i,name]of names.entries()){
  const file=path.join(i<16?source:path.join(__dirname,'Generated'),name+'.png'),input=fs.readFileSync(file);
  const data=await sharp(input).resize(content,content).ensureAlpha().raw().toBuffer();
  // Transparent gutter carries nearest border RGB to keep filtered edges from turning black.
  const cell=Buffer.alloc(cellSize*cellSize*4);
  for(let y=0;y<cellSize;y++)for(let x=0;x<cellSize;x++){
   const src=(Math.max(0,Math.min(content-1,y-gutter))*content+Math.max(0,Math.min(content-1,x-gutter)))*4,dst=(y*cellSize+x)*4;
   cell[dst]=data[src];cell[dst+1]=data[src+1];cell[dst+2]=data[src+2];cell[dst+3]=x>=gutter&&x<cellSize-gutter&&y>=gutter&&y<cellSize-gutter?data[src+3]:0;
  }
  layers.push({input:await sharp(cell,{raw:{width:cellSize,height:cellSize,channels:4}}).png().toBuffer(),left:(i%columns)*cellSize,top:Math.floor(i/columns)*cellSize});
  sprites.push({name,sourceHash:crypto.createHash('sha256').update(input).digest('hex'),uv:{x:((i%columns)*cellSize+gutter)/atlasSize,y:1-(Math.floor(i/columns)*cellSize+cellSize-gutter)/atlasSize,width:content/atlasSize,height:content/atlasSize}});
 }
 const atlas=path.join(out,'chapter-details.png');await sharp({create:{width:1024,height:1024,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(layers).png().toFile(atlas);
 const bytes=fs.statSync(atlas).size;fs.writeFileSync(path.join(out,'details.json'),JSON.stringify({width:1024,height:1024,bytes,sprites},null,2)+'\n');console.log(JSON.stringify({sprites:sprites.length,bytes,atlas}));
})().catch(e=>{console.error(e);process.exitCode=1;});
