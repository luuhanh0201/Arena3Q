// Regression audit against the accepted P4b revision, independent of repacking UVs.
const fs=require('fs'),cp=require('child_process'),assert=require('assert/strict'),crypto=require('crypto'),path=require('path');
const previous=p=>JSON.parse(cp.execFileSync('git',['show','5b1d605:ArtSource/Chapter01/2D/P4/Export/'+p],{encoding:'utf8'}));
const current=p=>JSON.parse(fs.readFileSync(path.join(__dirname,'Export',p)));
const old=previous('placements.json').patches,now=current('placements.json').patches;
const positions=p=>{const {uv,...rest}=p;return rest;};
assert.deepEqual(now.slice(0,old.length).map(positions),old.map(positions));
const oldSprites=previous('details.json').sprites,sprites=current('details.json').sprites;
for(const s of oldSprites)assert.equal(sprites.find(n=>n.name===s.name).sourceHash,s.sourceHash);
for(const s of sprites){const file=path.join(__dirname,oldSprites.some(p=>p.name===s.name)?'../Assets/Export/Sprites':'Generated',s.name+'.png');assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),s.sourceHash);}
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
assert.equal(hash(path.join(__dirname,'Export/chapter-details.png')),hash('Assets/ThirdParty/Chapter01/_Presentation/chapter-details.png'));
const baked=JSON.parse(fs.readFileSync('Logs/P4b-details-placement.json'));
const added=now.slice(old.length).filter(p=>baked.placedIds.includes(p.id));
const report={passed:true,originalPlacements:old.length,originalSpriteHashes:oldSprites.length,allSpriteHashes:sprites.length,added:added.length,byType:Object.fromEntries(['wildflowers','fallen-cloth','village-litter'].map(n=>[n,added.filter(p=>p.sprite===n).length])),atlasHash:hash(path.join(__dirname,'Export/chapter-details.png'))};
fs.writeFileSync('NghiemThu/Chapter01/XemTruoc_P4Supplement_20261031/placement-check.json',JSON.stringify(report,null,2));console.log(report);
