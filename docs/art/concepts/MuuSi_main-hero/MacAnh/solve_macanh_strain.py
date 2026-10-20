"""Solve screened surface weights with a spatial multigrid preconditioner.

Only numerical weights change. Constraints come from the posed-edge audit;
the original surface remains the graph even on coarser solver levels.
"""
from pathlib import Path
import sys,time,json
import numpy as np
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix,diags
from scipy.sparse.linalg import splu
cache=project/'Library/MacAnhDeformation';started=time.time()
d=np.load(cache/'candidate8.npz');co=d['co'];ids=d['indices'];weights=d['weights'];names=d['names']
edges=np.load(cache/'edges.npy');length=np.linalg.norm(co[edges[:,0]]-co[edges[:,1]],axis=1)
valid=length>.00002;ratio=np.ones(len(edges),np.float32);ratio[valid]=np.load(cache/'edge_ratio_Candidate8.npy')
bad=(ratio>2.5)&((ratio-1)*length>.0015);n=len(co)
adj=coo_matrix((np.ones(len(edges)*2,np.float32),(np.r_[edges[:,0],edges[:,1]],np.r_[edges[:,1],edges[:,0]])),shape=(n,n)).tocsr()
active=np.zeros(n,bool);active[edges[bad].ravel()]=True
for i in range(75):active|=adj@active>0
local=np.flatnonzero(active|(adj@active>0));mapping=np.full(n,-1,np.int32);mapping[local]=np.arange(len(local))
keep=(mapping[edges]>=0).all(axis=1);e=mapping[edges[keep]]
conductance=np.ones(len(edges),np.float32)
conductance[bad]=np.minimum(400,np.maximum(1,((ratio[bad]-1)*length[bad]/.0015)**2))
v=conductance[keep]
W=coo_matrix((np.r_[v,v],(np.r_[e[:,0],e[:,1]],np.r_[e[:,1],e[:,0]])),shape=(len(local),len(local))).tocsr()
initial=np.zeros((len(local),len(names)),np.float32)
for j in range(ids.shape[1]):np.add.at(initial,(np.arange(len(local)),ids[local,j]),weights[local,j])
move=active[local];fixed=~move;points=co[local[move]]
degree=np.asarray(W.sum(axis=1)).ravel()[move]
pin=np.full(len(points),.00008,np.float32)
hand=[i for i,name in enumerate(names) if any(digit in name for digit in ('Hand','Thumb','Index','Middle','Ring','Little'))]
hand_mass=initial[move][:,hand].sum(axis=1)
pin[hand_mass>.6]=.035
pin[initial[move][:,list(names).index('Head')]>.85]=.025
A=(diags(degree*(1+pin))-W[move][:,move]).tocsr()
b=initial[move]*(degree*pin)[:,None]+W[move][:,fixed]@initial[fixed]
x=initial[move].copy()
levels=[]
for cell in [.004,.008,.016,.032,.064,.128,.256]:
    levels.append(dict(A=A,inv=(1/A.diagonal()).astype(np.float32)))
    if A.shape[0]<160:break
    _,groups=np.unique(np.floor(points/cell).astype(np.int32),axis=0,return_inverse=True)
    count=int(groups.max())+1
    if count==len(groups):continue
    P=coo_matrix((np.ones(len(groups),np.float32),(np.arange(len(groups)),groups)),shape=(len(groups),count)).tocsr()
    levels[-1]['P']=P
    mass=np.bincount(groups,minlength=count)
    points=np.stack([np.bincount(groups,weights=points[:,j],minlength=count)/mass for j in range(3)],axis=1)
    A=(P.T@A@P).tocsr()
    print('MULTIGRID_LEVEL',cell,A.shape[0],time.time()-started,flush=True)
if levels[-1]['A'].shape!=A.shape:levels.append(dict(A=A,inv=(1/A.diagonal()).astype(np.float32)))
lu=splu(levels[-1]['A'].tocsc())
def cycle(k,rhs,guess):
    level=levels[k]
    if k==len(levels)-1:return lu.solve(rhs)
    matrix=level['A'];inverse=level['inv'][:,None]
    for i in range(2):guess+=.7*(rhs-matrix@guess)*inverse
    residual=rhs-matrix@guess;P=level['P'];coarse=P.T@residual
    guess+=P@cycle(k+1,coarse,np.zeros_like(coarse))
    for i in range(2):guess+=.7*(rhs-matrix@guess)*inverse
    return guess
for iteration in range(24):
    x=cycle(0,b,x)
    residual=float(np.max(abs(b-levels[0]['A']@x)/degree[:,None]))
    print('MULTIGRID_CYCLE',iteration,residual,time.time()-started,flush=True)
    if residual<.00002:break
assert np.isfinite(x).all()
x=np.maximum(x,0);x/=x.sum(axis=1,keepdims=True)
new_ids=np.zeros((n,16),np.int16);new_weights=np.zeros((n,16),np.float32)
new_ids[:,:ids.shape[1]]=ids;new_weights[:,:ids.shape[1]]=weights
rows=local[move]
for a in range(0,len(rows),20000):
    stop=min(a+20000,len(rows));w=x[a:stop]
    order=np.argsort(w,axis=1)[:,-17:][:,::-1];picked=np.take_along_axis(w,order,axis=1)
    sparse=np.maximum(picked[:,:16]-picked[:,16,None],0)
    assert np.all(sparse.sum(axis=1)>.05)
    sparse/=sparse.sum(axis=1,keepdims=True)
    new_ids[rows[a:stop]]=order[:,:16];new_weights[rows[a:stop]]=sparse
np.savez_compressed(cache/'candidate_solved.npz',co=co,indices=new_ids,weights=new_weights,names=names)
report=dict(vertices=len(rows),max_influences=16,cycles=iteration+1,residual=residual,seconds=time.time()-started)
(root/'Production/DeformationFix/StrainSolve.json').write_text(json.dumps(report,indent=2))
print('STRAIN_SOLVED',report,flush=True)
