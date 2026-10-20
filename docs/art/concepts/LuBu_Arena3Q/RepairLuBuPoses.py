"""Keep the victory gesture but solve the weapon arm outside the head/body."""
import bpy
import os
import sys
sys.stdout.reconfigure(line_buffering=True)
root=os.path.dirname(os.path.abspath(__file__))
path=os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source=open(path,encoding='utf-8-sig').read()
ns={'__file__':path}
exec(compile(source[:source.rindex('\nLOG.clear()')],path,'exec'),ns)
rig=bpy.data.objects['LuBu_Rig']; scene=bpy.context.scene
place=ns['_place']
def fast_place(*args):
    mods=[m for m in bpy.data.objects['LuBu_Body'].modifiers if m.type=='ARMATURE']
    flags=[m.show_viewport for m in mods]
    for m in mods: m.show_viewport=False
    try: return place(*args)
    finally:
        for m,flag in zip(mods,flags): m.show_viewport=flag
ns['_place']=fast_place

# The final recovery key of Attack02 was a collision-repair overshoot. Use the
# adjacent safe recovery pose for this single frame, retaining the attack itself.
ns['play']('Attack02')
attack=rig.animation_data.action
scene.frame_set(23)
safe=ns['_bonus_pose_capture'](rig)
rig.animation_data.action=None
ns['_bonus_pose_apply'](rig,safe,force_quaternion=True)
rig.animation_data.action=attack
for p in rig.pose.bones:
    p.keyframe_insert('rotation_quaternion',frame=22)
    if p.name=='Hips': p.keyframe_insert('location',frame=22)

ns['play']('Victory')
old=rig.animation_data.action
poses=[]
for frame in range(1,152,5):
    scene.frame_set(frame)
    poses.append((frame,ns['_bonus_pose_capture'](rig)))
old.name='LuBu_Victory_BeforeClearanceRepair'
rig.animation_data.action=None
action=bpy.data.actions.new('LuBu_Victory'); action.use_fake_user=True
for frame,pose in poses:
    rig.animation_data.action=None
    ns['_bonus_pose_apply'](rig,pose,force_quaternion=True)
    bpy.context.view_layer.update()
    lift=min(1.0,(frame-1)/30)
    ns['weapon']((-0.32,-0.14,-0.02+0.24*lift),(0,-0.12,1))
    rig.animation_data.action=action
    for p in rig.pose.bones:
        p.scale=(1,1,1)
        p.keyframe_insert('rotation_quaternion',frame=frame)
        p.keyframe_insert('scale',frame=frame)
        if p.name=='Hips': p.keyframe_insert('location',frame=frame)
    print('VICTORY_SAFE_KEY',frame,flush=True)
action.frame_range=(1,151)
ns['playall']()
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('LUBU_POSES_REPAIRED',flush=True)
