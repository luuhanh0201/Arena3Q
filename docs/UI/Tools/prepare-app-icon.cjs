const fs = require('node:fs'), path = require('node:path');
const root = path.resolve(__dirname, '../../..');
const sharp = require(path.join(root, 'AgentComms/codex/staging/ui-tools/node_modules/sharp'));
const output = path.join(root, 'Assets/Arena3Q/Art/UI/AppIcon');
const logo = path.join(root, 'Assets/Arena3Q/Art/UI/Startup/UI/Logo/logo_arena3q.png');
async function main() {
  fs.mkdirSync(output, { recursive: true });
  const background = { r: 10, g: 20, b: 36, alpha: 1 };
  const center = async width => {
    const image = await sharp(logo).trim().resize({ width }).png().toBuffer();
    const meta = await sharp(image).metadata();
    return { input: image, left: Math.round((1024 - meta.width) / 2), top: Math.round((1024 - meta.height) / 2) };
  };
  await sharp({ create: { width: 1024, height: 1024, channels: 4, background } }).png().toFile(path.join(output, 'Background.png'));
  await sharp({ create: { width: 1024, height: 1024, channels: 4, background } }).composite([await center(900)]).png().toFile(path.join(output, 'Arena3Q.png'));
  await sharp({ create: { width: 1024, height: 1024, channels: 4, background: { ...background, alpha: 0 } } }).composite([await center(640)]).png().toFile(path.join(output, 'Foreground.png'));
  console.log('Prepared launcher icons from the supplied logo, preserving its aspect ratio.');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
