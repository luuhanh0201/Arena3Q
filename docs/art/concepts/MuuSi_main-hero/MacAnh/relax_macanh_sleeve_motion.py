"""Bake a short inertial lag for loose sleeves without changing body/fan timing."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Matrix
rig=bpy.data.objects['MacAnh_Rig'];scene=bpy.context.scene
report=[]
for action in list(bpy.data.actions):
    if not action.name.startswith('MacAnh_'):continue
    rig.animation_data.action=action
    first,last=map(int,action.frame_range)
    for name in ('Sleeve_Right_1','Sleeve_Left_1'):
        # This raised-fan cast has a narrow feather/crystal clearance corridor.
        # Preserve its authored right cuff; the abrupt swing is on the free arm.
        if action.name=='MacAnh_Skill01' and name=='Sleeve_Right_1':continue
        bone=rig.pose.bones[name]
        snapshot=[]
        for frame in range(first,last+1):
            scene.frame_set(frame)
            snapshot.append((bone.matrix.copy(),bone.parent.matrix.copy()))
        targets=[]
        for frame in range(len(snapshot)):
            center=frame-.75
            q=snapshot[frame][0].to_quaternion()
            total=0
            for k in range(max(0,frame-6),min(len(snapshot),frame+6)):
                w=math.exp(-.5*((k-center)/2.0)**2)
                other=snapshot[k][0].to_quaternion()
                if q.dot(other)<0:other.negate()
                if total==0:q=other
                else:q=q.slerp(other,w/(total+w))
                total+=w
            # Exact original endpoints retain clip-transition and loop poses.
            envelope=min(1,frame/5,(len(snapshot)-1-frame)/5)
            envelope=envelope*envelope*(3-2*envelope)
            q=snapshot[frame][0].to_quaternion().slerp(q,envelope)
            desired=Matrix.Translation(snapshot[frame][0].translation)@q.to_matrix().to_4x4()
            basis=bone.bone.matrix_local.inverted()@bone.parent.bone.matrix_local@snapshot[frame][1].inverted()@desired
            targets.append(basis.to_quaternion())
        previous=None
        for frame,q in enumerate(targets,first):
            if previous is not None and previous.dot(q)<0:q.negate()
            bone.rotation_quaternion=q
            bone.keyframe_insert('rotation_quaternion',frame=frame)
            previous=q.copy()
        report.append(dict(action=action.name,bone=name,frames=len(targets)))
    curves=action.layers[0].strips[0].channelbag(action.slots[0]).fcurves if action.layers else action.fcurves
    for curve in curves:
        if any('"'+n+'"' in curve.data_path for n in ('Sleeve_Right_1','Sleeve_Left_1')) and 'rotation_quaternion' in curve.data_path:
            for key in curve.keyframe_points:
                key.interpolation='BEZIER';key.handle_left_type=key.handle_right_type='AUTO_CLAMPED'
rig.animation_data.action=bpy.data.actions.get('MacAnh_Idle');scene.frame_set(0)
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve()
assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
out.with_suffix('.json').write_text(json.dumps(report,indent=2))
print('SLEEVE_RELAX_COMPLETE',flush=True)
