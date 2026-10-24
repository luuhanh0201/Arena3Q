const fs = require('node:fs'), path = require('node:path');
const root = path.resolve(__dirname, '../../..');
const sharp = require(path.join(root, 'AgentComms/codex/staging/ui-tools/node_modules/sharp'));
const review = path.join(root, 'NghiemThu/UI/XemTruoc_Polish_20261024');
async function main() {
  const before = await sharp(path.join(review, 'before/app-icon.png')).resize(512, 512).png().toBuffer();
  const after = await sharp(path.join(root, 'Assets/Arena3Q/Art/UI/AppIcon/Arena3Q.png')).resize(512, 512).png().toBuffer();
  await sharp({ create: { width: 1024, height: 552, channels: 4, background: '#0a1424' } }).composite([
    { input: Buffer.from('<svg width="1024" height="40"><text x="16" y="28" fill="#ffd877" font-size="22">BEFORE</text><text x="528" y="28" fill="#ffd877" font-size="22">AFTER</text></svg>'), left: 0, top: 0 },
    { input: before, left: 0, top: 40 }, { input: after, left: 512, top: 40 }
  ]).png().toFile(path.join(review, 'comparisons/BeforeAfter_AppIcon.png'));
  const file = path.join(review, 'xem_ui.html'); let html = fs.readFileSync(file, 'utf8');
  html = html.replace('</body>', '').replace('</html>', '') + '<section><h2>Icon Android và chuyển màn</h2><img src="comparisons/BeforeAfter_AppIcon.png"><p>Giữ nguyên logo gốc và tỷ lệ hình. Android adaptive dùng nền và lớp logo riêng.</p>';
  for (const name of ['14_transition_early', '14_transition_middle', '14_transition_done', '15_close_settings_middle', '15_close_settings_done'])
    if (fs.existsSync(path.join(review, 'after', name + '.png'))) html += `<p>${name}</p><img src="after/${name}.png">`;
  html += '<h2>Âm thanh intro trước và sau</h2><p>Trước:</p><video controls width="640" src="before/intro-original.mp4"></video><p>Sau:</p><video controls width="640" src="../../../Assets/Arena3Q/Art/UI/Startup/Video/Arena3Q_Splash_loud.mp4"></video></section></html>';
  html = html.replace('</html>', '<section><h2>Nhạc nền đã bỏ 45 giây đầu, còn 4:15</h2><audio controls preload="metadata" src="../../../Assets/Arena3Q/Art/UI/Startup/Video/BGM_From45s.wav"></audio></section></html>');
  fs.writeFileSync(file, html);
}
main().catch(error => { console.error(error); process.exitCode = 1; });
