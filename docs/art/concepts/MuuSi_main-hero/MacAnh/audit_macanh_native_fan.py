"""Triangle contacts from Unity's imported geometry and sampled native poses."""
import sys,json,struct,time
from pathlib import Path
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri
root=Path(__file__).resolve().parent;project=root.parents[4]
label=sys.argv[sys.argv.index('--lod')+1]
proof=json.loads((project/'Library/MacAnhNativeContacts'/(label+'.json')).read_text())
assert proof['passed'] and proof['fanParts']==6
stream=open(project/'Library/MacAnhNativeContacts'/(label+'.bin'),'rb')
def integer():return struct.unpack('<i',stream.read(4))[0]
def floats(count):return np.frombuffer(stream.read(count*4),dtype='<f4')
def ints(count):return np.frombuffer(stream.read(count*4),dtype='<i4')
def string():return stream.read(integer()).decode('utf-8')
assert stream.read(8)==b'MAFAN001'
n,nt,nb,nf,ns=(integer() for _ in range(5))
rest=floats(n*3).reshape(n,3);faces=ints(nt).reshape(-1,3)
indices=np.empty((n,4),np.int32);weights=np.empty((n,4),np.float32);palm=np.empty(n,bool)
for i in range(n):indices[i]=ints(4);weights[i]=floats(4);palm[i]=bool(stream.read(1)[0])
assert np.allclose(weights.sum(axis=1),1,atol=1e-5)
allowed=palm[faces].all(axis=1);parts=[]
for i in range(nf):
    name=string();nv=integer();ntri=integer()
    parts.append((name,floats(nv*3).reshape(nv,3),ints(ntri).reshape(-1,3)))
def crosses(a,b):
    for first,second in [(a,b),(b,a)]:
        for i in range(3):
            start=first[i];delta=first[(i+1)%3]-start
            hit=intersect_ray_tri(*second,delta,start,True)
            if hit is not None and (hit-start).length<=delta.length+1e-7:return True
    return False
report={'lod':label,'source':'Unity imported mesh/weights and sampled native animation; pose reconstruction independently checked against BakeMesh every half-frame',
        'unity_proof':proof,'clips':[],'allowed_contact':'palm/finger surfaces only, all six fan parts tested'}
clips={};started=time.time()
for sample in range(ns):
    name=string();seconds=float(floats(1)[0]);matrices=floats(nb*16).reshape(nb,4,4)
    transforms=floats(nf*16).reshape(nf,4,4)
    body=np.zeros_like(rest)
    for column in range(4):
        if np.any(weights[:,column]):
            selected=matrices[indices[:,column]]
            body+=(np.einsum('nij,nj->ni',selected[:,:3,:3],rest)+selected[:,:3,3])*weights[:,column,None]
    worlds=[vertices@transform[:3,:3].T+transform[:3,3] for (_,vertices,_),transform in zip(parts,transforms)]
    all_fan=np.concatenate(worlds);low=all_fan.min(axis=0)-1e-6;high=all_fan.max(axis=0)+1e-6
    triangle_points=body[faces]
    relevant=np.flatnonzero(~allowed & (triangle_points.max(axis=1)>=low).all(axis=1)&(triangle_points.min(axis=1)<=high).all(axis=1))
    ids,inverse=np.unique(faces[relevant],return_inverse=True)
    body_points=[Vector(v) for v in body[ids]];body_faces=inverse.reshape(-1,3)
    tree=BVHTree.FromPolygons(body_points,body_faces,all_triangles=True)
    total=0;details=[]
    for (part,vertices,triangles),world in zip(parts,worlds):
        fp=[Vector(v) for v in world];ft=BVHTree.FromPolygons(fp,triangles,all_triangles=True)
        for a,b in tree.overlap(ft):
            if crosses([body_points[i] for i in body_faces[a]],[fp[i] for i in triangles[b]]):
                total+=1
                if len(details)<20:details.append({'part':part,'native_body_triangle':int(relevant[a]),'fan_triangle':b})
    if name not in clips:
        if clips:print('NATIVE_FAN_QA',next(reversed(clips)),clips[next(reversed(clips))]['max_contacts'],flush=True)
        clips[name]={'name':name,'samples':0,'max_contacts':0,'worst':None}
    result=clips[name];result['samples']+=1
    if total>result['max_contacts']:result['max_contacts']=total;result['worst']={'seconds':seconds,'frame':seconds*30,'contacts':details}
assert not stream.read(1),'Unexpected trailing pose data'
report['clips']=list(clips.values());report['seconds']=time.time()-started
report['passed']=len(clips)==11 and ns==proof['expectedSamples'] and all(c['max_contacts']==0 for c in clips.values())
target=root/'Production/LOD'/('ImportedFanContacts_'+label+'.json');target.write_text(json.dumps(report,indent=2))
print('NATIVE_FAN_CONTACT_AUDIT_COMPLETE',label,'passed',report['passed'],flush=True)
assert report['passed'],'Actual imported fan still penetrates a non-palm surface'
