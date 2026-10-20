"""Capture the approved source rig to URP 2D sprites; retain exact posed socket tracks in metres."""
import bpy,os,sys,json,math,time,hashlib
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=os.path.dirname(os.path.abspath(__file__))
PROJECT=os.path.abspath(os.path.join(ROOT,'../../../../..'))
sys.path.insert(0,os.path.join(PROJECT,'Library','MacAnhTools','Python'))
from PIL import Image
OUT=os.path.join(PROJECT,'Assets','Arena3Q','Art','Heroes','MacAnh','Sprites')
CACHE=os.path.join(PROJECT,'Library','MacAnhCapture')
if '--out' in sys.argv:OUT=os.path.abspath(sys.argv[sys.argv.index('--out')+1])
if '--cache' in sys.argv:CACHE=os.path.abspath(sys.argv[sys.argv.index('--cache')+1])
os.makedirs(OUT,exist_ok=True); os.makedirs(CACHE,exist_ok=True)
source_hash=hashlib.file_digest(open(bpy.data.filepath,'rb'),'sha256').hexdigest()
fingerprint={'source_sha256':source_hash,'ground_projection':.75,'size':256,'light':'top-left revised 2026-10-20'}
fingerprint_path=os.path.join(CACHE,'CaptureSource.json')
if '--reuse-cache' in sys.argv:
    assert os.path.exists(fingerprint_path) and json.load(open(fingerprint_path))==fingerprint,'Capture cache belongs to another model/projection'
with open(fingerprint_path,'w') as f:json.dump(fingerprint,f,indent=2)
manifest=json.load(open(os.path.join(ROOT,'Production','MacAnh_AnimationManifest.json')))
only=sys.argv[sys.argv.index('--only')+1].split(',') if '--only' in sys.argv else None
size=256; ortho=2.5; ppu=size/ortho
rig=bpy.data.objects['MacAnh_Rig']; scene=bpy.context.scene
body=bpy.data.objects[rig['source_body_object']]
assert max(sum(g.weight>0 for g in v.groups) for v in body.data.vertices)<=4
for modifier in body.modifiers:
    if modifier.type=='ARMATURE':modifier.show_render=True;modifier.show_viewport=True;modifier.use_deform_preserve_volume=False
scene.render.engine='BLENDER_EEVEE_NEXT'; scene.eevee.taa_render_samples=8
scene.render.resolution_x=size*4; scene.render.resolution_y=size*2; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.render.film_transparent=True
scene.render.image_settings.compression=20
# Collection instances share the full evaluated mesh: eight angles in one render,
# without decimation, retopology or eight independent skin solves.
subject=bpy.data.collections.new('MacAnhCaptureSubject')
for obj in list(scene.objects):
    if obj.type not in ('MESH','ARMATURE','EMPTY'): continue
    for collection in list(obj.users_collection): collection.objects.unlink(obj)
    subject.objects.link(obj)
for name,location,energy,area in [('Key',(-3,-4,5),2.0,3),('Fill',(3,-1,2),.7,3),('Rim',(1,3,3),1.0,2)]:
    data=bpy.data.lights.new(name,'SUN'); data.energy=energy; data.angle=.35
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=location
    obj.rotation_euler=(Vector((0,0,.85))-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('MacAnhSpriteCamera',bpy.data.cameras.new('MacAnhSpriteCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'; camera.data.ortho_scale=ortho*4
target=Vector((0,0,.8)); camera.location=target+Vector((0,-4,4*.75/math.sqrt(1-.75**2)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.context.view_layer.update()
right=camera.matrix_world.to_3x3() @ Vector((1,0,0)); up=camera.matrix_world.to_3x3() @ Vector((0,1,0))
instances=[]
for direction in range(8):
    obj=bpy.data.objects.new('Direction_'+str(direction),None); obj.instance_type='COLLECTION'; obj.instance_collection=subject
    obj.rotation_euler.z=direction*math.pi/4+math.pi/2
    obj.location=right*((direction%4-1.5)*ortho)+up*((.5-direction//4)*ortho)
    scene.collection.objects.link(obj); instances.append(obj)
bpy.context.view_layer.update()
def pixel(point):
    p=world_to_camera_view(scene,camera,point)
    return (p.x*size*4,(1-p.y)*size*2)
def socket(point,ground):
    px,py=pixel(point); return {'x':(px-ground[0])/ppu,'y':(ground[1]-py)/ppu}
for clip in manifest['clips']:
    name=clip['name']
    if only and name not in only: continue
    start=time.time(); entries=[]; clipped=[]
    rig.animation_data.action=bpy.data.actions['MacAnh_'+name]
    # Loops omit their duplicate endpoint; non-loops retain the complete recovery pose.
    labels=list(range(clip['last'] if clip['loop'] else clip['last']+1))
    if '--test-one' in sys.argv: labels=labels[:1]
    by_direction=[[] for _ in range(8)]
    for frame in labels:
        scene.frame_set(frame); bpy.context.view_layer.update()
        batch_path=os.path.join(CACHE,f'{name}_batch_{frame:03d}.png'); scene.render.filepath=batch_path
        reusable=False
        if '--reuse-cache' in sys.argv and os.path.exists(batch_path):
            try:
                with Image.open(batch_path) as cached:
                    reusable=cached.size==(size*4,size*2); cached.verify()
            except Exception: reusable=False
        if not reusable: bpy.ops.render.render(write_still=True)
        batch=Image.open(batch_path).convert('RGBA')
        for direction in range(8):
            instance=instances[direction]; transform=instance.matrix_world
            offset=(direction%4*size,direction//4*size)
            global_ground=pixel(transform @ Vector((0,0,0)))
            ground=(global_ground[0]-offset[0],global_ground[1]-offset[1])
            stem=f'{name}_{direction}_{frame:03d}'
            path=os.path.join(CACHE,stem+'.png')
            im=batch.crop((offset[0],offset[1],offset[0]+size,offset[1]+size)); im.save(path)
            box=im.getchannel('A').getbbox()
            if box is None: raise RuntimeError('Empty sprite '+stem)
            if box[0]<=1 or box[1]<=1 or box[2]>=size-1 or box[3]>=size-1: clipped.append(stem)
            box=(max(0,box[0]-2),max(0,box[1]-2),min(size,box[2]+2),min(size,box[3]+2))
            sockets={}
            fan=bpy.data.objects['Socket_Fan_Tip']
            sockets['fanTip']=socket(transform @ fan.matrix_world.to_translation(),global_ground)
            for key,bone in [('leftHand','Socket_Hand_L'),('cast','Socket_Cast')]:
                sockets[key]=socket(transform @ rig.matrix_world @ rig.pose.bones[bone].head,global_ground)
            by_direction[direction].append({'name':stem,'path':path,'box':box,'ground':ground,'sockets':sockets})
        if frame%10==0: print('MACANH_CAPTURED_FRAME',name,frame,round(time.time()-start,1),flush=True)
    entries=[e for rows in by_direction for e in rows]
    if '--test-one' in sys.argv: print('MACANH_BATCH_TEST_COMPLETE',flush=True); break
    if clipped: raise RuntimeError('Cropped capture frames: '+str(clipped[:20]))
    pages=[]; frames=[]; page=Image.new('RGBA',(2048,2048)); page_index=0; x=y=row=0
    def save_page():
        filename=f'{name}_{page_index:02d}.png'; page.save(os.path.join(OUT,filename),compress_level=6); pages.append(filename)
    for e in entries:
        image=Image.open(e['path']).convert('RGBA').crop(e['box']); w,h=image.size
        if x+w+2>2048: x=0; y+=row+4; row=0
        if y+h+2>2048: save_page(); page_index+=1; page=Image.new('RGBA',(2048,2048)); x=y=row=0
        page.paste(image,(x,y)); gx,gy=e['ground']; bx,by,_,_=e['box']
        frames.append({'name':e['name'],'page':page_index,'x':x,'y':2048-y-h,'width':w,'height':h,
            'pivot_x':(gx-bx)/w,'pivot_y':1-(gy-by)/h,'sockets':e['sockets']})
        x+=w+4; row=max(row,h)
    save_page()
    result={'name':name,'frames':len(labels),'directions':8,'duration':clip['duration'],'loop':clip['loop'],'capture':fingerprint,
        'pixelsPerUnit':ppu,'pages':pages,'sprites':frames,'events':clip['events'],'clipped':clipped}
    with open(os.path.join(OUT,name+'.json'),'w',encoding='utf-8') as f: json.dump(result,f,indent=2)
    print('MACANH_EXPORTED_CLIP',name,len(frames),len(pages),round(time.time()-start,1),flush=True)
print('MACANH_SPRITE_EXPORT_COMPLETE',flush=True)
