"""Lữ Bố battle LOD for the 3D presentation (docs/KeHoach/ChuyenSang3D.md P3).

blender -b LuBu_Arena3Q_Grip.blend -P ExportLuBuBattle3D.py -- --out <folder> [--body 24000] [--weapon 6000]
        [--stride 2] [--no-export]

Works on copies in memory; the .blend is never saved.
1. Body and weapon copies are decimated to the battle budget (the decimator interpolates the weights, UVs stay).
2. Unity skins linearly with at most 4 bones, while the approved Blender look uses the Armature with Preserve Volume
   (dual quaternion) plus the Corrective Smooth on CS_Zone. New linear 4-bone weights are fitted per vertex so the
   LOD follows that look over every clip: least squares over all sampled frames, weights >= 0 summing to 1, a small
   pull towards the old weights; candidates are the vertex's own bones and its neighbours' bones.
3. Surface audit per clip (docs/Tuong/Model3D_Rules.md, same metric as DiagnoseLuBuSkin.py): edges stretched > 1.35
   or squashed < 0.65 by more than 0.4 % of the height, for the Blender look, plain top-4 linear skinning and the fit.
4. FBX: rig + both meshes + the 7 clips (Unity units, no 100x scale); the weapon's images are saved next to it.
Writes <out>/LuBu_Battle_Audit.json and prints LUBU_EXPORT_DONE at the end.
"""
import json
import os
import sys
import time

import bpy
import numpy as np

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def arg(name, default):
    return argv[argv.index(name) + 1] if name in argv else default


OUT = os.path.abspath(arg('--out', os.path.join(os.path.dirname(bpy.data.filepath), 'Battle3D')))
BODY_TRIS = int(arg('--body', 32000))
WEAPON_TRIS = int(arg('--weapon', 6000))
STRIDE = int(arg('--stride', 2))
CLIPS = ['Idle', 'Walk', 'Run', 'Attack01', 'Attack02', 'Victory', 'Defeat']
CANDIDATES = 6
INFLUENCES = 4
os.makedirs(OUT, exist_ok=True)

scene = bpy.context.scene
rig = bpy.data.objects['LuBu_Rig']
body_src = bpy.data.objects['LuBu_Body']
weapon_src = bpy.data.objects['LuBu_Weapon']
started = time.time()


def log(*items):
    print('[LuBu3D]', f'{time.time() - started:6.1f}s', *items, flush=True)


def copy_object(src, name):
    obj = src.copy()
    obj.data = src.data.copy()
    obj.name = name
    obj.data.name = name
    scene.collection.objects.link(obj)
    return obj


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def decimate(obj, budget):
    before = triangles(obj)
    if before > budget:
        activate(obj)
        modifier = obj.modifiers.new('Budget', 'DECIMATE')
        modifier.decimate_type = 'COLLAPSE'
        modifier.ratio = budget / before
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_move_to_index(modifier='Budget', index=0)
        bpy.ops.object.modifier_apply(modifier='Budget')
    return before, triangles(obj)


body = copy_object(body_src, 'LuBu_Battle_Body')
weapon = copy_object(weapon_src, 'LuBu_Battle_Weapon')
# The full-resolution weapon would still be evaluated every frame; this session never saves the file. The full body
# stays: its approved deformation is the target the LOD follows.
bpy.data.objects.remove(weapon_src, do_unlink=True)
log('body', decimate(body, BODY_TRIS), 'weapon', decimate(weapon, WEAPON_TRIS))

me = body.data
n = len(me.vertices)
group_names = [g.name for g in body.vertex_groups]
bones = [b.name for b in rig.data.bones if b.use_deform and b.name in group_names]
bone_of_group = {body.vertex_groups[name].index: j for j, name in enumerate(bones)}
B = len(bones)
W = np.zeros((n, B), dtype=np.float64)
for v in me.vertices:
    for g in v.groups:
        j = bone_of_group.get(g.group)
        if j is not None:
            W[v.index, j] = g.weight
W /= np.maximum(W.sum(1, keepdims=True), 1e-9)
rest = np.empty(n * 3)
me.vertices.foreach_get('co', rest)
rest = rest.reshape(-1, 3)
rest_h = np.concatenate([rest, np.ones((n, 1))], 1)
edges = np.empty(len(me.edges) * 2, dtype=np.int64)
me.edges.foreach_get('vertices', edges)
edges = edges.reshape(-1, 2)
height = float(np.ptp(rest[:, 2]))
L0 = np.linalg.norm(rest[edges[:, 0]] - rest[edges[:, 1]], axis=1)
valid = L0 > 1e-6 * height
log(f'{n} vertices, {B} deform bones, height {height:.3f} m')

mesh_to_rig = rig.matrix_world.inverted() @ body.matrix_world
rig_to_mesh = mesh_to_rig.inverted()
rest_inverse = {name: rig.data.bones[name].matrix_local.inverted() for name in bones}


def skin_matrices():
    mats = np.empty((B, 4, 4))
    for j, name in enumerate(bones):
        mats[j] = np.array(rig_to_mesh @ rig.pose.bones[name].matrix @ rest_inverse[name] @ mesh_to_rig)
    return mats


def evaluated(obj=None):
    obj = obj or body
    depsgraph = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(depsgraph)
    mesh = ev.to_mesh()
    co = np.empty(len(mesh.vertices) * 3)
    mesh.vertices.foreach_get('co', co)
    ev.to_mesh_clear()
    return co.reshape(-1, 3)


# Every LOD vertex follows the point of the full-resolution surface it came from (nearest point, barycentric), so
# the target is the approved look of the full mesh, not Corrective Smooth re-run on the coarser LOD.
from mathutils import Vector
from mathutils.bvhtree import BVHTree
src_me = body_src.data
src_me.calc_loop_triangles()
src_rest = np.empty(len(src_me.vertices) * 3)
src_me.vertices.foreach_get('co', src_rest)
src_rest = src_rest.reshape(-1, 3)
src_tris = np.empty(len(src_me.loop_triangles) * 3, dtype=np.int64)
src_me.loop_triangles.foreach_get('vertices', src_tris)
src_tris = src_tris.reshape(-1, 3)
bvh = BVHTree.FromPolygons([tuple(p) for p in src_rest], [tuple(int(i) for i in t) for t in src_tris])
transfer_tri = np.empty((n, 3), dtype=np.int64)
transfer_bary = np.empty((n, 3))
for i in range(n):
    location, _, index, _ = bvh.find_nearest(Vector(rest[i]))
    a, b, c = src_tris[index]
    pa, pb, pc = src_rest[a], src_rest[b], src_rest[c]
    v0, v1, v2 = pb - pa, pc - pa, np.array(location) - pa
    d00, d01, d11, d20, d21 = v0 @ v0, v0 @ v1, v1 @ v1, v2 @ v0, v2 @ v1
    denom = d00 * d11 - d01 * d01
    v = (d11 * d20 - d01 * d21) / denom if denom > 1e-18 else 0.0
    w = (d00 * d21 - d01 * d20) / denom if denom > 1e-18 else 0.0
    transfer_tri[i] = (a, b, c)
    transfer_bary[i] = (1 - v - w, v, w)
transfer_offset = rest - np.einsum('nk,nkd->nd', transfer_bary, src_rest[transfer_tri])
src_edges = np.empty(len(src_me.edges) * 2, dtype=np.int64)
src_me.edges.foreach_get('vertices', src_edges)
src_edges = src_edges.reshape(-1, 2)
src_L0 = np.linalg.norm(src_rest[src_edges[:, 0]] - src_rest[src_edges[:, 1]], axis=1)
src_valid = src_L0 > 1e-6 * height


def transferred(points):
    return np.einsum('nk,nkd->nd', transfer_bary, points[transfer_tri]) + transfer_offset


def bad_fraction_source(points):
    L = np.linalg.norm(points[src_edges[:, 0]] - points[src_edges[:, 1]], axis=1)
    r = np.where(src_valid, L / np.where(src_valid, src_L0, 1), 1.0)
    bad = src_valid & (((r > 1.35) & (L - src_L0 > 0.004 * height)) | ((r < 0.65) & (src_L0 - L > 0.004 * height)))
    return float(bad.sum()) / max(1, int(src_valid.sum()))


def bad_edges(points):
    L = np.linalg.norm(points[edges[:, 0]] - points[edges[:, 1]], axis=1)
    r = np.where(valid, L / np.where(valid, L0, 1), 1.0)
    return int((valid & (((r > 1.35) & (L - L0 > 0.004 * height)) | ((r < 0.65) & (L0 - L > 0.004 * height)))).sum())


def bad_against(points, reference):
    # Edges whose length differs from the approved shape itself (not from the rest pose): distortion the skinning adds.
    L = np.linalg.norm(points[edges[:, 0]] - points[edges[:, 1]], axis=1)
    R = np.linalg.norm(reference[edges[:, 0]] - reference[edges[:, 1]], axis=1)
    ok = valid & (R > 1e-6 * height)
    r = np.where(ok, L / np.where(ok, R, 1), 1.0)
    return int((ok & (((r > 1.35) & (L - R > 0.004 * height)) | ((r < 0.65) & (R - L > 0.004 * height)))).sum())


def linear(weights, mats):
    # Σ_b w_b · (M_b · v)
    moved = np.einsum('bij,nj->bni', mats[:, :3, :], rest_h)
    return np.einsum('nb,bni->ni', weights, moved)


def top(weights, k):
    keep = np.argsort(-weights, 1)[:, :k]
    limited = np.zeros_like(weights)
    rows = np.arange(len(weights))[:, None]
    limited[rows, keep] = weights[rows, keep]
    return limited / np.maximum(limited.sum(1, keepdims=True), 1e-9)


# ---- Sample the approved deformation over every clip ----
frames, targets, matrices, source_bad = [], [], [], []
for clip in CLIPS:
    action = bpy.data.actions['LuBu_' + clip]
    rig.animation_data.action = action
    first, last = map(int, action.frame_range)
    for frame in sorted(set(list(range(first, last + 1, STRIDE)) + [last])):
        scene.frame_set(frame)
        frames.append((clip, frame))
        full = evaluated(body_src)
        source_bad.append(bad_fraction_source(full))
        targets.append(transferred(full).astype(np.float32))
        matrices.append(skin_matrices().astype(np.float32))
T = np.stack(targets)
M = np.stack(matrices)
F = len(frames)
log(f'sampled {F} frames')

# ---- Candidates: own bones and neighbours' bones ----
neighbour = np.zeros_like(W)
degree = np.zeros(n)
np.add.at(neighbour, edges[:, 0], W[edges[:, 1]])
np.add.at(neighbour, edges[:, 1], W[edges[:, 0]])
np.add.at(degree, edges[:, 0], 1)
np.add.at(degree, edges[:, 1], 1)
score = W + 0.5 * neighbour / np.maximum(degree, 1)[:, None]
candidates = np.argsort(-score, 1)[:, :CANDIDATES]
candidate_live = np.take_along_axis(score, candidates, 1) > 1e-6

# ---- Fit ----
fitted = np.zeros_like(W)
K = CANDIDATES
chunk = 384
for start in range(0, n, chunk):
    idx = np.arange(start, min(n, start + chunk))
    c = candidates[idx]
    Mc = M[:, c]                                                   # F, m, K, 4, 4
    P = np.einsum('fmkij,mj->fmki', Mc[..., :3, :], rest_h[idx].astype(np.float32)).astype(np.float64)
    t = T[:, idx].astype(np.float64)                               # F, m, 3
    G = np.einsum('fmki,fmli->mkl', P, P)
    h = np.einsum('fmki,fmi->mk', P, t)
    w0 = np.take_along_axis(W[idx], c, 1)
    w0 /= np.maximum(w0.sum(1, keepdims=True), 1e-9)
    reg = 1e-3 * np.trace(G, axis1=1, axis2=2) / K
    G = G + reg[:, None, None] * np.eye(K)[None]
    h = h + reg[:, None] * w0
    active = candidate_live[idx].copy()
    for iteration in range(6):
        a = active.astype(np.float64)
        Ga = G * a[:, :, None] * a[:, None, :] + np.eye(K)[None] * (1 - a)[:, :, None]
        kkt = np.zeros((len(idx), K + 1, K + 1))
        kkt[:, :K, :K] = Ga
        kkt[:, :K, K] = a
        kkt[:, K, :K] = a
        rhs = np.concatenate([h * a, np.ones((len(idx), 1))], 1)
        w = np.linalg.solve(kkt, rhs[..., None])[..., 0][:, :K] * a
        negative = (w < 0) & active
        over = active.sum(1) > INFLUENCES
        if not negative.any() and not over.any():
            break
        active &= ~negative
        # Too many influences left: drop the smallest of each such vertex and solve again.
        rank = np.argsort(np.argsort(-np.where(active, w, -np.inf), 1), 1)
        active &= ~(over[:, None] & (rank >= INFLUENCES) & ~negative.any(1, keepdims=True))
    w = np.clip(w, 0, None) * active
    w /= np.maximum(w.sum(1, keepdims=True), 1e-9)
    rows = np.arange(len(idx))[:, None]
    fitted[idx[:, None], c] = w
fitted = top(fitted, INFLUENCES)
log('weights fitted')

# ---- Audit: Blender look, plain top-4 linear, fitted ----
plain = top(W, INFLUENCES)
report = {'source': bpy.data.filepath, 'height': height, 'vertices': n, 'body_triangles': triangles(body),
          'weapon_triangles': triangles(weapon), 'frames': F, 'stride': STRIDE, 'clips': {}}
for clip in CLIPS:
    rows = [i for i, (name, _) in enumerate(frames) if name == clip]
    item = {'blender_bad_edges_max': 0, 'plain_bad_edges_max': 0, 'fitted_bad_edges_max': 0,
            'source_bad_fraction_max': max(source_bad[i] for i in rows), 'fitted_bad_fraction_max': 0.0,
            'plain_rms_mm': 0.0, 'fitted_rms_mm': 0.0, 'fitted_max_mm': 0.0,
            'plain_vs_approved_bad_max': 0, 'fitted_vs_approved_bad_max': 0}
    for i in rows:
        mats = M[i].astype(np.float64)
        target = T[i].astype(np.float64)
        p_plain = linear(plain, mats)
        p_fit = linear(fitted, mats)
        item['blender_bad_edges_max'] = max(item['blender_bad_edges_max'], bad_edges(target))
        item['plain_bad_edges_max'] = max(item['plain_bad_edges_max'], bad_edges(p_plain))
        item['plain_vs_approved_bad_max'] = max(item['plain_vs_approved_bad_max'], bad_against(p_plain, target))
        item['fitted_vs_approved_bad_max'] = max(item['fitted_vs_approved_bad_max'], bad_against(p_fit, target))
        fit_bad = bad_edges(p_fit)
        item['fitted_bad_fraction_max'] = max(item['fitted_bad_fraction_max'], fit_bad / max(1, int(valid.sum())))
        if fit_bad > item['fitted_bad_edges_max']:
            item['fitted_bad_edges_max'], item['worst_frame'] = fit_bad, frames[i][1]
        item['plain_rms_mm'] = max(item['plain_rms_mm'], float(np.sqrt(((p_plain - target) ** 2).sum(1).mean())) * 1000)
        item['fitted_rms_mm'] = max(item['fitted_rms_mm'], float(np.sqrt(((p_fit - target) ** 2).sum(1).mean())) * 1000)
        item['fitted_max_mm'] = max(item['fitted_max_mm'], float(np.sqrt(((p_fit - target) ** 2).sum(1)).max()) * 1000)
    # Passed: in Unity's skinning the LOD adds no stretched / squashed edge to the approved full-resolution look
    # (edge lengths against that look, not against the rest pose: long LOD edges across a bent joint change chord
    # length even on a perfect surface).
    item['passed'] = item['fitted_vs_approved_bad_max'] == 0
    report['clips'][clip] = item
    log('AUDIT', clip, item)
report['passed'] = all(c['passed'] for c in report['clips'].values())

# ---- Write the fitted weights; Unity's skinning from here on ----
for name in group_names:
    if name not in bones:
        body.vertex_groups.remove(body.vertex_groups[name])
for j, name in enumerate(bones):
    group = body.vertex_groups[name]
    group.remove(list(range(n)))
    for i in np.nonzero(fitted[:, j] > 1e-5)[0]:
        group.add([int(i)], float(fitted[i, j]), 'REPLACE')
for modifier in list(body.modifiers):
    if modifier.type == 'CORRECTIVE_SMOOTH':
        body.modifiers.remove(modifier)
    elif modifier.type == 'ARMATURE':
        modifier.use_deform_preserve_volume = False

# Check in Blender itself (same linear skinning) on the frames the audit flagged.
check = {}
for clip in CLIPS:
    frame = report['clips'][clip].get('worst_frame')
    if frame is None:
        continue
    rig.animation_data.action = bpy.data.actions['LuBu_' + clip]
    scene.frame_set(frame)
    check[clip] = bad_edges(evaluated())
report['blender_linear_check'] = check
log('blender linear check', check)

with open(os.path.join(OUT, 'LuBu_Battle_Audit.json'), 'w', encoding='utf-8') as f:
    json.dump(report, f, indent=1)

if '--no-export' not in argv:
    # Only the 7 clips go into the FBX (no backups, the weapon drop is played in Unity).
    for action in list(bpy.data.actions):
        if action.name not in {'LuBu_' + c for c in CLIPS}:
            bpy.data.actions.remove(action)
    rig.animation_data.action = bpy.data.actions['LuBu_Idle']
    scene.frame_set(1)
    for image in {node.image for slot in weapon.material_slots if slot.material and slot.material.node_tree
                  for node in slot.material.node_tree.nodes if node.type == 'TEX_IMAGE' and node.image}:
        path = os.path.join(OUT, f'LuBu_Weapon_{image.name.replace(".", "_")}.png')
        image.filepath_raw = path
        image.file_format = 'PNG'
        image.save()
        report.setdefault('weapon_images', []).append(os.path.basename(path))
    activate(rig)
    body.select_set(True)
    weapon.select_set(True)
    path = os.path.join(OUT, 'LuBu_Battle.fbx')
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, use_armature_deform_only=False,
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
                             mesh_smooth_type='OFF', path_mode='STRIP', embed_textures=False)
    report['fbx'] = path
    report['fbx_bytes'] = os.path.getsize(path)
    with open(os.path.join(OUT, 'LuBu_Battle_Audit.json'), 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=1)
log('passed' if report['passed'] else 'NOT PASSED', 'report', os.path.join(OUT, 'LuBu_Battle_Audit.json'))
print('LUBU_EXPORT_DONE', flush=True)
