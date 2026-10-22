"""The fan follows the hand as far as it can without cutting into the cloth (user 2026-10-22: "lúc đánh hướng quạt bị
xoay sai ... chưa có ở vũ khí").

fix_macanh_fan_clearance.py keyed the grip socket so the fan kept one safe world orientation in every frame (it turned by
only 5-8 degrees while the arm swung 25-57 degrees): no contact, but no swing either. Simply following the hand cuts the
feathers into the sleeves and the head (up to 1807 crossing triangles in Skill02). This re-keys only Socket_Hand_R, on a
copy, frame by frame:
  - target = the hold it has standing (Idle frame 0) relative to the right hand, flicked about its own X axis around
    every release frame (wind-up back, strike forward, settle);
  - the fan is turned from the old safe orientation towards that target by the largest share (1 … 0) that leaves the
    feathers, frame and crystal clear of cloth and head (palm and fingers may touch: they hold it), measured exactly like
    audit_macanh_fan_contacts.py;
  - the share is smoothed over time (no jerks), never above what was clear; the grip stays in the palm.

blender -b Production/LOD/MacAnh_Battle_FanClearance.blend -P fix_macanh_fan_follow.py -- --lod Battle [--swing 35]
Writes Production/LOD/MacAnh_<lod>_FanFollow.blend (or --out <blend>) and FanFollowRevision_<lod or out name>.json; then run
audit_macanh_fan_contacts.py on it (half frames) before exporting.
"""
import bpy, sys, json, math
import numpy as np
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri

root = Path(__file__).resolve().parent
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
label = argv[argv.index('--lod') + 1] if '--lod' in argv else 'Battle'
swing = float(argv[argv.index('--swing') + 1]) if '--swing' in argv else 35.0
SHARES = [1.0, .85, .7, .55, .4, .25, .1, 0.0]
scene = bpy.context.scene
rig = bpy.data.objects['MacAnh_Rig']
fan = bpy.data.objects['MacAnh_Fan']
body = bpy.data.objects[rig['source_body_object']]
SOCKET = 'Socket_Hand_R'
RELEASES = {'Attack01': [14], 'Attack02': [15, 24, 33], 'Skill01': [33], 'Skill02': [30], 'Skill03': [27],
            'Ultimate': [75], 'DodgeAttack': [24]}

for modifier in body.modifiers:
    if modifier.type == 'ARMATURE':
        modifier.show_viewport = True
        modifier.use_deform_preserve_volume = False
fan_parts = [bpy.data.objects[n] for n in ('Fan_Feathers', 'Fan_GoldFrame', 'Fan_Crystal')]
for obj in fan_parts:
    obj.data.calc_loop_triangles()
fan_meshes = {o.name: (np.array([(*v.co, 1) for v in o.data.vertices]), [tuple(t.vertices) for t in o.data.loop_triangles])
              for o in fan_parts}
regions = [int(v.value) for v in body.data.attributes['GarmentRegion'].data]
body.data.calc_loop_triangles()
body_faces = [tuple(t.vertices) for t in body.data.loop_triangles]
body_polygons = [t.polygon_index for t in body.data.loop_triangles]


def crosses(a, b):
    for first, second in ((a, b), (b, a)):
        for i in range(3):
            start = first[i]
            delta = first[(i + 1) % 3] - start
            hit = intersect_ray_tri(*second, delta, start, True)
            if hit is not None and (hit - start).length <= delta.length + 1e-7:
                return True
    return False


def body_tree():
    dg = bpy.context.evaluated_depsgraph_get()
    evaluated = body.evaluated_get(dg)
    mesh = evaluated.to_mesh()
    points = [body.matrix_world @ v.co for v in mesh.vertices]
    evaluated.to_mesh_clear()
    return points, BVHTree.FromPolygons(points, body_faces, all_triangles=True)


def contacts(points, tree):
    total = 0
    for obj in fan_parts:
        vertices, faces = fan_meshes[obj.name]
        world = vertices @ np.array(obj.matrix_world).T
        fan_points = [Vector(v) for v in world[:, :3]]
        other = BVHTree.FromPolygons(fan_points, faces, all_triangles=True)
        for a, b in tree.overlap(other):
            if regions[body_polygons[a]] in (3, 4):        # palm and fingers hold it
                continue
            if crosses([points[i] for i in body_faces[a]], [fan_points[i] for i in faces[b]]):
                total += 1
    return total


def hand_world():
    return rig.matrix_world @ rig.pose.bones['RightHand'].matrix


def flick(name, frame, last):
    if name in ('Idle', 'Walk', 'Run', 'Dodge'):
        return 4.0 * math.sin(frame / max(1, last) * math.tau)
    total = 0.0
    for r in RELEASES.get(name, []):
        total += (-.6 * math.exp(-((frame - (r - 5)) / 3.5) ** 2) + math.exp(-((frame - (r + 1)) / 3.0) ** 2)
                  - .3 * math.exp(-((frame - (r + 7)) / 5.0) ** 2))
    return swing * total


def update(frame):
    scene.frame_set(frame)
    bpy.context.view_layer.update()


socket = rig.pose.bones[SOCKET]
rig.animation_data.action = bpy.data.actions['MacAnh_Idle']
update(0)
hold = (hand_world().inverted() @ fan.matrix_world).to_quaternion()
bind = socket.matrix.inverted() @ rig.matrix_world.inverted() @ fan.matrix_world   # socket → fan, constant


def socket_for(grip, rotation):
    return rig.matrix_world.inverted() @ Matrix.Translation(grip) @ rotation.to_matrix().to_4x4() @ bind.inverted()


report = {'source': bpy.data.filepath, 'edited_channel': SOCKET, 'swing_degrees': swing, 'shares': SHARES, 'clips': []}
for action in sorted(bpy.data.actions, key=lambda a: a.name):
    if not action.name.startswith('MacAnh_'):
        continue
    name = action.name.removeprefix('MacAnh_')
    rig.animation_data.action = action
    lo, hi = map(int, action.frame_range)
    frames = list(range(lo, hi + 1))
    samples = []
    # Pass 1: on the old (safe) keys, per frame: grip, safe and target orientations and the largest clear share.
    for frame in frames:
        update(frame)
        grip = fan.matrix_world.translation.copy()
        safe = fan.matrix_world.to_quaternion()
        target = hand_world().to_quaternion() @ hold @ Matrix.Rotation(math.radians(flick(name, frame, hi)), 4, 'X').to_quaternion()
        old = socket.matrix.copy()
        points, tree = body_tree()           # the cloth does not depend on the socket
        best = 0.0
        for share in SHARES:
            socket.matrix = socket_for(grip, safe.slerp(target, share))
            bpy.context.view_layer.update()
            if contacts(points, tree) == 0:
                best = share
                break
        socket.matrix = old
        bpy.context.view_layer.update()
        samples.append((grip, safe, target, best))
    raw = np.array([s[3] for s in samples])
    # Smooth: a widened minimum (the fan starts turning away before a tight spot), blurred, never above the clear share.
    widened = np.array([raw[max(0, i - 3):i + 4].min() for i in range(len(raw))])
    kernel = np.exp(-np.arange(-4, 5) ** 2 / (2 * 1.6 ** 2))
    blurred = np.convolve(np.pad(widened, 4, mode='edge'), kernel / kernel.sum(), mode='valid')
    shares = np.minimum(raw, blurred)
    curves = action.layers[0].strips[0].channelbag(action.slots[0]).fcurves if action.layers else action.fcurves

    def key_all():
        previous = None
        for frame, (grip, safe, target, _), share in zip(frames, samples, shares):
            update(frame)
            socket.matrix = socket_for(grip, safe.slerp(target, float(share)))
            q = socket.rotation_quaternion.copy()
            if previous is not None and previous.dot(q) < 0:
                q.negate()
                socket.rotation_quaternion = q
            previous = q.copy()
            socket.scale = (1, 1, 1)
            for channel in ('location', 'rotation_quaternion', 'scale'):
                socket.keyframe_insert(channel, frame=frame)
        for curve in curves:
            if '"' + SOCKET + '"' in curve.data_path:
                for key in curve.keyframe_points:
                    key.interpolation = 'BEZIER'
                    key.handle_left_type = key.handle_right_type = 'AUTO_CLAMPED'

    # Pass 2: key, then check the half frames between keys (the curves can swing past a clear key); where one
    # still touches, lower the share of both neighbouring keys a step and key again.
    key_all()
    for attempt in range(10):
        touching = []
        for i in range(len(frames) - 1):
            scene.frame_set(frames[i], subframe=.5)
            bpy.context.view_layer.update()
            if contacts(*body_tree()):
                touching.append(i)
        if not touching:
            break
        for i in touching:
            for j in (i - 1, i, i + 1, i + 2):
                if 0 <= j < len(shares):
                    shares[j] = max(0.0, shares[j] - .15)
        print('FAN_FOLLOW', action.name, 'half-frame contacts at', [frames[i] + .5 for i in touching], flush=True)
        key_all()
    first_world = None
    max_world = 0.0
    for frame in frames:
        update(frame)
        world = fan.matrix_world.to_quaternion()
        first_world = first_world or world
        max_world = max(max_world, math.degrees(first_world.rotation_difference(world).angle))
    entry = {'name': action.name, 'frames': [lo, hi], 'mean_share': round(float(shares.mean()), 3),
             'min_share': round(float(shares.min()), 3), 'fan_world_max_deg': round(max_world, 1)}
    report['clips'].append(entry)
    print('FAN_FOLLOW', action.name, 'share mean', entry['mean_share'], 'min', entry['min_share'],
          'world', entry['fan_world_max_deg'], flush=True)

rig.animation_data.action = bpy.data.actions['MacAnh_Idle']
update(0)
destination = Path(argv[argv.index('--out') + 1]).resolve() if '--out' in argv else root / 'Production/LOD' / ('MacAnh_' + label + '_FanFollow.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(destination), compress=True)
report['destination'] = str(destination)
(root / 'Production/LOD' / ('FanFollowRevision_' + (destination.stem.removeprefix('MacAnh_') if '--out' in argv else label) + '.json')).write_text(json.dumps(report, indent=2))
print('FAN_FOLLOW_COMPLETE', label, flush=True)
