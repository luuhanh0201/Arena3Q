"""Smooth only newly added skin weights, preserving all source geometry/UV/materials."""
import bpy,os,json,time
ROOT=os.path.dirname(os.path.abspath(__file__))
rig=bpy.data.objects['MacAnh_Rig']; body=bpy.data.objects[rig['source_body_object']]
bpy.ops.object.select_all(action='DESELECT'); body.select_set(True); bpy.context.view_layer.objects.active=body
mods=[m for m in body.modifiers if m.type=='ARMATURE']
for m in mods: m.show_viewport=False
start=time.time()
bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
bpy.ops.object.vertex_group_smooth(group_select_mode='ALL',factor=.9,repeat=2)
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)
bpy.ops.object.mode_set(mode='OBJECT')
for m in mods: m.show_viewport=True
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(os.path.join(ROOT,'..','muu-si.blend')),compress=True)
print('MACANH_WEIGHT_SMOOTHED',time.time()-start,flush=True)

