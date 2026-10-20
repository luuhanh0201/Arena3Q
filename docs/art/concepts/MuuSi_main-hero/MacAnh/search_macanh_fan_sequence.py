"""Search fixed-grip orientations against cached, actually skinned difficult poses.

Exploration only: does not save a .blend or relax the contact criteria.
"""
import bpy, sys, math, json, time
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri

root = Path(__file__).resolve().parent
rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects[rig['source_body_object']]
fan = bpy.data.objects['MacAnh_Fan']
for mod in body.modifiers:
    if mod.type == 'ARMATURE':
        mod.show_viewport = True
        mod.use_deform_preserve_volume = False
body.data.calc_loop_triangles()
faces = [tuple(t.vertices) for t in body.data.loop_triangles]
regions = [body.data.attributes['GarmentRegion'].data[t.polygon_index].value for t in body.data.loop_triangles]
# Most constrained raised cuff first, then sweep, rest and gait.
poses = [('Skill03',20), ('Ultimate',28), ('Attack01',11.5), ('Skill02',11),
         ('Skill01',30.5), ('Attack02',10), ('DodgeAttack',15), ('Idle',0), ('Run',6)]
snapshots = []
for name, frame in poses:
    rig.animation_data.action = bpy.data.actions['MacAnh_'+name]
    bpy.context.scene.frame_set(int(frame), subframe=frame%1)
    bpy.context.view_layer.update()
    evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    points = [body.matrix_world @ v.co for v in mesh.vertices]
    evaluated.to_mesh_clear()
    snapshots.append((name, frame, points, BVHTree.FromPolygons(points, faces, all_triangles=True), fan.matrix_world.translation.copy()))
parts = []
for name in [obj.name for obj in bpy.data.objects if obj.type=='MESH' and obj.name.startswith('Fan_')]:
    obj = bpy.data.objects[name]
    obj.data.calc_loop_triangles()
    relative = fan.matrix_world.inverted() @ obj.matrix_world
    verts = np.array([(*relative @ v.co,1) for v in obj.data.vertices])
    parts.append((name,verts,[tuple(t.vertices) for t in obj.data.loop_triangles]))
def crosses(a,b):
    for first,second in [(a,b),(b,a)]:
        for i in range(3):
            start=first[i]; delta=first[(i+1)%3]-start
            hit=intersect_ray_tri(*second,delta,start,True)
            if hit is not None and (hit-start).length<=delta.length+1e-7:
                return True
    return False
camera=Vector((0,-.66,.75)).normalized()
results=[]; started=time.time()
candidates=[]
for x in [-.6,-.4,-.2,0,.2,.4,.6]:
    for y in [-.6,-.4,-.2,0,.2,.4]:
        for roll in [-90,-60,-30,-20,0,30,60,90,120,150,180]:
            up=Vector((x,y,1)).normalized()
            normal=-(camera-up*camera.dot(up)).normalized()
            axis=normal.cross(up).normalized()
            rotation=(Matrix((axis,normal,up)).transposed() @ Matrix.Rotation(math.radians(roll),3,'Z')).to_quaternion()
            candidates.append((x,y,roll,rotation))
for k in range(200):
    z=-.95+1.95*(k+.5)/200
    theta=k*2.399963; radius=math.sqrt(1-z*z)
    up=Vector((radius*math.cos(theta),radius*math.sin(theta),z))
    normal=-(camera-up*camera.dot(up)).normalized();axis=normal.cross(up).normalized()
    for roll in range(-180,181,30):
        rotation=(Matrix((axis,normal,up)).transposed()@Matrix.Rotation(math.radians(roll),3,'Z')).to_quaternion()
        candidates.append((up.x/up.z,up.y/up.z,roll,rotation))
# Start with orientations closest to the readable vertical fan; retain a complete search.
candidates.sort(key=lambda v:v[0]**2+(v[1]+.15)**2+((v[2]+20)/180)**2)
for ci,(x,y,roll,rotation) in enumerate(candidates):
    clear=0; contacts=0
    for name,frame,points,tree,grip in snapshots:
        transform=Matrix.Translation(grip) @ rotation.to_matrix().to_4x4()
        total=0
        for part,vertices,triangles in parts:
            world=vertices @ np.array(transform).T
            fp=[Vector(v) for v in world[:,:3]]
            ft=BVHTree.FromPolygons(fp,triangles,all_triangles=True)
            for a,b in tree.overlap(ft):
                if regions[a] not in (3,4) and crosses([points[i] for i in faces[a]],[fp[i] for i in triangles[b]]):
                    total+=1
        contacts+=total
        if total:break
        clear+=1
    result={'up':list(rotation@Vector((0,0,1))),'roll':roll,'clear_poses':clear,'first_failure_contacts':contacts,'q':list(rotation)}
    results.append(result)
    if clear==len(snapshots):print('COMMON_FAN_ORIENTATION_FOUND',result,flush=True)
    if ci%50==0:print('FAN_SEQUENCE_SEARCH',ci,'best',max(r['clear_poses'] for r in results),flush=True)
results.sort(key=lambda r:(-r['clear_poses'],r['first_failure_contacts']))
(root/'Production/LOD/FanSequenceSearch.json').write_text(json.dumps({'poses':poses,'seconds':time.time()-started,'best':results[:60]},indent=2))
print('FAN_SEQUENCE_SEARCH_COMPLETE',results[0],flush=True)
