import bpy
from mathutils import Matrix
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']]
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(20);bpy.context.view_layer.update()
dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);mesh=ev.to_mesh();posed=[v.co.copy() for v in mesh.vertices];ev.to_mesh_clear()
ids={i for p,a in zip(o.data.polygons,o.data.attributes['GarmentRegion'].data) if a.value==3 for i in p.vertices}
digits=('Thumb','Index','Middle','Ring','Little')
tag=o.data.attributes.get('GripDigit') or o.data.attributes.new('GripDigit','INT','POINT')
for i in ids:
 weights=[sum(g.weight for g in o.data.vertices[i].groups if o.vertex_groups[g.group].name.startswith('Right'+d)) for d in digits]
 tag.data[i].value=weights.index(max(weights))+1 if max(weights)>.30 else 0
grip=r.pose.bones['Socket_Hand_R'];grip.bone.use_deform=True;hand=r.pose.bones['RightHand']
gm=grip.matrix@grip.bone.matrix_local.inverted();hm=hand.matrix@hand.bone.matrix_local.inverted()
g=o.vertex_groups.get('Socket_Hand_R') or o.vertex_groups.new(name='Socket_Hand_R')
for i in ids:
 p=posed[i];t=1.0
 skin=gm*t+hm*(1-t);o.data.vertices[i].co=skin.inverted()@p
 for old in list(o.data.vertices[i].groups):o.vertex_groups[old.group].remove([i])
 if t>0:g.add([i],t,'REPLACE')
 if t<1:o.vertex_groups['RightHand'].add([i],1-t,'REPLACE')
o['grip_mode']='Fixed closed hand follows the animated grip; wrist continuity requires visual review.'
o.data.update();bpy.ops.wm.save_as_mainfile(filepath='D:/UnityProjects/Arena3Q/Library/MacAnh_GripSurface.blend',compress=True)
