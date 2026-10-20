"""Trial: round reconstructed internal contact seams, retaining original UV data."""
import bpy,bmesh,sys,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.view_layer.update()
bm=bmesh.new();bm.from_mesh(body.data)
caps={f for f in bm.faces if f.material_index>0}
cap_vertices={v for f in caps for v in f.verts}
boundary={v for v in cap_vertices if any(f not in caps for f in v.link_faces)}
rest={v:v.co.copy() for v in cap_vertices}
adj={v:[e.other_vert(v) for e in v.link_edges if e.other_vert(v) in boundary and any(f in caps for f in e.link_faces) and any(f not in caps for f in e.link_faces)] for v in boundary}
for iteration in range(12):
    updates={}
    for vertex,neighbours in adj.items():
        if len(neighbours)!=2:continue
        p=vertex.co.lerp(sum((v.co for v in neighbours),Vector())/2,.45)
        offset=p-rest[vertex]
        if offset.length>.022:p=rest[vertex]+offset.normalized()*.022
        updates[vertex]=p
    for vertex,point in updates.items():vertex.co=point
interior=cap_vertices-boundary
for iteration in range(25):
    updates={v:sum((e.other_vert(v).co for e in v.link_edges),Vector())/len(v.link_edges) for v in interior if v.link_edges}
    for vertex,point in updates.items():vertex.co=vertex.co.lerp(point,.6)
bm.normal_update();bm.to_mesh(body.data);bm.free();body.data.update()
# Smoothing coordinates invalidates the old spatial weight interpolation at
# those vertices. Re-evaluate on final rest positions, before normalizing/QA.
region=body.data.attributes['GarmentRegion'];groups={g.name:g for g in body.vertex_groups}
def smooth(a,b,value):
    t=max(0,min(1,(value-a)/(b-a)));return t*t*(3-2*t)
parts={}
for face in body.data.polygons:
    part=region.data[face.index].value
    for index in face.vertices:parts[index]=part
for vertex in body.data.vertices:
    part=parts[vertex.index];vx,vy,vz=vertex.co
    chest=max(smooth(.40,.88,vz),smooth(-.08,.26,vy)*smooth(.15,.48,vz));head=smooth(.65,.90,vz)
    if part in (3,4):weights={('Right' if part==3 else 'Left')+'Hand':1}
    elif part in (5,6):weights={('Right' if part==5 else 'Left')+'Foot':1}
    elif part in (1,2):
        side='Right' if part==1 else 'Left';u=max(0,min(2,(.73-vz)/.16));index=min(1,int(u));t=u-index
        weights={'Sleeve_'+side+'_'+str(i):weight*.6 for i,weight in zip((1,2,3),(.1,.8,.1))}
        weights['Sleeve_'+side+'_'+str(index+1)]+=.4*(1-t);weights['Sleeve_'+side+'_'+str(index+2)]+=.4*t
    else:weights={'Pelvis':(1-chest)*(1-head),'Chest':chest*(1-head),'Head':head}
    for group in list(vertex.groups):body.vertex_groups[group.group].remove([vertex.index])
    for name,value in weights.items():
        if value>0:groups[name].add([vertex.index],value,'REPLACE')
if '--region-materials' in sys.argv:
    styles={0:('InnerRobe',(.62,.59,.68,1)),1:('RightSleeveLining',(.065,.025,.12,1)),2:('LeftSleeveLining',(.065,.025,.12,1)),
        3:('RightWristInterior',(.62,.45,.37,1)),4:('LeftWristInterior',(.62,.45,.37,1)),5:('RightBootInterior',(.055,.04,.035,1)),6:('LeftBootInterior',(.055,.04,.035,1))}
    region=body.data.attributes['GarmentRegion'];metadata=[];indices={}
    for part,(suffix,colour) in styles.items():
        material=bpy.data.materials.new('MacAnh_'+label+'_'+suffix);material.use_nodes=True
        shader=material.node_tree.nodes['Principled BSDF'];shader.inputs['Base Color'].default_value=colour;shader.inputs['Roughness'].default_value=.85
        indices[part]=len(body.data.materials);body.data.materials.append(material)
        metadata.append({'name':material.name,'colour':list(colour),'roughness':.85})
    for face in body.data.polygons:
        if face.material_index>0:face.material_index=indices[region.data[face.index].value]
    used=sorted({face.material_index for face in body.data.polygons});remap={old:new for new,old in enumerate(used)}
    materials=[body.data.materials[index] for index in used]
    assignments=[remap[face.material_index] for face in body.data.polygons]
    body.data.materials.clear()
    for material in materials:body.data.materials.append(material)
    for face,index in zip(body.data.polygons,assignments):face.material_index=index
    body['lod_inner_materials']=json.dumps(metadata)
    body['lod_cap_material']=''
else:
    for slot in list(body.material_slots)[1:]:
        material=bpy.data.materials.new('MacAnh_'+label+'_InnerSilk');material.use_nodes=True
        shader=material.node_tree.nodes['Principled BSDF'];shader.inputs['Base Color'].default_value=(.065,.025,.12,1)
        shader.inputs['Roughness'].default_value=.75;slot.material=material
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0)
tag=sys.argv[sys.argv.index('--tag')+1] if '--tag' in sys.argv else 'RoundedSeamsTrial'
destination=root/'Production/LOD'/('MacAnh_'+label+'_'+tag+'.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
print('ROUNDED_INNER_FABRIC_TRIAL_COMPLETE',label,flush=True)
