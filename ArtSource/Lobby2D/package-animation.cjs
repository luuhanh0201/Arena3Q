'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('./tooling.cjs')('sharp');
const project=path.resolve(__dirname,'../..'),assetRoot=path.join(project,'Assets/Arena3Q/Lobby2D');
const names=['Idle','Run','Attack'],durations=[2,.9,1.1],pivot=[.5,.2];
const hash=data=>crypto.createHash('sha256').update(data).digest('hex');
async function main(){
  const manifests=[];
  for(let k=0;k<3;k++){
    const name=names[k],folder=path.join(assetRoot,'Characters/LuBu/Animations',name),previews=path.join(__dirname,'WebFrames',name);
    fs.mkdirSync(previews,{recursive:true});const frames=[],previewBuffers=[],atlasParts=[];
    for(let i=0;i<24;i++){
      const file='LuBu_'+name+'_'+String(i).padStart(3,'0')+'.png',source=path.join(folder,file);
      const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
      if(info.width!==2048||info.height!==2048)throw Error('Wrong source dimensions: '+file);
      let zero=0,edge=0,solid=0;
      for(let y=0;y<2048;y++)for(let x=0;x<2048;x++){
        const a=data[(y*2048+x)*4+3];if(a===0)zero++;if(a>8)solid++;
        if(a>8&&(x===0||y===0||x===2047||y===2047))edge++;
      }
      if(!zero||edge||solid<10000)throw Error('Empty, opaque background or clipped source frame: '+file);
      const thumb=await sharp(source).resize(512,512).png().toBuffer();fs.writeFileSync(path.join(previews,file),thumb);
      previewBuffers.push(await sharp(thumb).ensureAlpha().raw().toBuffer());
      atlasParts.push({input:thumb,left:(i%6)*512,top:Math.floor(i/6)*512});
      frames.push({index:i,file,start:i*durations[k]/24,duration:durations[k]/24,pivot,width:2048,height:2048,
        sha256:hash(fs.readFileSync(source)),pixelHash:hash(data),alphaZero:zero,alphaEdge:edge});
    }
    if(new Set(frames.map(f=>f.pixelHash)).size!==24)throw Error('Duplicated decoded frame in '+name);
    const loop=k<2,events=k===2?[{name:'Impact',frame:13,time:13*durations[k]/24,source:'rig clock; VFX never deals damage'}]:[];
    const manifest={version:1,name:k===2?'LuBu_Attack_01':'LuBu_'+name,duration:durations[k],loop,frames,pivot,
      canvas:[2048,2048],frameRate:24/durations[k],inPlace:true,
      virtualRunSpeedPixelsPerSecond:k===1?190/(.42*.9):0,
      atlas:{file:'atlas.png',columns:6,rows:4,cellSize:512,usage:'review; independent source frames retain 2048px'},events};
    fs.writeFileSync(path.join(folder,'frames.json'),JSON.stringify(manifest,null,2)+'\n');
    fs.writeFileSync(path.join(folder,'events.json'),JSON.stringify(events,null,2)+'\n');
    fs.writeFileSync(path.join(folder,'timing.csv'),'index,file,start_seconds,duration_seconds,pivot_x,pivot_y,event\n'+frames.map(f=>[f.index,f.file,f.start.toFixed(8),f.duration.toFixed(8),...pivot,k===2&&f.index===13?'Impact':''].join(',')).join('\n')+'\n');
    await sharp({create:{width:3072,height:2048,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(atlasParts).png().toFile(path.join(folder,'atlas.png'));
    await sharp(Buffer.concat(previewBuffers),{raw:{width:512,height:512*24,channels:4,pageHeight:512}})
      .gif({delay:Array.from({length:24},(_,i)=>(Math.round((i+1)*durations[k]*100/24)-Math.round(i*durations[k]*100/24))*10),loop:loop?0:1,effort:7,dither:.6}).toFile(path.join(folder,'preview.gif'));
    manifests.push(manifest);console.log('PACKAGED '+name+' unique=24 alpha=RGBA no-clipping pivot=0.5,0.2');
  }
  const sourcesFile=path.join(__dirname,'sources.json'),sources=JSON.parse(fs.readFileSync(sourcesFile,'utf8'));
  const spec=JSON.parse(fs.readFileSync(path.join(__dirname,'spec.json'),'utf8'));
  for(const layer of spec.rigLayers)sources['Layer_'+layer]={path:'Assets/Arena3Q/Lobby2D/Characters/LuBu/Layers/'+layer+'.png',visualStatus:'native-rig-export'};
  const poses={Run_LeftContact:['Run',0],Run_RightContact:['Run',12],Attack_Anticipation:['Attack',7],Attack_Impact:['Attack',13],Attack_FollowThrough:['Attack',17],Attack_Recovery:['Attack',23]};
  for(const [id,[clip,index]]of Object.entries(poses))sources['Key_'+id]={path:'Assets/Arena3Q/Lobby2D/Characters/LuBu/Animations/'+clip+'/LuBu_'+clip+'_'+String(index).padStart(3,'0')+'.png',visualStatus:'native-rig-key-pose'};
  fs.writeFileSync(sourcesFile,JSON.stringify(sources,null,2)+'\n');
  fs.writeFileSync(path.join(__dirname,'animation-delivery.json'),JSON.stringify({version:1,clips:manifests},null,2)+'\n');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
