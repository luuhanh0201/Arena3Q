const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const root = path.resolve(__dirname, '..'), settings = require('./web-settings.json');
const out = path.join(root, 'NghiemThu/Release/Latest_20261025');
const editor = 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/PlaybackEngines/AndroidPlayer';
(async () => {
  const packed = JSON.parse(fs.readFileSync(path.join(out, 'Android-content.json'), 'utf8'));
  const build = JSON.parse(fs.readFileSync(path.join(root, settings.output, 'build.json'), 'utf8'));
  assert.equal(packed.startupScene, 'Frontend');
  assert.equal(packed.battleScene, 'Battlefield_HighQuality');
  assert.equal(build.battleScene, packed.battleScene);
  assert.deepEqual(packed.scenes, settings.scenes);
  assert.equal(packed.requiredAssets.filter(asset => asset.includes('/Battlefield/Campaign/')).length, 2);
  const java = path.join(editor, 'OpenJDK/bin/java.exe');
  const signer = path.join(editor, 'SDK/build-tools/36.0.0/lib/apksigner.jar');
  const verify = file => execFileSync(java, ['-jar', signer, 'verify', '--verbose', '--print-certs', file], { encoding: 'utf8', maxBuffer: 1024 * 1024 });
  const signature = verify(path.join(root, settings.androidDownload));
  fs.writeFileSync(path.join(out, 'android-signature.txt'), signature);
  assert.match(signature, /Verified using v2 scheme.*true/);
  const digest = text => /Signer #1 certificate SHA-256 digest: ([a-f0-9]+)/i.exec(text)?.[1];
  assert.ok(digest(signature));
  assert.equal(digest(signature), digest(verify(path.join(root, 'Builds/Android/Arena3Q-Mobile-20261025.apk'))));
  const badging = execFileSync(path.join(editor, 'SDK/build-tools/36.0.0/aapt2.exe'), ['dump', 'badging', path.join(root, settings.androidDownload)], { encoding: 'utf8' });
  fs.writeFileSync(path.join(out, 'android-package.txt'), badging);
  assert.match(badging, /native-code: 'arm64-v8a'/);
  const previousBadging = execFileSync(path.join(editor, 'SDK/build-tools/36.0.0/aapt2.exe'), ['dump', 'badging', path.join(root, 'Builds/Android/Arena3Q-Mobile-20261025.apk')], { encoding: 'utf8' });
  const packageName = text => /^package: name='([^']+)'/m.exec(text)?.[1];
  assert.ok(packageName(badging));
  assert.equal(packageName(badging), packageName(previousBadging));
  const url = `http://127.0.0.1:${settings.port}/download/${path.basename(settings.androidDownload)}`;
  const response = await fetch(url, { method: 'HEAD' });
  assert.equal(response.status, 200);
  assert.equal(Number(response.headers.get('content-length')), fs.statSync(path.join(root, settings.androidDownload)).size);
  assert.equal(response.headers.get('content-type'), 'application/vnd.android.package-archive');
  const part = await fetch(url, { headers: { Range: 'bytes=0-3' } });
  assert.equal(part.status, 206);
  assert.equal(Buffer.from(await part.arrayBuffer()).readUInt32LE(), 0x04034b50);
  const result = { platform: packed.platform, startupScene: packed.startupScene, battleScene: packed.battleScene,
    currentCampaignAssetsPacked: packed.requiredAssets.length, signatureV2: true, sameSignerAsPreviousMobile: true,
    arm64: true, packageName: packageName(badging), samePackageAsPreviousMobile: true,
    downloadHead: 200, downloadRange: 206, apkBytes: fs.statSync(path.join(root, settings.androidDownload)).size, webContentHash: build.contentHash };
  fs.writeFileSync(path.join(out, 'delivery-verification.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result));
})().catch(error => { console.error(error); process.exitCode = 1; });
