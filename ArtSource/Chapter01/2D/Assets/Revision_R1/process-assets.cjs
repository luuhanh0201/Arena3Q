'use strict';
// Technical export steps explicitly requested by handoff 1305/1310/1312/1314.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../../../../Admin/node_modules/sharp');
const base=path.resolve(__dirname,'..'),repo=path.resolve(base,'../../../..');
const review=path.join(repo,'NghiemThu/Chapter01/XemTruoc_Sua2D_20261028');
const jobs=JSON.parse(fs.readFileSync(path.join(base,'prompts.json'),'utf8').replace(/^\uFEFF/,''));
const rgba=(data,w,h)=>sharp(data,{raw:{width:w,height:h,channels:4}});
const clamp=v=>Math.max(0,Math.min(255,Math.round(v)));
async function raw(file){const r=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});return {data:r.data,w:r.info.width,h:r.info.height};}
function cleanAlpha(im,preserveSoft=false){
 const {data,w,h}=im,N=w*h,original=Buffer.from(data),owner=new Int32Array(N).fill(-1),queue=new Int32Array(N);
 let end=0,head=0,max=0;for(let i=0;i<N;i++)max=Math.max(max,data[i*4+3]);
 if(!max)throw Error('Empty image');const threshold=Math.min(240,max);
 for(let i=0;i<N;i++){
  const k=i*4,a=data[k+3],hi=Math.max(data[k],data[k+1],data[k+2]),lo=Math.min(data[k],data[k+1],data[k+2]);
  const valid=preserveSoft?a>=8&&hi>20&&(hi-lo)/hi<.8:a>=threshold;
  if(valid){owner[i]=i;queue[end++]=i;}if(a>=threshold)data[k+3]=255;
 }
 while(head<end){const i=queue[head++],x=i%w,near=owner[i];
  for(const j of [x?i-1:-1,x<w-1?i+1:-1,i>=w?i-w:-1,i<N-w?i+w:-1])
   if(j>=0&&owner[j]<0){owner[j]=near;queue[end++]=j;}
 }
 // Replace contaminated semitransparent RGB and extend valid body color through
 // all transparent pixels (at least the requested 8 px; alpha itself stays soft).
 for(let i=0;i<N;i++)if(preserveSoft?owner[i]!==i:original[i*4+3]<threshold){const k=owner[i]*4;for(let c=0;c<3;c++)data[i*4+c]=original[k+c];}
 return im;
}
function stats(im){const {data,w,h}=im;let clear=0,opaque=0,soft=0,visible=0,blackClear=0,max=0;const sum=[0,0,0];
 for(let i=0;i<data.length;i+=4){const a=data[i+3];max=Math.max(max,a);if(!a){clear++;if(data[i]+data[i+1]+data[i+2]===0)blackClear++;}else{visible++;if(a===255)opaque++;else soft++;}for(let c=0;c<3;c++)sum[c]+=data[i+c];}
 return {width:w,height:h,alphaMax:max,opaquePixels:opaque,opaqueRatio:opaque/(w*h),opaqueVisibleRatio:opaque/Math.max(1,visible),softRatio:soft/(w*h),transparentPixels:clear,blackTransparentPixels:blackClear,meanRgb:sum.map(v=>+(v/(w*h)).toFixed(2))};
}
function bounds(im,threshold=8){let x0=im.w,y0=im.h,x1=-1,y1=-1;for(let y=0;y<im.h;y++)for(let x=0;x<im.w;x++)if(im.data[(y*im.w+x)*4+3]>=threshold){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}return {left:x0,top:y0,width:x1-x0+1,height:y1-y0+1};}
function tint(im,target){const s=stats(im);for(let i=0;i<im.data.length;i+=4)for(let c=0;c<3;c++)im.data[i+c]=clamp(im.data[i+c]*target[c]/s.meanRgb[c]);}
function periodic(im,band=32){const {data,w,h}=im;
 for(let y=0;y<h;y++)for(let c=0;c<3;c++){const d=data[(y*w+w-1)*4+c]-data[y*w*4+c];for(let k=0;k<band;k++){const a=(y*w+k)*4+c,b=(y*w+w-1-k)*4+c,t=.5*(1-k/band)**2;data[a]=clamp(data[a]+d*t);data[b]=clamp(data[b]-d*t);}}
 for(let x=0;x<w;x++)for(let c=0;c<3;c++){const d=data[((h-1)*w+x)*4+c]-data[x*4+c];for(let k=0;k<band;k++){const a=(k*w+x)*4+c,b=((h-1-k)*w+x)*4+c,t=.5*(1-k/band)**2;data[a]=clamp(data[a]+d*t);data[b]=clamp(data[b]-d*t);}}
 return im;
}
function seam(im){const {data,w,h}=im;let sx=0,sy=0,ix=0,iy=0;for(let y=0;y<h;y++)for(let c=0;c<3;c++){sx+=Math.abs(data[(y*w)*4+c]-data[(y*w+w-1)*4+c]);ix+=Math.abs(data[(y*w+Math.floor(w/2))*4+c]-data[(y*w+Math.floor(w/2)+1)*4+c]);}for(let x=0;x<w;x++)for(let c=0;c<3;c++){sy+=Math.abs(data[x*4+c]-data[((h-1)*w+x)*4+c]);iy+=Math.abs(data[(Math.floor(h/2)*w+x)*4+c]-data[((Math.floor(h/2)+1)*w+x)*4+c]);}return {horizontal:sx/(h*3),vertical:sy/(w*3),interiorHorizontal:ix/(h*3),interiorVertical:iy/(w*3)};}
async function resizeRaw(im,options){const r=await rgba(im.data,im.w,im.h).resize(options).raw().toBuffer({resolveWithObject:true});return {data:r.data,w:r.info.width,h:r.info.height};}
async function save(im,file){fs.mkdirSync(path.dirname(file),{recursive:true});await rgba(im.data,im.w,im.h).png().toFile(file);}
async function comparison(j,before,after){
 const backgrounds=['01_ground-earth','02_ground-path','03_ground-grass'];if(j.id==='08_riverbank')backgrounds.push('05_water');
 const width=700,height=360,rows=[];
 for(const name of backgrounds){const bg=await sharp(path.join(base,name+'.png')).resize(width,height,{fit:'cover'}).png().toBuffer();
  for(const [index,im] of [before,after].entries()){const over=await rgba(im.data,im.w,im.h).resize(width-40,height-30,{fit:'inside'}).png().toBuffer();const m=await sharp(over).metadata();const b=await sharp(bg).composite([{input:over,left:Math.floor((width-m.width)/2),top:Math.floor((height-m.height)/2)}]).png().toBuffer();rows.push({input:b,left:index*width,top:rows.length<2?0:Math.floor(rows.length/2)*height});}
 }
 // Labels are supplied by the review page, preserving identical image scale.
 const out=path.join(review,j.id+'-before-after.png');fs.mkdirSync(review,{recursive:true});
 await sharp({create:{width:width*2,height:height*backgrounds.length,channels:3,background:'#b99b6b'}}).composite(rows).png().toFile(out);
 return path.relative(repo,out).replaceAll('\\','/');
}
async function processOne(j){
 const source=path.join(__dirname,'Generated',j.id+'.png');const input=fs.existsSync(source)?source:path.join(__dirname,'Before',j.id+'.png');
 const before=await raw(path.join(__dirname,'Before',j.id+'.png'));let im=await raw(input);const originalStats=stats(im);
 if(j.id==='21_weapon-cards')for(let i=0;i<im.data.length;i+=4){const r=im.data[i],g=im.data[i+1],b=im.data[i+2];if(r>g*1.25&&r>b*1.4){const l=.2126*r+.7152*g+.0722*b;im.data[i]=clamp(l+.83*(r-l));im.data[i+1]=clamp(l+.83*(g-l));im.data[i+2]=clamp(l+.83*(b-l));}}
 if(j.id==='08_riverbank')for(let i=0;i<im.data.length;i+=4){const r=im.data[i],g=im.data[i+1],b=im.data[i+2];if(b>=r&&g>r&&g>150&&im.data[i+3]<240){im.data[i]=clamp(r*.92);im.data[i+1]=clamp(g*.92);im.data[i+2]=clamp(b*.92);}}
 if(j.id==='29_ritual-circle'){const b=bounds(im,200);const p=await rgba(im.data,im.w,im.h).extract(b).resize(960,960,{fit:'fill'}).extend({top:32,bottom:32,left:32,right:32,background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer();im=await raw(p);}
 if(j.transparent)cleanAlpha(im,j.id==='26_distant-mountains');else{
  if(j.id==='17_soldier-tent-cloth'||j.id==='18_command-tent-cloth')tint(im,[199,161,48]);
  // Keep the cloth base close to its prescribed ochre, not just the global mean.
  if(j.id==='18_command-tent-cloth')for(let i=0;i<im.data.length;i+=4)for(let c=0;c<3;c++)im.data[i+c]=clamp([199,161,48][c]+.55*(im.data[i+c]-[199,161,48][c]));
  if(j.id==='05_water')tint(im,[102,153,154]);periodic(im);
 }
 const output=path.join(base,j.id+'.png');await save(im,output);
 const exports=[];for(const size of [1024,512]){
  const exportHeight=['08_riverbank','12_rice-clumps','14_fallen-logs','26_distant-mountains'].includes(j.id)?size/2:size;
  let small=await resizeRaw(im,{width:size,height:exportHeight,fit:j.transparent?'contain':'fill',background:{r:0,g:0,b:0,alpha:0},kernel:'lanczos3'});
  if(j.transparent)cleanAlpha(small,j.id==='26_distant-mountains');else periodic(small,16);
  const file=path.join(base,'Export',String(size),j.id+'.png');await save(small,file);exports.push({size,file:path.relative(base,file).replaceAll('\\','/'),...stats(small),seam:j.transparent?undefined:seam(small)});
 }
 const pair=await comparison(j,before,im);
 const report={id:j.id,input:path.relative(base,input).replaceAll('\\','/'),before:stats(before),generated:originalStats,after:stats(im),bbox:bounds(im,200),sourceSeam:j.transparent?undefined:seam(im),exports,comparison:pair,sha256:crypto.createHash('sha256').update(fs.readFileSync(output)).digest('hex')};
 fs.mkdirSync(path.join(__dirname,'Metrics'),{recursive:true});fs.writeFileSync(path.join(__dirname,'Metrics',j.id+'.json'),JSON.stringify(report,null,2));
 if(j.transparent&&(report.after.alphaMax!==255||report.after.blackTransparentPixels!==0))throw Error('Alpha/padding failed '+j.id);
 console.log(j.id+' OK');return report;
}
async function main(){const ids=process.argv.slice(2);for(const j of jobs.filter(j=>!ids.length||ids.includes(j.id)))await processOne(j);}
if(require.main===module)main().catch(e=>{console.error(e);process.exitCode=1;});
module.exports={raw,cleanAlpha,stats,bounds,save,resizeRaw,base,repo,review};
