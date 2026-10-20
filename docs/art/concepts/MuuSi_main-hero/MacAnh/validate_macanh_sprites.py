"""Validate completed atlas metadata without opening Unity or modifying sprites."""
import os,sys,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parent; PROJECT=ROOT.parents[4]
sys.path.insert(0,str(PROJECT/'Library/MacAnhTools/Python'))
from PIL import Image
ART=PROJECT/'Assets/Arena3Q/Art/Heroes/MacAnh/Sprites'
if '--root' in sys.argv:ART=Path(sys.argv[sys.argv.index('--root')+1]).resolve()
manifest=json.loads((ROOT/'Production/MacAnh_AnimationManifest.json').read_text())
expected={c['name']:c for c in manifest['clips']}; result=[]; all_names=set()
for path in sorted(ART.glob('*.json')):
    data=json.loads(path.read_text()); issues=[]; spec=expected[data['name']]
    frames=spec['last']+(0 if spec['loop'] else 1)
    if data['frames']!=frames or len(data['sprites'])!=frames*8: issues.append('frame_count')
    if data['clipped']: issues.append('cropped_capture')
    if data['events']!=spec['events']: issues.append('event_mismatch')
    for page in data['pages']:
        with Image.open(ART/page) as image:
            if image.size!=(2048,2048): issues.append('atlas_size')
            image.verify()
    for f in data['sprites']:
        if f['name'] in all_names: issues.append('duplicate_name')
        all_names.add(f['name'])
        if f['page']>=len(data['pages']) or min(f['x'],f['y'])<0 or f['x']+f['width']>2048 or f['y']+f['height']>2048: issues.append('sprite_rect')
        if not (0<=f['pivot_x']<=1 and 0<=f['pivot_y']<=1): issues.append('pivot')
        for socket in f['sockets'].values():
            if not all(math.isfinite(v) for v in socket.values()): issues.append('socket_nan')
    result.append({'name':data['name'],'sprites':len(data['sprites']),'pages':len(data['pages']),'issues':sorted(set(issues))})
report={'root':str(ART.resolve()),'clips':result,'sprite_count':sum(c['sprites'] for c in result),'page_count':sum(c['pages'] for c in result),
    'png_bytes':sum(p.stat().st_size for p in ART.glob('*.png')),
    'passed':len(result)==11 and all(not c['issues'] for c in result)}
capture_source=ART.parent/'Raw/CaptureSource.json'
if capture_source.exists():report['source_sha256']=json.loads(capture_source.read_text())['source_sha256']
destination=Path(sys.argv[sys.argv.index('--report')+1]).resolve() if '--report' in sys.argv else ROOT/'Production/MacAnh_Sprite_QA.json'
destination.write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report)); assert report['passed']
