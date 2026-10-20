"""Package audited desktop artifacts and exact captured clip animations."""
import sys,json,hashlib,shutil,html
from pathlib import Path
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from PIL import Image,ImageDraw
out=root/'Production/Final';out.mkdir(exist_ok=True);(out/'Animations').mkdir(exist_ok=True)
manifest={'scope':'Desktop QA; Android measurements explicitly deferred by user 2026-10-20','lods':{},'original_sha256':hashlib.file_digest(open(root.parent/'muu-si.blend','rb'),'sha256').hexdigest(),
          'fan_revision':'Same 87 bones, fixed palm grip, socket follow-through, rigid common sleeve delta. Tassel tips sweep outward up to 0.04m; roots/UV/triangle count unchanged.'}
assert manifest['original_sha256']=='8bcb4755b01621f04758cf2a4be7fa147d1d7cd9b44f9fcfe82e16960a1f0af7'
for label in ('Battle','Portrait'):
    src=root/'Production/LOD'/('MacAnh_'+label+'_FanClearance.blend');dest=out/('MacAnh_'+label+'.blend')
    exported=json.loads((project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/LOD'/('Export_'+label+'.json')).read_text())
    audit=json.loads((root/'Production/LOD'/('ImportedSurface_'+label+'.json')).read_text());assert audit['passed']
    proof=json.loads((root/'Production/LOD'/('NormalOnlyRevision_'+label+'.json')).read_text());assert proof['equal']
    body_proof=json.loads((root/'Production/LOD'/('BodyDataUnchangedAfterFanFix_'+label+'.json')).read_text());assert body_proof['equal']
    contacts=json.loads((root/'Production/LOD'/('ImportedFanContacts_'+label+'.json')).read_text());assert contacts['passed']
    source_hash=hashlib.file_digest(open(src,'rb'),'sha256').hexdigest();assert source_hash==exported['source_sha256']
    shutil.copyfile(src,dest)
    manifest['lods'][label]={'blend':dest.name,'sha256':hashlib.file_digest(open(dest,'rb'),'sha256').hexdigest(),
        'triangles':exported['total_triangles'],'bones':exported['bones'],'max_influences':exported['max_influences'],
        'unity_surface_samples':sum(c['samples'] for c in audit['clips']),'height_m':audit['height'],
        'max_ratio':max(c['maxRatio'] for c in audit['clips']),'min_ratio':min(c['minRatio'] for c in audit['clips']),
        'native_fan_samples':sum(c['samples'] for c in contacts['clips']),'native_fan_max_non_palm_contacts':max(c['max_contacts'] for c in contacts['clips']),
        'native_reconstruction_error_m':contacts['unity_proof']['maxReconstructionErrorM'],
        'fbx':str(project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/LOD'/('MacAnh_'+label+'Rig.fbx'))}
raw=project/'Library/MacAnhFanClearanceSprites/Raw';sprites=project/'Library/MacAnhFanClearanceSprites/Atlases'
sprite_qa=json.loads((root/'Production/LOD/FinalSprite_QA.json').read_text())
assert sprite_qa['passed'] and sprite_qa['sprite_count']==3960
assert Path(sprite_qa['root']).resolve()==sprites.resolve()
assert sprite_qa['source_sha256']==manifest['lods']['Portrait']['sha256']
capture_source=json.loads((raw/'CaptureSource.json').read_text())
assert capture_source['source_sha256']==manifest['lods']['Portrait']['sha256']
cards=[]
for path in sorted(sprites.glob('*.json')):
    data=json.loads(path.read_text());name=data['name'];frames=[]
    for i in range(data['frames']):
        image=Image.open(raw/(name+'_7_'+str(i).zfill(3)+'.png')).convert('RGBA')
        canvas=Image.new('RGBA',(256,280),(65,67,77,255));canvas.alpha_composite(image,(0,24))
        ImageDraw.Draw(canvas).text((8,7),name+'  f'+str(i),fill='white');frames.append(canvas.convert('RGB'))
    target=out/'Animations'/(name+'.webp');frames[0].save(target,save_all=True,append_images=frames[1:],duration=[33 if i%3 else 34 for i in range(len(frames))],loop=0,lossless=True,method=2)
    cards.append('<figure><img loading="lazy" src="Animations/'+name+'.webp"><figcaption>'+name+'</figcaption></figure>')
(out/'Manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(out/'Review.html').write_text('''<!doctype html><html lang="vi"><meta charset="utf-8"><title>Mặc Ảnh – kiểm tra desktop</title><style>body{margin:24px;background:#22242d;color:#eee;font:16px system-ui}a{color:#c9afff}.grid{display:flex;flex-wrap:wrap;gap:12px}figure{margin:0;background:#41434d;padding:8px;border-radius:8px}img{width:256px;height:280px}figcaption{text-align:center}p{max-width:950px;line-height:1.5}</style><h1>Mặc Ảnh – bản sửa trên desktop</h1><p>Ảnh động dùng đúng từng frame đã chụp từ model sửa, góc 3/4. Mỗi clip tự phát lại từ đầu; việc phát lại không phải chuyển clip trong game. Hiệu ứng và chuyển clip được kiểm riêng bằng ảnh trong trận.</p><p><a href="MacAnh_Portrait.blend">Model ngoài trận</a> · <a href="MacAnh_Battle.blend">Model trong trận</a> · <a href="Manifest.json">Thông số và hash</a> · <a href="../LOD/Review_PortraitCandidatePhone/Idle_000_Front.png">Ảnh lớn</a></p><div class="grid">'''+''.join(cards)+'''</div><p>3D trong trận vẫn mặc định tắt. Không có kết quả FPS/PSS Android mới; người dùng yêu cầu hoãn đo thiết bị.</p></html>''',encoding='utf-8')
print('MACANH_DESKTOP_REVIEW_PACKAGED',json.dumps(manifest),flush=True)
