import bpy,numpy as np
from mathutils import Vector
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
for name in ['MacAnh_Fan','Fan_Feathers','Fan_GoldFrame',body.name]:
    obj=bpy.data.objects[name];ev=obj.evaluated_get(dg)
    print('FAN_MATRIX',name,'parent',obj.parent.name if obj.parent else None,'modifiers',[(m.name,m.type) for m in obj.modifiers],flush=True)
    print('world',np.array(obj.matrix_world).tolist(),'evaluated',np.array(ev.matrix_world).tolist(),flush=True)
    if obj.type=='MESH':
        points=np.array([obj.matrix_world@v.co for v in obj.data.vertices]);me=ev.to_mesh();ep=np.array([ev.matrix_world@v.co for v in me.vertices]);ev.to_mesh_clear()
        print('bounds raw',points.min(axis=0),points.max(axis=0),'evaluated',ep.min(axis=0),ep.max(axis=0),flush=True)
print('hand',list(rig.pose.bones['Socket_Hand_R'].head),flush=True)
