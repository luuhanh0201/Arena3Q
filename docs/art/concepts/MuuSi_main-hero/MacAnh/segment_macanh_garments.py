"""Trace incidental garment contacts along surface folds, not coordinate cuts.

Experimental retopology on approved LOD copies only. Reports and six-view
renders must be accepted independently from the deformation measurements.
"""
import bpy,bmesh,sys,json
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
detail=float(sys.argv[sys.argv.index('--sleeve-detail')+1]) if '--sleeve-detail' in sys.argv else .4
complete_sleeve='--complete-sleeve' in sys.argv
limbs_only='--limbs-only' in sys.argv
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import maximum_flow,breadth_first_order
from PIL import Image
from mathutils.bvhtree import BVHTree
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']];mesh=body.data
assert len(mesh.vertices)<150000
rig.animation_data.action=None
for p in rig.pose.bones:p.matrix_basis.identity()
for modifier in body.modifiers:modifier.show_viewport=False;modifier.show_render=False
bpy.context.view_layer.update();mesh.calc_loop_triangles()
assert len(mesh.loop_triangles)==len(mesh.polygons)
co=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',co);co=co.reshape(-1,3)
faces=np.array([tri.vertices[:] for tri in mesh.loop_triangles],np.int32)
loops=np.array([tri.loops[:] for tri in mesh.loop_triangles],np.int32)
uv=np.empty(len(mesh.loops)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);uv=uv.reshape(-1,2)
base=Image.open(project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/Image_0.jpg').resize((2048,2048))
pixels=np.asarray(base).astype(np.float32)/255
xy=np.clip(uv[loops],0,.999999)*2048
rgb=pixels[2047-xy[:,:,1].astype(int),xy[:,:,0].astype(int),:3].mean(axis=1)
centres=co[faces].mean(axis=1);x,y,z=centres.T
cross=np.cross(co[faces[:,1]]-co[faces[:,0]],co[faces[:,2]]-co[faces[:,0]])
area=np.linalg.norm(cross,axis=1)/2;normals=cross/np.maximum(2*area[:,None],1e-12)
raw=np.concatenate((faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]]));raw.sort(axis=1)
owner=np.tile(np.arange(len(faces)),3);order=np.lexsort(raw.T[::-1]);raw=raw[order];owner=owner[order]
equal=np.all(raw[1:]==raw[:-1],axis=1);first=np.flatnonzero(equal)
a=owner[first];b=owner[first+1]
length=np.linalg.norm(co[raw[first,0]]-co[raw[first,1]],axis=1)
fold=np.clip((normals[a]*normals[b]).sum(axis=1),-1,1)
colour=np.linalg.norm(rgb[a]-rgb[b],axis=1)
capacity=length*(.025+.975*np.exp(-8*(1-fold)))*(.12+.88*np.exp(-6*colour))
definitions={item['name']:item for item in json.loads((root/'Production/MacAnh_RigBuild.json').read_text())['bone_definitions']}
def distance(name):
    bone=definitions[name];start=np.array(bone['head']);delta=np.array(bone['tail'])-start
    t=np.clip(((centres-start)*delta).sum(axis=1)/(delta@delta),0,1)
    return np.linalg.norm(centres-start-t[:,None]*delta,axis=1)
def smooth(lo,hi,value):
    t=np.clip((value-lo)/(hi-lo),0,1);return t*t*(3-2*t)
labels=np.zeros(len(faces),np.int32)
def segment(part,probability,inside,outside):
    n=len(faces);probability=np.clip(probability,1e-4,1-1e-4)
    source_cost=-np.log(1-probability)*area*25
    sink_cost=-np.log(probability)*area*25
    source_cost[inside]=100;sink_cost[inside]=0
    outside=outside|(labels!=0);source_cost[outside]=0;sink_cost[outside]=100
    source=np.full(n,n);sink=np.full(n,n+1);nodes=np.arange(n)
    rows=np.r_[a,b,source,nodes];cols=np.r_[b,a,nodes,sink]
    costs=np.r_[capacity,capacity,source_cost,sink_cost]
    graph=coo_matrix((np.maximum(0,np.rint(costs*1000000)).astype(np.int64),(rows,cols)),shape=(n+2,n+2)).tocsr()
    flow=maximum_flow(graph,n,n+1)
    residual=graph-flow.flow;residual.data=(residual.data>0).astype(np.int64);residual.eliminate_zeros()
    reachable=breadth_first_order(residual,n,directed=True,return_predecessors=False)
    selected=reachable[reachable<n];labels[selected]=part
    print('GARMENT_SURFACE_CUT',part,'faces',len(selected),'hard_seeds',int(inside.sum()),flush=True)
for side,sign,part in [('Right',-1,3),('Left',1,4)]:
    palm=distance(side+'Hand')
    fingers=np.stack([distance(side+digit+str(i)) for digit in ('Thumb','Index','Middle','Ring','Little') for i in (1,2,3)],axis=1).min(axis=1)
    skin=np.clip((rgb[:,0]-rgb[:,2])/.09+.5,.1,1)
    probability=(1-smooth(.015,.065,fingers))*(.45+.55*skin)
    probability=np.maximum(probability,(1-smooth(.025,.055,palm))*.95)
    inside=(fingers<.012)|(palm<.018)
    outside=(palm>.12)|(z<.515)|(z>.69)|(x*sign<.30)|(y>-.045)
    segment(part,probability,inside&~outside,outside)
for side,sign,part in [('Right',-1,5),('Left',1,6)]:
    d=distance(side+'Foot');probability=(1-smooth(.025,.085,d))*(1-smooth(.12,.20,z))
    outside=(z>.21)|(x*sign<.035)|(x*sign>.21)|(y>.08)
    segment(part,probability,(d<.025)&(z<.12)&~outside,outside)
for side,sign,part in ([] if limbs_only else [('Right',-1,1),('Left',1,2)]):
    sleeve=np.stack([distance('Sleeve_'+side+'_'+str(i)) for i in (1,2,3)],axis=1).min(axis=1)
    robe=np.stack([distance('Robe_'+side+'_'+row+'_'+str(i)) for row in ('Inner','Outer') for i in (1,2,3)]+[distance('Chest')],axis=1).min(axis=1)
    white=np.clip((rgb.min(axis=1)-.45)/.25,0,1)
    probability=1/(1+np.exp(np.clip((sleeve-robe)*30-white*1.3,-30,30)))
    probability*=smooth(.18,.25,x*sign)*(1-smooth(.76,.86,z))*smooth(.25,.34,z)
    outside=(x*sign<.19)|(z<.28)|(z>.86)|(y>.25)|((y>.06)&(rgb.max(axis=1)<.43))
    inside=(sleeve<.035)&(x*sign>.27)&(y<-.055)&(z>.37)&(z<.72)
    if complete_sleeve:
        # The long cuff extends below the sleeve-chain endpoints. Do not force
        # those real external folds back into the robe through a z=.28 cutoff.
        probability=np.maximum(probability,.85*smooth(.23,.30,x*sign)*(1-smooth(-.08,.04,y))*smooth(.10,.25,z)*(1-smooth(.65,.78,z)))
        outside=(x*sign<.19)|(z<.10)|(z>.86)|(y>.25)|((y>.09)&(rgb.max(axis=1)<.43))
        inside|=(x*sign>.32)&(y<-.08)&(z>.18)&(z<.55)
        if '--wide-sleeve' in sys.argv:
            probability=np.maximum(probability,.99*smooth(.23,.34,x*sign)*(1-smooth(.12,.27,y))*smooth(.08,.18,z)*(1-smooth(.70,.84,z)))
            outside=(x*sign<.19)|(z<.08)|(z>.86)|(y>.29)
            inside|=(x*sign>.36)&(y<.15)&(z>.14)&(z<.68)
    segment(part,probability,inside&~outside,outside)
group_names=[g.name for g in body.vertex_groups]
new_co=[];new_faces=[];lookup={}
for face,part in zip(faces,labels):
    new_face=[]
    for index in face:
        key=(int(part),int(index))
        if key not in lookup:lookup[key]=len(new_co);new_co.append(co[index])
        new_face.append(lookup[key])
    new_faces.append(new_face)
replacement=bpy.data.meshes.new(mesh.name+'_SurfaceGarments');replacement.from_pydata(new_co,[],new_faces)
for material in mesh.materials:replacement.materials.append(material)
new_uv=replacement.uv_layers.new(name='UVMap')
for polygon,old_loops,old_polygon in zip(replacement.polygons,loops,mesh.polygons):
    polygon.use_smooth=old_polygon.use_smooth;polygon.material_index=old_polygon.material_index
    for new_loop,old_loop in zip(polygon.loop_indices,old_loops):new_uv.data[new_loop].uv=uv[old_loop]
body.data=replacement;mesh=replacement
body.vertex_groups.clear()
for name in group_names:body.vertex_groups.new(name=name)
bm=bmesh.new();bm.from_mesh(mesh);layer=bm.faces.layers.int.new('GarmentRegion')
for face,part in zip(bm.faces,labels):face[layer]=int(part)
cut=[e for e in bm.edges if e.is_boundary]
inner=bpy.data.materials.new('MacAnh hidden fabric lining');inner.use_nodes=True
inner.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.025,.012,.045,1)
body.data.materials.append(inner);lining=len(body.data.materials)-1;caps=0
cap_faces=[]
for part in range(7):
    boundary=[e for e in bm.edges if e.is_boundary and e.link_faces[0][layer]==part]
    result=bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
    for face in result['faces']:face[layer]=part;face.material_index=lining;caps+=1;cap_faces.append(face)
# Reconstruct hidden underlayers against the original sculpture. Flat hole-fill
# polygons otherwise become visible dark wedges when a sleeve moves away.
original_vertices=set(bm.verts)
triangles=bmesh.ops.triangulate(bm,faces=cap_faces)['faces'];cap_set=set(triangles)
interior=[edge for edge in bm.edges if len(edge.link_faces)==2 and all(face in cap_set for face in edge.link_faces)]
if interior:bmesh.ops.subdivide_edges(bm,edges=interior,cuts=3,use_grid_fill=True)
new_vertices=[vertex for vertex in bm.verts if vertex not in original_vertices]
if new_vertices:
    objects_before=set(bpy.data.objects);actions_before=set(bpy.data.actions)
    with bpy.data.libraries.load(str(root.parent/'muu-si.blend'),link=False) as (available,loaded):loaded.objects=[rig['source_body_object']]
    high=loaded.objects[0];bpy.context.scene.collection.objects.link(high);high.parent=None;high.matrix_world.identity();high.modifiers.clear()
    bpy.context.view_layer.update();tree=BVHTree.FromObject(high,bpy.context.evaluated_depsgraph_get())
    for vertex in new_vertices:
        point,normal,index,distance=tree.find_nearest(vertex.co)
        if point is not None and distance<.2:vertex.co=point-normal*.003
    for obj in list(bpy.data.objects):
        if obj not in objects_before:bpy.data.objects.remove(obj,do_unlink=True)
    for action in list(bpy.data.actions):
        if action not in actions_before:bpy.data.actions.remove(action)
deform=bm.verts.layers.deform.verify();groups={g.name:g.index for g in body.vertex_groups}
def scalar_smooth(a,b,value):return float(smooth(a,b,np.array(value)))
for vertex in bm.verts:
    parts={f[layer] for f in vertex.link_faces};assert len(parts)==1
    part=next(iter(parts));vx,vy,vz=vertex.co
    chest=max(scalar_smooth(.40,.88,vz),scalar_smooth(-.08,.26,vy)*scalar_smooth(.15,.48,vz))
    head=scalar_smooth(.65,.90,vz)
    torso={'Pelvis':(1-chest)*(1-head),'Chest':chest*(1-head),'Head':head}
    if part in (3,4):weights={('Right' if part==3 else 'Left')+'Hand':1}
    elif part in (5,6):weights={('Right' if part==5 else 'Left')+'Foot':1}
    elif part in (1,2):
        side='Right' if part==1 else 'Left'
        u=max(0,min(2,(.73-vz)/.16));index=min(1,int(u));t=u-index
        weights={'Sleeve_'+side+'_'+str(i):weight*(1-detail) for i,weight in zip((1,2,3),(.1,.8,.1))}
        weights['Sleeve_'+side+'_'+str(index+1)]+=detail*(1-t)
        weights['Sleeve_'+side+'_'+str(index+2)]+=detail*t
    else:
        weights=torso
        if limbs_only:
            # Keep the continuous exterior cloth intact. Only the actual fused
            # palm/foot contacts are separated; garment support has no cut edge.
            side='Right' if vx<0 else 'Left'
            amount=.9*scalar_smooth(0,.45,abs(vx))*scalar_smooth(.12,.38,vz)*(1-scalar_smooth(.69,.80,vz))*(1-scalar_smooth(.08,.30,vy))
            weights={name:value*(1-amount) for name,value in torso.items()}
            weights['Sleeve_'+side+'_2']=amount
    vertex[deform].clear();total=sum(weights.values())
    for name,weight in weights.items():
        if weight>0:vertex[deform][groups[name]]=weight/total
bm.normal_update();bm.to_mesh(mesh);bm.free();mesh.update()
for modifier in body.modifiers:
    if modifier.type=='ARMATURE':modifier.show_viewport=True;modifier.show_render=True;modifier.use_deform_preserve_volume=False
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0)
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
destination=root/'Production/LOD'/('MacAnh_'+label+('_WideSleeveTrial.blend' if '--wide-sleeve' in sys.argv else '_ContinuousClothTrial.blend' if limbs_only else '_CompleteSleeveTrial.blend' if complete_sleeve else '_SurfaceCutTrial.blend'))
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
(root/'Production/LOD'/('SurfaceCut_'+label+'.json')).write_text(json.dumps({'status':'trial; visual QA pending',
    'face_regions':{str(i):int((labels==i).sum()) for i in range(7)},'cut_edges':len(cut),'caps':caps,'vertices':len(mesh.vertices),'sleeve_detail':detail},indent=2))
print('MACANH_SURFACE_CUT_TRIAL_COMPLETE',flush=True)
