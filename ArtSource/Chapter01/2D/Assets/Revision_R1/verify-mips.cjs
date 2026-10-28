'use strict';
const fs=require('node:fs'),path=require('node:path'),sharp=require('../../../../../Admin/node_modules/sharp');
const {raw,base,review}=require('./process-assets.cjs');
const rgba=im=>sharp(im.data,{raw:{width:im.w,height:im.h,channels:4}});
(async()=>{
 const m=JSON.parse(fs.readFileSync(path.join(base,'Export/atlas.json'))),results=[];
 for(let p=0;p<m.pages.length;p++){
  const im=await raw(path.join(base,m.pages[p])),sprites=m.sprites.filter(s=>s.page===p+1);
  for(const level of [1,2,3,4]){const factor=2**level,small=await raw(await rgba(im).resize(im.w/factor,im.h/factor).png().toBuffer());
   for(const s of sprites){const isolated={w:im.w,h:im.h,data:Buffer.from(im.data)},r=s.rect;
    // Keep identical RGB everywhere; remove every other sprite's alpha only.
    for(let y=0;y<im.h;y++)for(let x=0;x<im.w;x++)if(x<r.x||x>=r.x+r.width||y<r.y||y>=r.y+r.height)isolated.data[(y*im.w+x)*4+3]=0;
    const control=await raw(await rgba(isolated).resize(im.w/factor,im.h/factor).png().toBuffer());let max=0;
    for(let y=r.y/factor;y<(r.y+r.height)/factor;y++)for(let x=r.x/factor;x<(r.x+r.width)/factor;x++){const i=(y*small.w+x)*4;for(let c=0;c<4;c++)max=Math.max(max,Math.abs(small.data[i+c]-control.data[i+c]));}
    results.push({name:s.name,level,maxChannelDifferenceFromIsolated:max});if(max>1)throw Error('Neighbor bleed '+s.name+' mip '+level+' delta '+max);
   }
  }
 }
 const edge=[];for(const size of [1024,512]){const im=await raw(path.join(base,'Export',String(size),'14_fallen-logs.png'));let soft=0,green=0;
  for(let i=0;i<im.data.length;i+=4){const [r,g,b,a]=im.data.subarray(i,i+4);if(a>0&&a<240){soft++;const hi=Math.max(r,g,b),lo=Math.min(r,g,b);if(g>1.2*r&&g>1.4*b&&(hi-lo)/hi>.7)green++;}}
  edge.push({size,softPixels:soft,vividGreenEdgePixels:green});
 }
 const panels=[];for(const [index,size] of [1024,512].entries())for(const [row,bg] of ['#B99B6B','#748358','#dddddd','#242424'].entries())panels.push({input:await sharp(path.join(base,'Export',String(size),'14_fallen-logs.png')).resize(700,350,{fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).flatten({background:bg}).png().toBuffer(),left:index*700,top:row*350});
 await sharp({create:{width:1400,height:1400,channels:3,background:'#fff'}}).composite(panels).png().toFile(path.join(review,'14-export-edge-proof.png'));
 const report={allPassed:true,comparisonCount:results.length,levels:[1,2,3,4],scope:'Offline Lanczos reduction only. Engine mip filter, compression and lower mips require Unity validation.',logEdges:edge,results};
 fs.writeFileSync(path.join(__dirname,'mip-check.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({allPassed:true,comparisons:results.length,logEdges:edge}));
})().catch(e=>{console.error(e);process.exitCode=1;});
