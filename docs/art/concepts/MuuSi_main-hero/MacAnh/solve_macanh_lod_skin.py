"""Transfer the continuous field, consolidate costume chains, retain four weights.

Writes a numerical candidate only. All clips must be audited after applying it.
"""
import sys,json,time
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.spatial import cKDTree
label=sys.argv[1] if len(sys.argv)>1 else 'Battle'
cache=project/'Library/MacAnhDeformation'
source=np.load(cache/'candidate_spatial4.npz')
source_co=source['co'];source_weights=source['weights'];source_indices=source['indices']
target=np.load(cache/label/'weights.npz');co=target['co'];names=list(target['names'])
assert list(source['names'])==names
tree=cKDTree(source_co);distance,nearest=tree.query(co,k=8,workers=4)
blend=np.exp(-(distance/.012)**2);blend/=blend.sum(axis=1,keepdims=True)
values=np.zeros((len(co),len(names)),np.float32)
for bone in range(len(names)):
    dense=(source_weights*(source_indices==bone)).sum(axis=1)
    values[:,bone]=(dense[nearest]*blend).sum(axis=1)
for j,name in enumerate(names):
    destination=None
    if name.startswith(('Hair_','Ribbon_')):destination='Head'
    if name.startswith('Robe_'):destination='Pelvis'
    if name.startswith('Sleeve_'):destination='_'.join(name.split('_')[:2])+'_1'
    if name in ('Neck','Spine'):destination='Chest'
    if any(d in name for d in ('Thumb','Index','Middle','Ring','Little')):
        destination=('Right' if name.startswith('Right') else 'Left')+'Hand'
    if name.endswith(('UpperArm','LowerArm')):
        destination='Sleeve_'+('Right' if name.startswith('Right') else 'Left')+'_1'
    if destination is not None and destination!=name:
        values[:,names.index(destination)]+=values[:,j];values[:,j]=0
definitions={b['name']:b for b in json.loads((root/'Production/MacAnh_RigBuild.json').read_text())['bone_definitions']}
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def segment(name):
    d=definitions[name];a=np.array(d['head']);delta=np.array(d['tail'])-a
    t=np.clip(((co-a)*delta).sum(axis=1)/(delta@delta),0,1)
    return np.linalg.norm(co-a-t[:,None]*delta,axis=1)
for side in ('Right','Left'):
    # The full palm/finger envelope must move with the hand, including dark
    # finger seams. A tiny capsule had incorrectly assigned the fingertips to
    # sleeve/forearm, leaving visible slits even when the broad silhouette passed.
    alpha=1-smooth(.07,.23,segment(side+'Hand'))
    values*=1-alpha[:,None];values[:,names.index(side+'Hand')]+=alpha
# Remove finite jumps when a fifth influence loses support.
order=np.argsort(values,axis=1)[:,-5:][:,::-1]
picked=np.take_along_axis(values,order,axis=1)
sparse=np.maximum(picked[:,:4]-picked[:,4,None],0)
sparse/=sparse.sum(axis=1,keepdims=True)
np.savez_compressed(cache/label/'candidate_hand_envelope4.npz',co=co,indices=order[:,:4].astype(np.int16),weights=sparse,names=np.array(names))
print('COHERENT_LOD_CANDIDATE',label,len(co),int((sparse>0).sum(axis=1).max()),flush=True)
