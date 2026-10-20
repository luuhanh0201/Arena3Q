"""Smooth a surface graph, interpolate continuously, sparsify without top-k jumps.

Preparation only: writes a candidate NPZ, never edits the blend. Input must be
the actual weights dumped by audit_macanh_deformation.py in this work session.
"""
from pathlib import Path
import sys, time, json
import numpy as np
root=Path(__file__).resolve().parent; project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix,diags
from scipy.spatial import cKDTree
cache=project/'Library/MacAnhDeformation'
d=np.load(cache/'weights.npz'); co=d['co']; ids=d['indices']; weights=d['weights']; names=d['names']
edges=np.load(cache/'edges.npy'); started=time.time()
# A graph from actual surface edges avoids treating two nearby but disconnected
# shells as neighbours. Coarsening is only a numerical solver, never a mesh edit.
cells=np.floor(co/.0035).astype(np.int32)
_,mapping=np.unique(cells,axis=0,return_inverse=True)
mapping=mapping.astype(np.int32); count=int(mapping.max())+1
mass=np.bincount(mapping,minlength=count).astype(np.float32)
points=np.stack([np.bincount(mapping,weights=co[:,j],minlength=count)/mass for j in range(3)],axis=1).astype(np.float32)
values=np.zeros((count,len(names)),np.float32)
for j in range(ids.shape[1]): np.add.at(values,(mapping,ids[:,j]),weights[:,j])
values/=mass[:,None]
e=mapping[edges]; e=np.sort(e[e[:,0]!=e[:,1]],axis=1); e=np.unique(e,axis=0)
length=np.linalg.norm(points[e[:,0]]-points[e[:,1]],axis=1)
conductance=1/np.maximum(length,.001)
matrix=coo_matrix((np.r_[conductance,conductance],(np.r_[e[:,0],e[:,1]],np.r_[e[:,1],e[:,0]])),shape=(count,count)).tocsr()
degree=np.asarray(matrix.sum(axis=1)).ravel()
matrix=diags(1/np.maximum(degree,1))@matrix
initial=values.copy()
print('CONTINUITY_GRAPH',count,len(e),flush=True)
for iteration in range(240):
    values=.82*(matrix@values)+.178*values+.002*initial
    if iteration%40==0: print('CONTINUITY_DIFFUSION',iteration,time.time()-started,flush=True)
# Gaussian reconstruction removes voxel stair steps. Stable offsets prevent a
# near-coincident sample from becoming an unintended hard nearest-neighbour map.
tree=cKDTree(points)
new_ids=np.empty((len(co),8),np.int16); new_weights=np.empty((len(co),8),np.float32)
for a in range(0,len(co),12000):
    b=min(a+12000,len(co)); distance,nearest=tree.query(co[a:b],k=24,workers=4)
    blend=np.exp(-(distance/.008)**2); blend/=blend.sum(axis=1,keepdims=True)
    w=np.einsum('nk,nkg->ng',blend,values[nearest]).astype(np.float32)
    # Subtract the first discarded weight before pruning: an influence enters/leaves at
    # exactly zero, instead of dropping a finite 15–25% weight across one edge.
    order=np.argsort(w,axis=1)[:,-9:][:,::-1]
    picked=np.take_along_axis(w,order,axis=1)
    sparse=np.maximum(picked[:,:8]-picked[:,8,None],0)
    total=sparse.sum(axis=1,keepdims=True)
    weak=total[:,0]<1e-9
    sparse[weak]=np.array([1,0,0,0,0,0,0,0]);total[weak]=1
    sparse/=total
    new_ids[a:b]=order[:,:8];new_weights[a:b]=sparse
np.savez_compressed(cache/'candidate8.npz',co=co,indices=new_ids,weights=new_weights,names=names)
report=dict(method='surface diffusion / continuous reconstruction / ninth-weight subtraction',
            solver_vertices=count,iterations=240,max_influences=8,quantization=False,
            sum_error=float(abs(new_weights.sum(axis=1)-1).max()),seconds=time.time()-started)
(root/'Production/DeformationFix/ContinuityRepair.json').write_text(json.dumps(report,indent=2))
print('CONTINUITY_CANDIDATE',report,flush=True)
