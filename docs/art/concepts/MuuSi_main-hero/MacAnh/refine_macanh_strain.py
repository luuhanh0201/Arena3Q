"""Refine candidate weights along real edges that failed posed-surface QA."""
from pathlib import Path
import sys,time,json
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix,diags
cache=project/'Library/MacAnhDeformation'
d=np.load(cache/'candidate8.npz');co=d['co'];ids=d['indices'];weights=d['weights'];names=d['names']
edges=np.load(cache/'edges.npy');length=np.linalg.norm(co[edges[:,0]]-co[edges[:,1]],axis=1)
valid=length>.00002;ratio=np.ones(len(edges),np.float32)
ratio[valid]=np.load(cache/'edge_ratio_Candidate8.npy')
bad=(ratio>2.5)&((ratio-1)*length>.0015)
start=time.time();n=len(co)
adj=coo_matrix((np.ones(len(edges)*2,np.float32),(np.r_[edges[:,0],edges[:,1]],np.r_[edges[:,1],edges[:,0]])),shape=(n,n)).tocsr()
active=np.zeros(n,bool);active[edges[bad].ravel()]=True
for i in range(65):active|=adj@active>0
# The fixed outer boundary retains reviewed neighbouring regions.
local=np.flatnonzero(active | (adj@active>0));mapping=np.full(n,-1,np.int32);mapping[local]=np.arange(len(local))
keep=(mapping[edges]>=0).all(axis=1);e=mapping[edges[keep]]
conductance=np.ones(len(edges),np.float32)
conductance[bad]=np.minimum(400,np.maximum(1,((ratio[bad]-1)*length[bad]/.0015)**2))
v=conductance[keep]
matrix=coo_matrix((np.r_[v,v],(np.r_[e[:,0],e[:,1]],np.r_[e[:,1],e[:,0]])),shape=(len(local),len(local))).tocsr()
matrix=diags(1/np.maximum(np.asarray(matrix.sum(axis=1)).ravel(),1))@matrix
values=np.zeros((len(local),len(names)),np.float32)
for j in range(ids.shape[1]):np.add.at(values,(np.arange(len(local)),ids[local,j]),weights[local,j])
initial=values.copy();move=active[local]
print('STRAIN_REGION',int(bad.sum()),len(local),flush=True)
for iteration in range(600):
    average=matrix@values
    values[move]=.86*average[move]+.1395*values[move]+.0005*initial[move]
    if iteration%100==0:print('STRAIN_SMOOTH',iteration,time.time()-start,flush=True)
new_ids=np.zeros((n,16),np.int16);new_weights=np.zeros((n,16),np.float32)
new_ids[:,:ids.shape[1]]=ids;new_weights[:,:ids.shape[1]]=weights
for a in range(0,len(local),20000):
    b=min(len(local),a+20000);w=values[a:b];order=np.argsort(w,axis=1)[:,-17:][:,::-1]
    picked=np.take_along_axis(w,order,axis=1);sparse=np.maximum(picked[:,:16]-picked[:,16,None],0)
    assert np.all(sparse.sum(axis=1)>1e-5)
    sparse/=sparse.sum(axis=1,keepdims=True)
    new_ids[local[a:b]]=order[:,:16];new_weights[local[a:b]]=sparse
np.savez_compressed(cache/'candidate16.npz',co=co,indices=new_ids,weights=new_weights,names=names)
report=dict(bad_edges=int(bad.sum()),region_vertices=len(local),iterations=600,max_influences=16,seconds=time.time()-start)
(root/'Production/DeformationFix/StrainRefinement.json').write_text(json.dumps(report,indent=2))
print('STRAIN_REFINED',report,flush=True)
