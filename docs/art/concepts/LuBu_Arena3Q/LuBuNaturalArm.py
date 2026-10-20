"""Stable two-bone arm solve with a continuous elbow plane and no stretching.
Executed by SmoothLuBuMotion.py with its bpy/ns/root globals.
"""
from mathutils import Vector, Matrix, Quaternion
rig=bpy.data.objects['LuBu_Rig']
body=bpy.data.objects['LuBu_Body']
modifiers=[m for m in body.modifiers if m.type=='ARMATURE']
flags=[m.show_viewport for m in modifiers]
for m in modifiers: m.show_viewport=False
arm_names=('RightShoulder','RightUpperArm','RightLowerArm','RightHand')
socket_bind=rig.data.bones['RightHand'].matrix_local.inverted() @ rig.data.bones['Socket_Hand_R'].matrix_local
idle_p=(-0.29,-0.20,-0.05); idle_d=(-0.45,-0.10,1)
paths={
 'Attack01':[(1,idle_p,idle_d),(4,(-.27,-.13,.09),(-.35,.15,.92)),
             (7,(-.30,-.08,.10),(-.60,.45,.65)),(11,(-.17,-.30,-.03),(.65,-.72,.10)),
             (15,(-.06,-.28,-.08),(.85,-.40,-.05)),(21,(-.24,-.20,.02),(.35,-.30,.88)),(28,idle_p,idle_d)],
 'Attack02':[(1,idle_p,idle_d),(4,(-.27,-.13,-.04),(.05,-.50,.86)),
             (7,(-.30,-.10,-.05),(.05,-.97,.20)),(10,(-.17,-.36,-.02),(.32,-1,.12)),
             (13,(-.14,-.37,-.03),(.32,-1,.12)),(18,(-.21,-.24,-.02),(.12,-.8,.6)),
             (23,(-.23,-.19,-.02),(.03,-.3,.95)),(28,idle_p,idle_d)]}
def path_sample(keys,frame,column):
    index=next((i for i in range(len(keys)-1) if frame<=keys[i+1][0]),len(keys)-2)
    a,b=keys[index],keys[index+1]
    t=max(0,min(1,(frame-a[0])/(b[0]-a[0])))
    p0=Vector(keys[max(0,index-1)][column]); p1=Vector(a[column])
    p2=Vector(b[column]); p3=Vector(keys[min(len(keys)-1,index+2)][column])
    return .5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
upper_rest=rig.data.bones['RightUpperArm']; lower_rest=rig.data.bones['RightLowerArm']
rest_normal=((upper_rest.tail_local-upper_rest.head_local).cross(lower_rest.tail_local-lower_rest.head_local)).normalized()
roll_offsets={}
for rest in (upper_rest,lower_rest):
    y=(rest.tail_local-rest.head_local).normalized()
    x=y.cross(rest_normal).normalized(); z=x.cross(y).normalized()
    roll_offsets[rest.name]=Matrix((x,y,z)).transposed().inverted() @ rest.matrix_local.to_3x3()
def aim_bone(pose,target,normal):
    direction=(target-pose.head).normalized()
    x=direction.cross(normal).normalized(); z=x.cross(direction).normalized()
    rotation=Matrix((x,direction,z)).transposed() @ roll_offsets[pose.name]
    pose.matrix=Matrix.Translation(pose.head) @ rotation.to_4x4()
    pose.scale=(1,1,1)
    bpy.context.view_layer.update()
def place(point,shaft):
    for name in arm_names:
        rig.pose.bones[name].rotation_quaternion=(1,0,0,0)
        rig.pose.bones[name].scale=(1,1,1)
    bpy.context.view_layer.update()
    chest=rig.pose.bones['Chest']
    rotation=chest.matrix.to_3x3() @ chest.bone.matrix_local.to_3x3().inverted()
    grip_point=chest.head+rotation @ point
    shaft=(rotation @ shaft).normalized()
    front=rotation @ Vector((0,-1,0))
    side=front-shaft*front.dot(shaft)
    edge=side.normalized()
    z=edge.cross(shaft).normalized(); edge=shaft.cross(z).normalized()
    socket_rotation=Matrix((edge,shaft,z)).transposed()
    hand_rotation=socket_rotation @ socket_bind.to_3x3().inverted()
    wrist=grip_point-hand_rotation @ socket_bind.to_translation()
    clavicle=rig.pose.bones['RightShoulder']
    clavicle_rotation=clavicle.matrix.to_3x3()
    clavicle_axis=(clavicle_rotation @ Vector((0,1,0))).normalized()
    shoulder_turn=clavicle_axis.rotation_difference((wrist-clavicle.head).normalized())
    shoulder_turn=Quaternion(shoulder_turn.axis,min(shoulder_turn.angle,math.radians(35)))
    clavicle.matrix=Matrix.Translation(clavicle.head) @ (shoulder_turn.to_matrix() @ clavicle_rotation).to_4x4()
    clavicle.scale=(1,1,1)
    bpy.context.view_layer.update()
    upper=rig.pose.bones['RightUpperArm']; lower=rig.pose.bones['RightLowerArm']
    shoulder=upper.head.copy(); delta=wrist-shoulder
    l1=upper.bone.length; l2=lower.bone.length
    length=max(abs(l1-l2)+.004,min(delta.length,(l1+l2)*.985))
    axis=delta.normalized(); wrist=shoulder+axis*length
    pole=rotation @ (Vector((.8,.5,-.6)) if active_clip.startswith('Attack') else Vector((-.65,.2,-1)))
    pole=(pole-axis*pole.dot(axis)).normalized()
    along=(l1*l1-l2*l2+length*length)/(2*length)
    elbow=shoulder+axis*along+pole*math.sqrt(max(0,l1*l1-along*along))
    normal=((elbow-shoulder).cross(wrist-elbow)).normalized()
    aim_bone(upper,elbow,normal)
    aim_bone(lower,wrist,normal)
    hand=rig.pose.bones['RightHand']
    hand.matrix=Matrix.Translation(lower.tail) @ hand_rotation.to_4x4()
    hand.scale=(1,1,1)
    bpy.context.view_layer.update()

for name in ('Idle','Walk','Run','Attack01','Attack02'):
    active_clip=name
    ns['play'](name)
    action=rig.animation_data.action
    first,last=map(int,action.frame_range)
    previous={}
    for frame in range(first,last+1):
        bpy.context.scene.frame_set(frame)
        rig.animation_data.action=None
        if name in paths:
            point=path_sample(paths[name],frame,1)
            point.x=min(-.29,point.x)
            point.y=max(-.24,min(-.17,point.y))
            shaft=path_sample(paths[name],frame,2).normalized()
            # Keep the thrust/sweep slightly above horizontal: crossing the pole
            # of the blade's roll reference creates a spurious half-turn.
            shaft.z=max(0.10,shaft.z); shaft.normalize()
        else:
            point=Vector(idle_p); shaft=Vector(idle_d)
            if name=='Run': point=Vector((-.29,-.20,-.06)); shaft=Vector((.5,-.75,.65))
            elif name=='Walk':
                point.z+=.004*math.sin((frame-first)/(last-first)*math.tau)
                shaft=Vector((-.45,.35,1))
        place(point,shaft)
        rotations={bone:rig.pose.bones[bone].rotation_quaternion.normalized() for bone in arm_names}
        rig.animation_data.action=action
        for bone_name in arm_names:
            bone=rig.pose.bones[bone_name]
            q=rotations[bone_name]
            if bone_name in previous and previous[bone_name].dot(q)<0: q.negate()
            bone.rotation_quaternion=q; previous[bone_name]=q.copy()
            bone.keyframe_insert('rotation_quaternion',frame=frame)
            bone.keyframe_insert('scale',frame=frame)
    for curve in ns['_bonus_action_curves'](action):
        if not any('"'+bone+'"' in curve.data_path for bone in arm_names): continue
        for point in curve.keyframe_points:
            point.interpolation='BEZIER'; point.handle_left_type=point.handle_right_type='AUTO_CLAMPED'
        curve.update()
    print('NATURAL_ARM',name,flush=True)
for m,flag in zip(modifiers,flags): m.show_viewport=flag
