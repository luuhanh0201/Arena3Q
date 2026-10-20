"""Checks skin weights, UV preservation, bone scale and weapon clearance on actual poses."""
import bpy
import os
import json
import sys
sys.stdout.reconfigure(line_buffering=True)
root=os.path.dirname(os.path.abspath(__file__))
source_path=os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source=open(source_path,encoding='utf-8-sig').read()
ns={'__file__':source_path}
exec(compile(source[:source.rindex('\nLOG.clear()')],source_path,'exec'),ns)
rig=bpy.data.objects['LuBu_Rig']; body=bpy.data.objects['LuBu_Body']
step=0.5 if '--dense' in sys.argv else 1.0
report={'bones':len(rig.data.bones),'vertices':len(body.data.vertices),'sample_step':step,'clips':[]}
report['uv_checksum']=ns['_uv_fingerprint'](body.data)
report['unweighted']=sum(sum(g.weight for g in v.groups)<0.00001 for v in body.data.vertices)
report['max_weight_sum_error']=max(abs(sum(g.weight for g in v.groups)-1) for v in body.data.vertices)
report['max_influences']=max(sum(g.weight>0.00001 for g in v.groups) for v in body.data.vertices)
assert report['unweighted']==0 and report['max_influences']<=4
assert report['max_weight_sum_error']<0.001
ns['CLEAR_MARGIN']=0.0
for name in ns['EXPECTED_ACTIONS']:
    ns['play'](name)
    action=rig.animation_data.action
    item={'name':name,'first':int(action.frame_range[0]),'last':int(action.frame_range[1]),
          'samples':round((int(action.frame_range[1])-int(action.frame_range[0]))/step)+1,
          'max_scale_error':0,'weapon_intersections':[]}
    for sample in range(item['samples']):
        frame=item['first']+sample*step
        bpy.context.scene.frame_set(int(frame),subframe=frame-int(frame))
        error=max(abs(s-1) for p in rig.pose.bones for s in p.scale)
        item['max_scale_error']=max(item['max_scale_error'],error)
        # After release the falling weapon intentionally touches the floor/body vicinity.
        if name!='Defeat' and ns['_clearance'](ns['GRIP_GAP']) is not None:
            item['weapon_intersections'].append(frame)
    report['clips'].append(item)
    print('AUDIT',item,flush=True)
    assert item['max_scale_error']<0.001, item
with open(os.path.join(root,'RigAuditSmooth.json' if '--smooth' in sys.argv else 'RigAudit.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
assert not any(clip['weapon_intersections'] for clip in report['clips']), 'Weapon intersections remain; see RigAudit.json.'
print('LUBU_AUDIT_SUCCESS',flush=True)
