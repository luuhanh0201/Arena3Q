'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('../../../../Admin/node_modules/sharp');
const target=path.resolve(__dirname,'../../../../Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Textures/UI');
const specs={'medallion':256,'boss-medallion':256,'nameplate':512,'altar-banner':512,'chest-wood':256,'chest-silver':256,'chest-gold':256};
(async()=>{
 fs.mkdirSync(target,{recursive:true});const report=[];
 for(const [name,size] of Object.entries(specs)){
  const source=path.join(__dirname,'Generated',name+'.png');
  const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let left=info.width,top=info.height,right=0,bottom=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>4){left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);}
  left=Math.max(0,left-3);top=Math.max(0,top-3);right=Math.min(info.width-1,right+3);bottom=Math.min(info.height-1,bottom+3);
  const file=path.join(target,name+'.png');
  await sharp(source).extract({left,top,width:right-left+1,height:bottom-top+1}).resize({width:size,height:name==='nameplate'||name==='altar-banner'?128:size,fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).png().toFile(file);
  report.push({name,sourceWidth:info.width,sourceHeight:info.height,bytes:fs.statSync(file).size});
 }
 fs.writeFileSync(path.join(__dirname,'exports.json'),JSON.stringify(report,null,2)+'\n');console.log(report);
})().catch(e=>{console.error(e);process.exitCode=1;});
