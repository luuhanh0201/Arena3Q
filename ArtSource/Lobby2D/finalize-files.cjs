'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),assets='Assets/Arena3Q/Lobby2D';
function walk(dir){return fs.readdirSync(path.join(root,dir),{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(dir+'/'+e.name):[dir+'/'+e.name]);}
const localOnly=walk(assets).filter(f=>fs.statSync(path.join(root,f)).size>=1000000||f.endsWith('/atlas.png')).map(file=>({file,bytes:fs.statSync(path.join(root,file)).size,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')}));
fs.writeFileSync(path.join(root,assets,'.gitignore'),'# Large artwork stays local per project rule. Keep all Unity importer metadata.\n'+localOnly.map(f=>'/'+f.file.slice(assets.length+1)).join('\n')+'\n');
fs.writeFileSync(path.join(__dirname,'local-only-assets.json'),JSON.stringify({rule:'Only code and assets below approximately 1 MB in Git; no LFS.',restoreFrom:'Builds/Art/Arena3Q_Lobby2D.unitypackage',files:localOnly},null,2)+'\n');
console.log('Local-only assets: '+localOnly.length);
