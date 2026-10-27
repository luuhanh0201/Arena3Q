'use strict';
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
async function main(){
  const input=process.argv[2]; if(!input)throw Error('Pass the built-in generated image path.');
  const root=path.resolve(__dirname,'../..');
  const source=path.join(__dirname,'Generated/Recruit_Day_20261027.png');
  fs.copyFileSync(input,source);
  const destination=path.join(root,'Assets/Arena3Q/Art/UI/LobbyRecruitDay.png');
  await sharp(input).resize(960,375,{fit:'fill'}).png({compressionLevel:9}).toFile(destination);
  console.log(destination+' '+fs.statSync(destination).size+' bytes');
}
main().catch(error=>{console.error(error);process.exitCode=1});
