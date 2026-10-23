"""Candidate smooth secondary weights on the exterior; preserve face/limb regions."""
import bpy,sys,json,math
import numpy as np
from pathlib import Path
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
region={v.index:set() for v in m.vertices};exterior=set()
for p in m.polygons:
    for i in p.vertices:region[i].add(m.attributes['GarmentRegion'].data[p.index].value)
    if 'DualUvBody' in m.materials[p.material_index].name:exterior.update(p.vertices)
report={'hair':0,'robe':0}
for v in m.vertices:
    if v.index not in exterior or region[v.index]!={0}:continue
    x,y,z=v.co;side='Right' if x<0 else 'Left'
    # Blend over broad spatial intervals. Texture colour must not create a
    # discontinuity across adjacent vertices on the same surface.
    hair=smooth(.035,.20,y)*smooth(.30,.60,z)*(1-smooth(1.04,1.28,z))*smooth(.04,.18,abs(x))
    robe=(1-smooth(.42,.62,z))*smooth(.045,.15,z)*(1-hair)
    if max(hair,robe)<.005:continue
    old={o.vertex_groups[g.group].name:g.weight for g in v.groups}
    if hair>robe:
        family='Hair_'+side+'_';u=max(0,min(3,(1.22-z)/.19));amount=.18*hair*smooth(0,.3,hair-robe);key='hair'
    else:
        row='Inner'
        family='Robe_'+side+'_'+row+'_';u=max(0,min(2,(.57-z)/.16));amount=.25*robe*smooth(.02,.12,abs(x))*smooth(0,.3,robe-hair);key='robe'
    j=min((2 if key=='hair' else 1),int(u));t=u-j
    weights={n:a*(1-amount) for n,a in old.items()}
    for n,a in [(family+str(j+1),amount*(1-t)),(family+str(j+2),amount*t)]:weights[n]=weights.get(n,0)+a
    # At most two source bones plus two chain bones for this region.
    weights=dict(sorted(weights.items(),key=lambda p:-p[1])[:4]);total=sum(weights.values())
    for g in list(v.groups):o.vertex_groups[g.group].remove([v.index])
    for n,a in weights.items():
        if a>1e-7:o.vertex_groups[n].add([v.index],a/total,'REPLACE')
    report[key]+=1
tassels=bpy.data.objects['Fan_Tassels']
for v in tassels.data.vertices:
    depth=max(0,min(1,(-.025-v.co.z)/.13))
    v.co.y-=.10*depth*depth
tassels.data.update()
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
Path('Logs/MacAnh-secondary-weight-candidate.json').write_text(json.dumps(report,indent=2))
print('SECONDARY_WEIGHTS_CANDIDATE_COMPLETE',report,flush=True)
