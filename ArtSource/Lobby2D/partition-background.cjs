'use strict';
// Registered sprite export: distribute the unchanged source pixels between semantic draw layers.
// All color/geometry comes from the generated canonical plate. No repaint, resizing or camera regeneration.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const project=path.resolve(__dirname,'../..'),out=path.join(__dirname,'Registered');fs.mkdirSync(out,{recursive:true});
function inside(x,y,poly){let hit=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){
  const a=poly[i],b=poly[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])hit=!hit;
}return hit;}
const foreground=[[0,0],[450,0],[430,180],[540,270],[520,510],[590,680],[660,795],[1205,840],[1205,620],[1310,620],[1320,900],[1510,930],[1510,705],[1615,705],[1615,950],[1785,985],[1785,680],[1875,680],[1875,900],[1920,900],[1920,1080],[0,1080]];
const farLine=[[0,520],[450,380],[700,490],[880,410],[1040,320],[1200,500],[1380,500],[1460,350],[1660,450],[1920,530]];
function lineY(x){for(let i=1;i<farLine.length;i++)if(x<=farLine[i][0]){const a=farLine[i-1],b=farLine[i];return a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0]);}return 530;}
// Inner lantern apertures only. The fixture remains in its architecture layer.
const lights=[[68,797,36,70],[356,560,22,38],[1260,720,31,75],[1540,800,24,55],[1835,790,24,62],[1118,665,15,35],[869,674,11,27],[782,644,11,23]];
async function main(){
  const {data,info}=await sharp('Assets/Arena3Q/Lobby2D/Backgrounds/Lobby/Environment/Lobby_Architecture.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const masks=['Far_Background','Buildings','Foreground'].map(()=>Buffer.alloc(data.length));
  const lighting=Buffer.alloc(data.length);let assigned=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
    const i=(y*info.width+x)*4;const layer=inside(x+.5,y+.5,foreground)?2:y<lineY(x)?0:1;
    data.copy(masks[layer],i,i,i+4);if(data[i+3])assigned++;
    let strength=0;
    for(const [cx,cy,rx,ry]of lights){const d=((x-cx)/rx)**2+((y-cy)/ry)**2;if(d<1)strength=Math.max(strength,(1-d)**1.4);}
    if(strength>0&&data[i]>100&&data[i+1]>65&&data[i]>data[i+2]*1.1){
      lighting[i]=data[i];lighting[i+1]=data[i+1];lighting[i+2]=data[i+2];lighting[i+3]=Math.round(data[i+3]*strength);
    }
  }
  const names=['Far_Background','Buildings','Foreground'];
  const sources=JSON.parse(fs.readFileSync(path.join(__dirname,'sources.json'),'utf8'));
  for(let j=0;j<3;j++){
    const file=path.join(out,names[j]+'.png');await sharp(masks[j],{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(file);
    sources['Environment_'+names[j]]={path:path.relative(project,file).replaceAll('\\','/'),visualStatus:'registered-source-pixels'};
  }
  const file=path.join(out,'Lighting.png');await sharp(lighting,{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(file);
  sources.Environment_Lighting={path:path.relative(project,file).replaceAll('\\','/'),visualStatus:'registered-source-pixels'};
  let mismatch=0;for(let i=0;i<data.length;i+=4){const chosen=masks.find(m=>m[i+3]!==0);if(!chosen){if(data[i+3]!==0)mismatch++;continue;}for(let c=0;c<4;c++)if(chosen[i+c]!==data[i+c])mismatch++;}
  fs.writeFileSync(path.join(__dirname,'sources.json'),JSON.stringify(sources,null,2)+'\n');
  fs.writeFileSync(path.join(__dirname,'background-registration.json'),JSON.stringify({width:info.width,height:info.height,assignedPixels:assigned,pixelMismatches:mismatch,passed:mismatch===0,foreground,farLine,lights},null,2)+'\n');
  if(mismatch)throw Error('Layer registration failed');console.log('REGISTERED_BACKGROUND_OK pixels='+assigned+' mismatches=0');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
