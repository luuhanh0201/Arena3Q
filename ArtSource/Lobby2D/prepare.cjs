'use strict';
// File-format export only: resample/letterbox; never synthesize animation or repaint artwork.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
let sharp;
try { sharp=require('sharp'); } catch {
  sharp=require('./tooling.cjs')('sharp');
}
const project=path.resolve(__dirname,'../..');
const spec=JSON.parse(fs.readFileSync(path.join(__dirname,'spec.json'),'utf8'));
const sources=JSON.parse(fs.readFileSync(path.join(__dirname,'sources.json'),'utf8'));
const sha=data=>crypto.createHash('sha256').update(data).digest('hex');
async function main(){
  const delivered=[];
  for(const asset of spec.assets){
    const source=sources[asset.id]; if(!source)continue;
    const from=path.resolve(project,source.path);
    const original=fs.readFileSync(from);
    const archive=path.join(__dirname,'Generated',asset.id+'_source'+path.extname(from));
    fs.mkdirSync(path.dirname(archive),{recursive:true});
    if(path.resolve(from)!==path.resolve(archive))fs.writeFileSync(archive,original);
    const dest=path.resolve(project,spec.assetRoot,asset.file);
    if(!dest.startsWith(path.resolve(project,spec.assetRoot)+path.sep))throw Error('Path escapes asset root');
    const pad=asset.padding||0;
    const exported=await sharp(original).ensureAlpha().resize(asset.width-pad*2,asset.height-pad*2,
      {fit:'contain',background:{r:0,g:0,b:0,alpha:0},kernel:'lanczos3'})
      .extend({top:pad,bottom:pad,left:pad,right:pad,background:{r:0,g:0,b:0,alpha:0}})
      .png({compressionLevel:9}).toBuffer();
    fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,exported);
    const {data,info}=await sharp(exported).raw().toBuffer({resolveWithObject:true});
    let zero=0,opaque=0,edge=0,minX=info.width,minY=info.height,maxX=-1,maxY=-1;
    for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
      const a=data[(y*info.width+x)*4+3];if(a===0)zero++;if(a===255)opaque++;
      if(a>8){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);
        if(x===0||y===0||x===info.width-1||y===info.height-1)edge++;}
    }
    delivered.push({...asset,path:spec.assetRoot+'/'+asset.file,source:path.relative(project,archive).replaceAll('\\','/'),
      sourceHash:sha(original),sha256:sha(exported),pixelHash:sha(data),bytes:exported.length,
      alphaZero:zero,alphaOpaque:opaque,alphaEdge:edge,bounds:[minX,minY,maxX,maxY],
      visualStatus:source.visualStatus||'draft',
      technicalPassed:maxX>=0&&(!asset.transparent||zero>0)&&(asset.allowEdge||edge===0)});
  }
  const manifest={version:1,generatedAt:new Date().toISOString(),status:'in-production',assets:delivered};
  fs.writeFileSync(path.join(__dirname,'delivery.json'),JSON.stringify(manifest,null,2)+'\n');
  // Persist portable workspace sources after copying outputs out of Codex's private generation folder.
  for(const asset of delivered)sources[asset.id]={path:asset.source,visualStatus:asset.visualStatus};
  fs.writeFileSync(path.join(__dirname,'sources.json'),JSON.stringify(sources,null,2)+'\n');
  const missing=spec.assets.filter(a=>!delivered.some(d=>d.id===a.id)).map(a=>a.id);
  const report={delivered:delivered.length,expected:spec.assets.length,missing,
    technicalFailures:delivered.filter(a=>!a.technicalPassed).map(a=>a.id),
    complete:missing.length===0&&delivered.every(a=>a.technicalPassed),scope:'base-image technical checks only; see acceptance-report.json for animation, web review and deferred device gate'};
  fs.writeFileSync(path.join(__dirname,'technical-report.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report,null,2));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
