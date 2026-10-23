import bpy
from mathutils import Matrix
from mathutils.bvhtree import BVHTree
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];h=bpy.data.objects['Fan_Handle']
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(20);bpy.context.view_layer.update()
digits=('Thumb','Index','Middle','Ring','Little')
weights={v.index:{d:sum(g.weight for g in v.groups if o.vertex_groups[g.group].name.startswith('Right'+d)) for d in digits} for v in o.data.vertices}
for step in range(4):
 bpy.context.view_layer.update();ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh()
 tree=BVHTree.FromPolygons([h.matrix_world@v.co for v in h.data.vertices],[list(p.vertices) for p in h.data.polygons])
 shifts={}
 for d in digits:
  ids=[i for i,w in weights.items() if max(w,key=w.get)==d and w[d]>.30]
  nearest=min(((tree.find_nearest(o.matrix_world@m.vertices[i].co),i) for i in ids),key=lambda item:item[0][3])
  (point,normal,face,dist),i=nearest
  if dist>.0015:
   p=o.matrix_world@m.vertices[i].co;shifts[d]=((point-p)*(1-.0010/dist),weights[i][d]);print('CLOSE',step,d,i,dist,flush=True)
 ev.to_mesh_clear()
 if not shifts:break
 for v in o.data.vertices:
  delta=sum((shift*min(1,weights[v.index][d]/peak) for d,(shift,peak) in shifts.items()),start=h.location*0)
  if delta.length<1e-8:continue
  skin=Matrix([[0]*4 for _ in range(4)])
  for g in v.groups:
   b=r.pose.bones.get(o.vertex_groups[g.group].name)
   if b:skin+=(b.matrix@b.bone.matrix_local.inverted())*g.weight
  v.co+=skin.to_3x3().inverted()@delta
 o.data.update()
bpy.ops.wm.save_as_mainfile(filepath='D:/UnityProjects/Arena3Q/Library/MacAnh_GripClosed.blend',compress=True)
