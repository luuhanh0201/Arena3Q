"""Repair quaternion hemisphere flips and loop tangents without changing the rig/skin."""
import bpy
import math
import os
import sys
from mathutils import Quaternion, Matrix
sys.stdout.reconfigure(line_buffering=True)
root=os.path.dirname(os.path.abspath(__file__))
path=os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source=open(path,encoding='utf-8-sig').read()
ns={'__file__':path}
exec(compile(source[:source.rindex('\nLOG.clear()')],path,'exec'),ns)
ns['stop_playall']()
ns['play']('Idle')
bpy.context.scene.frame_set(1)
grip={p.name:p.rotation_quaternion.copy() for p in bpy.data.objects['LuBu_Rig'].pose.bones
      if p.name.startswith('Right') and any(d in p.name for d in ('Thumb','Index','Middle','Ring','Little'))}
for name in ns['EXPECTED_ACTIONS']:
    action=bpy.data.actions['LuBu_'+name]
    curves=ns['_bonus_action_curves'](action)
    groups={}
    for curve in curves:
        if curve.data_path.endswith('.rotation_quaternion'):
            groups.setdefault(curve.data_path,{})[curve.array_index]=curve
    changes=0
    for data_path,group in groups.items():
        if len(group)!=4: continue
        times=sorted({float(k.co.x) for curve in group.values() for k in curve.keyframe_points})
        previous=None
        samples=[]
        for time in times:
            q=Quaternion([group[i].evaluate(time) for i in range(4)])
            bone_name=data_path.split('"')[1]
            if name!='Defeat' and bone_name in grip: q=grip[bone_name].copy()
            q.normalize()
            if previous is not None and previous.dot(q)<0:
                q.negate(); changes+=1
            previous=q.copy(); samples.append((time,q))
        for i,curve in group.items():
            for time,q in samples:
                point=next((p for p in curve.keyframe_points if abs(p.co.x-time)<0.001),None)
                if point is None: point=curve.keyframe_points.insert(time,q[i],options={'FAST'})
                point.co.y=q[i]
                point.interpolation='BEZIER'
                point.handle_left_type=point.handle_right_type='AUTO_CLAMPED'
            curve.update()
    # Match the outgoing/incoming velocity at the loop boundary (no hitch each stride).
    if name in ('Idle','Walk','Run'):
        for curve in curves:
            points=curve.keyframe_points
            if len(points)<3 or curve.data_path.endswith('.scale'): continue
            first,second,penultimate,last=points[0],points[1],points[-2],points[-1]
            if abs(first.co.y-last.co.y)>0.001: continue
            span=(second.co.x-first.co.x)+(last.co.x-penultimate.co.x)
            slope=(second.co.y-penultimate.co.y)/max(span,0.001)
            first.handle_left_type=first.handle_right_type='FREE'
            last.handle_left_type=last.handle_right_type='FREE'
            dt=(second.co.x-first.co.x)/3
            first.handle_left=(first.co.x-dt,first.co.y-slope*dt)
            first.handle_right=(first.co.x+dt,first.co.y+slope*dt)
            dt=(last.co.x-penultimate.co.x)/3
            last.handle_left=(last.co.x-dt,last.co.y-slope*dt)
            last.handle_right=(last.co.x+dt,last.co.y+slope*dt)
            curve.update()
    print('SMOOTHED',name,'hemisphere corrections',changes,flush=True)
exec(compile(open(os.path.join(root,'LuBuNaturalArm.py'),encoding='utf-8-sig').read(),'LuBuNaturalArm.py','exec'))
# Seat the hand on the shaft and shorten the rear overhang for the chibi arm.
# Keep the blade, forward shaft, topology and UVs intact; the long rear end
# otherwise crosses the upper arm during the thrust.
weapon_mesh=bpy.data.objects['LuBu_Weapon'].data
weapon_mesh.transform(Matrix.Translation((0,0.10,0)))
for vertex in weapon_mesh.vertices:
    if vertex.co.y < 0: vertex.co.y *= 0.25
ns['fix_defeat_kneel_drop']()
ns['playall']()
output=os.path.join(root,'LuBu_Arena3Q_Smooth.blend')
bpy.ops.wm.save_as_mainfile(filepath=output,compress=True)
print('LUBU_SMOOTH_SOURCE_SUCCESS',flush=True)
