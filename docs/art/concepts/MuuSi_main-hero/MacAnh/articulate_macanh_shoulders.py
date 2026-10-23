"""Rigid shoulder plates follow a blend of chest and upper-arm orientation."""
import bpy,bmesh,sys,math
from pathlib import Path
from mathutils import Vector,Matrix
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
actions=[a for a in bpy.data.actions if a.name.startswith('MacAnh_')]
samples={}
for a in actions:
 r.animation_data.action=a;rows=[]
 for f in range(int(a.frame_range[0]),int(a.frame_range[1])+1):
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
  rows.append((f,{n:r.pose.bones[n].matrix@r.data.bones[n].matrix_local.inverted() for n in ['Chest','RightUpperArm','LeftUpperArm']}))
 samples[a.name]=rows
bpy.context.view_layer.objects.active=r;r.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for side,sign in [('Right',-1),('Left',1)]:
 b=r.data.edit_bones.new(side+'ShoulderPlate');b.head=(sign*.17,-.15,.93);b.tail=b.head+Vector((0,0,.05));b.parent=r.data.edit_bones['Chest']
bpy.ops.object.mode_set(mode='OBJECT')
# Ornament contacts were welded into the cloth. Separate their boundary before
# giving the metal a rigid transform, otherwise adjoining cloth is pulled apart.
bm=bmesh.new();bm.from_mesh(m)
for side,sign in [('Right',-1),('Left',1)]:
 faces={f for f in bm.faces if f.material_index==0 and .10<sign*f.calc_center_median().x<.28 and -.23<f.calc_center_median().y<-.09 and .87<f.calc_center_median().z<.99}
 boundary=[e for e in bm.edges if any(f in faces for f in e.link_faces) and any(f not in faces for f in e.link_faces)]
 bmesh.ops.split_edges(bm,edges=boundary)
bm.to_mesh(m);bm.free();m.update()
for side,sign in [('Right',-1),('Left',1)]:
 ids={i for p in m.polygons if p.material_index==0 and .10<sign*p.center.x<.28 and -.23<p.center.y<-.09 and .87<p.center.z<.99 for i in p.vertices}
 group=o.vertex_groups.new(name=side+'ShoulderPlate')
 for i in ids:
  for g in list(m.vertices[i].groups):o.vertex_groups[g.group].remove([i])
  group.add([i],1,'REPLACE')
 print('SHOULDER_PLATE',side,len(ids),flush=True)
for a in actions:
 r.animation_data.action=a
 for f,transforms in samples[a.name]:
  bpy.context.scene.frame_set(f)
  for side in ('Right','Left'):
   b=r.pose.bones[side+'ShoulderPlate'];chest=transforms['Chest'];arm=transforms[side+'UpperArm']
   q=chest.to_quaternion().slerp(arm.to_quaternion(),.4)
   head=chest@b.bone.head_local
   b.matrix=Matrix.Translation(head)@q.to_matrix().to_4x4()@b.bone.matrix_local.to_quaternion().to_matrix().to_4x4()
   b.keyframe_insert('location',frame=f);b.keyframe_insert('rotation_quaternion',frame=f)
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(20)
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
