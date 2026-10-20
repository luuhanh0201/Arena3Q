"""Trial: replace exposed contact-hole caps with a smooth anatomical undergarment.

Original exterior polygons, UVs, textures and rig stay intact. New hidden lining
has no source UV to preserve. Trial must pass surface/contact and visual QA.
"""
import bpy,bmesh,sys,math,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
label=sys.argv[sys.argv.index('--lod')+1]
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.view_layer.update()
bm=bmesh.new();bm.from_mesh(body.data);region=bm.faces.layers.int.get('GarmentRegion')
deform=bm.verts.layers.deform.verify();groups={g.name:g.index for g in body.vertex_groups}
lining=next(i for i,m in enumerate(body.data.materials) if m.name.endswith('_InnerRobe'))
remove=[face for face in bm.faces if face[region]==0 and face.material_index==lining]
removed=len(remove);bmesh.ops.delete(bm,geom=remove,context='FACES')
def vertex(point,weights):
    v=bm.verts.new(point)
    for name,weight in weights.items():
        if weight>0:v[deform][groups[name]]=weight
    return v
def surface(rings):
    for a,b in zip(rings,rings[1:]):
        for i in range(len(a)):
            j=(i+1)%len(a)
            for vs in [(a[i],a[j],b[j]),(a[i],b[j],b[i])]:
                f=bm.faces.new(vs);f.material_index=lining;f[region]=0;f.smooth=True
def smooth(a,b,value):
    t=max(0,min(1,(value-a)/(b-a)));return t*t*(3-2*t)
rings=[]
for z,rx,ry in [(.34,.14,.08),(.40,.15,.085),(.48,.155,.09),(.56,.16,.095),(.64,.17,.10),(.72,.18,.10),(.79,.20,.105),(.84,.15,.09),(.88,.085,.07)]:
    chest=smooth(.40,.88,z);head=smooth(.65,.90,z)
    weights={'Pelvis':(1-chest)*(1-head),'Chest':chest*(1-head),'Head':head}
    rings.append([vertex((rx*math.cos(i*math.tau/24),-.005+ry*math.sin(i*math.tau/24),z),weights) for i in range(24)])
surface(rings)
for side in ('Right','Left'):
    for part in ('UpperArm','LowerArm'):
        bone=rig.data.bones[side+part];axis=(bone.tail_local-bone.head_local).normalized()
        x=axis.cross(Vector((0,0,1))).normalized();y=axis.cross(x).normalized()
        # Overlapping small shirt sleeves cover the reconstructed armhole, below
        # the original ornate outer garment. Stop before the palm/grip opening.
        start=bone.head_local-axis*.012
        end=bone.tail_local+axis*(.012 if part=='UpperArm' else -.035)
        rings=[]
        for k in range(5):
            t=k/4;center=start.lerp(end,t)
            radius=(.037*(1-t)+.030*t) if part=='UpperArm' else (.031*(1-t)+.025*t)
            rings.append([vertex(center+radius*(x*math.cos(i*math.tau/16)+y*math.sin(i*math.tau/16)),{side+part:1}) for i in range(16)])
        surface(rings)
bm.normal_update();bm.to_mesh(body.data);bm.free();body.data.update()
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0)
path=root/'Production/LOD'/('MacAnh_'+label+'_UndergarmentTrial.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
print('ANATOMICAL_UNDERLAY_TRIAL_SAVED',label,'removed_contact_cap_faces',removed,flush=True)
