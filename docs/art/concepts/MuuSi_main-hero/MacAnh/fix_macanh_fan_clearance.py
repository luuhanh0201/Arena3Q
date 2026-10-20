"""Revise only the grip socket and loose sleeve action channels on LOD copies."""
import bpy,sys,json,hashlib,math
from pathlib import Path
root=Path(__file__).resolve().parent
sys.path.insert(0,str(root))
from macanh_fan_clearance import apply_pose,gesture,sweep_tassels
label=sys.argv[sys.argv.index('--lod')+1]
source=Path(bpy.data.filepath)
assert source.resolve()!=(root.parent/'muu-si.blend').resolve()
rig=bpy.data.objects['MacAnh_Rig'];fan=bpy.data.objects['MacAnh_Fan']
names=['Socket_Hand_R','Sleeve_Right_1']
report={'source':str(source),'edited_channels':names,'tassel_tip_sweep_m':.04,'clips':[]}
for action in sorted(bpy.data.actions,key=lambda a:a.name):
    if not action.name.startswith('MacAnh_'):continue
    rig.animation_data.action=action
    lo,hi=map(int,action.frame_range);samples=[];max_grip_error=0
    # Sample the entire old action first; editing a previous key must never
    # contaminate interpolation of the next unmodified sample.
    for frame in range(lo,hi+1):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        grip=fan.matrix_world.translation.copy()
        turn,rotation=gesture(action.name.removeprefix('MacAnh_'),frame,hi)
        apply_pose(rig,fan,turn,rotation)
        max_grip_error=max(max_grip_error,(fan.matrix_world.translation-grip).length)
        samples.append({name:(rig.pose.bones[name].location.copy(),rig.pose.bones[name].rotation_quaternion.copy()) for name in names})
    previous={}
    for frame,values in zip(range(lo,hi+1),samples):
        for name,(location,q) in values.items():
            bone=rig.pose.bones[name]
            if name in previous and previous[name].dot(q)<0:q.negate()
            bone.location=location;bone.rotation_quaternion=q;bone.scale=(1,1,1);previous[name]=q.copy()
            for channel in ('location','rotation_quaternion','scale'):bone.keyframe_insert(channel,frame=frame)
    curves=action.layers[0].strips[0].channelbag(action.slots[0]).fcurves if action.layers else action.fcurves
    for curve in curves:
        if any('"'+name+'"' in curve.data_path for name in names):
            for key in curve.keyframe_points:
                key.interpolation='BEZIER';key.handle_left_type=key.handle_right_type='AUTO_CLAMPED'
    assert max_grip_error<1e-5
    report['clips'].append({'name':action.name,'frames':[lo,hi],'grip_error_m':max_grip_error})
sweep_tassels(.04)
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(0)
destination=root/'Production/LOD'/('MacAnh_'+label+'_FanClearance.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
report['destination']=str(destination)
(root/'Production/LOD'/('FanClearanceRevision_'+label+'.json')).write_text(json.dumps(report,indent=2))
print('FAN_CLEARANCE_ACTION_REVISION_COMPLETE',label,flush=True)
