'use strict';
const fs=require('node:fs'),zlib=require('node:zlib'),crypto=require('node:crypto'),path=require('node:path');
async function main(){
 const file='Builds/Art/Arena3Q_Lobby2D.unitypackage',stream=fs.createReadStream(file).pipe(zlib.createGunzip());
 let buffer=Buffer.alloc(0),remaining=0,pad=0,current='',parts=[],paths=[];
 for await(const chunk of stream){buffer=Buffer.concat([buffer,chunk]);while(true){
  if(remaining){const n=Math.min(remaining,buffer.length);if(current.endsWith('/pathname'))parts.push(buffer.subarray(0,n));remaining-=n;buffer=buffer.subarray(n);if(remaining)break;if(parts.length){paths.push(Buffer.concat(parts).toString('utf8').replace(/\0+$/,''));parts=[];}}
  if(pad){if(buffer.length<pad)break;buffer=buffer.subarray(pad);pad=0;}
  if(buffer.length<512)break;
  const header=buffer.subarray(0,512);buffer=buffer.subarray(512);if(header.every(v=>v===0))continue;
  current=header.subarray(0,100).toString().replace(/\0.*$/,'');remaining=parseInt(header.subarray(124,136).toString().replace(/\0.*$/,'').trim(),8)||0;pad=(512-remaining%512)%512;
 }}
 for(const name of ['README.txt','UI/Lucide_LICENSE.txt','UI/Fonts/OFL.txt','Unity/LobbyArtReview.unity','Unity/Prefabs/LuBu_Skeletal_UI.prefab'])if(!paths.includes('Assets/Arena3Q/Lobby2D/'+name))throw Error('Package missing '+name);
 const frames=paths.filter(p=>/\/Animations\/(Idle|Run|Attack)\/LuBu_\w+_\d{3}\.png$/.test(p));if(frames.length!==72)throw Error('Package frame count '+frames.length);
 const report={file,bytes:fs.statSync(file).size,sha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),assetPaths:paths.length,frameCount:frames.length,passed:true,paths};
 fs.writeFileSync(path.join(__dirname,'package-report.json'),JSON.stringify(report,null,2)+'\n');console.log('PACKAGE_VERIFIED '+paths.length+' paths;72 source frames;licenses and prefabs included');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
