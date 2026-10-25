// Reproducible screenshot comparison and source-art integrity check for the local QA report.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '..');
const sharp = require(path.join(root, 'AgentComms/codex/staging/ui-tools/node_modules/sharp'));
const out = path.join(root, 'NghiemThu/WebGame/20261025');
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
(async () => {
  const crop = { left: 510, top: 800, width: 580, height: 180 };
  const parts = await Promise.all(['flow-before-font', 'flow'].map(folder => sharp(path.join(out, folder, '10-battle.png')).extract(crop).toBuffer()));
  const labels = Buffer.from('<svg width="1180" height="25"><g fill="#ffd877" font-family="Arial" font-size="16"><text x="12" y="18">BEFORE</text><text x="612" y="18">AFTER</text></g></svg>');
  await sharp({ create: { width: 1180, height: 210, channels: 3, background: '#0a1424' } }).composite([
    { input: parts[0], left: 0, top: 25 }, { input: parts[1], left: 600, top: 25 }, { input: labels, left: 0, top: 0 }
  ]).png().toFile(path.join(out, 'font-before-after.png'));
  const registry = JSON.parse(fs.readFileSync(path.join(root, 'Admin/reviews/skill-icons-20261025.json'), 'utf8'));
  const icons = registry.skills.map(entry => ({ id: entry.id, original: hash(path.join(root, entry.texturePath)) === entry.sha256 }));
  if (icons.length !== 42 || icons.some(icon => !icon.original)) throw new Error('Approved icon artwork changed');
  const art = [
    ['UI/Backgrounds/keyart_3heroes.png', 'background.png'], ['UI/Logo/logo_arena3q.png', 'logo.png'],
    ['UI/Sprites/Buttons/btn_gold.png', 'btn_gold.png'], ['UI/Sprites/Buttons/btn_blue.png', 'btn_blue.png'],
    ['UI/Sprites/Frames/frame_panel.png', 'panel.png'], ['UI/Sprites/Ornaments/crest_dragon.png', 'crest.png'],
    ['Fonts/BeVietnamPro-Regular.ttf', 'regular.ttf'], ['Fonts/BeVietnamPro-ExtraBold.ttf', 'bold.ttf']
  ].map(([source, target]) => ({ source, target, original: hash(path.join(root, 'Assets/Arena3Q/Art/UI/Startup', source)) === hash(path.join(root, 'Builds/WebGL/Arena3Q/art', target)) }));
  if (art.some(item => !item.original)) throw new Error('Web UI differs from mobile source art');
  fs.writeFileSync(path.join(out, 'source-integrity.json'), JSON.stringify({ icons, art }, null, 2));
  console.log(JSON.stringify({ originalIcons: icons.length, identicalMobileArt: art.length, comparison: 'font-before-after.png' }));
})().catch(error => { console.error(error); process.exitCode = 1; });
