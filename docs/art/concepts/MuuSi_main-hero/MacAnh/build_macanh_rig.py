"""User approved rigging the MAIN muu-si.blend after a byte-identical backup.
Run on Production/Backups/muu-si_before_rig.blend. No topology/UV/material edits.
Build a bespoke measured skeleton, proxy heat weights and semantic cloth/hair regions.
"""
import bpy, os, sys, json, math, hashlib, gc
import numpy as np
from mathutils import Vector
from mathutils.kdtree import KDTree
sys.stdout.reconfigure(line_buffering=True)
ROOT=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(ROOT,'Production'); SOURCE=os.path.abspath(os.path.join(ROOT,'..','muu-si.blend'))
baseline=json.load(open(os.path.join(OUT,'MacAnh_Preflight.json'),encoding='utf-8'))
body=next(o for o in bpy.data.objects if o.type=='MESH')
assert not any(o.type=='ARMATURE' for o in bpy.data.objects),'Run on the BEFORE-RIG backup, not an already rigged file.'
assert len(body.data.vertices)==1476550 and len(body.data.polygons)==2953808
assert max(abs(s-1) for s in body.scale)<1e-6
mesh=body.data
co=np.empty(len(mesh.vertices)*3,dtype=np.float32); mesh.vertices.foreach_get('co',co); co=co.reshape(-1,3)
assert hashlib.sha256(co.tobytes()).hexdigest()==baseline['objects'][0]['mesh']['coordinates_sha256']

armature=bpy.data.armatures.new('MacAnh_Skeleton')
rig=bpy.data.objects.new('MacAnh_Rig',armature); bpy.context.scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
defs=[]
def bone(name,head,tail,parent=None,deform=True):
    b=armature.edit_bones.new(name); b.head=head; b.tail=tail; b.use_deform=deform
    if parent: b.parent=armature.edit_bones[parent]
    b.use_connect=False
    defs.append({'name':name,'head':list(head),'tail':list(tail),'parent':parent,'deform':deform})
    return b
bone('Root',(0,0,0),(0,0,.12),deform=False)
bone('Pelvis',(0,.035,.43),(0,.035,.56),'Root')
bone('Spine',(0,.035,.56),(0,.015,.77),'Pelvis')
bone('Chest',(0,.015,.77),(0,.005,.92),'Spine')
bone('Neck',(0,.005,.92),(0,.005,1.015),'Chest')
bone('Head',(0,.005,1.015),(0,.015,1.46),'Neck')
digits={
 'Thumb':[(.354,-.178,.614),(.342,-.171,.597),(.331,-.163,.579),(.329,-.159,.568)],
 'Index':[(.388,-.185,.602),(.410,-.184,.580),(.425,-.182,.565),(.432,-.180,.557)],
 'Middle':[(.390,-.153,.602),(.413,-.152,.578),(.431,-.150,.559),(.438,-.149,.551)],
 'Ring':[(.385,-.124,.605),(.406,-.123,.581),(.421,-.121,.565),(.428,-.120,.558)],
 'Little':[(.376,-.098,.611),(.395,-.095,.592),(.409,-.092,.578),(.414,-.090,.571)]}
for side,sign in [('Right',-1),('Left',1)]:
    s=lambda p:(p[0]*sign,p[1],p[2])
    bone(side+'Shoulder',s((.055,.005,.895)),s((.190,-.025,.855)),'Chest')
    bone(side+'UpperArm',s((.190,-.025,.855)),s((.284,-.095,.744)),side+'Shoulder')
    bone(side+'LowerArm',s((.284,-.095,.744)),s((.354,-.169,.632)),side+'UpperArm')
    bone(side+'Hand',s((.354,-.169,.632)),s((.404,-.158,.580)),side+'LowerArm')
    for digit,points in digits.items():
        for i in range(3): bone(side+digit+str(i+1),s(points[i]),s(points[i+1]),side+'Hand' if i==0 else side+digit+str(i))
    bone(side+'Thigh',s((.105,.025,.46)),s((.108,.012,.25)),'Pelvis')
    bone(side+'Shin',s((.108,.012,.25)),s((.108,-.005,.08)),side+'Thigh')
    bone(side+'Foot',s((.108,-.005,.08)),s((.108,-.124,.045)),side+'Shin')
    bone(side+'Toe',s((.108,-.124,.045)),s((.108,-.170,.040)),side+'Foot')
    # These sleeve chains follow the measured wide sleeve, not the biceps skin.
    for i,(h,t) in enumerate([((.237,-.04,.78),(.31,-.075,.64)),((.31,-.075,.64),(.35,-.09,.49)),((.35,-.09,.49),(.37,-.07,.34))]):
        bone('Sleeve_'+side+'_'+str(i+1),s(h),s(t),side+'UpperArm' if i==0 else 'Sleeve_'+side+'_'+str(i))
    for row,x in [('Outer',.29),('Inner',.12)]:
        parent='Pelvis'
        for i in range(3):
            z=.57-i*.16
            name='Robe_'+side+'_'+row+'_'+str(i+1)
            bone(name,s((x,.025,z)),s((x+.035,.025,z-.16)),parent); parent=name
    parent='Head'
    for i in range(4):
        z=1.22-i*.19
        name='Hair_'+side+'_'+str(i+1)
        bone(name,s((.17+i*.018,.19+i*.018,z)),s((.188+i*.018,.208+i*.018,z-.19)),parent); parent=name
    parent='Head'
    for i in range(3):
        name='Ribbon_'+side+'_'+str(i+1)
        bone(name,s((.32+i*.02,.12,1.24-i*.17)),s((.34+i*.02,.12,1.07-i*.17)),parent); parent=name

# Socket rest transforms are explicit; no existing names/rest pose are overwritten.
bone('Socket_Hand_R',(-.385,-.171,.603),(-.385,-.171,.643),'RightHand',False)
bone('Socket_Hand_L',(.385,-.171,.603),(.385,-.171,.643),'LeftHand',False)
bone('Socket_Cast',(0,-.22,.88),(0,-.22,.92),'Chest',False)
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True
for p in rig.pose.bones:
    p.rotation_mode='QUATERNION'; p.scale=(1,1,1)

# A closed, temporary voxel proxy solves heat weights; the actual mesh stays intact.
proxy=body.copy(); proxy.data=body.data.copy(); proxy.name='Temporary_MacAnh_HeatProxy'
bpy.context.scene.collection.objects.link(proxy)
bpy.ops.object.select_all(action='DESELECT'); proxy.select_set(True); bpy.context.view_layer.objects.active=proxy
remesh=proxy.modifiers.new('Closed heat proxy','REMESH'); remesh.mode='VOXEL'; remesh.voxel_size=.012
bpy.ops.object.modifier_apply(modifier=remesh.name)
print('MACANH_PROXY',len(proxy.data.vertices),flush=True)
deform_flags={b.name:b.use_deform for b in armature.bones}
core_names=[]
for b in armature.bones:
    b.use_deform=b.use_deform and not (b.name.startswith(('Hair_','Ribbon_','Robe_','Sleeve_')) or any(d in b.name for d in digits))
    if b.use_deform: core_names.append(b.name)
for obj in (proxy,rig): obj.scale=(10,10,10)
proxy.select_set(True); rig.select_set(True); bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for obj in (proxy,rig): obj.scale=(1,1,1)
bpy.context.view_layer.update()
valid=[v for v in proxy.data.vertices if sum(g.weight for g in v.groups)>.00001]
print('MACANH_PROXY_VALID',len(valid),len(proxy.data.vertices),flush=True)
names=[b.name for b in armature.bones if deform_flags[b.name]]
name_to_id={name:i for i,name in enumerate(names)}
proxy_indices=np.zeros((len(proxy.data.vertices),4),dtype=np.int16)
proxy_weights=np.zeros((len(proxy.data.vertices),4),dtype=np.float32)
solver='Blender bone heat on voxel proxy'
if len(valid)/len(proxy.data.vertices)>.998:
    for vertex in valid:
        values=sorted([(name_to_id[proxy.vertex_groups[g.group].name],g.weight) for g in vertex.groups if proxy.vertex_groups[g.group].name in name_to_id],key=lambda x:-x[1])[:4]
        total=sum(w for _,w in values)
        for j,(index,weight) in enumerate(values): proxy_indices[vertex.index,j]=index; proxy_weights[vertex.index,j]=weight/total
else:
    # The original robe/hair form includes disconnected closed islands. Blender's
    # heat solver rejects those. Solve a seeded graph diffusion on the TEMP proxy,
    # then refine fingers/cloth/hair by their measured semantic regions below.
    solver='Seeded surface-graph diffusion on voxel proxy (bone heat failed)'
    valid=list(proxy.data.vertices)
    pc=np.empty(len(valid)*3,dtype=np.float32); proxy.data.vertices.foreach_get('co',pc); pc=pc.reshape(-1,3)
    distances=[]
    for name in core_names:
        b=armature.bones[name]; a=np.array(b.head_local,dtype=np.float32); d=np.array(b.tail_local-b.head_local,dtype=np.float32)
        t=np.clip(((pc-a)*d).sum(axis=1)/float(d@d),0,1)
        distances.append(np.sqrt(((pc-a-t[:,None]*d)**2).sum(axis=1)+1e-10))
    distance=np.stack(distances,axis=1)
    smooth=np.exp(-35*(distance-distance.min(axis=1,keepdims=True)))
    smooth/=smooth.sum(axis=1,keepdims=True)
    closest=distance.argmin(axis=1); fixed=distance.min(axis=1)<.023
    initial=smooth.copy()
    edge=np.empty(len(proxy.data.edges)*2,dtype=np.int32); proxy.data.edges.foreach_get('vertices',edge); edge=edge.reshape(-1,2)
    start=np.r_[edge[:,0],edge[:,1]]; end=np.r_[edge[:,1],edge[:,0]]
    # Do not diffuse across narrow accidental cloth/hair contacts between limbs.
    near_family=np.array([0 if n in ('Pelvis','Spine','Chest','Neck','Head') else (1 if n.startswith('Right') else 2) for n in core_names])[closest]
    keep=(near_family[start]==near_family[end])|(distance.min(axis=1)[start]>.12)
    start=start[keep]; end=end[keep]
    degree=np.bincount(start,minlength=len(pc)).astype(np.float32)
    for iteration in range(24):
        updated=np.zeros_like(smooth)
        for j in range(smooth.shape[1]): updated[:,j]=np.bincount(start,weights=smooth[end,j],minlength=len(pc))
        updated=(updated+smooth)/(degree[:,None]+1)
        smooth=.84*updated+.16*initial
        smooth[fixed]=initial[fixed]
    best=np.argsort(smooth,axis=1)[:,-4:][:,::-1]
    proxy_indices[:]=np.array([name_to_id[n] for n in core_names],dtype=np.int16)[best]
    proxy_weights[:]=np.take_along_axis(smooth,best,axis=1)
    proxy_weights/=proxy_weights.sum(axis=1,keepdims=True)
    del pc,distance,distances,smooth,initial,updated,start,end,edge; gc.collect()
    print('MACANH_PROXY_DIFFUSION_SOLVED',len(valid),flush=True)
tree=KDTree(len(valid))
for v in valid: tree.insert(v.co,v.index)
tree.balance()
nearest=np.fromiter((tree.find(Vector(p))[1] for p in co),dtype=np.int32,count=len(co))
indices=proxy_indices[nearest].copy(); weights=proxy_weights[nearest].copy()
proxy_mesh=proxy.data; bpy.data.objects.remove(proxy,do_unlink=True); bpy.data.meshes.remove(proxy_mesh)
for b in armature.bones: b.use_deform=deform_flags[b.name]
del nearest,proxy_indices,proxy_weights,valid,tree; gc.collect()
print('MACANH_HEAT_TRANSFERRED',len(co),flush=True)

# Read a representative UV per vertex. Packed source images are sampled, never edited.
loops=np.empty(len(mesh.loops),dtype=np.int32); mesh.loops.foreach_get('vertex_index',loops)
uv=np.empty(len(mesh.uv_layers.active.data)*2,dtype=np.float32); mesh.uv_layers.active.data.foreach_get('uv',uv); uv=uv.reshape(-1,2)
_,first=np.unique(loops,return_index=True); vertex_uv=uv[first].copy()
del loops,uv,first; gc.collect()
image=bpy.data.images.get('Image_0')
pixels=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32); image.pixels.foreach_get(pixels)
pixels=pixels.reshape(image.size[1],image.size[0],4)
xy=np.clip(vertex_uv,0,.999999)*np.array([image.size[0],image.size[1]])
rgb=pixels[xy[:,1].astype(np.int32),xy[:,0].astype(np.int32),:3].copy()
del pixels,vertex_uv,xy; image.buffers_free(); gc.collect()

def segment_weights(mask,candidates,sharpness=14):
    rows=np.flatnonzero(mask)
    if not len(rows): return
    points=co[rows]; distances=[]
    for name in candidates:
        b=armature.bones[name]; a=np.array(b.head_local,dtype=np.float32); delta=np.array(b.tail_local-b.head_local,dtype=np.float32)
        t=np.clip(((points-a)*delta).sum(axis=1)/max(float(delta@delta),1e-8),0,1)
        distances.append(((points-(a+t[:,None]*delta))**2).sum(axis=1))
    distances=np.stack(distances,axis=1)
    best=np.argsort(distances,axis=1)[:,:4]
    selected=np.take_along_axis(distances,best,axis=1)
    value=np.exp(-sharpness*(np.sqrt(selected+1e-7)-np.sqrt(selected[:,0:1]+1e-7)))
    value/=value.sum(axis=1,keepdims=True)
    ids=np.array([name_to_id[n] for n in candidates],dtype=np.int16)
    indices[rows]=0; weights[rows]=0
    indices[rows,:best.shape[1]]=ids[best]; weights[rows,:best.shape[1]]=value

x,y,z=co.T
lum=rgb.max(axis=1); saturation=(rgb.max(axis=1)-rgb.min(axis=1))/np.maximum(lum,.001)
hair=(y>.065)&(z>.34)&(z<1.25)&(lum<.43)&(saturation<.48)
ribbon=(y>.04)&(z>.62)&(z<1.35)&(np.abs(x)>.29)&(saturation>.38)&(rgb[:,2]>rgb[:,0]*1.05)
head=(z>1.01)&~hair&~ribbon
indices[head]=0; weights[head]=0; indices[head,0]=name_to_id['Head']; weights[head,0]=1
for side,sign in [('Right',-1),('Left',1)]:
    half=x*sign>0
    segment_weights(hair&half,['Hair_'+side+'_'+str(i+1) for i in range(4)],22)
    segment_weights(ribbon&half,['Ribbon_'+side+'_'+str(i+1) for i in range(3)],25)
    # Hands receive only their own hand/digit weights; sleeves cannot pull fingertips.
    hand=half&(np.abs(x)>.32)&(np.abs(x)<.47)&(z>.535)&(z<.655)&(y<-.075)&(y>-.25)&(lum>.42)&(saturation<.30)
    hand_names=[side+'Hand']+[side+d+str(i+1) for d in digits for i in range(3)]
    segment_weights(hand,hand_names,65)
    sleeve=half&(np.abs(x)>.205)&(z>.33)&(z<.84)&~hand&~hair&~ribbon
    segment_weights(sleeve,[side+'UpperArm',side+'LowerArm']+['Sleeve_'+side+'_'+str(i+1) for i in range(3)],23)
    # Shoes stay on the foot; flowing fabric avoids thigh/hand contamination.
    robe=half&(z<.58)&~hair&~sleeve&~hand
    robe_names=['Pelvis']+['Robe_'+side+'_'+row+'_'+str(i+1) for row in ('Inner','Outer') for i in range(3)]
    segment_weights(robe,robe_names,24)
    shoe=half&(np.abs(x)>.045)&(np.abs(x)<.185)&(y<.035)&(z<.145)
    segment_weights(shoe,[side+'Foot',side+'Toe'],30)
del rgb,lum,saturation; gc.collect()
sys.path.insert(0,ROOT)
from macanh_skin_regions import refine_regions
refined_counts=refine_regions(co,indices,weights,names,armature)
print('MACANH_REFINED_REGIONS',refined_counts,flush=True)

# Quantize normalized weights to 1/512, adjusting the strongest so sums stay exactly 1.
weights/=np.maximum(weights.sum(axis=1,keepdims=True),1e-10)
quantized=np.rint(weights*512).astype(np.int32)
strong=np.argmax(weights,axis=1)
quantized[np.arange(len(co)),strong]+=512-quantized.sum(axis=1)
assert quantized.min()>=0 and np.all(quantized.sum(axis=1)==512)
weights=quantized.astype(np.float32)/512
np.savez_compressed(os.path.join(OUT,'MacAnh_SkinWeights.npz'),names=np.array(names),indices=indices,weights=weights)
groups=[body.vertex_groups.new(name=name) for name in names]
flat_ids=indices.ravel(); flat_q=quantized.ravel(); flat_vertex=np.repeat(np.arange(len(co),dtype=np.int32),4)
nonzero=flat_q>0
key=flat_ids[nonzero].astype(np.int32)*513+flat_q[nonzero]
vertex=flat_vertex[nonzero]; order=np.argsort(key,kind='stable'); key=key[order]; vertex=vertex[order]
boundaries=np.r_[0,np.flatnonzero(np.diff(key))+1,len(key)]
for a,b in zip(boundaries[:-1],boundaries[1:]):
    code=int(key[a]); groups[code//513].add(vertex[a:b].tolist(),(code%513)/512,'REPLACE')
del key,vertex,order,flat_vertex,flat_q,flat_ids,nonzero; gc.collect()
body.parent=rig; body.matrix_parent_inverse.identity(); body.matrix_basis.identity()
bpy.context.view_layer.update()
assert max(abs(body.matrix_world[i][j]-(1 if i==j else 0)) for i in range(4) for j in range(4))<1e-6
modifier=body.modifiers.new('MacAnh Preserve Volume Skin','ARMATURE'); modifier.object=rig; modifier.use_deform_preserve_volume=True

# Add the separately validated fan, preserving its own logical mesh parts/materials.
weapon_path=os.path.join(OUT,'Weapon','MacAnh_Fan.blend')
with bpy.data.libraries.load(weapon_path,link=False) as (available,loaded):
    loaded.objects=['MacAnh_Fan','Socket_Grip','Socket_Fan_Tip','Fan_Handle','Fan_GoldFrame','Fan_Feathers','Fan_Crystal','Fan_Tassels','Fan_Ornaments']
for obj in loaded.objects:
    if obj: bpy.context.scene.collection.objects.link(obj)
fan=bpy.data.objects['MacAnh_Fan']; socket=rig.pose.bones['Socket_Hand_R']
fan.parent=rig; fan.parent_type='BONE'; fan.parent_bone='Socket_Hand_R'
# Bone parenting adds the bone's tail translation. Cancel explicitly at the grip head.
bpy.context.view_layer.update()
fan.matrix_world=__import__('mathutils').Matrix.Translation(rig.matrix_world @ socket.head)
# Orientation/posed grip will be tuned by the animation script using a fixed rest bind.
fan['rest_bind_matrix']=[list(row) for row in fan.matrix_basis]
rig['source_body_object']=body.name; rig['units_meters']=True
rig['original_height_including_crown_m']=1.7
rig['user_authorization']='rig directly in the main source, after byte-identical backup'

# Snapshot checks: only new bones/weights/modifier/fan, never change rest geometry or UV.
coordinates=np.empty(len(mesh.vertices)*3,dtype=np.float32); mesh.vertices.foreach_get('co',coordinates)
assert hashlib.sha256(coordinates.tobytes()).hexdigest()==baseline['objects'][0]['mesh']['coordinates_sha256']
uv_values=np.empty(len(mesh.uv_layers.active.data)*2,dtype=np.float32); mesh.uv_layers.active.data.foreach_get('uv',uv_values)
assert hashlib.sha256(uv_values.tobytes()).hexdigest()==baseline['objects'][0]['mesh']['uv_sha256']['UVMap']
report={'source':SOURCE,'bones':len(armature.bones),'deform_bones':len(names),'weight_solver':solver,'bone_definitions':defs,
        'vertices':len(mesh.vertices),'triangles':len(mesh.polygons),'height_including_crown_m':1.7,
        'unweighted':int(np.sum(weights.sum(axis=1)<.999)),
        'max_influences':int(np.max(np.sum(weights>0,axis=1))),
        'max_weight_sum_error':float(np.max(np.abs(weights.sum(axis=1)-1))),
        'coordinates_sha256':hashlib.sha256(coordinates.tobytes()).hexdigest(),
        'uv_sha256':hashlib.sha256(uv_values.tobytes()).hexdigest(),
        'regions':{'head':int(head.sum()),'hair':int(hair.sum()),'ribbon':int(ribbon.sum())},
        'animation_tested':False,'posed_grip_tested':False}
assert report['unweighted']==0 and report['max_influences']<=4
with open(os.path.join(OUT,'MacAnh_RigBuild.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
bpy.context.view_layer.objects.active=rig; bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=SOURCE,compress=True)
print('MACANH_RIG_SAVED_TO_MAIN',json.dumps({k:report[k] for k in ('bones','vertices','unweighted','max_influences','max_weight_sum_error','regions')}),flush=True)
