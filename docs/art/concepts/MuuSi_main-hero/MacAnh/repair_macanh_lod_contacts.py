"""Provisional garment separation on a NEW LOD, never the original sculpture.

The generated source joins hanging sleeves/fingertips/robe at incidental contact.
Separate those contact boundaries and close the hidden interiors. This changes
topology; visual inspection of every seam is mandatory before accepting it.
"""
import bpy,bmesh,sys,json,itertools
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent;rig=bpy.data.objects['MacAnh_Rig']
body=bpy.data.objects[rig['source_body_object']]
assert len(body.data.vertices)<150000,'Only a separately approved LOD may be edited'
rig.animation_data.action=None
for p in rig.pose.bones:p.matrix_basis.identity()
bpy.context.view_layer.update()
for mod in body.modifiers:mod.show_viewport=False;mod.show_render=False
# Clip through polygons at the seam itself. Assigning whole triangles by their
# centres leaves a visible sawtooth boundary, especially at the battle LOD.
mesh=body.data;mesh.calc_loop_triangles();uv=mesh.uv_layers.active.data
group_names=[g.name for g in body.vertex_groups]
directions=[Vector(p).normalized() for p in itertools.product((-1,0,1),repeat=3) if p!=(0,0,0)]
volumes=[]
for sign,part in [(-1,3),(1,4)]:
    centre=Vector((sign*.385,-.15,.595))
    volumes.append((part,[(n,.08+n.dot(centre)) for n in directions]))
for sign,part in [(-1,5),(1,6)]:
    # A plane is positive inside; coefficient notation is d - n.dot(point).
    volumes.append((part,[(Vector((0,0,1)),.18),(Vector((-sign,0,0)),-.035),
        (Vector((sign,0,0)),.19),(Vector((0,1,0)),.08)]))
for sign,part in [(-1,1),(1,2)]:
    volumes.append((part,[(Vector((-sign,0,0)),-.235),(Vector((0,0,1)),.79),(Vector((0,1,0)),.13)]))
def clip(poly,n,d):
    inside=[];outside=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        da=d-n.dot(a[0]);db=d-n.dot(b[0]);ia=da>=-1e-9;ib=db>=-1e-9
        (inside if ia else outside).append(a)
        if ia!=ib:
            t=da/(da-db);point=(a[0].lerp(b[0],t),a[1].lerp(b[1],t))
            inside.append(point);outside.append(point)
    return inside,outside
vertices=[];faces=[];face_uv=[];regions=[];materials=[];lookup={};count=0
def emit(poly,part,material):
    global count
    if len(poly)<3:return
    ids=[]
    for point,_ in poly:
        key=(part,)+tuple(round(v,7) for v in point)
        if key not in lookup:lookup[key]=len(vertices);vertices.append(tuple(point))
        ids.append(lookup[key])
    for i in range(1,len(ids)-1):
        if len({ids[0],ids[i],ids[i+1]})<3:continue
        faces.append((ids[0],ids[i],ids[i+1]));face_uv.append((tuple(poly[0][1]),tuple(poly[i][1]),tuple(poly[i+1][1])))
        regions.append(part);materials.append(material)
for triangle in mesh.loop_triangles:
    pending=[[(mesh.vertices[mesh.loops[l].vertex_index].co.copy(),uv[l].uv.copy()) for l in triangle.loops]]
    for part,planes in volumes:
        next_pending=[]
        for poly in pending:
            if any(all(d-n.dot(p[0]) < -1e-9 for p in poly) for n,d in planes):
                next_pending.append(poly);continue
            remaining=poly
            for n,d in planes:
                if not remaining:break
                remaining,outside=clip(remaining,n,d)
                if len(outside)>=3:next_pending.append(outside)
            if len(remaining)>=3:emit(remaining,part,triangle.material_index);count+=1
        pending=next_pending
    for poly in pending:emit(poly,0,triangle.material_index)
replacement=bpy.data.meshes.new(mesh.name+'_GarmentSeams');replacement.from_pydata(vertices,[],faces)
for material in mesh.materials:replacement.materials.append(material)
uv_layer=replacement.uv_layers.new(name='UVMap')
for face,coords,material in zip(replacement.polygons,face_uv,materials):
    face.material_index=material;face.use_smooth=True
    for loop,value in zip(face.loop_indices,coords):uv_layer.data[loop].uv=value
body.data=replacement
body.vertex_groups.clear()
for name in group_names:body.vertex_groups.new(name=name)
bm=bmesh.new();bm.from_mesh(replacement);layer=bm.faces.layers.int.new('GarmentRegion')
for face,part in zip(bm.faces,regions):face[layer]=part
inner=bpy.data.materials.new('MacAnh hidden garment lining');inner.diffuse_color=(.025,.012,.045,1)
inner.use_nodes=True;bsdf=inner.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(.025,.012,.045,1);bsdf.inputs['Roughness'].default_value=.9
body.data.materials.append(inner);material_index=len(body.data.materials)-1
caps=[]
for part in range(7):
    boundary=[e for e in bm.edges if len(e.link_faces)==1 and e.link_faces[0][layer]==part]
    result=bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
    for face in result['faces']:face[layer]=part;face.material_index=material_index;caps.append(face)
bm.verts.ensure_lookup_table();bm.verts.index_update()
deform=bm.verts.layers.deform.verify()
groups={g.name:g.index for g in body.vertex_groups}
def smooth(a,b,x):
    q=max(0,min(1,(x-a)/(b-a)));return q*q*(3-2*q)
for vertex in bm.verts:
    parts={f[layer] for f in vertex.link_faces}
    assert len(parts)==1
    part=next(iter(parts));x,y,z=vertex.co
    weights={}
    if part in (3,4):weights[('Right' if part==3 else 'Left')+'Hand']=1
    elif part in (5,6):weights[('Right' if part==5 else 'Left')+'Foot']=1
    elif part in (1,2):
        side='Right' if part==1 else 'Left'
        u=max(0,min(2,(.73-z)/.16));i=min(1,int(u));t=u-i
        weights['Sleeve_'+side+'_'+str(i+1)]=1-t
        weights['Sleeve_'+side+'_'+str(i+2)]=t
    else:
        chest=smooth(.40,.88,z);head=max(smooth(.65,.90,z),smooth(.04,.17,y)*smooth(.25,.48,z))
        weights={'Pelvis':(1-chest)*(1-head),'Chest':chest*(1-head),'Head':head}
    vertex[deform].clear()
    for name,weight in weights.items():
        if weight>0:vertex[deform][groups[name]]=weight
bm.normal_update();bm.to_mesh(body.data);bm.free();body.data.update()
for mod in body.modifiers:
    if mod.type=='ARMATURE':mod.show_viewport=True;mod.show_render=True;mod.use_deform_preserve_volume=False
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0)
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
destination=root/'Production/LOD'/('MacAnh_'+label+'_SeparatedTrial.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
report={'status':'trial; inspect all new seams/caps before approval','cut_edges':count,
    'interior_caps':len(caps),'vertices':len(body.data.vertices),'output':str(destination)}
(root/'Production/LOD'/('Separation_'+label+'.json')).write_text(json.dumps(report,indent=2))
print('GARMENT_SEPARATION_TRIAL_COMPLETE',report,flush=True)
