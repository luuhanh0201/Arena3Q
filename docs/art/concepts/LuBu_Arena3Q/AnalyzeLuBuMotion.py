import bpy
import json
import math
import os
import sys
root=os.path.dirname(os.path.abspath(__file__))
source=open(os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py'),encoding='utf-8-sig').read()
ns={}
exec(compile(source[:source.rindex('\nLOG.clear()')],'LuBuLibrary','exec'),ns)
rig=bpy.data.objects['LuBu_Rig']
for obj in bpy.context.scene.objects:
    for modifier in obj.modifiers: modifier.show_viewport=False
report={}
for name in ns['EXPECTED_ACTIONS']:
    ns['play'](name)
    action=rig.animation_data.action
    previous={}; changes=[]
    for frame in range(int(action.frame_range[0]),int(action.frame_range[1])+1):
        bpy.context.scene.frame_set(frame)
        for bone in rig.pose.bones:
            q=bone.rotation_quaternion.normalized()
            if bone.name in previous:
                angle=math.degrees(2*math.acos(min(1,abs(q.dot(previous[bone.name])))))
                changes.append((round(angle,2),frame,bone.name))
            previous[bone.name]=q.copy()
    report[name]=sorted(changes,reverse=True)[:12]
with open(os.path.join(root,'MotionAfter.json' if '--after' in sys.argv else 'MotionBefore.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print(json.dumps(report),flush=True)
