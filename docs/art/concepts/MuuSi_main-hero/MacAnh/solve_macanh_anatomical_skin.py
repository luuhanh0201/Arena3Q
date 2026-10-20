"""Analytical continuous garment envelopes, independent of flawed source masks."""
from pathlib import Path
import json,sys
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
label=sys.argv[1] if len(sys.argv)>1 else 'Battle'
cache=project/'Library/MacAnhDeformation'/label
d=np.load(cache/'weights.npz');co=d['co'];names=list(d['names']);x,y,z=co.T
definitions={b['name']:b for b in json.loads((root/'Production/MacAnh_RigBuild.json').read_text())['bone_definitions']}
def smooth(a,b,t):
    q=np.clip((t-a)/(b-a),0,1);return q*q*(3-2*q)
def segment(name):
    b=definitions[name];a=np.array(b['head']);delta=np.array(b['tail'])-a
    t=np.clip(((co-a)*delta).sum(axis=1)/(delta@delta),0,1)
    return np.linalg.norm(co-a-t[:,None]*delta,axis=1)
values=np.zeros((len(co),len(names)),np.float32)
chest=smooth(.40,.88,z)
values[:,names.index('Chest')]=chest;values[:,names.index('Pelvis')]=1-chest
for side,sign in [('Right',-1),('Left',1)]:
    sleeve_distance=np.stack([segment('Sleeve_'+side+'_'+str(i)) for i in (1,2,3)],axis=1).min(axis=1)
    alpha=(1-smooth(.025,.17,sleeve_distance))*smooth(.16,.26,x*sign)
    # Garment follows its draped chain, avoiding arm weights in the long hem.
    values*=1-alpha[:,None];values[:,names.index('Sleeve_'+side+'_1')]+=alpha
    hand=1-smooth(.08,.24,segment(side+'Hand'))
    values*=1-hand[:,None];values[:,names.index(side+'Hand')]+=hand
    # Foot-to-shin transitions span the actual ankle, not a box on the hem.
    foot=(1-smooth(.045,.14,segment(side+'Foot')))*(1-smooth(.12,.23,z))
    values*=1-foot[:,None];values[:,names.index(side+'Foot')]+=foot
head=np.maximum(smooth(.90,1.08,z),smooth(.04,.17,y)*smooth(.25,.48,z))
values*=1-head[:,None];values[:,names.index('Head')]+=head
order=np.argsort(values,axis=1)[:,-5:][:,::-1]
picked=np.take_along_axis(values,order,axis=1)
sparse=np.maximum(picked[:,:4]-picked[:,4,None],0);sparse/=sparse.sum(axis=1,keepdims=True)
np.savez_compressed(cache/'candidate_anatomical4.npz',co=co,indices=order[:,:4].astype(np.int16),weights=sparse,names=np.array(names))
print('ANATOMICAL_SKIN_CANDIDATE',label,len(co),flush=True)
