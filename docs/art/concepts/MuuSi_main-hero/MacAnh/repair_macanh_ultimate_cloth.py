"""Repair garment skinning only. --save commits the reviewed repair to the main blend."""
import bpy,sys,numpy as np,json,hashlib,os
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent;project=root.parents[4];cache=project/'Library/MacAnhRigAudit'
out=root/'Production/UltimateFix';out.mkdir(parents=True,exist_ok=True)
d=np.load(cache/'actual_saved_weights_before.npz');part=np.load(cache/'cloth_partition.npz')
body=bpy.data.objects['Mesh_0.001'];rig=bpy.data.objects['MacAnh_Rig'];co=d['co'];x,y,z=co.T
names=list(d['names']);ids={str(n):i for i,n in enumerate(names)}
indices=d['indices'].copy();weights=d['weights'].copy();factor=part['factor'];region=part['region']
def digest_geometry():
    xyz=np.empty(len(body.data.vertices)*3,np.float32);body.data.vertices.foreach_get('co',xyz)
    uv=np.empty(len(body.data.loops)*2,np.float32);body.data.uv_layers.active.data.foreach_get('uv',uv)
    tri=np.empty(len(body.data.loops),np.int32);body.data.loops.foreach_get('vertex_index',tri)
    return dict(co=hashlib.sha256(xyz.tobytes()).hexdigest(),uv=hashlib.sha256(uv.tobytes()).hexdigest(),topology=hashlib.sha256(tri.tobytes()).hexdigest())
before=digest_geometry()
def action_hashes():
    result={}
    for a in bpy.data.actions:
        h=hashlib.sha256()
        curves=a.layers[0].strips[0].channelbag(a.slots[0]).fcurves if a.layers else a.fcurves
        for curve in curves:
            h.update((curve.data_path+str(curve.array_index)).encode())
            for key in curve.keyframe_points:
                h.update(np.array([*key.co,*key.handle_left,*key.handle_right],np.float32).tobytes())
                h.update(key.interpolation.encode())
        result[a.name]=h.hexdigest()
    return result
action_before=action_hashes()
def family(points,candidates,sharp=24):
    distance=[]
    for name in candidates:
        b=rig.data.bones[name];a=np.array(b.head_local);delta=np.array(b.tail_local-b.head_local)
        t=np.clip(((points-a)*delta).sum(axis=1)/max(delta@delta,1e-8),0,1)
        distance.append(np.linalg.norm(points-a-t[:,None]*delta,axis=1))
    distance=np.stack(distance,axis=1);v=np.exp(-sharp*(distance-distance.min(axis=1,keepdims=True)))
    return v/v.sum(axis=1,keepdims=True)
for side,sign in [('Right',-1),('Left',1)]:
    rows=np.flatnonzero(region&(x*sign>0));pts=co[rows]
    sleeve=[side+'UpperArm',side+'LowerArm']+['Sleeve_'+side+'_'+str(i+1) for i in range(3)]
    robe=['Pelvis']+['Robe_'+side+'_'+r+'_'+str(i+1) for r in ('Inner','Outer') for i in range(3)]
    v=np.zeros((len(rows),len(names)),np.float32)
    # Preserve existing weights at the perimeter; the repair has no box-shaped seam.
    blend=np.minimum.reduce([np.clip((abs(x[rows])-.18)/.055,0,1),np.clip((z[rows]-.10)/.055,0,1),np.clip((.87-z[rows])/.04,0,1)])
    for j in range(4):np.add.at(v,(np.arange(len(rows)),indices[rows,j]),weights[rows,j]*(1-blend))
    v[:,[ids[n] for n in sleeve]]+=family(pts,sleeve)*factor[rows,None]*blend[:,None]
    v[:,[ids[n] for n in robe]]+=family(pts,robe)*(1-factor[rows,None])*blend[:,None]
    best=np.argsort(v,axis=1)[:,-4:][:,::-1];value=np.take_along_axis(v,best,axis=1);value/=value.sum(axis=1,keepdims=True)
    indices[rows]=best;weights[rows]=value
rows=np.flatnonzero(region)
# Remove tiny weight islands along the fused cloth surface. Spatial boxes alone
# leave neighbouring vertices following different limbs and stretch texture seams.
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix,diags
edges=np.load(cache/'surface_edges.npy');e=edges[region[edges].any(axis=1)]
local=np.unique(e);mapping=np.full(len(co),-1,np.int32);mapping[local]=np.arange(len(local))
e=mapping[e];matrix=coo_matrix((np.ones(len(e)*2,np.float32),(np.r_[e[:,0],e[:,1]],np.r_[e[:,1],e[:,0]])),shape=(len(local),len(local))).tocsr()
degree=np.asarray(matrix.sum(axis=1)).ravel();matrix=diags(1/np.maximum(degree,1))@matrix
active=region[local];values=np.zeros((len(local),len(names)),np.float32)
for j in range(4):np.add.at(values,(np.arange(len(local)),indices[local,j]),weights[local,j])
initial=values.copy()
for iteration in range(120):
    average=matrix@values
    values[active]=average[active]*.995+initial[active]*.005
best=np.argsort(values,axis=1)[:,-4:][:,::-1];value=np.take_along_axis(values,best,axis=1);value/=value.sum(axis=1,keepdims=True)
indices[rows]=best[active];weights[rows]=value[active]
del values,initial,average,matrix
q=np.rint(weights[rows]*512).astype(np.int32);strong=np.argmax(weights[rows],axis=1);q[np.arange(len(rows)),strong]+=512-q.sum(axis=1)
weights[rows]=q/512
# Touch only reviewed garment vertices, never clear groups for the whole model.
for g in body.vertex_groups:g.remove(rows.tolist())
codes=indices[rows].ravel().astype(np.int32)*513+q.ravel();verts=np.repeat(rows,4);valid=q.ravel()>0
codes=codes[valid];verts=verts[valid];order=np.argsort(codes);codes=codes[order];verts=verts[order]
breaks=np.r_[0,np.flatnonzero(np.diff(codes))+1,len(codes)]
for a,b in zip(breaks[:-1],breaks[1:]):
    code=int(codes[a]);body.vertex_groups[code//513].add(verts[a:b].tolist(),(code%513)/512,'REPLACE')
assert digest_geometry()==before
assert action_hashes()==action_before
assert np.max(abs(weights.sum(axis=1)-1))<1e-5
report=dict(vertices_reweighted=len(rows),geometry_before=before,geometry_after=digest_geometry(),actions_untouched=True,action_hashes=action_before,max_influences=4)
(out/'ClothRepair.json').write_text(json.dumps(report,indent=2))
if '--save' in sys.argv:
    # Save the original state/frame, not a diagnostic pose or camera.
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath,compress=True)
    np.savez_compressed(cache/'actual_saved_weights_after.npz',co=co,indices=indices,weights=weights,names=np.array(names))
    print('ULTIMATE_CLOTH_SAVED',report,flush=True)
else:
    scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=12
    scene.render.resolution_x=scene.render.resolution_y=900;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    for name,loc,energy in [('Key',(2,-3,4),350),('Fill',(-3,-2,2),180),('Rim',(1,3,3),400)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.size=3
        ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,.85))-ob.location).to_track_quat('-Z','Y').to_euler()
    cam=bpy.data.objects.new('ClothAuditCamera',bpy.data.cameras.new('ClothAuditCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.2
    cam.location=(0,-4,.95);cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler()
    rig.animation_data.action=bpy.data.actions['MacAnh_Ultimate']
    for f in (0,30,60,75):
        scene.frame_set(f);bpy.context.view_layer.update();scene.render.filepath=str(out/f'Ultimate_ClothDraft_{f:03d}.png');bpy.ops.render.render(write_still=True)
    print('ULTIMATE_CLOTH_DRAFT_DONE',flush=True)
