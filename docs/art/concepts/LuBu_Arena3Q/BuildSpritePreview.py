"""Build a small review sheet and check that exported frames have transparent borders."""
import bpy
import json
import os
import numpy as np
import hashlib
root=os.path.dirname(os.path.abspath(__file__))
project=os.path.abspath(os.path.join(root,'../../../..'))
art=os.path.join(project,'Assets/Arena3Q/Art/Heroes/LuBu')
manifest=json.load(open(os.path.join(art,'animations.json'),encoding='utf-8'))
sheet=np.zeros((7*256,4*256,4),dtype=np.float32)
clipped=[]
for row,clip in enumerate(manifest['clips']):
    name=clip['name']; count=clip['frames']
    image=bpy.data.images.load(os.path.join(art,name+'.png'),check_existing=False)
    values=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32)
    image.pixels.foreach_get(values)
    pixels=values.reshape(image.size[1],image.size[0],4)
    for d in range(4):
        for f in range(count):
            frame=pixels[(3-d)*256:(4-d)*256,f*256:(f+1)*256]
            alpha=frame[:,:,3]
            if max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max())>0.1:
                clipped.append({'clip':name,'direction':d,'frame':f})
    for col,index in enumerate([0,count//3,2*count//3,count-1]):
        sheet[(6-row)*256:(7-row)*256,col*256:(col+1)*256]=pixels[3*256:4*256,index*256:(index+1)*256]
    bpy.data.images.remove(image)
preview=bpy.data.images.new('LuBu animation review',width=4*256,height=7*256,alpha=True)
preview.pixels.foreach_set(sheet.ravel())
preview.filepath_raw=os.path.join(root,'LuBu_AnimationPreview.png'); preview.file_format='PNG'; preview.save()
def uv_hash(mesh):
    values=np.empty(len(mesh.uv_layers.active.data)*2,dtype=np.float32)
    mesh.uv_layers.active.data.foreach_get('uv',values)
    return hashlib.sha256(values.tobytes()).hexdigest()
with bpy.data.libraries.load(os.path.join(root,'LuBu_Texture_UV_DaXuLy.blend')) as (source,loaded):
    loaded.meshes=source.meshes
source_uv=uv_hash(loaded.meshes[0])
with bpy.data.libraries.load(os.path.join(root,'LuBu_Arena3Q_Rigged_Validated.blend')) as (source,loaded):
    loaded.objects=['LuBu_Body']
rigged_uv=uv_hash(loaded.objects[0].data)
assert source_uv==rigged_uv, 'UV data changed from the supplied model.'
with open(os.path.join(root,'SpriteAudit.json'),'w',encoding='utf-8') as f:
    json.dump({'frames':sum(c['frames']*4 for c in manifest['clips']),'clipped':clipped,
               'source_uv_checksum':source_uv,'rigged_uv_checksum':rigged_uv},f,indent=2)
print('SPRITE_AUDIT',len(clipped),'clipped frames',flush=True)
assert not clipped, 'Some sprites are cropped; see SpriteAudit.json.'
