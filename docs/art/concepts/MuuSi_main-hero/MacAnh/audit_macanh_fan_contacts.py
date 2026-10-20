"""Actual feather/frame against deformed cloth/head triangles, every half frame."""
import bpy,sys,json,time
import math
import numpy as np
from pathlib import Path
from mathutils.bvhtree import BVHTree
from mathutils import Vector
from mathutils import Matrix
from mathutils.geometry import intersect_ray_tri
root=Path(__file__).resolve().parent;rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
pitch=float(sys.argv[sys.argv.index('--pitch')+1]) if '--pitch' in sys.argv else None
if pitch is not None:
    fan_root=bpy.data.objects['MacAnh_Fan'];fan_root.matrix_basis=fan_root.matrix_basis@Matrix.Rotation(math.radians(pitch),4,'X')
fan_root=bpy.data.objects['MacAnh_Fan'];fan_bind=fan_root.matrix_basis.copy()
if '--clearance-motion' in sys.argv:
    sys.path.insert(0,str(root))
    from macanh_fan_clearance import apply_pose,gesture
stabilize='--stabilize' in sys.argv
cuff_lag=float(sys.argv[sys.argv.index('--cuff-lag')+1]) if '--cuff-lag' in sys.argv else 0
drape=float(sys.argv[sys.argv.index('--drape')+1]) if '--drape' in sys.argv else 0
sleeve_offset=float(sys.argv[sys.argv.index('--sleeve-offset')+1]) if '--sleeve-offset' in sys.argv else 0
sleeve_turn=float(sys.argv[sys.argv.index('--sleeve-turn')+1]) if '--sleeve-turn' in sys.argv else 0
if '--tassel-sweep' in sys.argv:
    sweep=float(sys.argv[sys.argv.index('--tassel-sweep')+1])
    tassels=bpy.data.objects['Fan_Tassels']
    for vertex in tassels.data.vertices:
        depth=max(0,min(1,(-.025-vertex.co.z)/.13))
        vertex.co.y-=sweep*depth*depth
    tassels.data.update()
if stabilize:
    up=Vector(tuple(float(v) for v in sys.argv[sys.argv.index('--up')+1].split(','))).normalized()
    facing=Vector((0,-.66,.75));normal=-(facing-up*facing.dot(up)).normalized();axis=normal.cross(up).normalized()
    roll=float(sys.argv[sys.argv.index('--roll')+1]) if '--roll' in sys.argv else 0
    blend=float(sys.argv[sys.argv.index('--blend')+1]) if '--blend' in sys.argv else .9
    safe_rotation=(Matrix((axis,normal,up)).transposed()@Matrix.Rotation(math.radians(roll),3,'Z')).to_quaternion()
for modifier in body.modifiers:
    if modifier.type=='ARMATURE':modifier.show_viewport=True;modifier.use_deform_preserve_volume=False
fan_objects=[bpy.data.objects[name] for name in ('Fan_Feathers','Fan_GoldFrame','Fan_Crystal')]
if '--all-parts' in sys.argv:
    fan_objects=[obj for obj in bpy.data.objects if obj.type=='MESH' and obj.name.startswith('Fan_')]
for obj in fan_objects:obj.data.calc_loop_triangles()
fan_meshes={obj.name:(np.array([(*v.co,1) for v in obj.data.vertices]),[tuple(p.vertices) for p in obj.data.loop_triangles]) for obj in fan_objects}
regions=[int(v.value) for v in body.data.attributes['GarmentRegion'].data]
body.data.calc_loop_triangles();body_faces=[tuple(p.vertices) for p in body.data.loop_triangles]
body_polygons=[p.polygon_index for p in body.data.loop_triangles]
def crosses(a,b):
    for first,second in [(a,b),(b,a)]:
        for i in range(3):
            start=first[i];delta=first[(i+1)%3]-start
            hit=intersect_ray_tri(*second,delta,start,True)
            if hit is not None and (hit-start).length<=delta.length+1e-7:return True
    return False
report={'source':bpy.data.filepath,'allowed_contact':'Fan_Handle is the held grip; palm/finger surfaces are excluded. Feather/frame/crystal against cloth/head are measured.','clips':[]}
started=time.time()
for action in sorted(bpy.data.actions,key=lambda item:item.name):
    if not action.name.startswith('MacAnh_'):continue
    rig.animation_data.action=action;lo,hi=map(int,action.frame_range)
    result={'action':action.name,'samples':0,'max_non_palm_intersections':0,'worst':None,'total_palm_contacts':0}
    for frame in np.arange(lo,hi+.1,.5):
        fan_root.matrix_basis=fan_bind
        bpy.context.scene.frame_set(int(frame),subframe=float(frame%1));bpy.context.view_layer.update()
        if '--clearance-motion' in sys.argv:
            turn,rotation=gesture(action.name.removeprefix('MacAnh_'),frame,hi)
            apply_pose(rig,fan_root,turn,rotation)
        if cuff_lag:
            second=rig.pose.bones['Sleeve_Right_2'];third=rig.pose.bones['Sleeve_Right_3'];head=second.head.copy()
            delta=Matrix.Translation(head)@Matrix.Rotation(math.radians(cuff_lag),4,'X')@Matrix.Translation(-head)
            old_second=second.matrix.copy();old_third=third.matrix.copy()
            second.matrix=delta@old_second;third.matrix=delta@old_third;bpy.context.view_layer.update()
        if drape:
            first=rig.pose.bones['Sleeve_Right_1'];head=first.head.copy();direction=first.tail-head
            turn=direction.rotation_difference(direction+Vector((0,drape,-drape)))
            delta=Matrix.Translation(head)@turn.to_matrix().to_4x4()@Matrix.Translation(-head)
            first.matrix=delta@first.matrix;bpy.context.view_layer.update()
        if stabilize:
            grip=fan_root.matrix_world.translation.copy();rotation=fan_root.matrix_world.to_quaternion().slerp(safe_rotation,blend)
            fan_root.matrix_world=Matrix.Translation(grip)@rotation.to_matrix().to_4x4();bpy.context.view_layer.update()
        if sleeve_offset:
            first=rig.pose.bones['Sleeve_Right_1']
            first.matrix=Matrix.Translation((0,sleeve_offset,0))@first.matrix
            bpy.context.view_layer.update()
        if sleeve_turn:
            first=rig.pose.bones['Sleeve_Right_1'];head=first.head.copy()
            first.matrix=Matrix.Translation(head)@Matrix.Rotation(math.radians(sleeve_turn),4,'X')@Matrix.Translation(-head)@first.matrix
            bpy.context.view_layer.update()
        if '--forearm-sleeve' in sys.argv:
            arm=rig.pose.bones['RightLowerArm'];delta=arm.matrix@arm.bone.matrix_local.inverted()
            for index in (1,2,3):
                sleeve=rig.pose.bones['Sleeve_Right_'+str(index)]
                sleeve.matrix=delta@sleeve.bone.matrix_local
                bpy.context.view_layer.update()
        dg=bpy.context.evaluated_depsgraph_get();evaluated=body.evaluated_get(dg);mesh=evaluated.to_mesh()
        points=[body.matrix_world@v.co for v in mesh.vertices];tree=BVHTree.FromPolygons(points,body_faces,all_triangles=True)
        total=0;details=[]
        for obj in fan_objects:
            vertices,faces=fan_meshes[obj.name];world=vertices@np.array(obj.matrix_world).T
            fan_points=[Vector(v) for v in world[:,:3]];fan=BVHTree.FromPolygons(fan_points,faces,all_triangles=True)
            overlaps=tree.overlap(fan)
            non_palm=[(a,b) for a,b in overlaps if regions[body_polygons[a]] not in (3,4) and crosses([points[i] for i in body_faces[a]],[fan_points[i] for i in faces[b]])]
            result['total_palm_contacts']+=len(overlaps)-len(non_palm);total+=len(non_palm)
            for a,b in non_palm[:10]:details.append({'object':obj.name,'body_face':body_polygons[a],'part':regions[body_polygons[a]],'material':body.data.materials[body.data.polygons[body_polygons[a]].material_index].name,'rest_center':list(body.data.polygons[body_polygons[a]].center),'fan_triangle':b,'body_center':list(sum((points[i] for i in body_faces[a]),points[0]*0)/len(body_faces[a]))})
        evaluated.to_mesh_clear();result['samples']+=1
        if total>result['max_non_palm_intersections']:result['max_non_palm_intersections']=total;result['worst']={'frame':float(frame),'contacts':details}
    report['clips'].append(result);print('FAN_CONTACT_QA',action.name,result['samples'],result['max_non_palm_intersections'],flush=True)
report['elapsed_seconds']=time.time()-started;report['passed']=all(item['max_non_palm_intersections']==0 for item in report['clips'])
suffix=('_Pitch'+str(int(pitch))) if pitch is not None else ''
if '--label' in sys.argv:suffix='_'+sys.argv[sys.argv.index('--label')+1]
(root/'Production/LOD'/('FanContactAudit'+suffix+'.json')).write_text(json.dumps(report,indent=2))
print('FAN_CONTACT_AUDIT_COMPLETE passed',report['passed'],flush=True)
