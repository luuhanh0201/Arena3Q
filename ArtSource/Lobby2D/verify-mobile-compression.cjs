'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),evidence=path.join(root,'NghiemThu/UI/LobbyCompression_20261027');
const before=JSON.parse(fs.readFileSync(path.join(evidence,'before-platforms.json'),'utf8'));
const after=JSON.parse(fs.readFileSync(path.join(evidence,'after-import-settings.json'),'utf8'));
const generatedPortrait='Assets/Arena3Q/Art/UI/Icons/LordPortrait_lord_strategist.png';
const checks=before.textures.map(entry=>{
  const relative=entry.path.replace('Lobby2D/UI/Resources/','Lobby2D/UI/ResourceBar/'),file=path.join(root,relative),meta=fs.readFileSync(file+'.meta','utf8');
  const blocks=(meta.match(/  platformSettings:\r?\n([\s\S]*?)(?=^  \w)/m)?.[1]||'').split(/(?=^  - serializedVersion:)/m);
  const platforms=['Android','iOS'].map(name=>{
    const block=blocks.find(block=>new RegExp('^    buildTarget: '+name+'\r?$','m').test(block))||'';
    return {name,astc4x4:/^    textureFormat: 48\r?$/m.test(block),overridden:/^    overridden: 1\r?$/m.test(block)};
  });
  const hash=crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
  return {path:relative,guidUnchanged:meta.match(/^guid: (.+)\r?$/m)?.[1].trim()===entry.guid,
    sourceUnchanged:hash===entry.pngSha256,
    regeneratedPortrait:relative===generatedPortrait&&after.textures.some(row=>row.path===relative&&row.pngSha256===hash&&row.width===512&&row.height===512),
    mipmapsOff:/^    enableMipMap: 0\r?$/m.test(meta),platforms};
});
const atlas=fs.readFileSync(path.join(root,'Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas'),'utf8');
const atlasSettings=['Android','iOS'].map(name=>{
  const blocks=(atlas.match(/    platformSettings:\r?\n([\s\S]*?)(?=^    packingSettings:)/m)?.[1]||'').split(/(?=^    - serializedVersion:)/m);
  const block=blocks.find(block=>new RegExp('^      m_BuildTarget: '+name+'\r?$','m').test(block))||'';
  return {name,astc4x4:/^      m_TextureFormat: 48\r?$/m.test(block),overridden:/^      m_Overridden: 1\r?$/m.test(block),max2048:/^      m_MaxTextureSize: 2048\r?$/m.test(block)};
});
const passes=row=>row.guidUnchanged&&(row.sourceUnchanged||row.regeneratedPortrait)&&row.mipmapsOff&&row.platforms.every(platform=>platform.astc4x4&&platform.overridden);
const passed=checks.every(passes)&&atlasSettings.every(row=>row.astc4x4&&row.overridden&&row.max2048);
const result={passed,count:checks.length,atlasSettings,checks};fs.writeFileSync(path.join(evidence,'offline-after-verification.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify({passed,count:checks.length,unchangedSources:checks.filter(row=>row.sourceUnchanged).length,regeneratedPortraits:checks.filter(row=>row.regeneratedPortrait).map(row=>row.path),atlasSettings,failures:checks.filter(row=>!passes(row))}));
if(!passed)process.exitCode=1;
