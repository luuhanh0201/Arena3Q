"""Prove a revision leaves body geometry/UVs/weights intact."""
import bpy,sys,json,hashlib
import numpy as np
from pathlib import Path
root=Path(__file__).resolve().parent
rig=bpy.data.objects['MacAnh_Rig'];current=bpy.data.objects[rig['source_body_object']]
other=Path(sys.argv[sys.argv.index('--other')+1]).resolve()
with bpy.data.libraries.load(str(other),link=False) as (available,loaded):loaded.objects=[rig['source_body_object']]
previous=loaded.objects[0]
def signatures(obj):
    mesh=obj.data;mesh.calc_loop_triangles();result={}
    for label,collection,key,width,dtype in [('positions',mesh.vertices,'co',3,np.float32),('triangles',mesh.loop_triangles,'vertices',3,np.int32)]:
        values=np.empty(len(collection)*width,dtype);collection.foreach_get(key,values);result[label]=hashlib.sha256(values.tobytes()).hexdigest()
    for index,layer in enumerate(mesh.uv_layers):
        values=np.empty(len(layer.data)*2,np.float32);layer.data.foreach_get('uv',values);result['uv'+str(index)]=hashlib.sha256(values.tobytes()).hexdigest()
    names=[g.name for g in obj.vertex_groups]
    groups=[sorted((names[g.group],float(g.weight)) for g in v.groups if g.weight>0) for v in mesh.vertices]
    result['weights']=hashlib.sha256(json.dumps(groups,separators=(',',':')).encode()).hexdigest()
    return result
a,b=signatures(current),signatures(previous)
report={'current':bpy.data.filepath,'previous':str(other),'current_data':a,'previous_data':b,'equal':a==b}
label=sys.argv[sys.argv.index('--lod')+1]
report_name=sys.argv[sys.argv.index('--report')+1] if '--report' in sys.argv else 'NormalOnlyRevision_'+label+'.json'
(root/'Production/LOD'/report_name).write_text(json.dumps(report,indent=2))
print('MACANH_NORMAL_ONLY_REVISION',label,'equal',report['equal'],flush=True)
assert report['equal'],'Normal revision changed audited geometry/UV/weights'
