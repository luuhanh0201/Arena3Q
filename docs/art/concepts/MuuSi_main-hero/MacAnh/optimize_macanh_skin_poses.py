"""Fit four-weight skinning against measured edge strain across action poses.

Numerical candidates only: the independent full-frame audit remains mandatory.
"""
import sys,time
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix
cache=project/'Library/MacAnhDeformation/Battle'
data=np.load(cache/'candidate_coherent4.npz');co=data['co'];ids=data['indices'];initial=data['weights'];names=data['names']
edges=np.load(cache/'edges.npy');rest=np.linalg.norm(co[edges[:,0]]-co[edges[:,1]],axis=1)
valid=rest>.00002;edges=edges[valid];rest=rest[valid]
upper=np.minimum(np.maximum(rest*1.28,rest+.006),np.maximum(rest*2.8,rest+.0017))
lower=np.minimum(rest*.72,rest-.006)
scale=np.maximum(rest,.01)
poses=np.load(cache/'poses.npz')['matrices']
# Cache displacement relative to each vertex's first influence. Common world
# translations then cancel in the gradient on the probability simplex.
homo=np.c_[co,np.ones(len(co),np.float32)]
basis=np.empty((len(co),4,len(poses),3),np.float32)
for k in range(4):basis[:,k]=np.einsum('pnij,nj->npi',poses[:,ids[:,k],:3,:],homo)
anchor=basis[:,0].copy();basis-=anchor[:,None]
row=np.repeat(np.arange(len(edges)),2);col=edges.ravel();value=np.tile([1.,-1.],len(edges))
incidence=coo_matrix((value,(row,col)),shape=(len(edges),len(co)),dtype=np.float32).tocsr()
weights=initial.copy();m=np.zeros_like(weights);v=np.zeros_like(weights);started=time.time()
def simplex(x):
    ordered=np.sort(x,axis=1)[:,::-1];sums=np.cumsum(ordered,axis=1)-1
    active=ordered-sums/np.arange(1,5)>0;rho=active.sum(axis=1)-1
    threshold=sums[np.arange(len(x)),rho]/(rho+1)
    return np.maximum(x-threshold[:,None],0).astype(np.float32)
for iteration in range(801):
    posed=anchor+np.einsum('nk,nkpi->npi',weights,basis)
    edge=(incidence@posed.reshape(len(co),-1)).reshape(len(edges),len(poses),3)
    length=np.linalg.norm(edge,axis=2)
    error=np.maximum(length-upper[:,None],0)+np.minimum(length-lower[:,None],0)
    force=edge*(error/(np.maximum(length,1e-8)*scale[:,None]**2))[:,:,None]
    vertex_force=(incidence.T@force.reshape(len(edges),-1)).reshape(len(co),len(poses),3)
    gradient=np.einsum('npi,nkpi->nk',vertex_force,basis)/len(poses)+.005*(weights-initial)
    m=.9*m+.1*gradient;v=.999*v+.001*gradient**2
    step=.012*(.999**iteration)
    weights=simplex(weights-step*(m/(1-.9**(iteration+1)))/(np.sqrt(v/(1-.999**(iteration+1)))+1e-8))
    if iteration%50==0:
        failed=(length>upper[:,None])|(length<lower[:,None])
        print('POSE_SKIN_OPTIMIZE',iteration,'violations',int(failed.sum()),'max_delta',float(abs(error).max()),'seconds',round(time.time()-started,2),flush=True)
        np.savez_compressed(cache/'candidate_posefit4.npz',co=co,indices=ids,weights=weights,names=names)
print('POSE_SKIN_OPTIMIZE_COMPLETE',flush=True)
