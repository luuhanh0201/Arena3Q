'use strict';
// Reproducible placements from the authored map layout; no changes to that layout.
const fs=require('node:fs'),path=require('node:path');
const layout=JSON.parse(fs.readFileSync('Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Data/CH01_MapLayout.json'));
const atlas=JSON.parse(fs.readFileSync(path.join(__dirname,'Export/details.json')));
let seed=731;const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296};
const patches=[];
function nearest(x,z){let d=Infinity,id=0;for(const a of layout.areas){const t=(a.centre[0]-x)**2+(a.centre[1]-z)**2;if(t<d){d=t;id=a.id-1;}}return id;}
function riverDistance(x,z){let d=Infinity;const p=layout.river.points;for(let i=2;i<p.length;i+=2){const dx=p[i]-p[i-2],dz=p[i+1]-p[i-1],t=Math.max(0,Math.min(1,((x-p[i-2])*dx+(z-p[i-1])*dz)/(dx*dx+dz*dz)));d=Math.min(d,Math.hypot(x-p[i-2]-dx*t,z-p[i-1]-dz*t));}return d;}
function add(sprite,x,z,width,height,yaw=0,upright=false,opacity=1){
 if(riverDistance(x,z)<layout.river.width*.5+.7)return;
 const uv=atlas.sprites.find(s=>s.name===sprite)?.uv;if(!uv)throw Error(sprite);
 patches.push({id:sprite+'-'+patches.length,stage:nearest(x,z),sprite,position:{x,y:0,z},size:{x:width,y:height},yaw,upright,opacity,uv});
}
for(const road of [layout.road,...layout.paths]){
 const p=road.points;
 for(let i=2;i<p.length;i+=2){const dx=p[i]-p[i-2],dz=p[i+1]-p[i-1],length=Math.hypot(dx,dz),yaw=-Math.atan2(dz,dx)*180/Math.PI;
  for(let t=2;t<length;t+=6.5){const x=p[i-2]+dx*t/length,z=p[i-1]+dz*t/length;add('28_ground-marks-1-1',x,z,3.1,1.1,yaw,false,.48);}
 }
}
for(const yard of layout.yards)for(let i=0;i<6;i++){
 const x=yard.centre[0]+(random()-.5)*yard.size[0]*.7,z=yard.centre[1]+(random()-.5)*yard.size[1]*.7;
 add(i<3?'28_ground-marks-2-1':'16_damage-decals-1-'+(i%2+1),x,z,i<3?1.7:1.1,i<3?1:1.1,random()*360,false,i<3?.48:.6);
}
for(const a of layout.areas.filter(a=>a.id<7))for(let i=0;i<7;i++){
 const angle=random()*Math.PI*2,r=4+random()*5,x=a.centre[0]+Math.cos(angle)*r,z=a.centre[1]+Math.sin(angle)*r;
 const size=a.id===3?.7+random()*.35:.18+random()*.12;
 add(a.id===3?'09_bamboo-leaves-1-1':'27_fallen-leaves-1-'+(i%2+1),x,z,size,size,random()*360,false,.8);
}
for(const p of layout.paddies)for(let row=-1;row<=1;row++)for(let col=-2;col<=2;col++)
 add('12_rice-clumps-1-'+((row+col+9)%2+1),p.centre[0]+col*1.4+(random()-.5)*.25,p.centre[1]+row*1.5,1.05,.8,0,true);
const p=layout.river.points;
for(let i=2;i<p.length;i+=2){const dx=p[i]-p[i-2],dz=p[i+1]-p[i-1],length=Math.hypot(dx,dz);
 for(let t=3;t<length;t+=8)for(const side of [-1,1]){
  const offset=layout.river.width*.5+1.2,x=p[i-2]+dx*t/length-dz/length*offset*side,z=p[i-1]+dz*t/length+dx/length*offset*side;
  if(Math.abs(z)>43||Math.abs(x)>68)continue;
  add('08_riverbank-1-1',x+dz/length*.3*side,z-dx/length*.3*side,4.5,1.3,-Math.atan2(dz,dx)*180/Math.PI+(side<0?180:0),false,.7);
  add('15_reeds-1-'+(random()<.5?1:2),x,z,1.2,1.15,0,true);
  add('11_grass-clumps-1-'+(random()<.5?1:2),x-dz/length*side,z+dx/length*side,.9,.65,0,true);
 }
}
// Append only: the accepted 270 original placements retain their positions and sizes.
const supplements=[];
for(const yard of layout.yards){
 const x=yard.centre[0],z=yard.centre[1],dx=yard.size[0]*.36,dz=yard.size[1]*.36;
 for(const [sprite,ox,oz,w,h,upright] of [['fallen-cloth',dx,dz,.38,.3,false],['village-litter',-dx,dz,.48,.38,false],['wildflowers',dx,-dz,.42,.4,true],['wildflowers',-dx,-dz,.36,.34,true]]){
  const start=patches.length;add(sprite,x+ox,z+oz,w,h,upright?0:random()*360,upright);
  if(patches.length>start)supplements.push(patches[patches.length-1]);
 }
}
fs.writeFileSync(path.join(__dirname,'Export/supplement-placements.json'),JSON.stringify({patches:supplements},null,2)+'\n');
const output={layout:'CH01_MapLayout.json',seed:731,patches};fs.writeFileSync(path.join(__dirname,'Export/placements.json'),JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify({total:patches.length,byStage:layout.areas.map(a=>patches.filter(p=>p.stage===a.id-1).length),upright:patches.filter(p=>p.upright).length}));
