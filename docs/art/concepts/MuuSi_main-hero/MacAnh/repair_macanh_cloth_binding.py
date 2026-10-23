"""Candidate: anchor cuffs to wrists and restore weighted secondary surfaces.

Read every authored pose before changing its keys. Keep hand/fan and event timing.
"""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Matrix, Vector
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];s=bpy.context.scene
groups={g.name:g.index for g in o.vertex_groups}
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
regions={v.index:set() for v in o.data.vertices}
for f in o.data.polygons:
    for i in f.vertices:regions[i].add(o.data.attributes['GarmentRegion'].data[f.index].value)
changed=0
for v in o.data.vertices:
    part=next(iter(regions[v.index]),0)
    if part not in (1,2):continue
    side='Right' if part==1 else 'Left';x,y,z=v.co
    wrist=r.data.bones[side+'Hand'].head_local
    # The opening at the hand follows the hand, while the hanging cloth follows
    # the wrist position with progressively less rotation farther down the hem.
    cuff=smooth(.38,.67,z)
    if side=='Right':
        v.co.y+=.10*(1-cuff)+.055*smooth(.60,.68,z)
    shoulder=0
    hem=1-cuff-shoulder
    tail=1-smooth(.33,.52,z)
    weights={side+'Hand':cuff,side+'UpperArm':shoulder,
             'Sleeve_'+side+'_2':hem*(1-tail),'Sleeve_'+side+'_3':hem*tail}
    for g in list(v.groups):o.vertex_groups[g.group].remove([v.index])
    for name,value in weights.items():
        if value>1e-7:o.vertex_groups[name].add([v.index],value,'REPLACE')
    changed+=1
# Match the visible fitted undersleeve to the pale inner garment. Only vertices
# on the added arm tubes are adjusted; the original ornamental mesh is retained.
mat=next((m for m in o.data.materials if m.name=='MacAnh_ContinuousInnerSleeves'),None)
if mat:
    colour=(.62,.59,.68,1)
    mat.diffuse_color=colour;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=colour
    metadata=json.loads(o.get('lod_inner_materials','[]'))
    for item in metadata:
        if item['name']==mat.name:item['colour']=colour
    o['lod_inner_materials']=json.dumps(metadata)
    ids={i for p in o.data.polygons if o.data.materials[p.material_index]==mat for i in p.vertices}
    for i in ids:
        v=o.data.vertices[i]
        if len(v.groups)!=1:continue
        name=o.vertex_groups[v.groups[0].group].name
        if not name.endswith(('UpperArm','LowerArm')):continue
        bone=r.data.bones[name];axis=(bone.tail_local-bone.head_local).normalized()
        center=bone.head_local+axis*(v.co-bone.head_local).dot(axis)
        v.co=center+(v.co-center)*(1.4 if name.endswith('UpperArm') else 1.25)
# Wrist driven drape: a rigid global sleeve rotation around its shoulder had
# moved the opening away from the hand. Position the drape about the wrist.
for action in list(bpy.data.actions):
    if not action.name.startswith('MacAnh_'):continue
    r.animation_data.action=action;first,last=map(int,action.frame_range)
    snapshots=[]
    for f in range(first,last+1):
        s.frame_set(f);bpy.context.view_layer.update()
        snapshots.append({n:p.matrix.copy() for n,p in r.pose.bones.items()})
    for f,pose in zip(range(first,last+1),snapshots):
        s.frame_set(f)
        chest=pose['Chest']@r.data.bones['Chest'].matrix_local.inverted()
        for side in ('Right','Left'):
            wrist=r.data.bones[side+'Hand'].head_local
            hand=pose[side+'Hand']@r.data.bones[side+'Hand'].matrix_local.inverted()
            anchor=hand@wrist
            for index,gravity in [(1,.05),(2,.08),(3,.12)]:
                p=r.pose.bones['Sleeve_'+side+'_'+str(index)]
                q=hand.to_quaternion().slerp(chest.to_quaternion(),gravity)
                clearance=Vector((0,0,0))
                delta=Matrix.Translation(anchor+clearance)@q.to_matrix().to_4x4()@Matrix.Translation(-wrist)
                p.matrix=delta@p.bone.matrix_local;bpy.context.view_layer.update()
                p.keyframe_insert('location',frame=f);p.keyframe_insert('rotation_quaternion',frame=f);p.keyframe_insert('scale',frame=f)
    print('CUFF_KEYS',action.name,flush=True)
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];s.frame_set(0)
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
print('CLOTH_BINDING_CANDIDATE_COMPLETE',changed,flush=True)
