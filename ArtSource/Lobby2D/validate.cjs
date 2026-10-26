'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('./tooling.cjs')('sharp');
const spec=JSON.parse(fs.readFileSync(path.join(__dirname,'spec.json'),'utf8'));
const root=path.resolve(__dirname,'../..',spec.assetRoot),issues=[];
async function inspect(file){
  const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let transparent=0,edge=0;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
    const a=data[(y*info.width+x)*4+3];if(a===0)transparent++;
    if(a>8&&(x===0||y===0||x===info.width-1||y===info.height-1))edge++;
  }
  return {width:info.width,height:info.height,transparent,edge,pixelHash:crypto.createHash('sha256').update(data).digest('hex')};
}
async function main(){
  const clips=[];
  const delivery=JSON.parse(fs.readFileSync(path.join(__dirname,'delivery.json'),'utf8'));
  for(const item of delivery.assets){
    const file=path.resolve(__dirname,'../..',item.path);
    if(!fs.existsSync(file)||crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')!==item.sha256)issues.push('Base image missing or changed since export: '+item.id);
  }
  for(const report of ['NghiemThu/LuBu/XemTruoc_LobbyRig_20261026/surface-audit.json','NghiemThu/UI/XemTruoc_Lobby2D_20261026/Web/report.json','ArtSource/Lobby2D/background-registration.json','ArtSource/Lobby2D/package-report.json']){
    if(!JSON.parse(fs.readFileSync(path.resolve(__dirname,'../..',report),'utf8')).passed)issues.push('Evidence failed: '+report);
  }
  for(const clip of spec.clips){
    const frames=[],missing=[];
    for(let i=0;i<clip.count;i++){
      const file=path.join(root,clip.folder,clip.prefix+'_'+String(i).padStart(3,'0')+'.png');
      if(!fs.existsSync(file)){missing.push(i);continue;}
      const metric=await inspect(file);frames.push({index:i,...metric});
      if(metric.width!==clip.width||metric.height!==clip.height||!metric.transparent||metric.edge)
        issues.push(clip.name+': invalid dimensions/alpha/clipping at '+i);
    }
    const unique=new Set(frames.map(f=>f.pixelHash)).size;
    if(missing.length)issues.push(clip.name+': missing '+missing.length+' real frames');
    if(unique!==frames.length)issues.push(clip.name+': duplicate decoded frames');
    const extraFiles=clip.requiredFiles.filter(file=>!fs.existsSync(path.join(root,clip.folder,file)));
    if(extraFiles.length)issues.push(clip.name+': missing timing/pivot/atlas/preview: '+extraFiles.join(','));
    const timing=path.join(root,clip.folder,'frames.json');
    if(fs.existsSync(timing)){
      const data=JSON.parse(fs.readFileSync(timing,'utf8'));
      if(data.frames?.length!==clip.count||data.loop!==clip.loop||Math.abs(data.duration-clip.duration)>.0001)
        issues.push(clip.name+': clip timing/order contract mismatch');
      else data.frames.forEach((f,i)=>{
        if(f.index!==i||!Array.isArray(f.pivot)||f.pivot.some((v,j)=>v!==clip.pivot[j]))issues.push(clip.name+': nonuniform pivot/order at '+i);
      });
    }
    if(Object.values(clip.visualReview).some(v=>v!==true))issues.push(clip.name+': visual animation gates pending');
    clips.push({name:clip.name,actualFrames:frames.length,uniqueFrames:unique,missing,extraFiles,frames});
  }
  const pendingGates=Object.entries(spec.qualityGates).filter(([key,v])=>v!==true&&!spec.deferredGates?.[key]).map(([key])=>key);
  const review=JSON.parse(fs.readFileSync(path.join(__dirname,'visual-review.json'),'utf8'));
  for(const item of review.files)if(crypto.createHash('sha256').update(fs.readFileSync(path.resolve(__dirname,'../..',item.file))).digest('hex')!==item.sha256)issues.push('Visual review stale: '+item.file);
  const art=JSON.parse(fs.readFileSync(path.join(__dirname,'technical-report.json'),'utf8'));
  const report={complete:issues.length===0&&pendingGates.length===0&&art.missing.length===0&&art.technicalFailures.length===0,
    clips,artMissing:art.missing,artTechnicalFailures:art.technicalFailures,pendingGates,issues,
    scope:spec.validationScope,deferredGates:spec.deferredGates,fullPlatformAcceptance:false};
  report.webScopePassed=report.complete;
  fs.writeFileSync(path.join(__dirname,'acceptance-report.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify({complete:report.complete,artMissing:art.missing.length,artTechnicalFailures:art.technicalFailures,clips:clips.map(c=>({name:c.name,actual:c.actualFrames,unique:c.uniqueFrames})),pendingGates,issues},null,2));
  if(!report.complete)process.exitCode=2;
}
main().catch(e=>{console.error(e);process.exitCode=1;});
