'use strict';
// QA comparisons only. No production artwork is modified.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('./tooling.cjs')('sharp');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
const out='NghiemThu/UI/XemTruoc_Lobby2D_20261026';
async function pair(name,left,right,size=640){
 const inputs=[];
 for(const [i,file]of [left,right].entries())inputs.push({input:await sharp(file).resize(size,size,{fit:'contain',background:'#17243a'}).flatten({background:'#17243a'}).png().toBuffer(),left:i*size,top:32});
 inputs.push({input:new Resvg(`<svg xmlns="http://www.w3.org/2000/svg" width="${size*2}" height="32"><rect width="100%" height="100%" fill="#101018"/><text x="12" y="23" fill="#ffd166" font-size="18">BEFORE</text><text x="${size+12}" y="23" fill="#ffd166" font-size="18">AFTER</text></svg>`).render().asPng(),left:0,top:0});
 await sharp({create:{width:size*2,height:size+32,channels:4,background:'#17243a'}}).composite(inputs).png().toFile(out+'/'+name+'.png');
}
async function main(){
 await pair('Rig_ExtraHand_Before_After','NghiemThu/LuBu/XemTruoc_LobbyRig_20261026/BeforeJointFix/Attack_008.png','NghiemThu/LuBu/XemTruoc_LobbyRig_20261026/LuBu_Attack_01_008.png');
 await pair('Avatar_Before_After','ArtSource/Lobby2D/Generated/LuBu_Avatar_v01.png','Assets/Arena3Q/Lobby2D/Characters/LuBu/Portraits/LuBu_Avatar.png',400);
 await pair('Silver_Before_After','ArtSource/Lobby2D/Generated/Icon_Silver_v01.png','Assets/Arena3Q/Lobby2D/UI/Icons/Silver.png',300);
 console.log('QA comparison pairs saved');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
