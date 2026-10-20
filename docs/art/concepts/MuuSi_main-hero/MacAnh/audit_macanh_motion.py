"""Measure authored motion at integer and half frames, including event timing."""
import bpy,json,sys,hashlib
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent;rig=bpy.data.objects['MacAnh_Rig']
for obj in bpy.data.objects:
    if obj.type=='MESH':
        for mod in obj.modifiers:mod.show_viewport=False;mod.show_render=False
label=sys.argv[sys.argv.index('--label')+1] if '--label' in sys.argv else 'After'
results=[]
for action in sorted(bpy.data.actions,key=lambda a:a.name):
    if not action.name.startswith('MacAnh_'):continue
    rig.animation_data.action=action;lo,hi=map(int,action.frame_range)
    wrist=[];tip=[];scales=[]
    for f in np.arange(lo,hi+.1,.5):
        bpy.context.scene.frame_set(int(f),subframe=float(f%1));bpy.context.view_layer.update()
        wrist.append(list(rig.pose.bones['RightHand'].head))
        tip.append(list(bpy.data.objects['Socket_Fan_Tip'].matrix_world.translation))
        scales.append(max(abs(v-1) for p in rig.pose.bones for v in p.scale))
    wrist=np.array(wrist);tip=np.array(tip)
    assert np.isfinite(wrist).all() and np.isfinite(tip).all() and max(scales)<1e-5
    distance=np.linalg.norm(np.diff(wrist[::2],axis=0),axis=1)
    result=dict(name=action.name,half_frame_samples=len(wrist),
                max_wrist_step_m=float(distance.max()),max_step_frame=int(distance.argmax())+lo+1,
                max_fan_tip_step_m=float(np.linalg.norm(np.diff(tip[::2],axis=0),axis=1).max()),
                max_scale_error=max(scales),events=[dict(name=m.name,frame=m.frame) for m in action.pose_markers])
    results.append(result);print('MOTION_QA',label,result,flush=True)
(root/'Production/DeformationFix'/('MotionAudit_'+label+'.json')).write_text(json.dumps(results,indent=2))
