"""Read-only audit: actual saved skin weights and arm leakage below the waist."""
import bpy,os,json,hashlib,numpy as np,time
from pathlib import Path
root=Path(__file__).resolve().parent
out=root/'Production'/'DodgeAttackFix';out.mkdir(parents=True,exist_ok=True)
cache=root.parents[4]/'Library'/'MacAnhRigAudit';cache.mkdir(parents=True,exist_ok=True)
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
for mod in body.modifiers:mod.show_viewport=False;mod.show_render=False
co=np.empty(len(body.data.vertices)*3,dtype=np.float32);body.data.vertices.foreach_get('co',co);co=co.reshape(-1,3)
names=[g.name for g in body.vertex_groups]
indices=np.zeros((len(co),4),np.int16);weights=np.zeros((len(co),4),np.float32)
for v in body.data.vertices:
    values=sorted([(g.group,g.weight) for g in v.groups if g.weight>0],key=lambda x:-x[1])[:4]
    for j,(group,weight) in enumerate(values):indices[v.index,j]=group;weights[v.index,j]=weight
arm_ids=[i for i,n in enumerate(names) if n.startswith(('RightShoulder','RightUpperArm','RightLowerArm','RightHand','RightThumb','RightIndex','RightMiddle','RightRing','RightLittle','LeftShoulder','LeftUpperArm','LeftLowerArm','LeftHand','LeftThumb','LeftIndex','LeftMiddle','LeftRing','LeftLittle','Sleeve_'))]
hair_ids=[i for i,n in enumerate(names) if n.startswith(('Hair_','Ribbon_'))]
arm=(weights*np.isin(indices,arm_ids)).sum(axis=1)
hair=(weights*np.isin(indices,hair_ids)).sum(axis=1)
report={'source':bpy.data.filepath,'source_sha256':hashlib.file_digest(open(bpy.data.filepath,'rb'),'sha256').hexdigest(),
 'coordinates_sha256':hashlib.sha256(co.tobytes()).hexdigest(),'vertices':len(co),'bones':len(rig.data.bones),
 'parents':{b.name:b.parent.name if b.parent else None for b in rig.data.bones},'regions':{}}
x,y,z=co.T
for label,mask in [('central_lower',(abs(x)<.23)&(z<.565)&(z>.1)&(hair<.1)),('front_lower',(y<-.035)&(z<.565)&(z>.1)&(hair<.1)),('all_lower',(z<.565)&(hair<.1))]:
    bad=np.flatnonzero(mask&(arm>.01));dominant={}
    for idx in arm_ids:
        count=int(np.sum(mask&((weights*(indices==idx)).sum(axis=1)>.05)))
        if count:dominant[names[idx]]=count
    order=bad[np.argsort(arm[bad])[-20:]] if len(bad) else []
    report['regions'][label]={'vertices':int(mask.sum()),'arm_influenced':len(bad),'max_arm_mass':float(arm[mask].max(initial=0)),
      'groups':dominant,'examples':[{'vertex':int(k),'co':co[k].tolist(),'weights':[(names[int(i)],float(w)) for i,w in zip(indices[k],weights[k]) if w>0]} for k in order]}
np.savez_compressed(cache/'actual_saved_weights_before.npz',co=co,indices=indices,weights=weights,names=np.array(names),arm=arm,hair=hair)
(out/'LowerCloth_Audit_Before.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('LOWER_CLOTH_AUDIT',json.dumps({k:{a:b for a,b in v.items() if a!='examples'} for k,v in report['regions'].items()}),flush=True)
