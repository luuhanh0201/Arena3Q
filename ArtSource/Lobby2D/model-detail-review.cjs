'use strict';
// Registration-free source captures use the same camera, pose and crop before and after AA/reflection.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const evidence=path.resolve(__dirname,'../../NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027');
const out=path.join(evidence,'model-details_'+new Date().toISOString().replace(/[:.]/g,'-'));
const areas={Crown:{left:355,top:0,width:380,height:280},Fan:{left:0,top:235,width:335,height:490},
  Face:{left:340,top:315,width:350,height:250},Robe:{left:210,top:530,width:680,height:490},
  Hem:{left:340,top:850,width:630,height:174}};
const toLinear=x=>x<=.04045?x/12.92:Math.pow((x+.055)/1.055,2.4);
const toSrgb=x=>x<=.0031308?x*12.92:1.055*Math.pow(x,1/2.4)-.055;
async function read(file){
  const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const straight=Buffer.from(data);
  for(let i=0;i<data.length;i+=4){
    const alpha=data[i+3]/255;
    if(alpha<=0){straight[i]=straight[i+1]=straight[i+2]=0;continue;}
    if(alpha>=1)continue;
    for(let c=0;c<3;c++)straight[i+c]=Math.round(Math.min(1,toSrgb(Math.min(1,toLinear(data[i+c]/255)/alpha)))*255);
  }
  return {data,straight,info};
}
function stats(image){
  let opaque=0,partial=0,clear=0,cyanOpaqueLower=0,goldCrown=0,crownOpaque=0;
  const {data,info}=image;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
    const i=(y*info.width+x)*4,alpha=data[i+3],r=data[i],g=data[i+1],b=data[i+2];
    if(alpha===0)clear++;else if(alpha===255)opaque++;else partial++;
    if(alpha>250&&y>850&&x>340&&x<970&&g>100&&b>120&&g>r+12&&b>r+18)cyanOpaqueLower++;
    if(alpha>250&&x>=355&&x<735&&y<280){crownOpaque++;if(r>g*1.12&&g>b*1.15)goldCrown++;}
  }
  return {width:info.width,height:info.height,opaque,partialAlpha:partial,clear,cyanOpaqueLower,
    crownWarmGoldPixels:goldCrown,crownOpaquePixels:crownOpaque,crownWarmGoldFraction:crownOpaque?goldCrown/crownOpaque:0};
}
const title=(text,w)=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="40"><rect width="100%" height="100%" fill="#101927"/><text x="12" y="27" fill="#ffd877" font-family="Arial" font-size="18">${text}</text></svg>`);
async function main(){
  fs.mkdirSync(out,{recursive:true});
  const beforeSource='surface-probe_20261027_180854/Current.png';
  const before=await read(path.join(evidence,beforeSource));
  const after=await read(path.join(evidence,'LordSurface.png'));
  if(before.info.width!==after.info.width||before.info.height!==after.info.height)throw Error('Source camera sizes differ.');
  const files=[];
  for(const [name,rect] of Object.entries(areas)){
    const crops=await Promise.all([before,after].map(image=>sharp(image.straight,{raw:image.info}).extract(rect).flatten({background:'#243040'}).resize(rect.width*2,rect.height*2,{kernel:'nearest'}).png().toBuffer()));
    const w=rect.width*2,h=rect.height*2;
    const file=name+'_before-after.png';
    await sharp({create:{width:w*2,height:h+40,channels:3,background:'#243040'}}).composite([
      {input:title('Before / fallback reflection',w),left:0,top:0},{input:title('After / explicit warm capture cube',w),left:w,top:0},
      {input:crops[0],left:0,top:40},{input:crops[1],left:w,top:40}]).png().toFile(path.join(out,file));files.push(file);
  }
  const albedo=await read(path.resolve(__dirname,'../../Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/LOD/Portrait_BaseColor.jpg'));
  let sourceAlbedoCyanPixels=0;
  for(let i=0;i<albedo.data.length;i+=4){const r=albedo.data[i],g=albedo.data[i+1],b=albedo.data[i+2];
    if(g>100&&b>120&&g>r+12&&b>r+18)sourceAlbedoCyanPixels++;}
  const report={before:stats(before),after:stats(after),sourceAlbedoCyanPixels,
    sources:{before:beforeSource,after:'LordSurface.png'},
    notes:['Same source camera and Idle quarter-cycle; no registration transform.',
      'Saved render RGB is premultiplied. For PNG crop display only, unpremultiply in linear space before flattening onto the shared matte.',
      'Cyan count only scans near-opaque pixels in the lower robe ROI. It is a narrow regression signal, not whole-model artistic acceptance.',
      'Warm-gold pixel fraction is descriptive; it does not replace comparison with the actual battle model.']};
  fs.writeFileSync(path.join(out,'quality-metrics.json'),JSON.stringify(report,null,2));
  fs.writeFileSync(path.join(out,'review.html'),`<!doctype html><html lang="vi"><meta charset="utf-8"><title>Model sảnh - chi tiết AA và vàng</title><style>body{background:#101927;color:#e6ecf5;font:17px system-ui}main{max-width:1700px;margin:auto;padding:20px}img{max-width:100%;height:auto}a,h1{color:#ffd877}figure{margin:22px 0}</style><main><h1>Model sảnh: mép, vàng và da</h1><p>Ảnh nguồn cùng camera, cùng pha Idle. Phóng200% bằng nearest để nhìn rõ mép. Khi xuất crop so sánh, chỉ giải premultiply trong linear để trình xem PNG hiển thị đúng như UI premultiplied; không sửa màu vùng đục hoặc nguồn model.</p><p><a href="quality-metrics.json">Số đo alpha, điểm cyan và tỷ lệ pixel vàng</a></p>${files.map(file=>`<figure><img src="${file}"><figcaption>${file}</figcaption></figure>`).join('')}</main></html>`);
  console.log(JSON.stringify({out,report},null,2));
}
main().catch(error=>{console.error(error);process.exitCode=1});
