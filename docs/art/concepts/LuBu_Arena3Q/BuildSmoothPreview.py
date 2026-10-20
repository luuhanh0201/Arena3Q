"""Validate packed captures, UV preservation and create a grounded review sheet."""
import bpy, json, os, sys, numpy as np, hashlib
root=os.path.dirname(os.path.abspath(__file__))
project=os.path.abspath(os.path.join(root,'../../../..'))
art=os.path.join(project,'Assets/Arena3Q/Art/Heroes/LuBu/Smooth')
manifest=json.load(open(os.path.join(art,'animations.json'),encoding='utf-8'))
partial='--partial' in sys.argv
assert manifest['directions']==8 and (partial or len(manifest['clips'])==7)
assert not manifest['clipped']
sheet=np.zeros((7*256,8*256,4),dtype=np.float32)
views=np.zeros_like(sheet)
total=0; pages=0
for row,clip in enumerate(manifest['clips']):
    assert len(clip['sprites'])==8*clip['frames']
    assert clip['source_stamp']==str(os.stat(os.path.join(root,'LuBu_Arena3Q_Smooth.blend')).st_mtime_ns)
    cache={}
    for index,path in enumerate(clip['pages']):
        image=bpy.data.images.load(os.path.join(art,path),check_existing=False)
        values=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32)
        image.pixels.foreach_get(values)
        cache[index]=values.reshape(image.size[1],image.size[0],4)
        bpy.data.images.remove(image); pages+=1
    for frame in clip['sprites']:
        pixels=cache[frame['page']]
        x,y,w,h=[frame[k] for k in ('x','y','width','height')]
        assert 0<=x<x+w<=pixels.shape[1] and 0<=y<y+h<=pixels.shape[0]
        assert 0<=frame['pivot_x']<=1 and 0<=frame['pivot_y']<=1
        crop=pixels[y:y+h,x:x+w]
        assert crop[:,:,3].max()>.1
        total+=1
    for col in range(8):
        index=round((clip['frames']-1)*col/7)
        frame=clip['sprites'][7*clip['frames']+index]
        x,y,w,h=[frame[k] for k in ('x','y','width','height')]
        crop=cache[frame['page']][y:y+h,x:x+w]
        left=round(128-frame['pivot_x']*w)
        ground=128-.55*5/np.sqrt(34)/clip['ortho_scale']*256
        bottom=round(ground-frame['pivot_y']*h)
        assert 0<=left<=256-w and 0<=bottom<=256-h
        sheet[(6-row)*256+bottom:(6-row)*256+bottom+h,col*256+left:col*256+left+w]=crop
    for direction in range(8):
        frame=clip['sprites'][direction*clip['frames']+clip['frames']//2]
        x,y,w,h=[frame[k] for k in ('x','y','width','height')]
        left=round(128-frame['pivot_x']*w)
        ground=128-.55*5/np.sqrt(34)/clip['ortho_scale']*256
        bottom=round(ground-frame['pivot_y']*h)
        views[(6-row)*256+bottom:(6-row)*256+bottom+h,direction*256+left:direction*256+left+w]=cache[frame['page']][y:y+h,x:x+w]
preview=bpy.data.images.new('Smooth animation review',width=8*256,height=7*256,alpha=True)
preview.pixels.foreach_set(sheet.ravel())
preview.filepath_raw=os.path.join(root,'LuBu_SmoothPreview_Partial.png' if partial else 'LuBu_SmoothPreview.png')
preview.file_format='PNG'; preview.save()
if partial:
    print('PARTIAL_PREVIEW',total,flush=True)
    sys.exit(0)
preview.pixels.foreach_set(views.ravel())
preview.filepath_raw=os.path.join(root,'LuBu_EightDirections.png'); preview.save()
def uv_hash(mesh):
    values=np.empty(len(mesh.uv_layers.active.data)*2,dtype=np.float32)
    mesh.uv_layers.active.data.foreach_get('uv',values)
    return hashlib.sha256(values.tobytes()).hexdigest()
smooth_uv=uv_hash(bpy.data.objects['LuBu_Body'].data)
with bpy.data.libraries.load(os.path.join(root,'LuBu_Texture_UV_DaXuLy.blend')) as (source,loaded):
    loaded.meshes=source.meshes
source_uv=uv_hash(loaded.meshes[0])
assert source_uv==smooth_uv
report={'frames':total,'directions':8,'pages':pages,'clipped':manifest['clipped'],
        'source_uv_checksum':source_uv,'smooth_uv_checksum':smooth_uv}
with open(os.path.join(root,'SpriteAuditSmooth.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print('SMOOTH_SPRITE_AUDIT_SUCCESS',json.dumps(report),flush=True)
