// Uses only the supplied artwork and SVG shapes from the handoff, plus licensed Lucide icons.
// npm.cmd install --prefix AgentComms/codex/staging/ui-tools --no-audit --no-fund @resvg/resvg-js sharp playwright
// node docs/UI/Tools/prepare-assets.cjs
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../..');
const sharp = require(path.join(root, 'AgentComms/codex/staging/ui-tools/node_modules/sharp'));
const {Resvg} = require(path.join(root, 'AgentComms/codex/staging/ui-tools/node_modules/@resvg/resvg-js'));
const source = path.join(root, 'docs/UI/Arena3Q_UI_Handoff');
const dest = path.join(root, 'Assets/Arena3Q/Art/UI/Startup');
const generated = path.join(dest, 'Generated');
fs.mkdirSync(generated, {recursive: true});
function svgPng(name, svg, scale = 2) {
  if (!svg.includes('xmlns=')) svg = svg.replace('<svg', '<svg xmlns="http://www.w3.org/2000/svg"');
  fs.writeFileSync(path.join(generated, name + '.svg'), svg);
  fs.writeFileSync(path.join(generated, name + '.png'), new Resvg(svg, {fitTo:{mode:'zoom',value:scale}}).render().asPng());
}
const load = fs.readFileSync(path.join(source, 'reference/mockup/Loading.dc.html'), 'utf8');
const svgs = [...load.matchAll(/<svg\b[\s\S]*?<\/svg>/g)].map(m=>m[0]);
const cart = svgs.find(x=>x.includes('width="132"'));
if (!cart) throw new Error('Handoff cart SVG missing');
const flagPath = cart.match(/<path class="a3q-flag"[^>]*>.*?<\/path>/)[0];
const wheelGroup = cart.match(/<g class="a3q-wheel">[\s\S]*?<\/g>/)[0];
let body = cart.replace(/<g class="a3q-wheel">[\s\S]*?<\/g>/g, '').replace(flagPath, '');
svgPng('cart', body);
svgPng('wheel', `<svg width="24" height="24" viewBox="24 58 24 24">${wheelGroup}</svg>`);
svgPng('flag', `<svg width="26" height="20" viewBox="22 2 26 20">${flagPath}</svg>`);
svgPng('spear', svgs.find(x=>x.includes('width="72"')));
svgPng('rounded', '<svg width="32" height="32"><rect x="0" y="0" width="32" height="32" rx="14" fill="white"/></svg>', 1);
svgPng('gradient', '<svg width="4" height="330"><defs><linearGradient id="g" x2="0" y2="1"><stop stop-color="#080402" stop-opacity="0"/><stop offset=".42" stop-color="#080402" stop-opacity=".85"/><stop offset="1" stop-color="#080402" stop-opacity=".96"/></linearGradient></defs><rect width="4" height="330" fill="url(#g)"/></svg>',1);
svgPng('vignette', '<svg width="1280" height="720"><defs><radialGradient id="g" cx="50%" cy="57%" r="60%"><stop stop-color="#060403" stop-opacity=".7"/><stop offset=".72" stop-color="#060403" stop-opacity=".3"/><stop offset="1" stop-color="#060403" stop-opacity=".5"/></radialGradient></defs><rect width="1280" height="720" fill="url(#g)"/></svg>',1);
async function main() {
  // Match CSS overlay opacity in Unity's linear colour space.
  for (const name of ['gradient','vignette']) {
    const file=path.join(generated,name+'.png');
    const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    for(let i=3;i<data.length;i+=4)data[i]=Math.round(255*(1-Math.pow(1-data[i]/255,2.2)));
    await sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(file);
  }
  await sharp(path.join(source,'Assets/UI/Logo/logo_arena3q.png')).resize({width:1024}).png({compressionLevel:9}).toFile(path.join(dest,'UI/Logo/logo_arena3q.png'));
  await sharp(path.join(source,'Assets/UI/Backgrounds/keyart_3heroes.png')).resize(1280,720).png({palette:true,colours:256,dither:.4,compressionLevel:9}).toFile(path.join(dest,'UI/Backgrounds/keyart_3heroes.png'));
  await sharp(path.join(source,'Assets/UI/Backgrounds/keyart_3heroes.png')).resize(1280,720).blur(6).jpeg({quality:90}).toFile(path.join(generated,'keyart_blur.jpg'));
  for (const file of fs.readdirSync(path.join(dest,'Lucide')).filter(x=>x.endsWith('.svg'))) {
    const svg = fs.readFileSync(path.join(dest,'Lucide',file),'utf8').replaceAll('currentColor','#ffffff');
    fs.writeFileSync(path.join(dest,'Lucide',file),svg);
    fs.writeFileSync(path.join(dest,'Lucide',file.replace('.svg','.png')),new Resvg(svg,{fitTo:{mode:'width',value:128}}).render().asPng());
  }
  const files = [...fs.readdirSync(generated), 'UI/Logo/logo_arena3q.png','UI/Backgrounds/keyart_3heroes.png'];
  console.log(files.map(x=>({file:x,bytes:fs.statSync(x.startsWith('UI/')?path.join(dest,x):path.join(generated,x)).size})));
}
main().catch(e=>{console.error(e);process.exitCode=1});
