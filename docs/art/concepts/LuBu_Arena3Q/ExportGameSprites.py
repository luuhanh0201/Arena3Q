"""Run with Blender background on LuBu_Arena3Q_7Anims.blend.
Keeps the source mesh and UVs; exports directional sprite atlases and rig diagnostics.
"""
import bpy
import json
import math
import os
import sys
import numpy as np
from mathutils import Vector

sys.stdout.reconfigure(line_buffering=True)
ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(ROOT, '../../../..'))
OUT = os.path.join(PROJECT, 'Assets/Arena3Q/Art/Heroes/LuBu')
os.makedirs(OUT, exist_ok=True)
source = open(os.path.join(ROOT, 'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py'), encoding='utf-8-sig').read()
ns = {'__file__': os.path.join(ROOT, 'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')}
exec(compile(source[:source.rindex('\nLOG.clear()')], 'LuBuRigLibrary', 'exec'), ns)
rig = bpy.data.objects['LuBu_Rig']
body = bpy.data.objects['LuBu_Body']
for modifier in body.modifiers:
    if modifier.type == 'ARMATURE': modifier.use_deform_preserve_volume = True

# Audit actual skinning, rather than counting vertex groups alone.
deform_indices = {body.vertex_groups[b.name].index for b in rig.data.bones
                  if b.use_deform and b.name in body.vertex_groups}
weights = [[g.weight for g in v.groups if g.group in deform_indices and g.weight > 0.00001]
           for v in body.data.vertices]
report = {'vertices': len(weights), 'bones': len(rig.data.bones),
          'unweighted': sum(not w for w in weights),
          'over_four_influences': sum(len(w) > 4 for w in weights),
          'max_weight_sum_error': max(abs(sum(w)-1) for w in weights), 'clips': []}
assert report['unweighted'] == 0, report
assert report['over_four_influences'] == 0, report
assert report['max_weight_sum_error'] < 0.001, report

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT'
if hasattr(scene.eevee, 'taa_render_samples'): scene.eevee.taa_render_samples = 16
scene.render.resolution_x = scene.render.resolution_y = 256
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.view_settings.view_transform = 'Standard'
scene.world.color = (0.35, 0.35, 0.35)
for obj in list(scene.objects):
    if obj.type in {'CAMERA', 'LIGHT'}: bpy.data.objects.remove(obj, do_unlink=True)
def aim(obj, point): obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
for name, loc, energy, size in [('Key',(2,-3,4),180,4),('Fill',(-3,-1,2),100,3),('Rim',(1,3,3),160,3)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=loc; aim(obj,(0,0,0.6))
cam=bpy.data.objects.new('SpriteCamera',bpy.data.cameras.new('SpriteCamera'))
scene.collection.objects.link(cam); scene.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=2.15
# Shared camera target and pivot prevent feet skating when frames change.
target=Vector((0,0,0.55))
directions=[(-3,-4,3),(-3,4,3),(3,4,3),(3,-4,3)]
clips=[('Idle',8,True),('Walk',12,True),('Run',12,True),('Attack01',12,False),
       ('Attack02',12,False),('Victory',24,False),('Defeat',28,False)]
preview = '--preview-idle' in sys.argv
fix_framing = '--fix-framing' in sys.argv
if preview: clips = clips[:1]
if fix_framing:
    clips=[clip for clip in clips if clip[0] in ('Attack01','Defeat')]
    report['clips']=json.load(open(os.path.join(OUT,'animations.json'),encoding='utf-8'))['clips']
for name,count,loop in clips:
    ortho_scale={'Attack01':2.4,'Defeat':3.2}.get(name,2.15)
    cam.data.ortho_scale=ortho_scale
    ns['play'](name)
    action=rig.animation_data.action
    start,end=map(float,action.frame_range)
    duration=(end-start)/30
    pixels=np.zeros((4*256,count*256,4),dtype=np.float32)
    for direction,offset in enumerate(directions):
        cam.location=target+Vector(offset); aim(cam,target)
        for i in range(count):
            frame=start+(end-start)*i/(count if loop else count-1)
            scene.frame_set(int(frame),subframe=frame-int(frame))
            scene.render.filepath=os.path.join(PROJECT,'Temp/LuBuTools/frame.png')
            bpy.ops.render.render(write_still=True)
            img=bpy.data.images.load(scene.render.filepath,check_existing=False)
            rgba=np.empty(256*256*4,dtype=np.float32); img.pixels.foreach_get(rgba)
            # Linear image pixels are saved through Blender's PNG encoder once.
            pixels[(3-direction)*256:(4-direction)*256,i*256:(i+1)*256]=rgba.reshape(256,256,4)
            bpy.data.images.remove(img)
        print('CAPTURE', name, direction, flush=True)
    atlas=bpy.data.images.new(name,width=count*256,height=4*256,alpha=True)
    atlas.pixels.foreach_set(pixels.ravel()); atlas.filepath_raw=os.path.join(OUT,name+'.png'); atlas.file_format='PNG'; atlas.save()
    bpy.data.images.remove(atlas)
    entry={'name':name,'frames':count,'duration':duration,'loop':loop,'ortho_scale':ortho_scale}
    report['clips']=[entry if c['name']==name else c for c in report['clips']]
    if not any(c['name']==name for c in report['clips']): report['clips'].append(entry)
    with open(os.path.join(OUT,'animations.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
    print('EXPORTED',name,flush=True)
if not preview and not fix_framing:
    ns['play']('Idle')
    weapon=bpy.data.objects.get('LuBu_Weapon')
    if weapon: weapon.hide_render=True
    scene.render.resolution_x=scene.render.resolution_y=512
    target=Vector((0,0,0.82)); cam.location=target+Vector((3,-4,2)); aim(cam,target)
    cam.data.ortho_scale=0.78
    scene.render.filepath=os.path.join(OUT,'Portrait.png')
    bpy.ops.render.render(write_still=True)
    if weapon: weapon.hide_render=False
    target=Vector((0,0,0.55)); cam.location=target+Vector((-3,-4,3)); aim(cam,target)
    cam.data.ortho_scale=3.2
    scene.render.resolution_x=scene.render.resolution_y=768
    ns['playall']()
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath, compress=True)
print('LUBU_EXPORT_SUCCESS',report,flush=True)
