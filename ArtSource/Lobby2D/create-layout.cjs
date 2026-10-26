'use strict';
const fs=require('node:fs'),path=require('node:path');
const nodes=[];const v=(x,y)=>({x,y});
const gold={r:1,g:.82,b:.4,a:1},white={r:1,g:1,b:1,a:1};
function node(id,parent,kind,anchor,pos,size,extra={}){
  nodes.push({id,parent,kind,anchor:v(...anchor),position:v(...pos),size:v(...size),pivot:v(.5,.5),
    spriteId:'',text:'',tint:white,preserveAspect:kind===0,pixelMultiplier:1,...extra});return id;
}
function image(id,parent,spriteId,a,p,s,extra={}){return node(id,parent,0,a,p,s,{spriteId,...extra});}
function text(id,parent,value,a,p,s,fontSize,tint=white){return node(id,parent,1,a,p,s,{text:value,fontSize,tint});}
image('Sky','','Sky_Morning',[0,0],[0,0],[0,0],{cover:true,preserveAspect:false,effect:'sky'});
for(const part of ['Far_Background','Buildings','Foreground'])image('Architecture_'+part,'','Environment_'+part,[0,0],[0,0],[0,0],{cover:true,preserveAspect:false,effect:'environment'});
image('Lighting','','Environment_Lighting',[0,0],[0,0],[0,0],{cover:true,preserveAspect:false,effect:'lighting'});
node('AmbientVFX','',2,[0,0],[0,0],[0,0],{cover:true,effect:'ambientMotes',spriteId:'Ambient_Mote'});
node('SafeArea','',2,[0,0],[0,0],[0,0],{stretch:true,safeArea:true});
image('LuBu','SafeArea','LuBu_FullBody',[.4,0],[0,170],[1000,1000],{pivot:v(.519,.087),effect:'heroAnchor',referencePoint:v(.4,170/1080)});
image('Profile','SafeArea','Player_Name',[0,1],[310,-83],[600,132],{sliced:true,preserveAspect:false});
image('ProfileOrnament','Profile','Panel_Border',[.5,.5],[0,0],[600,132],{sliced:true,preserveAspect:false,pixelMultiplier:4});
image('Avatar','Profile','LuBu_Avatar',[0,.5],[66,0],[112,112]);
text('PlayerName','Profile','{playerName}',[0,.5],[235,25],[210,40],32);
text('Level','Profile','Lv.{level}',[0,.5],[195,-12],[130,30],23,gold);
text('Power','Profile','Chiến Lực: {power}',[0,.5],[315,-43],[370,32],25,gold);
image('VIP','Profile','VIP_{vip}',[1,1],[-118,-32],[225,85],{mask:true});
image('VIPSheen','VIP','UI_LightSweep',[.5,.5],[0,0],[42,85],{effect:'vipSheen',period:7,activeSeconds:1.5,amplitude:.25,travel:300,minVip:8,fadeSeconds:.2});
text('VIPLabel','VIP','VIP {vip}',[.5,.5],[0,-3],[160,40],26,gold);
['Banknotes','Silver','Stamina'].forEach((name,i)=>{
  image(name,'SafeArea','Resource_Bar',[.5,1],[-147+i*238,-47],[226,67],{sliced:true,preserveAspect:false});
  image(name+'Icon',name,'Icon_'+name,[0,.5],[30,0],[62,62]);
  text(name+'Value',name,'{'+name.toLowerCase()+'}',[.5,.5],[18,0],[155,42],24);
});
[['Friends','Hảo Hữu',-172],['Mail','Thư',-109],['Settings','Cài Đặt',-46]].forEach(([name,label,x])=>{
  image('Top'+name,'SafeArea','Icon_'+name,[1,1],[x,-45],[48,48],{action:1,destination:label});
});
image('MailNotification','TopMail','Icon_Notification',[1,1],[-2,-2],[17,17],{effect:'notification',tint:{r:.9,g:.03,b:.06,a:1}});
const left=[['Quests','Nhiệm Vụ'],['Events','Sự Kiện'],['Welfare','Phúc Lợi'],['Friends','Hảo Hữu'],['Guild','Bang Hội']];
const bottom=[['Lobby','Sảnh'],['Heroes','Tướng'],['Lineup','Đội Hình'],['Equipment','Trang Bị'],['Bag','Túi'],['More','Khác']];
image('Left','SafeArea','Navigation_Vertical',[0,.5],[79,-9],[136,630],{sliced:true,preserveAspect:false});
left.forEach(([name,label],i)=>{
  image('Left'+name,'Left','Icon_'+name,[.5,1],[0,-65-i*120],[80,80],{action:1,destination:label});
  text('Left'+name+'Label','Left'+name,label,[.5,0],[0,-18],[128,32],23);
});
image('Bottom','SafeArea','Navigation_Horizontal',[0,0],[599,83],[1168,138],{sliced:true,preserveAspect:false});
image('BottomOrnament','Bottom','Panel_Border',[.5,.5],[0,0],[1168,138],{sliced:true,preserveAspect:false,pixelMultiplier:4});
bottom.forEach(([name,label],i)=>{
  image('Bottom'+name,'Bottom','Icon_'+name,[0,.5],[105+i*191,10],[91,91],{action:1,destination:label});
  text('Bottom'+name+'Label','Bottom'+name,label,[.5,0],[0,-13],[176,33],25);
});
[['Event','Sự Kiện nổi bật'],['Recruit','Chiêu Mộ'],['Arena','Đấu Trường'],['Guild','Bang Hội']].forEach(([name,label],i)=>{
  const small=i>=2,size=small?[277,174]:[570,219],pos=small?[i===2?-452:-160,-689]:[-306,-239-i*235];
  image('Card'+name,'SafeArea','Banner_'+name,[1,1],pos,size,{action:1,destination:label,preserveAspect:false,effect:name==='Event'?'bannerCycle':name==='Recruit'?'recruitMotes':'',mask:name==='Recruit'});
  if(name==='Recruit')image('RecruitSheen','CardRecruit','UI_LightSweep',[.5,.5],[0,0],[55,219],{effect:'recruitSheen',period:8,activeSeconds:2,amplitude:.15,travel:650,fadeSeconds:.2});
  image('Card'+name+'Border','Card'+name,'Panel_Border',[.5,.5],[0,0],size,{sliced:true,preserveAspect:false,pixelMultiplier:4});
  text('Card'+name+'Label','Card'+name,label,[.5,.25],[0,0],[size[0]-32,62],small?29:38,gold);
});
image('Battle','SafeArea','Battle_Normal',[1,0],[-315,125],[590,203],{action:1,destination:'Xuất Chiến',pressedSpriteId:'Battle_Pressed',disabledSpriteId:'Battle_Disabled'});
image('BattleGlow','Battle','Battle_Highlight',[.5,.5],[0,0],[590,203],{effect:'ctaGlow',period:5,activeSeconds:5,amplitude:.28,travel:0,fadeSeconds:.2});
text('BattleLabel','Battle','Xuất Chiến',[.5,.5],[35,-32],[410,75],48,gold);
['Sáng','Trưa','Chiều','Tối','Đêm'].forEach((label,i)=>{
  image('Time'+i,'SafeArea','Secondary_Normal',[.5,1],[-240+i*120,-108],[115,38],{action:2,timeIndex:i,preserveAspect:false});
  text('TimeLabel'+i,'Time'+i,label,[.5,.5],[0,0],[108,33],21);
});
const times=[['Sáng','Morning',[1,.97,.91]],['Trưa','Noon',[1,1,1]],['Chiều','Afternoon',[1,.81,.66]],['Tối','Evening',[.66,.65,.86]],['Đêm','Night',[.43,.53,.78]]]
  .map(([label,id,c],i)=>({label,skyId:'Sky_'+id,environmentTint:{r:c[0],g:c[1],b:c[2],a:1},lightingAlpha:[0,0,.1,.5,.85][i]}));
fs.writeFileSync(path.join(__dirname,'layout.json'),JSON.stringify({nodes,times,
  bottom:bottom.map(([icon,label])=>({label,iconId:'Icon_'+icon})),left:left.map(([icon,label])=>({label,iconId:'Icon_'+icon}))},null,2)+'\n');
console.log(`LAYOUT ${nodes.length} independent nodes, all labels dynamic TMP`);
