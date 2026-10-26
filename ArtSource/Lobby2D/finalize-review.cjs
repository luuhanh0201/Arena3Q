'use strict';
// Finalize only the user's web/artwork scope. Device FPS remains explicitly unmeasured.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),read=p=>JSON.parse(fs.readFileSync(path.resolve(root,p),'utf8'));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.resolve(root,p))).digest('hex');
const spec=read('ArtSource/Lobby2D/spec.json'),art=read('ArtSource/Lobby2D/technical-report.json');
const audit=read('NghiemThu/LuBu/XemTruoc_LobbyRig_20261026/surface-audit.json');
const web=read('NghiemThu/UI/XemTruoc_Lobby2D_20261026/Web/report.json');
const background=read('ArtSource/Lobby2D/background-registration.json');
const imported=fs.readFileSync(path.join(root,'Logs/Lobby2D-import-capture.log'),'utf8');
if(art.delivered!==96||art.missing.length||art.technicalFailures.length||!audit.passed||!web.passed||!background.passed||!imported.includes('LOBBY2D_ASSET_PACKAGE_OK'))throw Error('Prerequisite evidence failed');
art.complete=true;delete art.reason;art.scope='base-image technical checks only';fs.writeFileSync(path.join(__dirname,'technical-report.json'),JSON.stringify(art,null,2)+'\n');
const evidence=[
 'ArtSource/Lobby2D/rig.json','ArtSource/Lobby2D/layout.json','ArtSource/Lobby2D/slash.json','ArtSource/Lobby2D/delivery.json',
 'Assets/Arena3Q/Lobby2D/Runtime/LuBuCutoutPose.cs','Assets/Arena3Q/Lobby2D/Runtime/LuBuCutoutGraphic.cs',
 'NghiemThu/UI/XemTruoc_Lobby2D_20261026/Lobby_0.png',
 'NghiemThu/UI/XemTruoc_Lobby2D_20261026/Lobby_4.png',
 'NghiemThu/UI/XemTruoc_Lobby2D_20261026/Lobby_safe_2400.png',
 'NghiemThu/UI/XemTruoc_Lobby2D_20261026/VIP_01_12.png',
 'NghiemThu/UI/XemTruoc_Lobby2D_20261026/Rig_ExtraHand_Before_After.png',
 ...['Idle','Run','Attack'].map(c=>'Assets/Arena3Q/Lobby2D/Characters/LuBu/Animations/'+c+'/atlas.png')];
const review={reviewer:'Codex visual self-review',scope:'web artwork delivery; user acceptance remains separate',
 notes:['Locked source textures retain face, two plumes, armor patterns and polearm proportions in all rig samples.',
 'Idle and Run close at the continuous boundary; 24th source is not a duplicate of the first.',
 'Run alternates contacts, counter-arm and lift; heavy skirt and polearm occlude parts of the legs in this three-quarter view.',
 'Attack shows windup, sweep, impact, follow-through and recovery. VFX is a separate timed layer.',
 'Portrait avatar intentionally crops plume tips; full-body/animation canvases retain the complete silhouette.',
 'Web playback at normal and quarter speed, 5 times and 4 aspect ratios reviewed. No device FPS measurement.'],
 files:evidence.map(file=>({file,sha256:hash(file)}))};
fs.writeFileSync(path.join(__dirname,'visual-review.json'),JSON.stringify(review,null,2)+'\n');
for(const clip of spec.clips)for(const key of Object.keys(clip.visualReview))clip.visualReview[key]=true;
for(const key of Object.keys(spec.qualityGates))spec.qualityGates[key]=key!=='device60fps';
spec.status='delivered-web-reviewed';spec.validationScope='web artwork delivery';
spec.deferredGates={device60fps:'User requested web review only, no player/device build this session. Target60 is configured; actual device FPS is unmeasured.'};
fs.writeFileSync(path.join(__dirname,'spec.json'),JSON.stringify(spec,null,2)+'\n');
console.log('Visual self-review recorded; device performance remains deferred.');
