'use strict';
// Only importer/platform metadata is changed. PNG bytes and Sprite identities are preserved.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const evidence=path.join(root,'NghiemThu/UI/LobbyCompression_20261027');
const roots=['Assets/Arena3Q/Lobby2D/UI','Assets/Arena3Q/Lobby2D/Backgrounds','Assets/Arena3Q/Art/UI/Startup','Assets/Arena3Q/Art/UI/Icons'];
function walk(dir){return fs.existsSync(dir)?fs.readdirSync(dir,{withFileTypes:true}).flatMap(entry=>entry.isDirectory()?walk(path.join(dir,entry.name)):[path.join(dir,entry.name)]):[];}
const files=roots.flatMap(folder=>walk(path.join(root,folder))).concat(path.join(root,'Assets/Arena3Q/Art/UI/LobbyRecruitDay.png'))
  .filter(file=>/\.(png|jpe?g)$/i.test(file)&&!/[\\/](Fonts|Video)[\\/]/i.test(file)&&fs.existsSync(file+'.meta'));
const platforms=text=>(text.match(/  platformSettings:\r?\n([\s\S]*?)(?=^  \w)/m)?.[1]||'').split(/(?=^  - serializedVersion:)/m).filter(block=>block.trim()).map(block=>{
  const read=key=>block.match(new RegExp('^    '+key+': (.*)$','m'))?.[1]?.trim();
  return {name:read('buildTarget'),maxTextureSize:Number(read('maxTextureSize')),format:Number(read('textureFormat')),
    textureCompression:Number(read('textureCompression')),compressionQuality:Number(read('compressionQuality')),overridden:read('overridden')==='1'};
});
const before=files.map(file=>{
  const meta=fs.readFileSync(file+'.meta','utf8'),relative=path.relative(root,file).replaceAll('\\','/');
  const destination=path.join(evidence,'before-meta',relative+'.meta');fs.mkdirSync(path.dirname(destination),{recursive:true});
  if(!fs.existsSync(destination))fs.writeFileSync(destination,meta);
  return {path:relative,guid:meta.match(/^guid: (.+)$/m)?.[1],pngSha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),
    mipmapEnabled:meta.match(/^    enableMipMap: (\d+)/m)?.[1]==='1',platforms:platforms(meta)};
});
fs.mkdirSync(evidence,{recursive:true});
if(!fs.existsSync(path.join(evidence,'before-platforms.json')))fs.writeFileSync(path.join(evidence,'before-platforms.json'),JSON.stringify({textures:before},null,2));
const atlasPath='Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas';
const atlas=fs.readFileSync(path.join(root,atlasPath),'utf8');
const atlasCopy=path.join(evidence,'before-meta',atlasPath);fs.mkdirSync(path.dirname(atlasCopy),{recursive:true});
if(!fs.existsSync(atlasCopy))fs.writeFileSync(atlasCopy,atlas);
function textureBlock(name,max){return `  - serializedVersion: 4\n    buildTarget: ${name}\n    maxTextureSize: ${max}\n    resizeAlgorithm: 0\n    textureFormat: 48\n    textureCompression: 1\n    compressionQuality: 100\n    crunchedCompression: 0\n    allowsAlphaSplitting: 0\n    overridden: 1\n    ignorePlatformSupport: 0\n    androidETC2FallbackOverride: 0\n    forceMaximumCompressionQuality_BC6H_BC7: 0\n`;}
for(const file of files){
  let meta=fs.readFileSync(file+'.meta','utf8').replaceAll('\r\n','\n');
  meta=meta.replace(/(    enableMipMap: )\d+/, '$10');
  const match=meta.match(/  platformSettings:\n([\s\S]*?)(?=^  \w)/m);if(!match)throw Error('Missing platformSettings: '+file);
  let body=match[1];
  for(const name of ['Android','iOS']){
    const blocks=body.split(/(?=^  - serializedVersion:)/m).filter(value=>value.trim());
    const index=blocks.findIndex(block=>new RegExp('^    buildTarget: '+name+'$','m').test(block));
    const existing=index>=0?blocks[index]:'';
    const max=Number(existing.match(/^    maxTextureSize: (\d+)/m)?.[1]||meta.match(/^  maxTextureSize: (\d+)/m)?.[1]||2048);
    if(index>=0)blocks[index]=textureBlock(name,max);else blocks.push(textureBlock(name,max));
    body=blocks.join('');
  }
  meta=meta.replace(match[0],'  platformSettings:\n'+body);fs.writeFileSync(file+'.meta',meta);
}
let changedAtlas=atlas.replaceAll('\r\n','\n');
const atlasMatch=changedAtlas.match(/    platformSettings:\n([\s\S]*?)(?=^    packingSettings:)/m);
if(!atlasMatch)throw Error('Missing atlas platform settings');
let atlasBody=atlasMatch[1];
for(const name of ['Android','iOS']){
  const blocks=atlasBody.split(/(?=^    - serializedVersion:)/m).filter(value=>value.trim());
  const index=blocks.findIndex(block=>new RegExp('^      m_BuildTarget: '+name+'$','m').test(block));
  const mobile=`    - serializedVersion: 4\n      m_BuildTarget: ${name}\n      m_MaxTextureSize: 2048\n      m_ResizeAlgorithm: 0\n      m_TextureFormat: 48\n      m_TextureCompression: 1\n      m_CompressionQuality: 100\n      m_CrunchedCompression: 0\n      m_AllowsAlphaSplitting: 0\n      m_Overridden: 1\n      m_IgnorePlatformSupport: 0\n      m_AndroidETC2FallbackOverride: 0\n      m_ForceMaximumCompressionQuality_BC6H_BC7: 0\n`;
  if(index>=0)blocks[index]=mobile;else blocks.push(mobile);atlasBody=blocks.join('');
}
changedAtlas=changedAtlas.replace(atlasMatch[0],'    platformSettings:\n'+atlasBody);fs.writeFileSync(path.join(root,atlasPath),changedAtlas);
fs.writeFileSync(path.join(evidence,'offline-change-summary.json'),JSON.stringify({count:files.length,platforms:['Android','iOS'],format:'ASTC_4x4',atlasMax:2048,
  canonicalNote:'Unity serializes iPhone API settings as iOS. Existing Lobby2D iOS ASTC4x4 was valid.'},null,2));
console.log(JSON.stringify({textures:files.length,atlas:atlasPath,before:path.relative(root,evidence).replaceAll('\\','/')}));
