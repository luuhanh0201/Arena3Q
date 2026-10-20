"""Read-only surface connectivity audit for the Ultimate garment pull."""
import bpy,sys,json,numpy as np
from pathlib import Path
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components
body=bpy.data.objects['Mesh_0.001']
for m in body.modifiers:m.show_viewport=False;m.show_render=False
mesh=body.data;n=len(mesh.vertices)
co=np.empty(n*3,np.float32);mesh.vertices.foreach_get('co',co);co=co.reshape(-1,3)
edges=np.empty(len(mesh.edges)*2,np.int32);mesh.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2)
graph=coo_matrix((np.ones(len(edges),np.uint8),(edges[:,0],edges[:,1])),shape=(n,n)).tocsr()
count,labels=connected_components(graph,directed=False)
cache=project/'Library/MacAnhRigAudit';np.save(cache/'surface_components.npy',labels)
np.save(cache/'surface_edges.npy',edges)
loops=np.empty(len(mesh.loops),np.int32);mesh.loops.foreach_get('vertex_index',loops)
uv=np.empty(len(mesh.loops)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);uv=uv.reshape(-1,2)
_,first=np.unique(loops,return_index=True)
np.save(cache/'surface_uv.npy',uv[first])
report=[]
for label,size in enumerate(np.bincount(labels)):
    if size<100:continue
    pts=co[labels==label]
    report.append(dict(label=label,size=int(size),lo=pts.min(axis=0).tolist(),hi=pts.max(axis=0).tolist()))
out=root/'Production/UltimateFix';out.mkdir(parents=True,exist_ok=True)
(out/'SurfaceComponents.json').write_text(json.dumps(dict(count=count,components=report),indent=2))
print('CLOTH_COMPONENTS',count,sorted(report,key=lambda x:-x['size'])[:30],flush=True)
