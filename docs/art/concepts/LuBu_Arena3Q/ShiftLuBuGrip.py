"""Moves Lu Bu's right-hand grip up the halberd shaft (user request 2026-10-20: "cầm sát đuôi quá, cầm cao lên 1 chút").

blender -b <in>.blend --python ShiftLuBuGrip.py -- --shift 0.10 [--save <out>.blend] [--preview <dir>] [--clips Idle,...]

1. Grip: the weapon mesh sits with the grip at its local origin (Socket_Hand_R), shaft along +Y towards the blade, so
   sliding the mesh by -shift along local Y puts the hand `shift` metres higher on the shaft (rest pose of the grip).
2. Slide: a higher grip lengthens the butt behind the hand. In frames where that longer butt would enter the body
   (elbow, upper arm, chest, cape) more than the old grip did, the shaft slides forward through the fist, back
   towards the old grip, by the smallest amount that clears it - the way a spear slides through the hand in a thrust.
   The slide is smoothed over time (no pops) and keyed as location of Socket_Hand_R along the shaft in that clip's
   action; every other clip gets a zero key so the value never leaks between clips. Hand, fingers and the rest of
   the pose are untouched; the Defeat drop action keeps working because its clip slides by zero.
Contact = weapon points inside the evaluated body (ray parity), outside the holding hand / forearm (which always
wraps the shaft). Report per clip: frames that slide, maximum slide, contacts old / higher grip / final.
"""
import bpy
import json
import os
import sys
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def arg(name, default, cast=str):
    return cast(argv[argv.index(name) + 1]) if name in argv else default


SHIFT = arg('--shift', 0.10, float)
SAVE = arg('--save', '')
PREVIEW = arg('--preview', '')
CLIPS = arg('--clips', 'Idle,Walk,Run,Attack01,Attack02,Victory,Defeat').split(',')
TOLERANCE = arg('--tolerance', 4, int)      # extra contact points allowed vs the old grip (sampling noise)
SLIDE_STEP = 0.01

root = os.path.dirname(os.path.abspath(__file__))
path = os.path.join(root, 'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source = open(path, encoding='utf-8-sig').read()
ns = {'__file__': path}
exec(compile(source[:source.rindex('\nLOG.clear()')], path, 'exec'), ns)

scene = bpy.context.scene
rig = bpy.data.objects['LuBu_Rig']
body = bpy.data.objects['LuBu_Body']
weapon = bpy.data.objects['LuBu_Weapon']
socket = rig.pose.bones['Socket_Hand_R']
me = weapon.data
wco = np.empty(len(me.vertices) * 3, dtype=np.float32)
me.vertices.foreach_get('co', wco)
wco = wco.reshape(-1, 3).astype(np.float64)
print(f'WEAPON local Y {wco[:, 1].min():.3f}..{wco[:, 1].max():.3f}; grip moves from 0.000 to {SHIFT:.3f}', flush=True)

names = [g.name for g in body.vertex_groups]
deform = {b.name for b in rig.data.bones if b.use_deform}
HAND = ('RightLowerArm', 'RightHand', 'RightThumb', 'RightIndex', 'RightMiddle', 'RightRing', 'RightLittle')
polys = [tuple(p.vertices) for p in body.data.polygons]
hand_vertex = np.zeros(len(body.data.vertices), dtype=bool)
for v in body.data.vertices:
    gs = [(g.weight, names[g.group]) for g in v.groups if names[g.group] in deform]
    hand_vertex[v.index] = bool(gs) and max(gs)[1].startswith(HAND)
hand_poly = np.array([hand_vertex[list(p)].mean() > 0.5 for p in polys])
# Butt half of the weapon (the part a higher grip pushes behind the hand) + a sparse sample of the rest.
butt = np.nonzero(wco[:, 1] < 0.35)[0]
sample = np.unique(np.concatenate([butt[::max(1, len(butt) // 1500)], np.arange(0, len(wco), max(1, len(wco) // 400))]))
pts = wco[sample]
rays = [Vector(v).normalized() for v in ((1, 0.13, 0.07), (-0.1, -1, 0.2), (0.05, 0.1, 1))]


def body_tree():
    dg = bpy.context.evaluated_depsgraph_get()
    ev = body.evaluated_get(dg)
    m = ev.to_mesh()
    a = np.empty(len(m.vertices) * 3)
    m.vertices.foreach_get('co', a)
    ev.to_mesh_clear()
    return BVHTree.FromPolygons([Vector(x) for x in a.reshape(-1, 3)], polys)


def contacts(tree, offset):
    """Weapon points inside the body outside the holding hand, with the mesh slid by -offset along local Y."""
    M = body.matrix_world.inverted() @ weapon.matrix_world
    n = 0
    for p in pts:
        pl = M @ Vector((p[0], p[1] - offset, p[2]))
        hits = 0
        for r in rays:
            h = tree.ray_cast(pl, r)
            if h[0] is not None and h[1].dot(r) > 0:
                hits += 1
        if hits >= 2:
            _, _, fi, _ = tree.find_nearest(pl)
            if fi is not None and not hand_poly[fi]:
                n += 1
    return n


def socket_key(action, frame, slide):
    """Key Socket_Hand_R so the weapon moves `slide` metres along its own +Y (back towards the old grip)."""
    socket.location = (0, 0, 0)
    bpy.context.view_layer.update()
    axis_arm = rig.matrix_world.inverted().to_3x3() @ (weapon.matrix_world.to_3x3() @ Vector((0, 1, 0))).normalized()
    pre = (socket.matrix @ socket.matrix_basis.inverted()).to_3x3()
    socket.location = pre.inverted() @ (axis_arm * slide)
    socket.keyframe_insert('location', frame=frame, group='Socket_Hand_R')


plan = {}
report = {'shift': SHIFT, 'clips': {}}
for clip in CLIPS:
    ns['play'](clip)
    act = rig.animation_data.action
    f0, f1 = map(int, act.frame_range)
    frames = list(range(f0, f1 + 1))
    need, old_c, high_c = [], [], []
    for f in frames:
        scene.frame_set(f)
        tree = body_tree()
        base = contacts(tree, 0.0)
        s, c = SHIFT, contacts(tree, SHIFT)
        high_c.append(c)
        while c > base + TOLERANCE and s > 1e-6:
            s = max(0.0, s - SLIDE_STEP)
            c = contacts(tree, s)
        need.append(SHIFT - s)
        old_c.append(base)
    need = np.array(need)
    # Smooth in time: hold the need 2 frames either side, then a 7-frame ease; never below what a frame needs.
    held = np.array([need[max(0, i - 2):i + 3].max() for i in range(len(need))])
    kernel = np.array([1, 2, 3, 4, 3, 2, 1], dtype=float)
    kernel /= kernel.sum()
    pad = np.pad(held, 3, mode='wrap' if act.use_cyclic else 'edge')
    slide = np.maximum(np.convolve(pad, kernel, mode='valid'), need)
    slide[slide < 0.003] = 0.0
    plan[clip] = (act, frames, slide)
    report['clips'][clip] = {'frames_sliding': int((slide > 0).sum()), 'max_slide_m': float(slide.max()),
                             'contacts_old_max': int(max(old_c)), 'contacts_higher_grip_max': int(max(high_c))}
    print(f'PLAN {clip:8s} slide frames {int((slide > 0).sum()):3d}/{len(frames)}  max slide {slide.max():.3f} m  '
          f'contacts old max {max(old_c)}  higher grip max {max(high_c)}', flush=True)


def render_set(tag):
    """Same frames and cameras before / after: full body 3/4 + side, and a close-up on the right forearm."""
    shots = {}
    for clip, t in (('Idle', 0.0), ('Walk', 0.25), ('Run', 0.5), ('Attack01', 0.45), ('Attack02', 0.3), ('Attack02', 0.45),
                    ('Victory', 0.6)):
        ns['play'](clip)
        act = rig.animation_data.action
        f0, f1 = act.frame_range
        f = int(f0 + (f1 - f0) * t)
        scene.frame_set(f)
        arm = rig.matrix_world @ rig.pose.bones['RightLowerArm'].matrix.to_translation()
        for vname, (target, off, scale) in {'34': (Vector((0, 0, 0.55)), Vector((3.5355, -3.5355, 3)), 2.9),
                                            'canh': (Vector((0, 0, 0.55)), Vector((5, 0, 1.2)), 2.9),
                                            'cancanh': (arm, Vector((1, 0.2, 1.5)).normalized() * 4, 0.6),
                                            'sau': (arm, Vector((-1, -1, 0.4)).normalized() * 4, 0.6)}.items():
            cam.location = target + off
            cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
            cam.data.ortho_scale = scale
            scene.render.filepath = os.path.join(PREVIEW, '_tmp.png')
            bpy.ops.render.render(write_still=True)
            img = bpy.data.images.load(scene.render.filepath, check_existing=False)
            a = np.empty(img.size[0] * img.size[1] * 4, dtype=np.float32)
            img.pixels.foreach_get(a)
            shots[f'{clip}_f{f:03d}_{vname}'] = a.reshape(img.size[1], img.size[0], 4)
            bpy.data.images.remove(img)
    os.remove(os.path.join(PREVIEW, '_tmp.png'))
    return shots


if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'TEXTURE'
    scene.render.resolution_x = scene.render.resolution_y = 460
    scene.render.film_transparent = False
    for o in bpy.data.objects:
        if o.type == 'CAMERA':
            o.hide_render = True
    cam = bpy.data.objects.new('GripCam', bpy.data.cameras.new('GripCam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = 'ORTHO'
    before = render_set('before')

# Apply: higher grip in the mesh, slide keys on Socket_Hand_R (zero keys on clips that do not slide).
me.vertices.foreach_set('co', (wco + [0, -SHIFT, 0]).astype(np.float32).ravel())
me.update()
for clip in ('Idle', 'Walk', 'Run', 'Attack01', 'Attack02', 'Victory', 'Defeat'):
    ns['play'](clip)
    act = rig.animation_data.action
    for fc in [fc for fc in act.fcurves if fc.data_path == 'pose.bones["Socket_Hand_R"].location']:
        act.fcurves.remove(fc)
    if clip in plan and plan[clip][2].max() > 0:
        _, frames, slide = plan[clip]
        for f, s in zip(frames, slide):
            scene.frame_set(f)
            socket_key(act, f, float(s))
    else:
        f0 = int(act.frame_range[0])
        scene.frame_set(f0)
        socket_key(act, f0, 0.0)
    for fc in act.fcurves:
        if fc.data_path == 'pose.bones["Socket_Hand_R"].location':
            for k in fc.keyframe_points:
                k.interpolation = 'LINEAR'
socket.location = (0, 0, 0)

final = {}
for clip in plan:
    ns['play'](clip)
    worst = 0
    for f in plan[clip][1][::2]:
        scene.frame_set(f)
        worst = max(worst, contacts(body_tree(), 0.0))   # mesh already shifted; keys already applied
    final[clip] = worst
    report['clips'][clip]['contacts_final_max'] = worst
    print(f'FINAL {clip:8s} contacts max {worst} (old grip max {report["clips"][clip]["contacts_old_max"]})', flush=True)

if PREVIEW:
    after = render_set('after')
    for key in before:
        b, a = before[key], after[key]
        gap = np.ones((b.shape[0], 12, 4), dtype=np.float32)
        gap[:, :, :3] = (1, 0.2, 0.2)
        sheet = np.concatenate([b, gap, a], axis=1)
        out = bpy.data.images.new('sheet', width=sheet.shape[1], height=sheet.shape[0], alpha=True)
        out.pixels.foreach_set(sheet.ravel())
        out.filepath_raw = os.path.join(PREVIEW, f'CamKich_{key}_truoc-trai_sau-phai.png')
        out.file_format = 'PNG'
        out.save()
        bpy.data.images.remove(out)
    json.dump(report, open(os.path.join(PREVIEW, 'grip_check.json'), 'w'), indent=1)

if SAVE:
    bpy.ops.wm.save_as_mainfile(filepath=SAVE)
    print('SAVED', SAVE, flush=True)
print('GRIP_DONE', flush=True)
