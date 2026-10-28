'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('../../../../../Admin/node_modules/sharp');
const {raw,cleanAlpha,bounds,save,resizeRaw,base}=require('./process-assets.cjs');
const {separate}=require('./component-regions.cjs');
const grids={7:[3,1],8:[1,2],9:[3,1],10:[3,1],11:[3,1],12:[2,1],13:[2,1],14:[2,1],15:[2,1],16:[2,2],19:[3,1],20:[1,1],21:[3,1],22:[2,1],23:[2,1],24:[2,1],25:[2,1],26:[1,3],27:[2,3],28:[1,3],29:[1,1]};
const rgba=im=>sharp(im.data,{raw:{width:im.w,height:im.h,channels:4}});
function seams(im,axis,count,start=0,end=axis==='x'?im.h:im.w){const length=axis==='x'?im.w:im.h,step=length/count,out=[0];
 for(let k=1;k<count;k++){let best=Infinity,cut=Math.round(k*step);for(let p=Math.max(out.at(-1)+1,Math.round((k-.35)*step));p<Math.min(length,Math.round((k+.35)*step));p++){
  let n=0;for(let q=start;q<end;q++){const i=(axis==='x'?q*im.w+p:p*im.w+q)*4;if(im.data[i+3]>16)n++;}
  const score=n*100000+Math.abs(p-k*step);if(score<best){best=score;cut=p;}}
 out.push(cut);}return out.concat(length);}
async function main(){
 const jobs=JSON.parse(fs.readFileSync(path.join(base,'prompts.json'),'utf8').replace(/^\uFEFF/,''));
 const entries=[];
 for(const job of jobs.filter(j=>j.transparent)){const im=await raw(path.join(base,job.id+'.png')),n=Number(job.id.slice(0,2)),[cols,rows]=grids[n],ys=seams(im,'y',rows),parts=n===9?separate(im,3):null;
  for(let row=0;row<rows;row++){const colCount=n===28&&row===1?2:cols,xs=seams(im,'x',colCount,ys[row],ys[row+1]);
   for(let col=0;col<colCount;col++){const region=parts?parts[col].rect:{left:xs[col],top:ys[row],width:xs[col+1]-xs[col],height:ys[row+1]-ys[row]};
    const slice=parts?parts[col].image:await raw(await rgba(im).extract(region).png().toBuffer()),box=bounds(slice,8);if(box.width<=0||box.height<=0)throw Error('Empty slice '+job.id);
    const trimmed=await raw(await rgba(slice).extract(box).png().toBuffer());
    let card=await resizeRaw(trimmed,{width:256,height:256,fit:'contain',background:{r:0,g:0,b:0,alpha:0},kernel:'lanczos3'});cleanAlpha(card,true);
    const name=job.id+'-'+(row+1)+'-'+(col+1);await save(card,path.join(base,'Export/Sprites',name+'.png'));
    entries.push({name,source:job.id+'.png',sourceRect:{x:region.left+box.left,y:region.top+box.top,width:box.width,height:box.height},extraction:parts?'connected alpha component with own edge fringe':'grid region',pivot: n===8||n===16||n===28||n===29?[.5,.5]:[.5,0],card});
   }
  }
 }
 const manifest={pageSize:1024,pageSizes:[],spriteCell:256,gutterBetweenRects:64,paddingMethod:'body RGB expanded through alpha-zero in each 288px slot; no rotation',coordinates:'pixels from top-left; pivots normalized x,y from bottom-left',sprites:[],pages:[]};
 for(let page=0;page<Math.ceil(entries.length/9);page++){
  const items=entries.slice(page*9,page*9+9),layers=[],pageSize=items.length===1?512:1024;
  for(let i=0;i<items.length;i++){const e=items[i],x=(i%3)*320+16,y=Math.floor(i/3)*320+16;
   let padded=await raw(await rgba(e.card).extend({top:16,bottom:16,left:16,right:16,background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer());cleanAlpha(padded,true);
   layers.push({input:await rgba(padded).png().toBuffer(),left:x-16,top:y-16});
   const {card,...meta}=e;manifest.sprites.push({...meta,page:page+1,rect:{x,y,width:256,height:256}});
  }
  const output=await sharp({create:{width:pageSize,height:pageSize,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(layers).png().toBuffer();
  const im=await raw(output);cleanAlpha(im,true);const file='Export/Atlas/atlas-'+String(page+1).padStart(2,'0')+'.png';await save(im,path.join(base,file));manifest.pages.push(file);manifest.pageSizes.push(pageSize);
 }
 fs.writeFileSync(path.join(base,'Export/atlas.json'),JSON.stringify(manifest,null,2));console.log(JSON.stringify({sprites:manifest.sprites.length,pages:manifest.pages.length}));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
