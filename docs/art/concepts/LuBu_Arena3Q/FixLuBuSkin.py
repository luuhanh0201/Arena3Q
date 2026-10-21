"""Repairs Lữ Bố's skin weights where the cloth tears (docs/QuyTac/Model3D_Rules.md, review C1–C4 of 2026-10-20).

blender -b LuBu_Arena3Q_Smooth.blend --python FixLuBuSkin.py -- [--save OUT.blend] [--arm-radius 0.075]
        [--seam 0.6] [--rings 6] [--iterations 30] [--alpha 0.5] [--report OUT.json]

Root causes found by DiagnoseLuBuSkin.py: (1) the proxy heat solve gave shoulder / arm weight to the robe panels under
the arms and to the hem (≈2 270 lower-body vertices per shoulder); (2) fix_regions() of the build script overrides
regions with hard boundaries (everything above the neck = 100 % Head, cape = cape + torso, feathers = inverse distance
^6, top 3), so neighbouring vertices follow different bones and the edge between them tears.
Repair, on a copy (never saves over the source): arm-family weight is removed from vertices that are not on the arm
(farther than --arm-radius from every arm bone), emptied vertices are filled from their neighbours, then the weight
field is smoothed only in a band of --rings rings around the sharp weight seams, limited to 4 influences and
normalised. A quick surface audit on each clip's worst frames is printed; the full audit is SurfaceAudit.py.
"""
import bpy
import json
import os
import sys
import time
import numpy as np

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def arg(name, default, cast=float):
    return cast(argv[argv.index(name) + 1]) if name in argv else default


SAVE = arg('--save', '', str)
REPORT = arg('--report', '', str)
ARM_INNER = arg("--arm-inner", 0.06)
ARM_OUTER = arg("--arm-outer", 0.14)
SEAM = arg('--seam', 0.6)
RINGS = arg('--rings', 6, int)
ITERATIONS = arg('--iterations', 30, int)
ALPHA = arg('--alpha', 0.5)
STRETCH, SQUASH, ABS_FRACTION = 1.35, 0.65, 0.004
# Worst frames of each clip in the 2026-10-20 audit (quick check only).
QUICK = {'Idle': [21], 'Walk': [1, 13], 'Run': [5, 14], 'Attack01': [13, 20], 'Attack02': [15, 22],
         'Victory': [37, 100], 'Defeat': [60, 117]}

started = time.time()
root = os.path.dirname(os.path.abspath(bpy.data.filepath))
source_path = os.path.join(root, 'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source = open(source_path, encoding='utf-8-sig').read()
ns = {'__file__': source_path}
exec(compile(source[:source.rindex('\nLOG.clear()')], source_path, 'exec'), ns)

rig = bpy.data.objects['LuBu_Rig']
body = bpy.data.objects['LuBu_Body']
me = body.data
n = len(me.vertices)
names = [g.name for g in body.vertex_groups]
deform = {b.name for b in rig.data.bones if b.use_deform}
cols = [i for i, g in enumerate(names) if g in deform]
non_deform = [i for i, g in enumerate(names) if g not in deform]
print("NON_DEFORM_GROUPS", [names[i] for i in non_deform], flush=True)

W = np.zeros((n, len(names)), dtype=np.float64)
for v in me.vertices:
    for g in v.groups:
        W[v.index, g.group] = g.weight
co = np.empty(n * 3)
me.vertices.foreach_get('co', co)
co = co.reshape(-1, 3)
edges = np.empty(len(me.edges) * 2, dtype=np.int64)
me.edges.foreach_get('vertices', edges)
edges = edges.reshape(-1, 2)
height = float(np.ptp(co[:, 2]))
a_idx, b_idx = edges[:, 0], edges[:, 1]
degree = np.bincount(np.r_[a_idx, b_idx], minlength=n).astype(np.float64)


def neighbour_mean(M):
    S = np.zeros_like(M)
    np.add.at(S, a_idx, M[b_idx])
    np.add.at(S, b_idx, M[a_idx])
    return S / np.maximum(degree, 1)[:, None]


def normalise(M):
    s = M.sum(1, keepdims=True)
    return np.where(s > 1e-9, M / np.maximum(s, 1e-12), M)


# ---- 0. groups that are not deform bones are ignored by the Armature modifier: their weight pins vertices at rest
nd_weight = float(W[:, non_deform].sum()) if non_deform else 0.0
if non_deform:
    W[:, non_deform] = 0.0
    W = normalise(W)
print(f"NON_DEFORM weight removed {nd_weight:.1f}", flush=True)

# ---- 1. arm-family weight only on the arm itself
ARM_KEYS = ('Shoulder', 'UpperArm', 'LowerArm', 'Hand', 'Thumb', 'Index', 'Middle', 'Ring', 'Little')
arm_cols = [i for i, g in enumerate(names) if any(k in g for k in ARM_KEYS)]
arm_bones = [b for b in rig.data.bones if b.use_deform and any(k in b.name for k in ARM_KEYS)]
dist = np.full(n, np.inf)
for b in arm_bones:
    a = np.array(b.head_local)
    ab = np.array(b.tail_local) - a
    t = np.clip(((co - a) @ ab) / max(ab @ ab, 1e-12), 0, 1)
    dist = np.minimum(dist, np.linalg.norm(co - (a + t[:, None] * ab), axis=1))
# Soft falloff (a hard cut at one radius makes a new seam): full arm weight within ARM_INNER of an arm bone,
# none beyond ARM_OUTER, smoothstep between.
t = np.clip((dist - ARM_INNER) / max(ARM_OUTER - ARM_INNER, 1e-6), 0, 1)
arm_scale = 1 - t * t * (3 - 2 * t)
before = float(W[:, arm_cols].sum())
W[:, arm_cols] *= arm_scale[:, None]
removed = before - float(W[:, arm_cols].sum())
off_arm = dist >= ARM_OUTER
# The cape hangs from the chest: cape vertices never follow the arm, however close to it they are.
if arg('--cape-no-arm', 0, int):
    cape_cols = [i for i, g in enumerate(names) if g.startswith('Cape')]
    cape_vertices = W[:, cape_cols].sum(1) > 0.5
    removed += float(W[np.ix_(cape_vertices, arm_cols)].sum())
    W[np.ix_(cape_vertices, arm_cols)] = 0.0
    off_arm |= cape_vertices
    print(f'CAPE without arm weight: {int(cape_vertices.sum())} vertices', flush=True)
forbid = np.zeros_like(W, dtype=bool)
forbid[np.ix_(off_arm, arm_cols)] = True
W = normalise(W)

# ---- 2. fill vertices left without weight from their neighbours
empty = W[:, cols].sum(1) < 1e-6
filled_total = int(empty.sum())
for _ in range(400):
    if not empty.any():
        break
    M = neighbour_mean(W)
    take = empty & (M.sum(1) > 1e-6)
    W[take] = M[take]
    W[forbid] = 0.0
    empty = W[:, cols].sum(1) < 1e-6
W = normalise(W)

# ---- 3. smooth only around sharp seams
seam_edges = np.abs(W[a_idx] - W[b_idx]).sum(1) > SEAM
zone = np.zeros(n, dtype=bool)
zone[a_idx[seam_edges]] = True
zone[b_idx[seam_edges]] = True
seam_vertices = int(zone.sum())
for _ in range(RINGS):
    grow = zone.copy()
    grow[a_idx[zone[b_idx]]] = True
    grow[b_idx[zone[a_idx]]] = True
    zone = grow
# The chibi head stays rigid (100 % Head, as designed in fix_regions): the transition lives below the neck line.
head_col = names.index('Head') if 'Head' in names else None
feather_cols = [i for i, g in enumerate(names) if g.startswith('Feather')]
if head_col is not None:
    head_line = rig.data.bones['Head'].head_local.z + 0.03
    rigid_head = (co[:, 2] > head_line) & (W[:, feather_cols].sum(1) < 0.05 if feather_cols else True)
    zone &= ~rigid_head
for _ in range(ITERATIONS):
    M = neighbour_mean(W)
    W[zone] = (1 - ALPHA) * W[zone] + ALPHA * M[zone]
    W[forbid] = 0.0
    W = normalise(W)

# Lower-body cloth (robe / cape panels that are not on a leg): a panel spanning both legs tears where its weights
# switch from one thigh to the other, so blend it much more (hips + both legs), keeping the leg armour itself.
ROBE_ITER = arg('--robe-iter', 0, int)
LEG_RADIUS = arg('--leg-radius', 0.07)
robe = np.zeros(n, dtype=bool)
if ROBE_ITER > 0:
    leg_bones = [b for b in rig.data.bones if b.use_deform and any(k in b.name for k in ('UpperLeg', 'LowerLeg'))]
    leg_dist = np.full(n, np.inf)
    for b in leg_bones:
        a = np.array(b.head_local)
        ab = np.array(b.tail_local) - a
        t = np.clip(((co - a) @ ab) / max(ab @ ab, 1e-12), 0, 1)
        leg_dist = np.minimum(leg_dist, np.linalg.norm(co - (a + t[:, None] * ab), axis=1))
    rel = (co[:, 2] - co[:, 2].min()) / height
    robe = (rel > 0.04) & (rel < 0.34) & (leg_dist > LEG_RADIUS)
    for _ in range(ROBE_ITER):
        M = neighbour_mean(W)
        W[robe] = 0.5 * W[robe] + 0.5 * M[robe]
        W[forbid] = 0.0
        W = normalise(W)
    print(f'ROBE blended {int(robe.sum())} vertices x {ROBE_ITER}', flush=True)

# ---- 4. four influences, normalised
order = np.argsort(-W, axis=1)
keep = np.zeros_like(W, dtype=bool)
np.put_along_axis(keep, order[:, :4], True, axis=1)
W = normalise(np.where(keep, W, 0.0))
print(f'FIX removed arm weight {removed:.1f} (off-arm vertices {int(off_arm.sum())}), filled {filled_total}, '
      f'seam vertices {seam_vertices}, smoothed zone {int(zone.sum())}, '
      f'max influences {int((W > 1e-6).sum(1).max())}, weight sum error {float(np.abs(W.sum(1) - 1).max()):.2e}', flush=True)

# ---- write back
for g in body.vertex_groups:
    g.remove(list(range(n)))
nz = np.nonzero(W > 1e-6)
for vi, gi in zip(*nz):
    body.vertex_groups[int(gi)].add([int(vi)], float(W[vi, gi]), 'REPLACE')
print(f'WROTE weights in {time.time() - started:.0f}s', flush=True)

# ---- optional Corrective Smooth (delta mush) after the Armature, limited to the seam zone. Lữ Bố is rendered to
# sprites in Blender, so this modifier is part of the final picture (Unity never skins this mesh).
CS_ITER = arg('--cs-iter', 0, int)
CS_FACTOR = arg('--cs-factor', 1.0)
CS_ALL = arg("--cs-all", 0, int)
for m in list(body.modifiers):
    if m.type == 'CORRECTIVE_SMOOTH':
        body.modifiers.remove(m)
if CS_ITER > 0:
    group = body.vertex_groups.get('CS_Zone') or body.vertex_groups.new(name='CS_Zone')
    group.remove(list(range(n)))
    # Feathered mask: full strength in the seam zone, fading over a few more rings, never on the rigid head.
    mask = zone.astype(np.float64)
    if CS_ALL:
        mask = np.ones(n)
    for _ in range(4):
        mask = np.maximum(mask, 0.6 * neighbour_mean(mask[:, None])[:, 0])
    if head_col is not None:
        mask[rigid_head] = 0.0
    for vi in np.nonzero(mask > 1e-3)[0]:
        group.add([int(vi)], float(mask[vi]), 'REPLACE')
    cs = body.modifiers.new('Seam Corrective Smooth', 'CORRECTIVE_SMOOTH')
    cs.vertex_group = group.name
    cs.iterations = CS_ITER
    cs.factor = CS_FACTOR
    cs.smooth_type = 'LENGTH_WEIGHTED'
    cs.rest_source = 'ORCO'
    cs.use_pin_boundary = False
    print(f'CORRECTIVE_SMOOTH iterations {CS_ITER} factor {CS_FACTOR} on {int((mask > 1e-3).sum())} vertices', flush=True)


# ---- quick surface audit on the worst frames
def coords():
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    ev = body.evaluated_get(dg)
    m = ev.to_mesh()
    arr = np.empty(n * 3)
    m.vertices.foreach_get('co', arr)
    ev.to_mesh_clear()
    return arr.reshape(-1, 3)


rig.data.pose_position = 'REST'
rest = coords()
rig.data.pose_position = 'POSE'
L0 = np.linalg.norm(rest[a_idx] - rest[b_idx], axis=1)
valid = L0 > 1e-6 * height
ABS = ABS_FRACTION * height
quick = {}
for clip, frames in QUICK.items():
    ns['play'](clip)
    for frame in frames:
        bpy.context.scene.frame_set(frame)
        p = coords()
        L = np.linalg.norm(p[a_idx] - p[b_idx], axis=1)
        r = np.where(valid, L / np.where(valid, L0, 1), 1.0)
        bad = valid & (((r > STRETCH) & (L - L0 > ABS)) | ((r < SQUASH) & (L0 - L > ABS)))
        spike = valid & (r > 3) & (L - L0 > 0.002)
        dom = W.argmax(1)
        pairs = {}
        for ea, eb in edges[bad]:
            k = " | ".join(sorted((names[dom[ea]], names[dom[eb]])))
            pairs[k] = pairs.get(k, 0) + 1
        top = sorted(pairs.items(), key=lambda kv: -kv[1])[:5]
        quick[f'{clip}:{frame}'] = {"pairs": top, 'bad': int(bad.sum()), 'spikes': int(spike.sum()), 'max_ratio': float(r.max()),
                                    'min_ratio': float(r[valid].min())}
        print(f'QUICK {clip}:{frame} bad {int(bad.sum())} spikes {int(spike.sum())} '
              f'ratio {float(r[valid].min()):.2f}..{float(r.max()):.2f}' + f" {top}", flush=True)

if REPORT:
    json.dump({"params": {"arm_inner": ARM_INNER, "arm_outer": ARM_OUTER, 'seam': SEAM, 'rings': RINGS, 'iterations': ITERATIONS,
                          'alpha': ALPHA}, 'quick': quick}, open(REPORT, 'w'), indent=1)
if SAVE:
    bpy.ops.wm.save_as_mainfile(filepath=SAVE)
    print('SAVED', SAVE, flush=True)
print(f'FIX_DONE {time.time() - started:.0f}s', flush=True)
