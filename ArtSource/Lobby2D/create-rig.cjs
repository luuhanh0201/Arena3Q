'use strict';
const fs=require('node:fs'),path=require('node:path');
const tex=JSON.parse(fs.readFileSync(path.join(__dirname,'rig-textures.json'),'utf8'));
const parts=[],ground=[650,1140],v=(x,y)=>({x,y}),world=([x,y])=>v(x-ground[0],ground[1]-y);
const joints={Pelvis:[750,835],Neck:[742,592],Hip_R:[678,833],Knee_R:[564,952],Ankle_R:[496,1093],Foot_R:[477,1130],Hip_L:[801,844],Knee_L:[823,958],Ankle_L:[821,1095],Foot_L:[828,1110],Shoulder_R:[615,645],Elbow_R:[530,690],Wrist_R:[482,726],Grip_R:[474,746],Shoulder_L:[832,676],Elbow_L:[892,735],Wrist_L:[927,802],Grip_L:[936,825]};
function area(poly){return poly.reduce((sum,p,i)=>{const n=poly[(i+1)%poly.length];return sum+p[0]*n[1]-n[0]*p[1]},0)/2;}
function triangulate(poly){
  const order=poly.map((_,i)=>i);if(area(poly)<0)order.reverse();const out=[];
  const cross=(a,b,c)=>(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]);
  let guard=0;
  while(order.length>3&&guard++<10000){let found=false;
    for(let i=0;i<order.length;i++){
      const ia=order[(i+order.length-1)%order.length],ib=order[i],ic=order[(i+1)%order.length],a=poly[ia],b=poly[ib],c=poly[ic];
      if(cross(a,b,c)<=.001)continue;
      const inside=order.some(j=>j!==ia&&j!==ib&&j!==ic&&cross(a,b,poly[j])>=0&&cross(b,c,poly[j])>=0&&cross(c,a,poly[j])>=0);
      if(inside)continue;out.push(ia,ib,ic);order.splice(i,1);found=true;break;
    }
    if(!found)throw Error('Non-simple polygon: '+JSON.stringify(poly));
  }
  out.push(...order);return out;
}
function part(name,layer,bone,poly,textureId='CleanMaster',extra={},uvMap=null){
  const texture=tex[textureId];
  const uv=poly.map(p=>{const q=uvMap?uvMap(p):p;return v(q[0]/texture.width,1-q[1]/texture.height);});
  parts.push({name,layer,bone,textureId,vertices:poly.map(world),uv,triangles:triangulate(poly),attachment:world([750,650]),onlyClip:-1,exceptClip:-1,...extra});
}
function grid(name,layer,bone,box,textureId,extra={}){
  const texture=tex[textureId],b=texture.bounds;
  const n=16,m=20,vertices=[],uv=[],triangles=[];
  for(let j=0;j<=m;j++)for(let i=0;i<=n;i++){
    vertices.push(world([box[0]+(box[2]-box[0])*i/n,box[1]+(box[3]-box[1])*j/m]));
    uv.push(v((b[0]+(b[2]-b[0])*i/n)/texture.width,1-(b[1]+(b[3]-b[1])*j/m)/texture.height));
  }
  for(let j=0;j<m;j++)for(let i=0;i<n;i++){const a=j*(n+1)+i;triangles.push(a,a+1,a+n+2,a,a+n+2,a+n+1);}
  parts.push({name,layer,bone,textureId,vertices,uv,triangles,attachment:world([750,650]),onlyClip:-1,exceptClip:-1,...extra});
}
function cap(layer,bone,center,radius){
  const poly=[];for(let i=0;i<16;i++)poly.push([center[0]+Math.cos(i*Math.PI/8)*radius,center[1]+Math.sin(i*Math.PI/8)*radius]);
  part(layer+'_HiddenJoint',layer,bone,poly,'CleanMaster',{},()=>[840,922]);
}
part('Cape_Upper','Cape_Upper','Torso',[[650,570],[860,645],[960,940],[850,1030],[650,990],[400,850],[420,760],[560,670]],'CapeUnderlay',{flexibility:3,flexLength:500,phase:.1},p=>[650+(p[0]-400)/560*300,650+(p[1]-570)/460*300]);
part('Cape_Left','Cape_Left','Torso',[[420,770],[363,774],[300,777],[233,793],[116,827],[114,850],[144,933],[205,962],[238,988],[274,974],[289,959],[308,987],[333,1005],[366,995],[401,1026],[445,1002],[466,972],[503,957],[528,928],[562,904],[585,883],[603,833],[579,815],[545,804],[500,800],[470,786]],'CleanMaster',{flexibility:4,flexLength:450,phase:.5});
part('Cape_Right','Cape_Right','Torso',[[864,862],[937,865],[990,880],[1061,896],[1102,936],[1092,974],[1065,945],[1015,920],[971,927],[1000,979],[1055,1043],[1029,1060],[984,1044],[946,1008],[890,959],[866,918]],'CleanMaster',{flexibility:4,flexLength:400,phase:-.6});
grid('Hair_Back','Hair_Back','Head',[510,415,750,790],'HairUnderlay',{attachment:world([735,428]),flexibility:2,flexLength:360,phase:-.6});
// Complete dark fabric joint backings overlap the armor pieces; they remain rigid with the corresponding bone.
cap('Thigh_L','Thigh_L',joints.Hip_L,30);
part('Thigh_L','Thigh_L','Thigh_L',[[774,821],[821,813],[850,855],[867,909],[854,968],[818,981],[793,941],[777,888]]);
cap('Shin_L','Shin_L',joints.Knee_L,28);
part('Shin_L','Shin_L','Shin_L',[[806,935],[851,936],[877,974],[864,1047],[845,1099],[805,1101],[796,1048],[797,984]]);
part('Foot_L','Foot_L','Foot_L',[[799,1057],[840,1056],[854,1077],[884,1094],[918,1115],[907,1131],[850,1133],[800,1119],[783,1101]],'CleanMaster',{exceptClip:1});
cap('Thigh_R','Thigh_R',joints.Hip_R,35);
part('Thigh_R','Thigh_R','Thigh_R',[[650,811],[698,818],[715,852],[669,899],[623,955],[582,981],[540,958],[553,923],[592,872]]);
cap('Shin_R','Shin_R',joints.Knee_R,30);
part('Shin_R','Shin_R','Shin_R',[[554,930],[605,936],[616,970],[578,1011],[553,1058],[528,1107],[480,1117],[461,1081],[486,1038],[505,978]]);
part('Foot_R','Foot_R','Foot_R',[[476,1067],[525,1071],[544,1103],[533,1142],[486,1156],[426,1140],[427,1115],[451,1086]],'CleanMaster',{exceptClip:1});
part('Foot_R_Run','Foot_R','Foot_R',[[447,1088],[580,1088],[580,1156],[447,1156]],'RunFootR',{onlyClip:1},p=>[170+(p[0]-447)/133*990,550+(p[1]-1088)/68*510]);
part('Foot_L_Run','Foot_L','Foot_L',[[786,1066],[919,1066],[919,1134],[786,1134]],'RunFootL',{onlyClip:1},p=>[170+(p[0]-786)/133*960,550+(p[1]-1066)/68*500]);
cap('UpperArm_L','UpperArm_L',joints.Shoulder_L,25);
part('UpperArm_L','UpperArm_L','UpperArm_L',[[811,662],[849,660],[881,693],[904,731],[892,754],[861,748],[844,716]]);
cap('Forearm_L','Forearm_L',joints.Elbow_L,24);
part('Forearm_L','Forearm_L','Forearm_L',[[870,725],[900,717],[920,751],[939,790],[935,802],[914,805],[891,787],[878,754]]);
part('Torso','Torso','Torso',[[636,563],[692,554],[739,576],[786,573],[829,604],[845,651],[870,699],[861,751],[847,803],[810,842],[757,858],[704,835],[671,786],[635,747],[616,685],[621,618]]);
part('Cloth_Front','Cloth_Front','Torso',[[671,759],[717,756],[771,745],[828,774],[850,810],[872,857],[879,920],[856,965],[822,984],[810,1033],[775,1058],[734,1039],[698,1048],[676,1024],[638,1038],[608,1013],[576,1002],[565,977],[589,942],[608,903],[623,851],[645,802]],'CleanMaster',{attachment:world([750,790]),flexibility:1.4,flexLength:280,phase:.7});
cap('UpperArm_R','UpperArm_R',joints.Shoulder_R,25);
part('UpperArm_R','UpperArm_R','UpperArm_R',[[559,617],[602,627],[610,655],[578,684],[541,711],[509,697],[502,669],[522,640]]);
cap('Forearm_R','Forearm_R',joints.Elbow_R,24);
part('Forearm_R','Forearm_R','Forearm_R',[[506,679],[548,687],[547,709],[518,727],[492,728],[464,720],[454,713],[481,702]]);
part('ShoulderArmor_L','ShoulderArmor_L','Torso',[[802,596],[833,581],[856,603],[875,611],[897,644],[916,665],[906,691],[881,706],[850,692],[827,666],[811,637]]);
part('ShoulderArmor_R','ShoulderArmor_R','Torso',[[464,548],[492,519],[532,490],[561,505],[598,520],[628,548],[655,570],[664,602],[649,631],[618,649],[587,647],[551,637],[518,623],[487,603],[475,578]]);
grid('Weapon','Weapon','Weapon',[150,565,1200,1155],'WeaponClean');
part('Head','Head','Head',[[635,493],[671,481],[712,483],[746,485],[783,500],[825,502],[842,529],[832,558],[799,589],[759,598],[721,580],[688,565],[658,555],[630,540],[625,515]]);
part('Hair_Front','Hair_Front','Head',[[579,440],[611,399],[658,389],[697,385],[746,404],[791,399],[842,432],[861,467],[876,487],[866,519],[851,552],[827,560],[813,518],[775,499],[735,488],[703,510],[663,501],[641,527],[615,512],[596,483]]);
part('Helmet','Helmet','Head',[[666,342],[692,301],[721,321],[745,311],[765,351],[792,326],[816,342],[825,378],[848,379],[825,411],[800,430],[740,406],[700,395],[682,372]]);
grid('Feather_Left','Feather_Left','Head',[105,110,733,770],'FeatherLeftClean',{attachment:world([722,361]),flexibility:6,flexLength:650,phase:-.3});
grid('Feather_Right','Feather_Right','Head',[266,152,944,698],'FeatherRightClean',{attachment:world([802,374]),flexibility:5,flexLength:580,phase:-.8});
part('Eyelid_R','Head','Head',[[681,500],[701,503],[730,515],[742,532],[719,542],[690,534]],'BlinkMaster',{eyelids:true});
part('Eyelid_L','Head','Head',[[756,522],[779,517],[804,521],[809,539],[789,553],[762,546]],'BlinkMaster',{eyelids:true});
part('Hand_R','Hand_R','Hand_R',[[440,715],[466,711],[488,720],[508,735],[508,753],[492,774],[474,776],[454,765],[439,751],[432,731]]);
part('Hand_L','Hand_L','Hand_L',[[915,797],[938,795],[959,813],[969,832],[954,851],[932,854],[913,838],[905,817]]);
function affinePart(name,s0,s1,s2,t0,t1,t2){
  const p=parts.find(p=>p.name===name),texture=tex[p.textureId];
  const ax=s1[0]-s0[0],ay=s1[1]-s0[1],bx=s2[0]-s0[0],by=s2[1]-s0[1],det=ax*by-ay*bx;
  p.vertices=p.uv.map(uv=>{
    const x=uv.x*texture.width-s0[0],y=(1-uv.y)*texture.height-s0[1];
    const a=(x*by-y*bx)/det,b=(ax*y-ay*x)/det;
    return world([t0[0]+a*(t1[0]-t0[0])+b*(t2[0]-t0[0]),t0[1]+a*(t1[1]-t0[1])+b*(t2[1]-t0[1])]);
  });
}
affinePart('Feather_Left',[1220,750],[50,1210],[650,30],[722,361],[105,770],[520,110]);
affinePart('Feather_Right',[910,690],[90,1150],[900,120],[802,374],[266,698],[870,160]);
// Preserve the polearm's proportions with one similarity transform, anchored on its actual black shaft.
{
  const p=parts.find(p=>p.name==='Weapon'),texture=tex.WeaponClean,s=[406,430],t=[474,746];
  const a=Math.atan2(181,324)-Math.atan2(358,357),scale=Math.hypot(181,324)/Math.hypot(358,357),c=Math.cos(a),d=Math.sin(a);
  p.vertices=p.uv.map(uv=>{const x=uv.x*texture.width-s[0],y=(1-uv.y)*texture.height-s[1];return world([t[0]+scale*(x*c-y*d),t[1]+scale*(x*d+y*c)]);});
}
const source={parts,joints:Object.entries(joints).map(([name,p])=>({name,point:world(p)})),attackGripCenter:world([720,665]),runCrouch:-60,
  clips:[
    {name:'LuBu_Idle',duration:2,loop:true,lean:[[0,0],[1,0]],crouch:[[0,0],[1,0]],hipShift:[[0,0],[1,0]],weaponAngle:[[0,0],[1,0]],gripBlend:[[0,0],[1,0]]},
    {name:'LuBu_Run',duration:.9,loop:true,lean:[[0,0],[1,0]],crouch:[[0,0],[1,0]],hipShift:[[0,0],[1,0]],weaponAngle:[[0,0],[1,0]],gripBlend:[[0,0],[1,0]]},
    {name:'LuBu_Attack_01',duration:1.1,loop:false,
      lean:[[0,0],[.16,-3],[.32,9],[.38,8],[.50,-5],[.57,-10],[.68,-7],[.82,-3],[.94,0],[1,0]],
      crouch:[[0,0],[.18,-8],[.35,-22],[.54,-12],[.67,-6],[.94,0],[1,0]],
      hipShift:[[0,0],[.32,-14],[.54,20],[.70,10],[.94,0],[1,0]],
      weaponAngle:[[0,0],[.16,44],[.32,179],[.38,189],[.48,94],[.54,-11],[.67,-46],[.82,-31],[.94,0],[1,0]],
      gripBlend:[[0,0],[.16,1],[.80,1],[.94,0],[1,0]]}
  ]};
source.clips.forEach(clip=>['lean','crouch','hipShift','weaponAngle','gripBlend'].forEach(key=>clip[key]=clip[key].map(([x,y])=>v(x,y))));
fs.writeFileSync(path.join(__dirname,'rig.json'),JSON.stringify(source,null,2)+'\n');
fs.writeFileSync(path.join(__dirname,'rig-texture-list.json'),JSON.stringify({textures:Object.entries(tex).map(([id,data])=>({id,path:data.path}))},null,2)+'\n');
console.log(`RIG_DATA ${parts.length} pieces, ${new Set(parts.map(p=>p.layer)).size} layer groups, ${parts.reduce((n,p)=>n+p.vertices.length,0)} vertices`);
