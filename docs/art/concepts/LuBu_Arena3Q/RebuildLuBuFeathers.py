"""Rebuilds the two pheasant-plume bone chains along the whole plume (review C2 of 2026-10-20: zigzag plumes).

blender -b <in>.blend --python RebuildLuBuFeathers.py -- --save <out>.blend [--blend-root 0.08] [--report out.json]

Root cause: add_feathers() of the build script only kept plume vertices above head_centre.z − 0.6·R, so the ring walk
stopped near the crown and the four Feather_0x bones sit at 0.90–0.98 of the height while the plume hangs down to
about half the body: most of the plume rides rigidly on tiny root bones and folds into corners when they turn.
Repair: flood-fill each plume over the mesh from its current feather-weighted vertices (outside the head sphere, no
height limit), measure the walking distance from the root, put the four existing bones (same names, so every action
still drives them) evenly along that centre line, and weight the plume by its position along the chain with smooth
overlaps; the root blends into Head. Topology / UVs are untouched.
"""
import bpy
import json
import sys
import numpy as np
from mathutils import Vector

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def arg(name, default, cast=str):
    return cast(argv[argv.index(name) + 1]) if name in argv else default


SAVE = arg('--save', '')
REPORT = arg('--report', '')
BLEND_ROOT = arg('--blend-root', 0.08, float)

rig = bpy.data.objects['LuBu_Rig']
body = bpy.data.objects['LuBu_Body']
me = body.data
n = len(me.vertices)
co = np.empty(n * 3)
me.vertices.foreach_get('co', co)
co = co.reshape(-1, 3)
edges = np.empty(len(me.edges) * 2, dtype=np.int64)
me.edges.foreach_get('vertices', edges)
edges = edges.reshape(-1, 2)
nbr = [[] for _ in range(n)]
for a, b in edges:
    nbr[a].append(b)
    nbr[b].append(a)
names = [g.name for g in body.vertex_groups]
W = np.zeros((n, len(names)))
for v in me.vertices:
    for g in v.groups:
        W[v.index, g.group] = g.weight

# Head sphere, the same estimate as add_feathers().
best_w, best_z = 0.0, 0.8
for i in range(30):
    z = 0.62 + i * 0.012
    xs = np.sort(np.abs(co[np.abs(co[:, 2] - z) < 0.006, 0]))
    if len(xs) > 10:
        w = xs[0]
        for x in xs:
            if x - w > 0.03:
                break
            w = x
        if w > best_w:
            best_w, best_z = w, z
R = best_w
head_top = co[:, 2].max()
head_c = np.array([0.0, 0.0, max(best_z, head_top - R * 1.05)])
print(f'HEAD centre z {head_c[2]:.3f} radius {R:.3f}', flush=True)

report = {'head_centre_z': float(head_c[2]), 'radius': float(R), 'sides': {}}
bpy.context.view_layer.objects.active = rig
for o in bpy.context.view_layer.objects:
    o.select_set(o == rig)
plans = {}
for side, sign in (('.L', 1), ('.R', -1)):
    cols = [i for i, g in enumerate(names) if g.startswith('Feather') and g.endswith(side)]
    seeds = np.nonzero(W[:, cols].sum(1) > 0.3)[0] if cols else np.array([], dtype=np.int64)
    outside = (np.linalg.norm(co - head_c, axis=1) > R * 1.12) & (co[:, 0] * sign > 0.015)
    seeds = seeds[outside[seeds]]
    plume = np.zeros(n, dtype=bool)
    stack = list(seeds)
    plume[seeds] = True
    while stack:
        i = stack.pop()
        for j in nbr[i]:
            if not plume[j] and outside[j]:
                plume[j] = True
                stack.append(j)
    ids = np.nonzero(plume)[0]
    # Root ring = plume vertices nearest the head; walking distance (BFS, edge lengths) from it.
    dist_head = np.linalg.norm(co[ids] - head_c, axis=1)
    root_ids = ids[dist_head < dist_head.min() + 0.01]
    s = np.full(n, np.inf)
    s[root_ids] = 0.0
    frontier = list(root_ids)
    import heapq
    heap = [(0.0, int(i)) for i in root_ids]
    heapq.heapify(heap)
    while heap:
        d, i = heapq.heappop(heap)
        if d > s[i]:
            continue
        for j in nbr[i]:
            if not plume[j]:
                continue
            nd = d + float(np.linalg.norm(co[i] - co[j]))
            if nd < s[j]:
                s[j] = nd
                heapq.heappush(heap, (nd, int(j)))
    reach = np.isfinite(s) & plume
    L = float(s[reach].max())
    # Centre line: mean position of each walking-distance band.
    bands = 24
    centre = []
    for k in range(bands + 1):
        t = L * k / bands
        m = reach & (np.abs(s - t) < L / bands)
        centre.append(co[m].mean(0) if m.any() else (centre[-1] if centre else co[root_ids].mean(0)))
    centre = np.array(centre)
    joints = [centre[int(round(k * bands / 4))] for k in range(5)]
    plans[side] = (ids[reach[ids]], s, L, joints)
    report['sides'][side] = {'plume_vertices': int(reach.sum()), 'length_m': L,
                             'joints_z_rel': [float((j[2] - co[:, 2].min()) / np.ptp(co[:, 2])) for j in joints]}
    print(f'PLUME {side}: {int(reach.sum())} vertices, length {L:.3f} m, joints z '
          f'{[round(float(j[2]), 3) for j in joints]}', flush=True)

# Move the existing four bones along the plume (edit mode); same names keep every action working.
bpy.ops.object.mode_set(mode='EDIT')
eb = rig.data.edit_bones
for side, (_, _, _, joints) in plans.items():
    for k in range(4):
        bone = eb.get(f'Feather_{k + 1:02d}{side}')
        if bone is None:
            raise RuntimeError(f'Feather_{k + 1:02d}{side} missing')
        bone.use_connect = False
    for k in range(4):
        bone = eb[f'Feather_{k + 1:02d}{side}']
        bone.head = Vector(joints[k])
        bone.tail = Vector(joints[k + 1])
        bone.use_connect = k > 0
bpy.ops.object.mode_set(mode='OBJECT')

# Weights along the chain: hat functions over the bone centres, root blended into Head.
head_col = names.index('Head')
for side, (ids, s, L, _) in plans.items():
    cols = [names.index(f'Feather_{k + 1:02d}{side}') for k in range(4)]
    u = s[ids] / L * 4.0                       # 0..4 along the chain
    centres = np.arange(4) + 0.5
    hats = np.clip(1.0 - np.abs(u[:, None] - centres[None, :]), 0, 1)
    hats[u <= 0.5, 0] = 1.0                     # before the first centre: first bone
    hats[u >= 3.5, 3] = 1.0                     # past the last centre: last bone
    hats /= hats.sum(1, keepdims=True)
    root_t = np.clip(s[ids] / max(BLEND_ROOT, 1e-6), 0, 1)
    W[ids] = 0.0
    W[np.ix_(ids, cols)] = hats * root_t[:, None]
    W[ids, head_col] = 1.0 - root_t

for g in body.vertex_groups:
    g.remove(list(range(n)))
for vi, gi in zip(*np.nonzero(W > 1e-6)):
    body.vertex_groups[int(gi)].add([int(vi)], float(W[vi, gi]), 'REPLACE')
print('FEATHERS_REWEIGHTED', flush=True)
if REPORT:
    json.dump(report, open(REPORT, 'w'), indent=1)
if SAVE:
    bpy.ops.wm.save_as_mainfile(filepath=SAVE)
    print('SAVED', SAVE, flush=True)
