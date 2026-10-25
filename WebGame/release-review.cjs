const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '..');
const sharp = require(path.join(root, 'AgentComms/codex/staging/ui-tools/node_modules/sharp'));
const out = path.join(root, 'NghiemThu/Release/Latest_20261025');
const settings = require('./web-settings.json');
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
(async () => {
  const images = await Promise.all(['before/web-battle.png', 'flow/10-battle.png'].map(file => sharp(path.join(out, file)).resize(800, 525).toBuffer()));
  const labels = Buffer.from('<svg width="1616" height="36"><g font-family="Arial" font-size="20" fill="#ffd877"><text x="12" y="25">TRƯỚC: MAP CŨ</text><text x="828" y="25">SAU: MAP MỚI TRONG LUỒNG ĐẦY ĐỦ</text></g></svg>');
  await sharp({ create: { width: 1616, height: 565, channels: 3, background: '#0a1424' } }).composite([
    { input: images[0], left: 0, top: 40 }, { input: images[1], left: 816, top: 40 }, { input: labels, left: 0, top: 0 }
  ]).png().toFile(path.join(out, 'web-map-before-after.png'));
  const build = JSON.parse(fs.readFileSync(path.join(root, settings.output, 'build.json'), 'utf8'));
  const payload = build.dataUrl.split('?')[0];
  const contentHash = hash(path.join(root, settings.output, payload));
  if (contentHash !== build.contentHash || !build.dataUrl.endsWith('?v=' + contentHash.slice(0, 20)) || build.battleScene !== 'Battlefield_HighQuality') throw Error('Web is not the current release');
  const registry = JSON.parse(fs.readFileSync(path.join(root, 'Admin/reviews/skill-icons-20261025.json'), 'utf8'));
  if (registry.skills.length !== 42 || registry.skills.some(s => hash(path.join(root, s.texturePath)) !== s.sha256)) throw Error('Approved icon source changed');
  const result = { contentHash, battleScene: build.battleScene, originalIcons: 42, screenshot: 'web-map-before-after.png' };
  if (fs.existsSync(path.join(root, settings.androidDownload))) {
    result.android = { file: settings.androidDownload, bytes: fs.statSync(path.join(root, settings.androidDownload)).size, sha256: hash(path.join(root, settings.androidDownload)) };
  }
  fs.writeFileSync(path.join(out, 'release-integrity.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result));
})().catch(error => { console.error(error); process.exitCode = 1; });
