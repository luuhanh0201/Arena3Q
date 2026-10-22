"""Add overlapping hidden arm sleeves reaching the palms.

Keeps exterior faces, UVs, action curves and fan intact. Writes a separate candidate.
"""
import bpy, bmesh, sys, json, math
from pathlib import Path
from mathutils import Vector
root = Path(__file__).resolve().parent
rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects[rig['source_body_object']]
action = rig.animation_data.action
rig.animation_data.action = None
for bone in rig.pose.bones: bone.matrix_basis.identity()
bpy.context.view_layer.update()
bm = bmesh.new(); bm.from_mesh(body.data)
deform = bm.verts.layers.deform.verify()
region = bm.faces.layers.int.get('GarmentRegion')
groups = {g.name:g.index for g in body.vertex_groups}
arm_groups = {groups[s+p] for s in ('Right','Left') for p in ('UpperArm','LowerArm')}
lining = next(i for i,m in enumerate(body.data.materials) if m.name.endswith('_InnerRobe'))
remove = [f for f in bm.faces if f.material_index == lining]
bmesh.ops.delete(bm, geom=remove, context='FACES')
material = bpy.data.materials.new('MacAnh_ContinuousInnerSleeves')
material.diffuse_color = (.16,.12,.24,1)
material.use_nodes = True
shader = material.node_tree.nodes.get('Principled BSDF')
shader.inputs['Base Color'].default_value = material.diffuse_color
shader.inputs['Roughness'].default_value = .85
body.data.materials.append(material); mat = len(body.data.materials)-1
added = 0
# Old contact-hole caps stretch across the empty spaces left by the separated
# arms. Replace them with a fitted closed torso, not wide sheets pinned to pelvis.
torso=[]
shape=[(.28,.12,.07),(.40,.15,.085),(.56,.16,.095),(.72,.18,.10),(.79,.20,.105),(.84,.15,.09),(.90,.075,.065)]
for section,(za,ra,da) in enumerate(shape[:-1]):
    zb,rb,db=shape[section+1]
    steps=math.ceil((zb-za)/.025)
    for k in range(steps):
        t=k/steps; z=za+(zb-za)*t; rx=ra+(rb-ra)*t; ry=da+(db-da)*t
        chest=max(0,min(1,(z-.40)/.48)); chest=chest*chest*(3-2*chest)
        weights={'Pelvis':1-chest,'Chest':chest}
        ring=[]
        for i in range(32):
            v=bm.verts.new((rx*math.cos(i*math.tau/32),-.005+ry*math.sin(i*math.tau/32),z))
            for name,w in weights.items():
                if w>0:v[deform][groups[name]]=w
            ring.append(v)
        torso.append(ring)
for a,b in zip(torso,torso[1:]):
    for i in range(32):
        j=(i+1)%32
        for vs in ((a[i],a[j],b[j]),(a[i],b[j],b[i])):
            f=bm.faces.new(vs);f.material_index=mat;f.smooth=True
            if region:f[region]=0
            added+=1
for side in ('Right','Left'):
    upper, lower, hand = [rig.data.bones[side+p] for p in ('UpperArm','LowerArm','Hand')]
    # Rounded overlapping ends cover the joints through their full articulation.
    # The source arm rolls can oppose each other; linear blending collapses a tube.
    points = [upper.head_local,lower.head_local,hand.head_local,hand.head_local.lerp(hand.tail_local,.12)]
    radii = [.037,.029,.016,.012]
    for segment in range(3):
        rings = []
        axis=(points[segment+1]-points[segment]).normalized()
        x=axis.cross(Vector((0,1,0))).normalized(); y=axis.cross(x).normalized()
        profiles=[]
        for k in range(5):
            angle=-math.pi/2+k*math.pi/8
            profiles.append((points[segment]+axis*radii[segment]*math.sin(angle),max(.0001,radii[segment]*math.cos(angle))))
        for k in range(1,5):
            t=k/4
            profiles.append((points[segment].lerp(points[segment+1],t),radii[segment]*(1-t)+radii[segment+1]*t))
        for k in range(1,5):
            angle=k*math.pi/8
            profiles.append((points[segment+1]+axis*radii[segment+1]*math.sin(angle),max(.0001,radii[segment+1]*math.cos(angle))))
        for center,radius in profiles:
            weights={side+('UpperArm','LowerArm','Hand')[segment]:1.0}
            ring=[]
            for i in range(20):
                v=bm.verts.new(center+radius*(x*math.cos(i*math.tau/20)+y*math.sin(i*math.tau/20)))
                for name,w in weights.items(): v[deform][groups[name]]=w
                ring.append(v)
            rings.append(ring)
        for a,b in zip(rings,rings[1:]):
            for i in range(20):
                j=(i+1)%20
                for vs in ((a[i],a[j],b[j]),(a[i],b[j],b[i])):
                    f=bm.faces.new(vs); f.material_index=mat; f.smooth=True
                    if region: f[region]=0
                    added+=1
bm.normal_update(); bm.to_mesh(body.data); bm.free(); body.data.update()
materials=json.loads(body.get('lod_inner_materials','[]'))
materials.append(dict(name=material.name,colour=list(material.diffuse_color),roughness=.85))
body['lod_inner_materials']=json.dumps(materials)
rig.animation_data.action=action
bpy.context.scene.frame_set(0)
destination=Path(sys.argv[sys.argv.index('--output')+1]).resolve()
assert destination != Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
print('ARM_BRIDGES_COMPLETE',dict(removed=len(remove),added=added,output=str(destination)),flush=True)
