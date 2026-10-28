'use strict';
// Export only the three revised opaque terrain tiles. Never rebuild R1 sprites.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../../../../Admin/node_modules/sharp');
const base=path.resolve(__dirname,'..'),review=path.resolve(base,'../../../../NghiemThu/Chapter01/XemTruoc_DoiMau_20261028');
const ids=['01_ground-earth','02_ground-path','04_paddy-ground'];
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
async function periodic(input,size){
 const {data,info}=await sharp(input).removeAlpha().resize(size,size).raw().toBuffer({resolveWithObject:true});
 const w=info.width,h=info.height,band=16,clamp=v=>Math.max(0,Math.min(255,Math.round(v)));
 for(let y=0;y<h;y++)for(let c=0;c<3;c++){const d=data[(y*w+w-1)*3+c]-data[y*w*3+c];for(let k=0;k<band;k++){const t=.5*(1-k/band)**2;data[(y*w+k)*3+c]=clamp(data[(y*w+k)*3+c]+d*t);data[(y*w+w-1-k)*3+c]=clamp(data[(y*w+w-1-k)*3+c]-d*t);}}
 for(let x=0;x<w;x++)for(let c=0;c<3;c++){const d=data[((h-1)*w+x)*3+c]-data[x*3+c];for(let k=0;k<band;k++){const t=.5*(1-k/band)**2;data[(k*w+x)*3+c]=clamp(data[(k*w+x)*3+c]+d*t);data[((h-1-k)*w+x)*3+c]=clamp(data[((h-1-k)*w+x)*3+c]-d*t);}}
 return sharp(data,{raw:{width:w,height:h,channels:3}}).png().toBuffer();
}
async function measure(file){
 const {data,info}=await sharp(file).removeAlpha().raw().toBuffer({resolveWithObject:true}),w=info.width,h=info.height;
 let mean=[0,0,0],edgeX=0,edgeY=0,detail=0;
 for(let y=0;y<h;y++)for(let x=0;x<w;x++)for(let c=0;c<3;c++){const i=(y*w+x)*3+c;mean[c]+=data[i];if(x)detail+=Math.abs(data[i]-data[i-3]);}
 for(let y=0;y<h;y++)for(let c=0;c<3;c++)edgeX+=Math.abs(data[y*w*3+c]-data[(y*w+w-1)*3+c]);
 for(let x=0;x<w;x++)for(let c=0;c<3;c++)edgeY+=Math.abs(data[x*3+c]-data[((h-1)*w+x)*3+c]);
 mean=mean.map(v=>+(v/(w*h)).toFixed(3));
 return {width:w,height:h,meanRgb:mean,encodedLuma:+(.2126*mean[0]+.7152*mean[1]+.0722*mean[2]).toFixed(3),horizontalDetail:+(detail/(h*(w-1)*3)).toFixed(3),seamX:edgeX/(h*3),seamY:edgeY/(w*3)};
}
async function main(){
 fs.mkdirSync(review,{recursive:true});
 for(const id of ids.filter(id=>!process.argv[2]||process.argv.slice(2).includes(id))){
  const input=path.join(__dirname,'Generated',id+'.png');if(!fs.existsSync(input))continue;
  const before=path.join(__dirname,'Before',id+'.png'),old=await sharp(before).metadata(),buffer=await periodic(input,old.width);
  fs.writeFileSync(path.join(base,id+'.png'),buffer);
  for(const size of [512,1024])fs.writeFileSync(path.join(base,'Export',String(size),id+'.png'),await periodic(buffer,size));
  const layers=[];
  for(const [col,file] of [before,buffer].entries())for(const [row,size] of [512,128].entries()){
   const tile=await sharp(file).resize(size,size).png().toBuffer();
   if(row===0)layers.push({input:tile,left:col*512,top:0});
   else for(let y=0;y<2;y++)for(let x=0;x<4;x++)layers.push({input:tile,left:col*512+x*128,top:512+y*128});
  }
  await sharp({create:{width:1024,height:768,channels:3,background:'#222222'}}).composite(layers).png().toFile(path.join(review,id+'-before-after.png'));
  const metrics={id,before:await measure(before),after:await measure(buffer),sha256:hash(buffer),generatedSha256:hash(fs.readFileSync(input))};
  fs.writeFileSync(path.join(__dirname,id+'.json'),JSON.stringify(metrics,null,2)+'\n');console.log(JSON.stringify(metrics));
  const manifestPath=path.join(base,'images.json'),manifestText=fs.readFileSync(manifestPath,'utf8');
  const updated=manifestText.replace(/\{[^{}]*\}/g,block=>{
   if(JSON.parse(block).file!==id+'.png')return block;
   for(const [key,value] of Object.entries({width:old.width,height:old.width,bytes:buffer.length,sha256:hash(buffer),status:'R2 scene-reference colour revision; pending integrated Unity review'}))block=block.replace(new RegExp('("'+key+'"\\s*:\\s*)("[^"\\r\\n]*"|[0-9]+)'),(_,prefix)=>prefix+JSON.stringify(value));
   return block;
  });
  fs.writeFileSync(manifestPath,updated);
 }
 const available=ids.filter(id=>fs.existsSync(path.join(review,id+'-before-after.png')));
 fs.writeFileSync(path.join(review,'review.html'),'<!doctype html><html lang="vi"><meta charset="utf-8"><title>Đối màu nền Chương 1</title><style>body{background:#20242a;color:#eee;font:18px system-ui;margin:24px}figure{max-width:1024px;margin:24px auto}img{width:100%}figcaption{padding:12px}a{color:#aaccff}</style><h1>Đối màu nền Chương 1</h1><p>Trái: trước · Phải: sau. Hàng trên: cùng kích thước 512px. Hàng dưới: lát lặp thu nhỏ 128px. Đây là ảnh nguồn, chưa phải render Unity.</p>'+available.map(id=>'<figure><figcaption>'+id+'</figcaption><img src="'+id+'-before-after.png"></figure>').join('')+'</html>');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
