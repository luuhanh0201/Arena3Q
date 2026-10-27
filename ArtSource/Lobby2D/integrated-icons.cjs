'use strict';
const fs=require('node:fs'),path=require('node:path');
const project=path.resolve(__dirname,'../..');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
const sourceRoot=path.join(project,'Admin/node_modules/lucide-react/dist/esm/icons');
const out=path.join(project,'Assets/Arena3Q/Art/UI/Icons');
for(const [name,file] of [['Plus','plus'],['ChevronRight','chevron-right'],['ChevronsRight','chevrons-right'],['Users','users']]) {
  const source=fs.readFileSync(path.join(sourceRoot,file+'.js'),'utf8');
  const match=source.match(/const __iconNode = ([\s\S]*?);/);
  if(!match)throw Error('Unknown Lucide module '+file);
  const nodes=JSON.parse(match[1].replace(/([{,]\s*)(\w+)\s*:/g,'$1"$2":'));
  const body=nodes.map(([tag,attrs])=>'<'+tag+' '+Object.entries(attrs).filter(([k])=>k!=='key').map(([k,v])=>`${k}="${v}"`).join(' ')+'/>').join('');
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${body}</svg>`;
  fs.writeFileSync(path.join(out,name+'.svg'),svg);
  fs.writeFileSync(path.join(out,name+'.png'),new Resvg(svg).render().asPng());
}
console.log('Exported four installed Lucide icons.');
