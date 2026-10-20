"""Build provisional LOD copies with existing UVs; never save the original.

Run against the working action file with --python-exit-code 1.
Interpolated four-influence weights require separate surface/visual approval.
"""
import bpy, json, time
from pathlib import Path

root=Path(__file__).resolve().parent
out=root/'Production/LOD'
out.mkdir(parents=True,exist_ok=True)
rig=bpy.data.objects['MacAnh_Rig']
body=bpy.data.objects[rig['source_body_object']]
rig.animation_data.action=None
for bone in rig.pose.bones: bone.matrix_basis.identity()
bpy.context.view_layer.update()
original=body.data
started=time.time()
report={'status':'provisional; surface/visual QA pending',
        'source':bpy.data.filepath,'bones':len(rig.data.bones),'lods':[]}
for label,target in [('Portrait',240000),('Battle',33000)]:
    body.data=original.copy()
    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True);bpy.context.view_layer.objects.active=body
    dec=body.modifiers.new('Controlled LOD reduction','DECIMATE')
    dec.ratio=target/len(original.polygons)
    dec.use_collapse_triangulate=True
    bpy.ops.object.modifier_move_up(modifier=dec.name)
    bpy.ops.object.modifier_apply(modifier=dec.name)
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)
    for modifier in body.modifiers:
        if modifier.type=='ARMATURE':modifier.use_deform_preserve_volume=False
    body.data.calc_loop_triangles()
    fan_triangles=0
    for obj in bpy.data.objects:
        if obj.type=='MESH' and obj!=body and obj.name.startswith('Fan_'):
            obj.data.calc_loop_triangles();fan_triangles+=len(obj.data.loop_triangles)
    item={'name':label,'vertices':len(body.data.vertices),
          'body_triangles':len(body.data.loop_triangles),'fan_triangles':fan_triangles,
          'total_triangles':len(body.data.loop_triangles)+fan_triangles,
          'max_influences':max(sum(g.weight>0 for g in v.groups) for v in body.data.vertices),
          'uv_layers':list(body.data.uv_layers.keys()),'seconds':time.time()-started}
    assert item['max_influences']<=4 and len(rig.data.bones)==87
    rig.animation_data.action=bpy.data.actions.get('MacAnh_Idle')
    bpy.context.scene.frame_set(0)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/('MacAnh_'+label+'_Working.blend')),compress=True)
    report['lods'].append(item)
    print('MACANH_LOD_BUILT',item,flush=True)
    rig.animation_data.action=None
    for bone in rig.pose.bones: bone.matrix_basis.identity()
    bpy.context.view_layer.update()
(out/'BuildReport.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MACANH_LODS_COMPLETE',flush=True)
