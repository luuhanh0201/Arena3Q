"""Refine new weights on the user-approved main file; rest mesh/UV stay unchanged."""
import bpy,os,sys,json,numpy as np,gc
ROOT=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,ROOT)
from macanh_skin_regions import refine_regions
rig=bpy.data.objects['MacAnh_Rig']; body=bpy.data.objects[rig['source_body_object']]
data=np.load(os.path.join(ROOT,'Production','MacAnh_SkinWeights.npz'))
names=list(data['names']); indices=data['indices'].copy(); weights=data['weights'].copy()
co=np.empty(len(body.data.vertices)*3,dtype=np.float32); body.data.vertices.foreach_get('co',co); co=co.reshape(-1,3)
counts=refine_regions(co,indices,weights,names,rig.data)
q=np.rint(weights*512).astype(np.int32); q[np.arange(len(co)),weights.argmax(axis=1)]+=512-q.sum(axis=1)
assert np.all(q.sum(axis=1)==512) and q.min()>=0
weights=q.astype(np.float32)/512
body.vertex_groups.clear(); groups=[body.vertex_groups.new(name=str(n)) for n in names]
valid=q.ravel()>0
key=indices.ravel()[valid].astype(np.int32)*513+q.ravel()[valid]
vertex=np.repeat(np.arange(len(co),dtype=np.int32),4)[valid]
order=np.argsort(key,kind='stable'); key=key[order]; vertex=vertex[order]
boundaries=np.r_[0,np.flatnonzero(np.diff(key))+1,len(key)]
for a,b in zip(boundaries[:-1],boundaries[1:]):
    code=int(key[a]); groups[code//513].add(vertex[a:b].tolist(),(code%513)/512,'REPLACE')
np.savez_compressed(os.path.join(ROOT,'Production','MacAnh_SkinWeights.npz'),names=np.array(names),indices=indices,weights=weights)
with open(os.path.join(ROOT,'Production','MacAnh_SkinRefinements.json'),'w') as f: json.dump(counts,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(os.path.join(ROOT,'..','muu-si.blend')),compress=True)
print('MACANH_SKIN_REFINED',counts,flush=True)
