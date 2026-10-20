"""Surface-distance garment partition; writes a review cache, never the source."""
from pathlib import Path
import sys,numpy as np,json
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import dijkstra
from PIL import Image,ImageDraw
cache=project/'Library/MacAnhRigAudit';d=np.load(cache/'actual_saved_weights_before.npz')
co=d['co'];x,y,z=co.T;ids=d['indices'];weights=d['weights'];names=d['names']
hand=(weights*np.isin(ids,[i for i,n in enumerate(names) if 'Hand' in n or any(a in n for a in ('Thumb','Index','Middle','Ring','Little'))])).sum(axis=1)
region=(abs(x)>.18)&(z>.10)&(z<.87)&(d['hair']<.1)&(hand<.1)
edges=np.load(cache/'surface_edges.npy');keep=region[edges].all(axis=1);e=edges[keep]
length=np.linalg.norm(co[e[:,0]]-co[e[:,1]],axis=1)
graph=coo_matrix((length,(e[:,0],e[:,1])),shape=(len(co),len(co))).tocsr()
armseed=region&(((abs(x)>.28)&(z>.67)&(z<.80)&(y<-.035))|((abs(x)>.36)&(z>.34)&(z<.62)&(y<.04)))
robeseed=region&(((abs(x)<.195)&(z<.57))|((y>.16)&(z<.62)))
arm=dijkstra(graph,directed=False,indices=np.flatnonzero(armseed),min_only=True)
robe=dijkstra(graph,directed=False,indices=np.flatnonzero(robeseed),min_only=True)
factor=np.zeros(len(co),np.float32)
valid=region&np.isfinite(arm)&np.isfinite(robe)
# 3 cm seam transition measured along the surface, independent of texture hues.
factor[valid]=np.clip(.5+(robe[valid]-arm[valid])/.16,0,1)
factor[region&np.isfinite(arm)&~np.isfinite(robe)]=1
np.savez_compressed(cache/'cloth_partition.npz',region=region,arm=arm,robe=robe,factor=factor)
uv=np.load(cache/'surface_uv.npy');tex=Image.open(next((project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source').glob('Image_0.jpg'))).convert('RGB')
pix=np.asarray(tex);xy=np.clip(uv,0,.999999)*np.array(tex.size);rgb=pix[(tex.height-1-xy[:,1].astype(int)),xy[:,0].astype(int)]
panels=[]
for title,axis,depth in [('Front',x,y),('Back',-x,-y),('Right side',y,x)]:
    w,h=650,1050;px=np.clip(((axis+.56)*550+15).astype(int),0,w-1);py=np.clip(((1.75-z)*550+40).astype(int),0,h-1);flat=py*w+px
    order=np.lexsort((depth,flat));unique,first=np.unique(flat[order],return_index=True);k=order[first]
    pixels=np.full((h*w,3),34,np.uint8);color=rgb[k].astype(float);active=region[k]
    overlay=np.stack([255*factor[k],200*(1-factor[k]),70+factor[k]*40],axis=1)
    color[active]=.35*color[active]+.65*overlay[active];pixels[unique]=color.astype(np.uint8)
    im=Image.fromarray(pixels.reshape(h,w,3));ImageDraw.Draw(im).text((10,10),title+' orange sleeve / green robe',fill='white');panels.append(im)
im=Image.new('RGB',(1950,1050))
for i,p in enumerate(panels):im.paste(p,(i*650,0))
im.save(root/'Production/UltimateFix/ClothPartition_Review.png')
print('PARTITION',int(region.sum()),int((factor>.5).sum()),flush=True)
