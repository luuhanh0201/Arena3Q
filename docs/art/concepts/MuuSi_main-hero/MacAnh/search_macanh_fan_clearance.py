"""Find whether a collision-free, readable fan orientation exists at fixed grip."""
import bpy,sys,json,math,numpy as np
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri
root=Path(__file__).resolve().parent;rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']];fan=bpy.data.objects['MacAnh_Fan']
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
body.data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in body.data.loop_triangles]
regions=[body.data.attributes['GarmentRegion'].data[t.polygon_index].value for t in body.data.loop_triangles]
ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();points=[body.matrix_world@v.co for v in mesh.vertices];ev.to_mesh_clear();tree=BVHTree.FromPolygons(points,triangles,all_triangles=True)
grip=fan.matrix_world.translation.copy();original=fan.matrix_world.to_quaternion();parts=[]
for name in ['Fan_Feathers','Fan_GoldFrame','Fan_Crystal']:
    obj=bpy.data.objects[name];obj.data.calc_loop_triangles()
    relative=fan.matrix_world.inverted()@obj.matrix_world
    verts=np.array([(*relative@v.co,1) for v in obj.data.vertices]);faces=[tuple(t.vertices) for t in obj.data.loop_triangles]
    parts.append((name,verts,faces))
def crosses(a,b):
    for first,second in [(a,b),(b,a)]:
        for i in range(3):
            start=first[i];delta=first[(i+1)%3]-start;hit=intersect_ray_tri(*second,delta,start,True)
            if hit is not None and (hit-start).length<=delta.length+1e-7:return True
    return False
camera=Vector((0,-.66,.75)).normalized();results=[]
for k in range(80):
    z=.15+.85*(k+.5)/80;angle=k*2.399963;r=math.sqrt(1-z*z);up=Vector((math.cos(angle)*r,math.sin(angle)*r,z))
    normal=-(camera-up*camera.dot(up)).normalized();axis=normal.cross(up).normalized();basis=Matrix((axis,normal,up)).transposed()
    for roll in [-60,-40,-20,0,20,40,60]:
        rotation=(basis@Matrix.Rotation(math.radians(roll),3,'Z')).to_quaternion();transform=Matrix.Translation(grip)@rotation.to_matrix().to_4x4()
        counts={};details=[]
        for name,verts,faces in parts:
            world=verts@np.array(transform).T;fp=[Vector(v) for v in world[:,:3]];ft=BVHTree.FromPolygons(fp,faces,all_triangles=True)
            pairs=[(a,b) for a,b in tree.overlap(ft) if regions[a] not in (3,4) and crosses([points[i] for i in triangles[a]],[fp[i] for i in faces[b]])]
            counts[name]=len(pairs)
        result={'up':list(up),'roll':roll,'contacts':counts,'total':sum(counts.values()),'q':list(rotation),'angle_from_original_degrees':math.degrees(original.rotation_difference(rotation).angle)}
        results.append(result)
        if result['total']==0:print('FAN_CLEAR_POSE_FOUND',result,flush=True)
results.sort(key=lambda item:(item['total'],item['angle_from_original_degrees']))
(root/'Production/LOD/FanOrientationSearch.json').write_text(json.dumps(results[:30],indent=2))
print('FAN_ORIENTATION_SEARCH_COMPLETE best',results[0],flush=True)
