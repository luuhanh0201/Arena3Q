'use strict';
// Extend the project's existing code-native Lucide/gradient/circle UI system.
const fs=require('node:fs'),path=require('node:path');
const project=path.resolve(__dirname,'../..');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
const lucideRoot=fs.existsSync(path.join(__dirname,'node_modules/lucide-react'))?path.join(__dirname,'node_modules/lucide-react'):path.join(project,'Admin/node_modules/lucide-react');
const out=path.join(__dirname,'Native');fs.mkdirSync(out,{recursive:true});
function svgFile(id,w,h,body){
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
  fs.writeFileSync(path.join(out,id+'.svg'),svg);fs.writeFileSync(path.join(out,id+'.png'),new Resvg(svg).render().asPng());
}
const defs='<defs><linearGradient id="metal" x2="0" y2="1"><stop stop-color="#FFD166"/><stop offset=".4" stop-color="#A66A28"/><stop offset="1" stop-color="#FFD166"/></linearGradient><linearGradient id="fill" x2="0" y2="1"><stop stop-color="#17243A"/><stop offset="1" stop-color="#101018"/></linearGradient></defs>';
for(const [id,w,h,fill] of [
  ['Panel_Background',512,256,true],['Resource_Bar',512,96,true],['Player_Name',640,160,true],
  ['Avatar_Frame',256,256,false],['Banner_Frame',1024,400,false],['Event_Card_Frame',512,256,false],
  ['Navigation_Horizontal',1024,144,true],['Navigation_Vertical',160,640,true],['Secondary_Normal',512,144,true]
]){
  svgFile(id,w,h,defs+`<path d="M18 3 H${w-18} L${w-3} 18 V${h-18} L${w-18} ${h-3} H18 L3 ${h-18} V18 Z" fill="${fill?'url(#fill)':'none'}" stroke="url(#metal)" stroke-width="3"/><path d="M23 10 H${w-23} L${w-10} 23 V${h-23} L${w-23} ${h-10} H23 L10 ${h-23} V23 Z" fill="none" stroke="#A66A28" stroke-width="1"/>`);
}
svgFile('Icon_Notification',128,128,'<circle cx="64" cy="64" r="43" fill="#fff"/>');
svgFile('UI_LightSweep',128,128,'<defs><linearGradient id="sheen"><stop stop-color="white" stop-opacity="0"/><stop offset=".5" stop-color="white" stop-opacity=".7"/><stop offset="1" stop-color="white" stop-opacity="0"/></linearGradient></defs><rect x="20" y="8" width="88" height="112" fill="url(#sheen)"/>');
for(const [id,file] of [['Icon_Mail','mail'],['Icon_Settings','settings']]){
  const source=fs.readFileSync(path.join(lucideRoot,'dist/esm/icons',file+'.js'),'utf8');
  const match=source.match(/const __iconNode = ([\s\S]*?);/);
  if(!match)throw Error('Unexpected installed Lucide module');
  const nodes=JSON.parse(match[1].replace(/([{,]\s*)(\w+)\s*:/g,'$1"$2":'));
  const body=nodes.map(([tag,attrs])=>'<'+tag+' '+Object.entries(attrs).filter(([k])=>k!=='key').map(([k,v])=>`${k}="${v}"`).join(' ')+'/>').join('');
  svgFile(id,128,128,`<g transform="translate(16 16) scale(4)" fill="none" stroke="#FFD166" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round">${body}</g>`);
}
fs.copyFileSync(path.join(lucideRoot,'LICENSE'),path.join(out,'Lucide_LICENSE.txt'));
const file=path.join(__dirname,'sources.json'),sources=JSON.parse(fs.readFileSync(file,'utf8'));
for(const name of fs.readdirSync(out).filter(f=>f.endsWith('.png'))){
  const id=path.basename(name,'.png');sources[id]={path:path.relative(project,path.join(out,name)).replaceAll('\\','/'),visualStatus:'native-ui'};
}
fs.writeFileSync(file,JSON.stringify(sources,null,2)+'\n');
console.log('Native UI frames and existing-library service icons exported; no labels baked.');
