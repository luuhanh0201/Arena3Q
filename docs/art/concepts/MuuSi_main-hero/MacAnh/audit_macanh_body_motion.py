"""Body acting audit (Motion4, 2026-10-22): arm/hand triangles crossing the torso by garment-region pair, feet
slipping, and smoothness (angular speed / acceleration / jerk at quarter frames) of pelvis, chest, head and both hands.
blender -b <blend> -P audit_macanh_body_motion.py -- <out.json> [Clip1,Clip2,...]
Compare against the same audit on the previous blend: crossings and jerk must not jump; a hand speed in the hundreds
of degrees per frame is a flip."""
import bpy, sys, math, json
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri
argv = sys.argv[sys.argv.index('--') + 1:]; out = argv[0]; only = argv[1].split(',') if len(argv) > 1 else None
rig = bpy.data.objects['MacAnh_Rig']; scene = bpy.context.scene
body = bpy.data.objects[rig['source_body_object']]
for m in body.modifiers:
    if m.type == 'ARMATURE': m.show_viewport = True; m.use_deform_preserve_volume = False
regions = [int(v.value) for v in body.data.attributes['GarmentRegion'].data]
body.data.calc_loop_triangles()
faces = [tuple(t.vertices) for t in body.data.loop_triangles]
region_of = [regions[t.polygon_index] for t in body.data.loop_triangles]
arm_tris = [i for i, r in enumerate(region_of) if r in (1, 2, 3, 4)]
torso_tris = [i for i, r in enumerate(region_of) if r not in (1, 2, 3, 4)]
def crosses(a, b):
    for first, second in ((a, b), (b, a)):
        for i in range(3):
            s = first[i]; d = first[(i + 1) % 3] - s
            h = intersect_ray_tri(*second, d, s, True)
            if h is not None and (h - s).length <= d.length + 1e-7: return True
    return False
def quat(name): return (rig.matrix_world @ rig.pose.bones[name].matrix).to_quaternion()
report = {}
for action in sorted(bpy.data.actions, key=lambda a: a.name):
    name = action.name.removeprefix('MacAnh_')
    if not action.name.startswith('MacAnh_') or (only and name not in only): continue
    rig.animation_data.action = action; lo, hi = map(int, action.frame_range)
    pairs = {}; worst = 0; feet0 = None; slip = 0
    for t in np.arange(lo, hi + 1e-6, .5):
        scene.frame_set(int(t), subframe=float(t % 1)); bpy.context.view_layer.update()
        feet = [rig.matrix_world @ rig.pose.bones[f].head for f in ('LeftFoot', 'RightFoot')]
        feet0 = feet0 or feet; slip = max(slip, max((a - b).length for a, b in zip(feet, feet0)))
        ev = body.evaluated_get(bpy.context.evaluated_depsgraph_get()); mesh = ev.to_mesh()
        pts = [body.matrix_world @ v.co for v in mesh.vertices]; ev.to_mesh_clear()
        arm = BVHTree.FromPolygons(pts, [faces[i] for i in arm_tris], all_triangles=True)
        torso = BVHTree.FromPolygons(pts, [faces[i] for i in torso_tris], all_triangles=True)
        count = 0; here = {}
        for a, b in arm.overlap(torso):
            fa = faces[arm_tris[a]]; fb = faces[torso_tris[b]]
            if set(fa) & set(fb): continue                     # neighbours along a seam
            if crosses([pts[i] for i in fa], [pts[i] for i in fb]):
                key = f'{region_of[arm_tris[a]]}-{region_of[torso_tris[b]]}'; here[key] = here.get(key, 0) + 1; count += 1
        for k, v in here.items(): pairs[k] = max(pairs.get(k, 0), v)
        if count > worst: worst = count; worst_t = float(t)
    smooth = {}
    for bone in ('Pelvis', 'Chest', 'Head', 'RightHand', 'LeftHand'):
        Q = []
        for t in np.arange(lo, hi + 1e-6, .25):
            scene.frame_set(int(t), subframe=float(t % 1)); bpy.context.view_layer.update(); Q.append(quat(bone))
        w = []
        for i in range(len(Q) - 1):
            axis, a = Q[i].rotation_difference(Q[i + 1]).to_axis_angle(); w.append(np.array(axis) * a * 4)
        w = np.array(w)
        smooth[bone] = dict(speed=round(float(np.linalg.norm(w, axis=1).max() * 57.3), 1),
                            acc=round(float(np.linalg.norm(np.diff(w, axis=0), axis=1).max() * 4 * 57.3), 1),
                            jerk=round(float(np.linalg.norm(np.diff(w, n=2, axis=0), axis=1).max() * 16 * 57.3)))
    report[name] = dict(arm_torso_crossings=worst, at=worst_t if worst else None, pairs=pairs, foot_slip_m=round(slip, 4), smooth=smooth)
    print('BODY', name, 'cross', worst, 'pairs', pairs, 'slip', round(slip, 4),
          ' '.join(f"{b}:{v['speed']}/{v['acc']}/{v['jerk']}" for b, v in smooth.items()), flush=True)
open(out, 'w').write(json.dumps(report, indent=1)); print('BODY_DONE', flush=True)
