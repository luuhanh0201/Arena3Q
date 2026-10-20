"""Extract action transforms for a pose-aware candidate solver, without saving."""
import bpy,sys
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parent;cache=root.parents[4]/'Library/MacAnhDeformation/Battle'
if '--cache-name' in sys.argv:cache=cache.parent/sys.argv[sys.argv.index('--cache-name')+1]
rig=bpy.data.objects['MacAnh_Rig'];data=np.load(cache/'weights.npz');names=list(data['names'])
rest=np.array([rig.data.bones[n].matrix_local.inverted() for n in names],np.float32)
matrices=[];labels=[]
for action in sorted(bpy.data.actions,key=lambda a:a.name):
    if not action.name.startswith('MacAnh_'):continue
    rig.animation_data.action=action
    lo,hi=map(int,action.frame_range)
    for frame in sorted(set(range(lo,hi+1,5))|{hi}|{m.frame for m in action.pose_markers}):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        matrices.append(np.array([rig.pose.bones[n].matrix for n in names],np.float32)@rest)
        labels.append(action.name+':'+str(frame))
np.savez_compressed(cache/'poses.npz',matrices=np.array(matrices),labels=labels)
print('MACANH_SKIN_POSES_CACHED',len(labels),flush=True)
