import bpy,json
from pathlib import Path
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
items=[]
for clip,frame in [('Idle',0),('Attack01',14),('Skill02',30),('Ultimate',30),('Ultimate',75)]:
    rig.animation_data.action=bpy.data.actions['MacAnh_'+clip];bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
    item={'clip':clip,'frame':frame,'bones':{}}
    for name in ['RightUpperArm','RightLowerArm','RightHand','Sleeve_Right_1']:
        p=rig.pose.bones[name];delta=p.matrix@p.bone.matrix_local.inverted()
        item['bones'][name]={'rotation_degrees':p.rotation_quaternion.angle*180/3.14159265,
            'rest_to_pose_translation':list(delta.translation),'head':list(p.head),'tail':list(p.tail)}
    items.append(item)
meshes=[{'name':o.name,'polygons':len(o.data.polygons),'parent':o.parent.name if o.parent else None} for o in bpy.data.objects if o.type=='MESH']
print('MACANH_POSE_DIAGNOSTIC',json.dumps({'poses':items,'meshes':meshes}),flush=True)
