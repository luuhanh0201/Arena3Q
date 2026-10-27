'use strict';
const fs=require('node:fs'),path=require('node:path');
const {Resvg}=require('./tooling.cjs')('@resvg/resvg-js');
const output=path.resolve(__dirname,'../../Assets/Arena3Q/Art/UI/Icons');
for(const name of ['HeroEnvelope','HeroCog']){
  const source=fs.readFileSync(path.join(output,name+'.svg'),'utf8').replaceAll('currentColor','white');
  fs.writeFileSync(path.join(output,name+'.png'),new Resvg(source,{fitTo:{mode:'width',value:128}}).render().asPng());
}
console.log('Heroicons Solid source SVGs rasterized at 128 px.');
