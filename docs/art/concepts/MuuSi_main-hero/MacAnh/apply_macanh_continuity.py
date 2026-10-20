"""Apply a reviewed numerical candidate on a separate blend, with source hashes."""
import bpy,sys,json,hashlib,time
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent; project=root.parents[4]
cache=project/'Library/MacAnhDeformation';out=root/'Production/DeformationFix'
if '--cache-name' in sys.argv:cache=cache/sys.argv[sys.argv.index('--cache-name')+1]
candidate=sys.argv[sys.argv.index('--candidate')+1] if '--candidate' in sys.argv else 'candidate8.npz'
d=np.load(cache/candidate);indices=d['indices'];weights=d['weights'];names=list(d['names'])
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
for mod in body.modifiers:mod.show_viewport=False;mod.show_render=False
def hashes():
    mesh=body.data
    co=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',co)
    loops=np.empty(len(mesh.loops),np.int32);mesh.loops.foreach_get('vertex_index',loops)
    uv=np.empty(len(mesh.loops)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv)
    return {k:hashlib.sha256(v.tobytes()).hexdigest() for k,v in [('co',co),('topology',loops),('uv',uv)]}
before=hashes();started=time.time()
assert before['co']==hashlib.sha256(d['co'].tobytes()).hexdigest(),'Stale candidate coordinates do not match the opened source'
q=np.rint(weights*16384).astype(np.int32)
q[np.arange(len(q)),weights.argmax(axis=1)]+=16384-q.sum(axis=1)
assert q.min()>=0 and np.all(q.sum(axis=1)==16384)
body.vertex_groups.clear();groups=[body.vertex_groups.new(name=str(n)) for n in names]
valid=q.ravel()>0
codes=indices.ravel()[valid].astype(np.int64)*16385+q.ravel()[valid]
verts=np.repeat(np.arange(len(q),dtype=np.int32),q.shape[1])[valid]
order=np.argsort(codes,kind='stable');codes=codes[order];verts=verts[order]
breaks=np.r_[0,np.flatnonzero(np.diff(codes))+1,len(codes)]
for a,b in zip(breaks[:-1],breaks[1:]):
    code=int(codes[a]);groups[code//16385].add(verts[a:b].tolist(),(code%16385)/16384,'REPLACE')
assert hashes()==before
for mod in body.modifiers:
    if mod.type=='ARMATURE':
        mod.show_viewport=True;mod.show_render=True
        mod.use_deform_preserve_volume=False # Match Unity's linear skinning.
report=dict(candidate=candidate,max_influences=q.shape[1],geometry_before=before,geometry_after=hashes(),seconds=time.time()-started)
(out/'AppliedWeights.json').write_text(json.dumps(report,indent=2))
destination=out/'MacAnh_Continuity.blend'
if '--output' in sys.argv:destination=Path(sys.argv[sys.argv.index('--output')+1]).resolve()
assert destination!=root.parent/'muu-si.blend','Original must remain immutable'
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
print('CONTINUITY_APPLIED',destination,report,flush=True)
