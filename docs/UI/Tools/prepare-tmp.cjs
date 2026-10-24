// Extract the TMP shaders/settings distributed with uGUI, preserving GUIDs and omitting unused demo fonts/sprites.
const fs = require('node:fs'),path = require('node:path'),zlib = require('node:zlib');
const root=path.resolve(__dirname,'../../..');
const pkg=fs.readdirSync(path.join(root,'Library/PackageCache')).find(x=>x.startsWith('com.unity.ugui@'));
const data=zlib.gunzipSync(fs.readFileSync(path.join(root,'Library/PackageCache',pkg,'Package Resources/TMP Essential Resources.unitypackage')));
const entries=new Map();
for(let offset=0;offset+512<=data.length;){
 const header=data.subarray(offset,offset+512);if(header.every(x=>x===0))break;
 const name=header.subarray(0,100).toString().replace(/\0.*$/s,'');const size=parseInt(header.subarray(124,136).toString().replace(/\0.*$/s,'').trim()||'0',8);
 const parts=name.split('/');if(parts.length===2){const entry=entries.get(parts[0])||{};entry[parts[1]]=data.subarray(offset+512,offset+512+size);entries.set(parts[0],entry);}
 offset+=512+Math.ceil(size/512)*512;
}
let count=0;
const shaders=new Set(['TMP_SDF.shader','TMP_SDF-Mobile.shader','TMPro.cginc','TMPro_Properties.cginc']);
for(const entry of entries.values()){
 if(!entry.pathname||!entry.asset)continue;const original=entry.pathname.toString().trim();let destination;
 if(original.endsWith('/TMP Settings.asset'))destination='Assets/Arena3Q/Content/Resources/TMP Settings.asset';
 else if(original.includes('/Shaders/')&&shaders.has(path.basename(original))||original.includes('/Resources/Style Sheets/')||original.includes('/Resources/LineBreaking'))destination='Assets/Arena3Q/Art/UI/Startup/TMP/'+original.replace('Assets/TextMesh Pro/','');
 else continue;
 const target=path.join(root,destination);fs.mkdirSync(path.dirname(target),{recursive:true});
 if(!fs.existsSync(target)){fs.writeFileSync(target,entry.asset);if(entry['asset.meta'])fs.writeFileSync(target+'.meta',entry['asset.meta']);count++;}
}
console.log('TMP resources extracted:',count);
