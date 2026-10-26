'use strict';
const fs = require('node:fs');
const path = require('node:path');
const root = __dirname;
const layers = ['Head','Hair_Front','Hair_Back','Helmet','Feather_Left','Feather_Right','Torso',
  ...['UpperArm','Forearm','Hand','Thigh','Shin','Foot','ShoulderArmor'].flatMap(n => [n+'_L',n+'_R']),
  'Cape_Upper','Cape_Left','Cape_Right','Cloth_Front','Weapon'];
const icons = ['Lobby','Heroes','Lineup','Equipment','Bag','More','Quests','Events','Welfare','Friends','Guild','Banknotes','Silver','Stamina'];
const times = ['Morning','Noon','Afternoon','Evening','Night'];
const assets = [];
function add(id, file, width, height, extra = {}) {
  assets.push({id, file, width, height, transparent:true, pivot:[0.5,0.5], border:[0,0,0,0], ...extra});
}
for (const name of ['FullBody','Head','Avatar','Bust','BattlePose'])
  add('LuBu_'+name, `Characters/LuBu/Portraits/LuBu_${name}.png`, 1254,1254,
    {role:'portrait', allowEdge:name==='Avatar', pivot:name==='FullBody' ? [0.519,0.087] : [0.5,0.5]});
for (const name of layers) add('Layer_'+name, `Characters/LuBu/Layers/${name}.png`,2048,2048,{role:'rig-layer',pivot:[.5,.2]});
for (const name of icons) add('Icon_'+name,`UI/Icons/${name}.png`,256,256,{role:'icon'});
for (const name of ['Mail','Settings','Notification']) add('Icon_'+name,`UI/Icons/${name}.png`,128,128,{role:'icon',padding:12});
for (const state of ['Normal','Pressed','Disabled','Highlight'])
  add('Battle_'+state,`UI/Buttons/Battle_${state}.png`,1024,352,{role:state==='Highlight'?'effect':'button',family:'battle',padding:16});
add('Panel_Border','UI/Panels/Panel_Border.png',1024,640,{role:'frame',border:[192,160,192,160]});
add('UI_LightSweep','Effects/UI/LightSweep.png',128,128,{role:'effect',padding:8});
add('Ambient_Mote','Effects/UI/Ambient_Mote.png',128,128,{role:'effect',padding:12});
for (const [id,dir,w,h,border] of [
  ['Panel_Background','Panels',512,256,[40,40,40,40]],
  ['Resource_Bar','Resources',512,96,[48,24,48,24]],
  ['Player_Name','Panels',640,160,[80,40,80,40]],
  ['Avatar_Frame','Panels',256,256,[64,64,64,64]],
  ['Banner_Frame','Banners',1024,400,[160,120,160,120]],
  ['Event_Card_Frame','Banners',512,256,[80,64,80,64]],
  ['Navigation_Horizontal','Navigation',1024,144,[96,36,96,36]],
  ['Navigation_Vertical','Navigation',160,640,[40,96,40,96]],
  ['Secondary_Normal','Buttons',512,144,[48,24,48,24]],
]) add(id,`UI/${dir}/${id}.png`,w,h,{role:'frame',border});
for (let i=1;i<=12;i++) {
  const name='VIP_'+String(i).padStart(2,'0');
  add(name,`UI/VIP/${name}.png`,640,240,{role:'vip',tier:i,padding:12});
}
for (const name of ['Recruit','Event','Arena','Guild'])
  add('Banner_'+name,`UI/Banners/${name}.png`,1024,400,{transparent:false,allowEdge:true,role:'banner'});
for (const name of times)
  add('Sky_'+name,`Backgrounds/Lobby/Sky/${name}.png`,1920,1080,{transparent:false,allowEdge:true,role:'sky'});
for (const name of ['Far_Background','Buildings','Foreground'])
  add('Environment_'+name,`Backgrounds/Lobby/Environment/${name}.png`,1920,1080,{allowEdge:true,role:'environment'});
add('Lobby_Architecture','Backgrounds/Lobby/Environment/Lobby_Architecture.png',1920,1080,{allowEdge:true,role:'environment-baseline'});
add('Environment_Lighting','Backgrounds/Lobby/Lighting/Lighting.png',1920,1080,{allowEdge:true,role:'environment-lighting'});
const keyPoses=['Run_LeftContact','Run_RightContact','Attack_Anticipation','Attack_Impact','Attack_FollowThrough','Attack_Recovery'];
for (const name of keyPoses) add('Key_'+name,`Characters/LuBu/KeyPoses/${name}.png`,2048,2048,{role:'key-pose',pivot:[0.5,0.2]});
const clips = [['Idle',2,true],['Run',0.9,true],['Attack',1.1,false]].map(([name,duration,loop])=>({
  name:name==='Attack'?'LuBu_Attack_01':'LuBu_'+name, folder:`Characters/LuBu/Animations/${name}`,
  prefix:'LuBu_'+name, count:24, width:2048,height:2048,pivot:[.5,.2],duration,loop,
  endpointPolicy:'sample t=i/24; exclude copied endpoint; Attack recovery finishes before the final sample',
  events:name==='Attack'?[{name:'Impact',frame:13,status:'proposed, not gameplay bound'}]:[],
  requiredFiles:['timing.csv','frames.json','atlas.png','preview.gif'],
  visualReview:{identity:false,surfaceIntegrity:false,weightAndInertia:false,footContacts:false,loopOrRecovery:false},
}));
const spec={version:1,assetRoot:'Assets/Arena3Q/Lobby2D',status:'in-production',
  referenceRoot:'docs/UI/assets',canvas:[1920,1080],palette:['#C91E1E','#7A1319','#FFD166','#A66A28','#101018','#17243A'],
  references:['Sảnh Game.png','animation1.png','farmes.png','12ba053f-ca19-4883-bae9-eb55f1552d38.png','5adabb22-538c-4d4e-bc33-732cc8098a41.png','a67b4d61-95f4-4751-80a5-f0867cb6ffe3.png'],
  menus:{bottom:['Sảnh','Tướng','Đội Hình','Trang Bị','Túi','Khác'],left:['Nhiệm Vụ','Sự Kiện','Phúc Lợi','Hảo Hữu','Bang Hội'],right:['Chiêu Mộ','Sự Kiện nổi bật','Đấu Trường','Bang Hội'],cta:'Xuất Chiến'},
  assets,clips,rigLayers:layers,qualityGates:{characterIdentity:false,registeredEnvironmentLayers:false,buttonStatesRegistered:false,rigHiddenSurfaces:false,unityImported:false,previewReviewed:false,device60fps:false}};
fs.writeFileSync(path.join(root,'spec.json'),JSON.stringify(spec,null,2)+'\n');
console.log(`SPEC ${assets.length} assets, ${clips.length} clips; status=in-production`);
