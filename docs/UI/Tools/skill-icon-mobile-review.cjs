const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {createRequire}=require('node:module');const root=path.resolve(__dirname,'../../..');process.chdir(root);const sharp=createRequire(path.join(root,'Admin/package.json'))('sharp');
const base='NghiemThu/Icons/Mobile_20261025';
const registry=JSON.parse(fs.readFileSync('Admin/reviews/skill-icons-20261025.json'));
async function metrics(a,b,size){
 const load=async p=>{let image=sharp(p);if(size)image=image.resize(size,size,{fit:'fill'});return image.ensureAlpha().raw().toBuffer({resolveWithObject:true});};
 const left=await load(a),right=await load(b);assert.deepEqual(left.info,right.info);
 let sq=0,mae=0,max=0,changed=0;
 for(let i=0;i<left.data.length;i+=4){let different=false;for(let c=0;c<3;c++){const d=Math.abs(left.data[i+c]-right.data[i+c]);sq+=d*d;mae+=d;max=Math.max(max,d);different||=d!==0;}if(different)changed++;}
 const samples=left.info.width*left.info.height*3,mse=sq/samples;
 return {width:left.info.width,height:left.info.height,psnr:mse?10*Math.log10(255*255/mse):null,meanAbsoluteError:mae/samples,maxChannelError:max,changedPixels:changed};
}
(async()=>{
 const result=[];for(const item of registry.skills){const a=base+'/before/'+item.id+'.png',b=base+'/after/'+item.id+'.png';result.push({id:item.id,name:item.name,full:await metrics(a,b),hud128:await metrics(a,b,128)});}
 const changed=result.filter(r=>r.full.changedPixels>0).length;
 fs.writeFileSync(base+'/image-metrics.json',JSON.stringify({images:result.length,changedImages:changed,method:'Unity RenderTexture readback, before versus platform-imported after; null PSNR means identical pixels.',results:result},null,2));
 fs.writeFileSync(base+'/review.json',JSON.stringify({skills:result.map(r=>({id:r.id,name:r.name,psnr:r.full.psnr,hudPsnr:r.hud128.psnr}))},null,2));
 const rows=[];for(const item of result){const a=await sharp(base+'/before/'+item.id+'.png').resize(96,96).png().toBuffer(),b=await sharp(base+'/after/'+item.id+'.png').resize(96,96).png().toBuffer();const cell=await sharp({create:{width:204,height:122,channels:3,background:'#101820'}}).composite([{input:a,left:4,top:3},{input:b,left:104,top:3},{input:Buffer.from(`<svg width="204" height="20"><text x="4" y="14" fill="white" font-family="Arial" font-size="10">${item.id}</text></svg>`),left:0,top:102}]).png().toBuffer();rows.push(cell);}
 const composites=rows.map((input,i)=>({input,left:(i%6)*204,top:Math.floor(i/6)*122}));
 await sharp({create:{width:1224,height:Math.ceil(rows.length/6)*122,channels:3,background:'#101820'}}).composite(composites).png().toFile(base+'/contact-before-after.png');
 for(const id of ['lubu_phuong_thien_tram','phong_vu_khon_tran','thien_muu_ho_tran']){
  const a=base+'/before/'+id+'.png',b=base+'/after/'+id+'.png',meta=await sharp(a).metadata();
  await sharp({create:{width:meta.width*2,height:meta.height,channels:4,background:'#101820'}}).composite([{input:a,left:0,top:0},{input:b,left:meta.width,top:0}]).png().toFile(base+'/comparison-'+id+'.png');
 }
 console.log(JSON.stringify({images:result.length,changedImages:changed,worst:result.filter(r=>r.full.psnr!==null).sort((a,b)=>a.full.psnr-b.full.psnr).slice(0,3)}));
})().catch(e=>{console.error(e);process.exitCode=1});
