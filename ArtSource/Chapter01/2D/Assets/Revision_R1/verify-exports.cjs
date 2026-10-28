'use strict';
const fs=require('node:fs'),path=require('node:path');
const crypto=require('node:crypto');
const sharp=require('../../../../../Admin/node_modules/sharp');
const {raw,stats,base,review}=require('./process-assets.cjs');
const rgba=im=>sharp(im.data,{raw:{width:im.w,height:im.h,channels:4}});
async function main(){
 const manifest=JSON.parse(fs.readFileSync(path.join(base,'Export/atlas.json'))),pages=await Promise.all(manifest.pages.map(p=>raw(path.join(base,p))));
 for(const [file,hash] of Object.entries(manifest.sourceHashes)){if(crypto.createHash('sha256').update(fs.readFileSync(path.join(base,file))).digest('hex')!==hash)throw Error('Stale atlas; source changed: '+file);}
 const results=[],panels=[];
 for(const [i,s] of manifest.sprites.entries()){
  const card=await raw(path.join(base,'Export/Sprites',s.name+'.png')),a=pages[s.page-1],r=s.rect;
  const crop=await raw(await rgba(a).extract({left:r.x,top:r.y,width:r.width,height:r.height}).png().toBuffer());
  let alphaError=0,rgbError=0,visible=0;
  for(let p=0;p<card.data.length;p+=4){alphaError=Math.max(alphaError,Math.abs(card.data[p+3]-crop.data[p+3]));if(card.data[p+3]>=8){visible++;for(let c=0;c<3;c++)rgbError+=Math.abs(card.data[p+c]-crop.data[p+c]);}}
  const meanRgbError=rgbError/(visible*3);if(alphaError>1||meanRgbError>2)throw Error('Atlas changed visible sprite '+s.name+' '+meanRgbError);
  results.push({name:s.name,maxAlphaError:alphaError,meanVisibleRgbError:meanRgbError});
  const label=Buffer.from('<svg width="256" height="30"><rect width="256" height="30" fill="#eee5d0"/><text x="6" y="20" font-size="12" fill="#222">'+s.name+'</text></svg>');
  const panel=await rgba(card).flatten({background:'#b99b6b'}).extend({top:30,bottom:0,left:0,right:0,background:'#eee5d0'}).composite([{input:label,left:0,top:0}]).png().toBuffer();
  panels.push({input:panel,left:(i%5)*256,top:Math.floor(i/5)*286});
 }
 await sharp({create:{width:1280,height:Math.ceil(panels.length/5)*286,channels:3,background:'#ddd'}}).composite(panels).png().toFile(path.join(review,'sprite-contact-sheet.png'));
 const atlasPairs=[];
 for(let page=0;page<pages.length;page++){
  const layers=manifest.sprites.filter(s=>s.page===page+1).map(s=>({input:path.join(base,'Export/Sprites',s.name+'.png'),left:s.rect.x,top:s.rect.y}));
  const before=await sharp({create:{width:pages[page].w,height:pages[page].h,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(layers).png().toBuffer();
  for(const [col,buffer] of [before,await rgba(pages[page]).png().toBuffer()].entries())atlasPairs.push({input:await sharp(buffer).flatten({background:'#b99b6b'}).resize(512,512).png().toBuffer(),left:col*512,top:page*512});
 }
 await sharp({create:{width:1024,height:512*pages.length,channels:3,background:'#b99b6b'}}).composite(atlasPairs).png().toFile(path.join(review,'atlas-before-after.png'));
 const metrics=fs.readdirSync(path.join(__dirname,'Metrics')).filter(p=>p.endsWith('.json')).map(p=>JSON.parse(fs.readFileSync(path.join(__dirname,'Metrics',p))));
 for(const m of metrics){if(m.after.alphaMax!==255)throw Error('Alpha max '+m.id);for(const e of m.exports){if((e.width&(e.width-1))||(e.height&(e.height-1)))throw Error('Non POT');if(e.seam&&(e.seam.horizontal>1||e.seam.vertical>1))throw Error('Seam '+m.id);}}
 const report={sourceCount:metrics.length,exportCount:metrics.reduce((s,m)=>s+m.exports.length,0),sprites:results.length,pages:pages.map(stats),spriteComparisons:results,allPassed:true,scope:'Technical image/packing checks only; scene, mip/compression, device FPS not validated'};
 fs.writeFileSync(path.join(__dirname,'export-check.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({sources:metrics.length,exports:report.exportCount,sprites:results.length,pages:pages.length,allPassed:true}));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
