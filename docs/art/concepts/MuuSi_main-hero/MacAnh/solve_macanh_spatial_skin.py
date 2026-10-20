"""Continuous metric-scale reconstruction of dense costume weights.

Unlike a nearest-sample transfer or an edge-degree diffusion, the reconstructed
field has a physical transition width even across sparse bridges in the mesh.
Small hand/foot regions use a finer field; their blend also has a soft boundary.
"""
from pathlib import Path
import sys,time,json
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.ndimage import gaussian_filter,map_coordinates
cache=project/'Library/MacAnhDeformation';started=time.time()
d=np.load(cache/'candidate8.npz');co=d['co'];ids=d['indices'];weights=d['weights'];names=d['names']
step=.01;origin=co.min(axis=0)-.02
position=(co-origin)/step;shape=tuple((np.ceil(position.max(axis=0)).astype(int)+3).tolist())
cell=np.rint(position).astype(int);flat=np.ravel_multi_index(cell.T,shape)
mass=np.bincount(flat,minlength=int(np.prod(shape))).reshape(shape).astype(np.float32)
broad=gaussian_filter(mass,sigma=10,mode='constant',truncate=3)
fine=gaussian_filter(mass,sigma=1.2,mode='constant',truncate=3)
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
query=position.T
values=np.empty((len(co),len(names)),np.float32)
details=np.empty_like(values)
for j,name in enumerate(names):
    source=(weights*(ids==j)).sum(axis=1)
    field=np.bincount(flat,weights=source,minlength=int(np.prod(shape))).reshape(shape).astype(np.float32)
    coarse=gaussian_filter(field,sigma=10,mode='constant',truncate=3)/np.maximum(broad,1e-12)
    small=gaussian_filter(field,sigma=1.2,mode='constant',truncate=3)/np.maximum(fine,1e-12)
    values[:,j]=map_coordinates(coarse,query,order=1,mode='nearest')
    details[:,j]=map_coordinates(small,query,order=1,mode='nearest')
    if j%10==0:print('SPATIAL_SKIN',j,len(names),time.time()-started,flush=True)
for side,sign in [('Right',-1),('Left',1)]:
    definitions={item['name']:item for item in json.loads((root/'Production/MacAnh_RigBuild.json').read_text())['bone_definitions']}
    def segment_distance(name):
        bone=definitions[name];a=np.array(bone['head'],np.float32);delta=np.array(bone['tail'],np.float32)-a
        t=np.clip(((co-a)*delta).sum(axis=1)/(delta@delta),0,1)
        return np.linalg.norm(co-a-t[:,None]*delta,axis=1)
    arm_family=[side+'UpperArm',side+'LowerArm']
    distances=np.stack([segment_distance(name) for name in arm_family],axis=1)
    arm_goal=np.exp(-(distances/.07)**2);arm_goal/=np.maximum(arm_goal.sum(axis=1,keepdims=True),1e-20)
    arm_alpha=(1-smooth(.025,.32,distances.min(axis=1)))*smooth(.09,.20,abs(co[:,0]))
    values*=1-arm_alpha[:,None]
    values[:,[list(names).index(name) for name in arm_family]]+=arm_goal*arm_alpha[:,None]
    for centre,r0,r1,family,anchor in [
        ((sign*.385,-.15,.60),.025,.10,[side+'Hand']+[side+digit+str(k) for digit in ('Thumb','Index','Middle','Ring','Little') for k in (1,2,3)],side+'Hand'),
        ((sign*.108,-.08,.07),.045,.15,[side+'Foot',side+'Toe'],side+'Foot')]:
        columns=[list(names).index(name) for name in family]
        goal=details[:,columns].copy()
        # A hand prior avoids normalising an almost-zero finger tail into a
        # sudden arbitrary finger assignment in the neighbouring sleeve.
        goal[:,family.index(anchor)]+=.05
        goal/=goal.sum(axis=1,keepdims=True)
        distance=np.linalg.norm(co-np.array(centre,np.float32),axis=1)
        if anchor.endswith('Hand'):distance=segment_distance(anchor)
        alpha=1-smooth(r0,r1,distance)
        values*=1-alpha[:,None];values[:,columns]+=goal*alpha[:,None]
# Crown and face retain a rigid head attachment; blend below them over 13 cm.
head=smooth(1.02,1.15,co[:,2]);values*=1-head[:,None]
values[:,list(names).index('Head')]+=head
values/=values.sum(axis=1,keepdims=True)
new_ids=np.empty((len(co),16),np.int16);new_weights=np.empty((len(co),16),np.float32)
for a in range(0,len(co),20000):
    b=min(a+20000,len(co));w=values[a:b];order=np.argsort(w,axis=1)[:,-17:][:,::-1]
    picked=np.take_along_axis(w,order,axis=1);sparse=np.maximum(picked[:,:16]-picked[:,16,None],0)
    assert np.all(sparse.sum(axis=1)>.05)
    sparse/=sparse.sum(axis=1,keepdims=True);new_ids[a:b]=order[:,:16];new_weights[a:b]=sparse
np.savez_compressed(cache/'candidate_spatial4.npz',co=co,indices=new_ids,weights=new_weights,names=names)
report=dict(method='normalized spatial field / trilinear interpolation',grid_step_m=step,cloth_sigma_m=.10,
            detail_sigma_m=.012,grid_shape=shape,max_influences=16,seconds=time.time()-started)
(root/'Production/DeformationFix/SpatialSkin.json').write_text(json.dumps(report,indent=2))
print('SPATIAL_SKIN_COMPLETE',report,flush=True)
