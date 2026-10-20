"""Compare original-atlas colour at corresponding low/high surface samples."""
import bpy,sys,json
from pathlib import Path
import numpy as np
from mathutils import Vector
from mathutils.geometry import barycentric_transform
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from PIL import Image
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
with bpy.data.libraries.load(str(root.parent/'muu-si.blend'),link=False) as (available,loaded):loaded.objects=[rig['source_body_object']]
high=loaded.objects[0];bpy.context.scene.collection.objects.link(high);high.parent=None;high.matrix_world.identity();high.modifiers.clear()
bpy.context.view_layer.update();tree=BVHTree.FromObject(high,bpy.context.evaluated_depsgraph_get())
image=np.asarray(Image.open(project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/Image_0.jpg').convert('RGB')).astype(np.float32)/255
h,w=image.shape[:2]
def colour(uv):
    x=max(0,min(w-1,float(uv[0])*w));y=max(0,min(h-1,(1-float(uv[1]))*h))
    x0,y0=int(x),int(y);x1,y1=min(w-1,x0+1),min(h-1,y0+1);a,b=x-x0,y-y0
    return (image[y0,x0]*(1-a)+image[y0,x1]*a)*(1-b)+(image[y1,x0]*(1-a)+image[y1,x1]*a)*b
faces=[f for f in body.data.polygons if f.material_index==0 and f.area>1e-9]
rng=np.random.default_rng(20261020);areas=np.array([f.area for f in faces]);areas/=areas.sum()
indices=rng.choice(len(faces),size=5000,replace=True,p=areas)
errors=[];distances=[];worst=[]
for fi in indices:
    face=faces[fi];point=sum((body.data.vertices[i].co for i in face.vertices),Vector())/len(face.vertices)
    hit,normal,index,distance=tree.find_nearest(point)
    hf=high.data.polygons[index];assert len(hf.vertices)==3
    hs=[high.data.vertices[i].co for i in hf.vertices]
    hu=[Vector((*high.data.uv_layers[0].data[i].uv,0)) for i in hf.loop_indices]
    uv=barycentric_transform(hit,*hs,*hu)
    low_uv=sum((body.data.uv_layers[0].data[i].uv for i in face.loop_indices),Vector((0,0)))/len(face.vertices)
    error=float(np.abs(colour(uv)-colour(low_uv)).mean());errors.append(error);distances.append(float(distance))
    if error>.2:worst.append({'face':face.index,'position':list(point),'mae':error,'distance_m':float(distance)})
report={'samples':len(errors),'rgb_mae':float(np.mean(errors)),'rgb_p95_error':float(np.percentile(errors,95)),
    'distance_p95_m':float(np.percentile(distances,95)),'rgb_error_above_0_2':len(worst),'worst':sorted(worst,key=lambda item:-item['mae'])[:20]}
(root/'Production/LOD/ColourCorrespondence.json').write_text(json.dumps(report,indent=2))
print('MACANH_COLOUR_CORRESPONDENCE',json.dumps({k:v for k,v in report.items() if k!='worst'}),flush=True)
