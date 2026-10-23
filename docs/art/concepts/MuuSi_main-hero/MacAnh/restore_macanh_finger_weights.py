"""Restore right-hand digit deformation; work on a separate candidate file."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
# Match the actual low-poly finger rows. Original little-finger landmarks lay
# outside the hand, so no surface could follow those joints.
bpy.context.view_layer.objects.active=r;r.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
landmarks={
 'Thumb':((-.354,-.185,.617),(-.333,-.207,.590)),
 'Index':((-.390,-.195,.602),(-.435,-.199,.565)),
 'Middle':((-.390,-.173,.602),(-.424,-.175,.552)),
 'Ring':((-.385,-.153,.605),(-.414,-.152,.551)),
 'Little':((-.376,-.130,.611),(-.405,-.132,.560))}
for digit,(start,end) in landmarks.items():
 start,end=Vector(start),Vector(end);points=[start.lerp(end,t) for t in (0,.46,.8,1)]
 for j in range(3):
  bone=r.data.edit_bones['Right'+digit+str(j+1)];bone.head=points[j];bone.tail=points[j+1]
bpy.ops.object.mode_set(mode='OBJECT')
ids={i for p,a in zip(m.polygons,m.attributes['GarmentRegion'].data) if a.value==3 for i in p.vertices}
# Keep continuous weights across wrist-lining contacts.
names=['RightHand']+['Right'+d+str(i) for d in ('Thumb','Index','Middle','Ring','Little') for i in (1,2,3)]
bones=[r.data.bones[n] for n in names]
def distance(p,b):
    a=b.head_local;d=b.tail_local-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared))
    return (p-a-d*t).length
counts={n:0 for n in names}
weights={}
for i in ids:
    v=m.vertices[i];ds=sorted((distance(v.co,b),n) for b,n in zip(bones,names))[:4]
    values=[math.exp(-250*(d-ds[0][0])) for d,n in ds];total=sum(values)
    weights[i]={n:w/total for (d,n),w in zip(ds,values)}
# Blend across actual mesh edges to avoid abrupt thumb/palm assignment where
# the authored joint landmarks do not coincide with the low-poly surface.
neighbors={i:set() for i in ids}
for edge in m.edges:
    a,b=edge.vertices
    if a in ids and b in ids:neighbors[a].add(b);neighbors[b].add(a)
for iteration in range(10):
    updated={}
    for i,original in weights.items():
        adjacent=neighbors[i]
        if not adjacent:updated[i]=original;continue
        combined={n:.65*original.get(n,0)+.35*sum(weights[j].get(n,0) for j in adjacent)/len(adjacent) for n in names}
        best=sorted(combined.items(),key=lambda item:item[1],reverse=True)[:4]
        total=sum(w for n,w in best);updated[i]={n:w/total for n,w in best}
    weights=updated
for i,values in weights.items():
    for g in list(m.vertices[i].groups):o.vertex_groups[g.group].remove([i])
    for n,w in values.items():
        o.vertex_groups[n].add([i],w,'REPLACE')
        if w>.05:counts[n]+=1
m.update()
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
print('FINGER_WEIGHTS_RESTORED',counts,flush=True)
