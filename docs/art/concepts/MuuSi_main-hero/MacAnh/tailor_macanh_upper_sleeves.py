"""Replace the smooth bridge silhouette with gathered fabric and source silk UVs.

The authored body/fan actions and exterior garment UVs stay intact. Work on a copy.
"""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
mat=next(x for x in m.materials if x.name=='MacAnh_ContinuousInnerSleeves')
material_index=list(m.materials).index(mat)
vertices={i for p in m.polygons if p.material_index==material_index for i in p.vertices}
coords={};count=0
for i in vertices:
    v=m.vertices[i]
    names=[o.vertex_groups[g.group].name for g in v.groups if g.weight>.99]
    name=names[0] if len(names)==1 else ''
    if not name.endswith(('UpperArm','LowerArm','Hand')):continue
    bone=r.data.bones[name];axis=(bone.tail_local-bone.head_local).normalized()
    x=axis.cross(Vector((0,1,0))).normalized();y=axis.cross(x).normalized()
    along=(v.co-bone.head_local).dot(axis);t=max(0,min(1,along/bone.length))
    center=bone.head_local+axis*along;radial=v.co-center
    angle=math.atan2(radial.dot(y),radial.dot(x));radius=radial.length
    if name.endswith('Hand'):
        coords[i]=((angle/math.tau)%1,t);continue
    # Small longitudinal pleats, gathered at shoulder/elbow and wrist. Rounded
    # end profiles retained from the watertight source, avoiding hard open rings.
    belly=math.sin(math.pi*t)**2
    base_factor=1.15+.12*belly
    folds=1+.10*math.cos(6*angle+1.2*t)+.06*math.sin(3*math.pi*t+2*angle)*belly
    if radius>1e-6:v.co=center+radial*base_factor*folds
    coords[i]=((angle/math.tau)%1,t);count+=1
uv=m.uv_layers[0]
for p in m.polygons:
    if p.material_index!=material_index:continue
    for li in p.loop_indices:
        i=m.loops[li].vertex_index
        if i in coords:u,v=coords[i]
        else:
            co=m.vertices[i].co;u=(math.atan2(co.y,co.x)/math.tau)%1;v=max(0,min(1,(co.z-.28)/.62))
        # Pale fabric with purple edging from the unchanged Image_0 atlas.
        uv.data[li].uv=(.344+.027*u,.489+.025*v)
mat.diffuse_color=(1,1,1,1);bs=mat.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(1,1,1,1)
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['Image_0']
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.6
metadata=json.loads(o['lod_inner_materials'])
for item in metadata:
    if item['name']==mat.name:
        item.update(colour=[1,1,1,1],roughness=.6,base_texture='Battle_BaseColor.jpg')
    if item['name'].endswith(('RightWristInterior','LeftWristInterior')):
        item['colour']=[.62,.45,.37,1]
    if item['name'].endswith(('RightSleeveLining','LeftSleeveLining')):
        item['colour']=[.20,.105,.32,1]
        mm=bpy.data.materials.get(item['name'])
        if mm:mm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=item['colour']
o['lod_inner_materials']=json.dumps(metadata);m.update()
# Lower robe follows the alternating stride with a small delay. Keep the first
# and last samples identical so both locomotion clips remain seamless loops.
from mathutils import Quaternion
for clip in ('Walk','Run'):
    action=bpy.data.actions['MacAnh_'+clip];r.animation_data.action=action
    first,last=map(int,action.frame_range)
    names=['Robe_'+side+'_Inner_'+str(i) for side in ('Right','Left') for i in (2,3)]
    poses=[]
    for frame in range(first,last+1):
        bpy.context.scene.frame_set(frame);poses.append({n:r.pose.bones[n].rotation_quaternion.copy() for n in names})
    for frame,pose in zip(range(first,last+1),poses):
        phase=(frame-first)/(last-first)*math.tau
        for n,q in pose.items():
            sign=1 if '_Right_' in n else -1
            angle=math.radians(7 if clip=='Run' else 5)*math.sin(phase-.35)*sign
            bone=r.pose.bones[n];bone.rotation_quaternion=q@Quaternion((1,0,0),angle)
            bone.keyframe_insert('rotation_quaternion',frame=frame)
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0)
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
print('TAILORED_SLEEVES_COMPLETE',count,flush=True)
