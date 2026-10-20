"""Headless, checkpointed build; never modifies the supplied source blend."""
import bpy
import os
import sys
sys.stdout.reconfigure(line_buffering=True)
root = os.path.dirname(os.path.abspath(__file__))
path = os.path.join(root, 'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source = open(path, encoding='utf-8-sig').read()
ns = {'__file__': path}
exec(compile(source[:source.rindex('\nLOG.clear()')], path, 'exec'), ns)
output = os.path.join(root, 'LuBu_Arena3Q_Rigged_Validated.blend')
bpy.context.scene.render.engine = 'CYCLES'
bpy.ops.wm.save_as_mainfile(filepath=output)
ns['_resolve_weapon_path']()
ns['_apply_safe_texture']()
if 'LuBu_Rig' not in bpy.data.objects:
    ns['step1']()
    bpy.ops.wm.save_as_mainfile(filepath=output)
    ns['step2']()
# Bone heat on the original disconnected UV mesh fails. Use the closed proxy solve
# when available, then constrain fingers/cloth/feathers to their own bone families.
weight_path=os.path.join(root,'LuBu_HeatWeights.npz')
if os.path.exists(weight_path):
    import numpy as np
    body=bpy.data.objects['LuBu_Body']; rig=bpy.data.objects['LuBu_Rig']
    weights=np.load(weight_path)
    body.vertex_groups.clear()
    names=list(weights['names'])
    indices=weights['indices']; values=weights['values']
    groups=[body.vertex_groups.new(name=str(n)) for n in names]
    for vertex in body.data.vertices:
        for group,value in zip(indices[vertex.index],values[vertex.index]):
            if group>=0 and value>0: groups[int(group)].add([vertex.index],float(value),'REPLACE')
    for bone in rig.data.bones:
        if bone.use_deform and bone.name not in body.vertex_groups: body.vertex_groups.new(name=bone.name)
    ns['fix_fingers'](body,rig)
    ns['fix_regions'](body,rig)
    ns['select_only'](body)
    bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
    bpy.ops.object.vertex_group_limit_total(group_select_mode='BONE_DEFORM',limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='BONE_DEFORM',lock_active=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    print('APPLIED_CLOSED_PROXY_HEAT_WEIGHTS',flush=True)
for modifier in bpy.data.objects['LuBu_Body'].modifiers:
    if modifier.type == 'ARMATURE': modifier.use_deform_preserve_volume = True
bpy.ops.wm.save_as_mainfile(filepath=output)
# IK only needs the skeleton. Avoid reskinning 99k vertices at every solver iteration.
place=ns['_place']
def fast_place(*args):
    mods=[m for m in bpy.data.objects['LuBu_Body'].modifiers if m.type=='ARMATURE']
    flags=[m.show_viewport for m in mods]
    for mod in mods: mod.show_viewport=False
    try: return place(*args)
    finally:
        for mod,flag in zip(mods,flags): mod.show_viewport=flag
ns['_place']=fast_place
for function_name in ('anim_idle','anim_walk','anim_run','anim_attack_sweep','anim_attack_thrust'):
    def checkpoint(fn=ns[function_name],label=function_name):
        print('BUILDING',label,flush=True)
        result=fn()
        bpy.ops.wm.save_as_mainfile(filepath=output)
        print('BUILT',label,flush=True)
        return result
    ns[function_name]=checkpoint
if bpy.data.objects.get('LuBu_Weapon'):
    ns['make_all_anims']()
ns['finishall']()
print('LUBU_RIG_SUCCESS', flush=True)
