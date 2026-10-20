"""Eight-view, high-rate capture with tightly packed sprites and grounded pivots.
Render-only weapon LOD reduces capture cost; the source rig/mesh is never saved.
"""
import bpy
import json
import math
import os
import sys
import numpy as np
from mathutils import Vector
sys.stdout.reconfigure(line_buffering=True)
root=os.path.dirname(os.path.abspath(__file__))
project=os.path.abspath(os.path.join(root,'../../../..'))
output=os.path.join(project,'Assets/Arena3Q/Art/Heroes/LuBu/Smooth')
scratch=os.path.join(project,'Library/LuBuTools/SmoothCapture',str(os.getpid()))
os.makedirs(output,exist_ok=True); os.makedirs(scratch,exist_ok=True)
path=os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source=open(path,encoding='utf-8-sig').read(); ns={'__file__':path}
exec(compile(source[:source.rindex('\nLOG.clear()')],path,'exec'),ns)
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE_NEXT'
if hasattr(scene.eevee,'taa_render_samples'): scene.eevee.taa_render_samples=8
scene.render.resolution_x=scene.render.resolution_y=256; scene.render.resolution_percentage=100
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='Standard'; scene.world.color=(.35,.35,.35)
weapon=bpy.data.objects.get('LuBu_Weapon')
if weapon:
    lod=weapon.modifiers.new('Capture only LOD','DECIMATE'); lod.ratio=.15
    bpy.context.view_layer.objects.active=weapon
    for obj in bpy.context.selected_objects: obj.select_set(False)
    weapon.select_set(True)
    bpy.ops.object.modifier_apply(modifier=lod.name)
for obj in list(scene.objects):
    if obj.type in ('LIGHT','CAMERA'): bpy.data.objects.remove(obj,do_unlink=True)
def aim(obj,target): obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
for name,loc,power,size in [('Key',(2,-3,4),180,4),('Fill',(-3,-1,2),100,3),('Rim',(1,3,3),160,3)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=loc; aim(obj,(0,0,.6))
camera=bpy.data.objects.new('SmoothCaptureCamera',bpy.data.cameras.new('SmoothCaptureCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'
target=Vector((0,0,.55))
directions=[(-5,0,3),(-3.5355,3.5355,3),(0,5,3),(3.5355,3.5355,3),
            (5,0,3),(3.5355,-3.5355,3),(0,-5,3),(-3.5355,-3.5355,3)]
clips=[('Idle',32,True),('Walk',48,True),('Run',32,True),('Attack01',55,False),
       ('Attack02',55,False),('Victory',121,False),('Defeat',121,False)]
PAGE=2048  # atlas page size: every Android/iOS GPU takes 2048, same as Mac Anh
only=sys.argv[sys.argv.index('--only')+1].split(',') if '--only' in sys.argv else None
manifest={'version':2,'directions':8,'cell_size':256,'clips':[],'clipped':[]}
manifest_path=os.path.join(output,'animations.json')
source_stamp=str(os.stat(bpy.data.filepath).st_mtime_ns)
for name,count,loop in clips:
    if only and name not in only: continue
    ns['play'](name)
    action=bpy.data.objects['LuBu_Rig'].animation_data.action
    first,last=map(float,action.frame_range); duration=(last-first)/30
    scale={'Attack01':2.8,'Attack02':2.8,'Victory':2.8,'Defeat':3.7}.get(name,2.5)
    camera.data.ortho_scale=scale
    pivot=np.array([128,.5*256-.55*5/math.sqrt(34)/scale*256])
    page=np.zeros((PAGE,PAGE,4),dtype=np.float32)
    page_index=0; x=y=row_height=used_height=0; pages=[]; sprites=[]; alpha_edges=[]
    def save_page():
        height=max(4,int(math.ceil(used_height/4))*4)
        image=bpy.data.images.new('Packed '+name,width=PAGE,height=height,alpha=True)
        image.pixels.foreach_set(page[:height].ravel())
        filename=f'{name}_p{page_index:02d}.png'
        image.filepath_raw=os.path.join(output,filename); image.file_format='PNG'; image.save()
        bpy.data.images.remove(image); pages.append(filename)
    for direction,offset in enumerate(directions):
        camera.location=target+Vector(offset); aim(camera,target)
        for index in range(count):
            frame=first+(last-first)*index/(count if loop else count-1)
            scene.frame_set(int(frame),subframe=frame-int(frame))
            scene.render.filepath=os.path.join(scratch,'frame.png')
            bpy.ops.render.render(write_still=True)
            rendered=bpy.data.images.load(scene.render.filepath,check_existing=False)
            rgba=np.empty(256*256*4,dtype=np.float32); rendered.pixels.foreach_get(rgba)
            rgba=rgba.reshape(256,256,4); bpy.data.images.remove(rendered)
            alpha=rgba[:,:,3]
            if max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max())>.1:
                alpha_edges.append({'direction':direction,'frame':index})
            yy,xx=np.where(alpha>.003)
            if len(xx)==0: raise RuntimeError(f'Empty frame {name} {direction} {index}')
            left=max(0,int(min(xx.min(),pivot[0]))-3); right=min(256,int(max(xx.max()+1,pivot[0]+1))+3)
            bottom=max(0,int(min(yy.min(),pivot[1]))-3); top=min(256,int(max(yy.max()+1,pivot[1]+1))+3)
            crop=rgba[bottom:top,left:right]; h,w=crop.shape[:2]
            if x+w+4>PAGE: x=0; y+=row_height+4; row_height=0
            if y+h+4>PAGE:
                save_page(); page_index+=1; page.fill(0); x=y=row_height=used_height=0
            page[y:y+h,x:x+w]=crop
            sprites.append({'name':f'{name}_{direction}_{index:03d}','page':page_index,'x':x,'y':y,'width':w,'height':h,
                            'pivot_x':float((pivot[0]-left)/w),'pivot_y':float((pivot[1]-bottom)/h)})
            x+=w+4; row_height=max(row_height,h); used_height=max(used_height,y+h+4)
        print('CAPTURED_SMOOTH',name,direction,flush=True)
    save_page()
    entry={'name':name,'frames':count,'duration':duration,'loop':loop,'ortho_scale':scale,'pages':pages,'sprites':sprites,
           'source_stamp':source_stamp,'clipped':[{'clip':name,**c} for c in alpha_edges]}
    clip_path=os.path.join(output,name+'.frames.json')
    temporary=clip_path+'.'+str(os.getpid())+'.tmp'
    with open(temporary,'w',encoding='utf-8') as f: json.dump(entry,f,indent=2)
    os.replace(temporary,clip_path)
    manifest['clips']=[]; manifest['clipped']=[]
    for clip_name,_,_ in clips:
        path=os.path.join(output,clip_name+'.frames.json')
        if os.path.exists(path):
            captured=json.load(open(path,encoding='utf-8'))
            if captured.get('source_stamp')==source_stamp:
                manifest['clips'].append(captured); manifest['clipped']+=captured.get('clipped',[])
    temporary=manifest_path+'.'+str(os.getpid())+'.tmp'
    with open(temporary,'w',encoding='utf-8') as f: json.dump(manifest,f,indent=2)
    os.replace(temporary,manifest_path)
    print('EXPORTED_SMOOTH',name,len(sprites),'sprites',len(pages),'pages',len(alpha_edges),'clipped',flush=True)
    assert not alpha_edges, f'Framing clipped {name}; adjust capture scale.'
assert len(manifest['clips'])==7 or only
print('LUBU_SMOOTH_EXPORT_SUCCESS',flush=True)
