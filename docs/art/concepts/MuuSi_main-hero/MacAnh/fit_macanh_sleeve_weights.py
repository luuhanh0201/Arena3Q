"""Fit only the three existing sleeve bones; freeze every other body region."""
from pathlib import Path
import sys,time
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix
label=sys.argv[1] if len(sys.argv)>1 else 'BattleSurfaceCut'
cache=project/'Library/MacAnhDeformation'/label
source=np.load(cache/'weights.npz');co=source['co'];names=list(source['names'])
old_ids=source['indices'];old_weights=source['weights'];order=np.argsort(old_weights,axis=1)[:,-3:][:,::-1]
ids=np.take_along_axis(old_ids,order,axis=1);initial=np.take_along_axis(old_weights,order,axis=1)
free=np.zeros(len(co),bool)
for side in ('Right','Left'):
    family=[names.index('Sleeve_'+side+'_'+str(i)) for i in (1,2,3)]
    selected=(old_weights*np.isin(old_ids,family)).sum(axis=1)>.99
    free|=selected;ids[selected]=family
    for index,bone in enumerate(family):initial[selected,index]=(old_weights[selected]*(old_ids[selected]==bone)).sum(axis=1)
edges=np.load(cache/'edges.npy');rest=np.linalg.norm(co[edges[:,0]]-co[edges[:,1]],axis=1)
keep=(rest>.00002)&(free[edges[:,0]]|free[edges[:,1]])
edges=edges[keep];rest=rest[keep]
upper=np.minimum(np.maximum(rest*1.29,rest+.006),np.maximum(rest*2.8,rest+.0017))
lower=np.minimum(rest*.71,rest-.006);scale=np.maximum(rest,.01)
poses=np.load(cache/'poses.npz')['matrices'];homo=np.c_[co,np.ones(len(co),np.float32)]
basis=np.empty((len(co),3,len(poses),3),np.float32)
for k in range(3):basis[:,k]=np.einsum('pnij,nj->npi',poses[:,ids[:,k],:3,:],homo)
anchor=basis[:,0].copy();basis-=anchor[:,None]
matrix=coo_matrix((np.tile([1.,-1.],len(edges)),(np.repeat(np.arange(len(edges)),2),edges.ravel())),shape=(len(edges),len(co)),dtype=np.float32).tocsr()
weights=initial.copy();moment=np.zeros_like(weights);variance=np.zeros_like(weights)
started=time.time();best=float('inf')
for iteration in range(601):
    posed=anchor+np.einsum('nk,nkpi->npi',weights,basis)
    edge=(matrix@posed.reshape(len(co),-1)).reshape(len(edges),len(poses),3)
    length=np.linalg.norm(edge,axis=2)
    error=np.maximum(length-upper[:,None],0)+np.minimum(length-lower[:,None],0)
    score=float((error**2/scale[:,None]**2).sum())
    if score<best:
        best=score;np.savez_compressed(cache/'candidate_sleevefit3.npz',co=co,indices=ids.astype(np.int16),weights=weights,names=np.array(names))
    force=edge*(error/(np.maximum(length,1e-8)*scale[:,None]**2))[:,:,None]
    vertex_force=(matrix.T@force.reshape(len(edges),-1)).reshape(len(co),len(poses),3)
    gradient=np.einsum('npi,nkpi->nk',vertex_force,basis)/len(poses)+.00005*(weights-initial)
    gradient[~free]=0
    moment=.9*moment+.1*gradient;variance=.999*variance+.001*gradient**2
    step=.01*(.995**iteration)
    tentative=weights-step*(moment/(1-.9**(iteration+1)))/(np.sqrt(variance/(1-.999**(iteration+1)))+1e-8)
    ordered=np.sort(tentative,axis=1)[:,::-1];sums=np.cumsum(ordered,axis=1)-1
    active=ordered-sums/np.arange(1,4)>0;rho=active.sum(axis=1)-1
    threshold=sums[np.arange(len(co)),rho]/(rho+1)
    weights=np.maximum(tentative-threshold[:,None],0).astype(np.float32);weights[~free]=initial[~free]
    if iteration%50==0:print('SLEEVE_WEIGHT_FIT',iteration,'loss',round(score,5),'max_error',float(abs(error).max()),'seconds',round(time.time()-started,1),flush=True)
print('SLEEVE_WEIGHT_FIT_COMPLETE',flush=True)
