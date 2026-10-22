"""Read-only whole-rig motion and joint coverage measurements at 120 Hz."""
import bpy,json,math,sys
import numpy as np
from pathlib import Path
rig=bpy.data.objects['MacAnh_Rig']; scene=bpy.context.scene
families={'hair':'Hair_','ribbon':'Ribbon_','robe':'Robe_','sleeve':'Sleeve_'}
report={}
for action in bpy.data.actions:
    if not action.name.startswith('MacAnh_'):continue
    rig.animation_data.action=action
    names=[b.name for b in rig.pose.bones]
    samples=[]; worlds=[]; joints=[]
    for f in np.arange(action.frame_range[0],action.frame_range[1]+.01,.25):
        scene.frame_set(int(f),subframe=float(f%1))
        samples.append([rig.pose.bones[n].rotation_quaternion.copy() for n in names])
        worlds.append([rig.pose.bones[n].matrix.to_quaternion() for n in names])
        joints.append(max((rig.pose.bones[s+a].tail-rig.pose.bones[s+b].head).length
                          for s in ('Right','Left') for a,b in [('UpperArm','LowerArm'),('LowerArm','Hand')]))
    data={}
    for family,prefix in families.items():
        ids=[i for i,n in enumerate(names) if n.startswith(prefix)]
        speeds=[abs(samples[k][i].rotation_difference(samples[k+1][i]).angle)*180/math.pi*120
                for k in range(len(samples)-1) for i in ids]
        ranges=[max(samples[0][i].rotation_difference(row[i]).angle for row in samples)*180/math.pi for i in ids]
        data[family]={'bones':len(ids),'max_local_degrees_per_second':max(speeds,default=0),
                      'max_local_range_degrees':max(ranges,default=0)}
        data[family]['worst_world']=max((dict(bone=names[i],frame=k/4,speed=min(q.angle,2*math.pi-q.angle)*180/math.pi*120)
            for k in range(len(worlds)-1) for i in ids
            for q in [worlds[k][i].rotation_difference(worlds[k+1][i])]),key=lambda x:x['speed'])
    report[action.name]={'joint_gap_m':max(joints),'secondary':data}
    print(action.name,report[action.name],flush=True)
Path(sys.argv[sys.argv.index('--')+1]).write_text(json.dumps(report,indent=2))
print('SECONDARY_AUDIT_COMPLETE',flush=True)
