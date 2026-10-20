"""Bake owned actions to a working copy; the original remains the bake source."""
import bpy,os,sys,json,math
ROOT=os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0,ROOT)
from macanh_motion import MacAnhMotion
motion=MacAnhMotion(); rig=motion.rig
clips=[('Idle',40,True),('Walk',36,True),('Run',24,True),('Dodge',24,False),('Attack01',24,False),
       ('Attack02',45,False),('Skill01',54,False),('Skill02',60,False),('Skill03',42,False),('Ultimate',105,False),('DodgeAttack',33,False)]
events={'Attack01':[(14,'SpawnWindSlash')],'Attack02':[(15,'SpawnTripleFeather_0'),(24,'SpawnTripleFeather_1'),(33,'SpawnTripleFeather_2')],
        'Skill01':[(33,'SpawnMagicArray')],'Skill02':[(30,'SpawnCyclone')],'Skill03':[(27,'ApplyShield')],
        'Ultimate':[(75,'OnUltimateHit')],'DodgeAttack':[(24,'SpawnCounterSlash')]}
only=sys.argv[sys.argv.index('--only')+1].split(',') if '--only' in sys.argv else None
mods=[m for m in motion.body.modifiers if m.type=='ARMATURE']
for m in mods: m.show_viewport=False
scene=bpy.context.scene; scene.render.fps=30
manifest={'fps':30,'frame_time_convention':'frame label f means f/30 seconds; frame 0 is a preroll pose',
          'in_place':True,'height_including_crown_m':1.7,'clips':[]}
manifest_path=os.path.join(ROOT,'Production','MacAnh_AnimationManifest.json')
if only and os.path.exists(manifest_path):
    manifest['clips']=[c for c in json.load(open(manifest_path))['clips'] if c['name'] not in only]
for name,last,loop in clips:
    if only and name not in only: continue
    action=bpy.data.actions.get('MacAnh_'+name)
    if action and action.get('owner')!='Arena3Q.MacAnh.Authoring': action=None
    if action: bpy.data.actions.remove(action)
    action=bpy.data.actions.new('MacAnh_'+name); action['owner']='Arena3Q.MacAnh.Authoring'; action.use_fake_user=True
    rig.animation_data_create(); previous={}
    for frame in range(last+1):
        rig.animation_data.action=None
        motion.pose(name,frame,last)
        values={p.name:(p.location.copy(),p.rotation_quaternion.normalized()) for p in rig.pose.bones}
        if loop and frame==0: first={n:(v[0].copy(),v[1].copy()) for n,v in values.items()}
        if loop and frame==last: values=first
        rig.animation_data.action=action
        for p in rig.pose.bones:
            p.location,q=values[p.name]
            if p.name in previous and previous[p.name].dot(q)<0: q=q.copy(); q.negate()
            p.rotation_quaternion=q; p.scale=(1,1,1); previous[p.name]=q.copy()
            p.keyframe_insert('location',frame=frame); p.keyframe_insert('rotation_quaternion',frame=frame); p.keyframe_insert('scale',frame=frame)
    curves=action.layers[0].strips[0].channelbag(action.slots[0]).fcurves if action.layers else action.fcurves
    for curve in curves:
        for key in curve.keyframe_points:
            key.interpolation='BEZIER'; key.handle_left_type=key.handle_right_type='AUTO_CLAMPED'
    for frame,label in events.get(name,[]): action.pose_markers.new(label).frame=frame
    manifest['clips'].append({'name':name,'first':0,'last':last,'duration':last/30,'loop':loop,
        'events':[{'frame':f,'time':f/30,'event':e} for f,e in events.get(name,[])]})
    print('MACANH_BAKED',name,last,flush=True)
for m in mods: m.show_viewport=True
rig.animation_data.action=bpy.data.actions.get('MacAnh_Idle'); scene.frame_start=0; scene.frame_end=40; scene.frame_set(0)
with open(os.path.join(ROOT,'Production','MacAnh_AnimationManifest.json'),'w',encoding='utf-8') as f: json.dump(manifest,f,indent=2)
destination=sys.argv[sys.argv.index('--output')+1] if '--output' in sys.argv else os.path.join(ROOT,'Production','DeformationFix','MacAnh_DrapedMotion.blend')
assert os.path.normcase(os.path.abspath(destination)) != os.path.normcase(os.path.abspath(os.path.join(ROOT,'..','muu-si.blend'))), 'Original source is immutable; choose a working output.'
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(destination),compress=True)
print('MACANH_ACTIONS_SAVED_TO_WORKING_COPY',flush=True)
