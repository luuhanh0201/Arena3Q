"""Finds Mặc Ảnh source faces whose base colour is the AI generator's grey 'static' (hidden robe parts), and either
paints them for review or moves them to the white inner-silk material.

blender -b <Portrait>.blend --python find_macanh_noise_texture.py -- [--apply] [--save <out>.blend] [--report <json>]
          [--grey-sat 0.10] [--noise 0.035] [--min-faces 40]

Detection on the 8192 base colour at 4096: a face is 'static' when the texture under it is grey (saturation below
--grey-sat, value 0.40..0.88) and grainy (mean absolute high-pass above --noise, i.e. pixel-to-pixel noise, not a
smooth gradient or a fold). Only connected groups of at least --min-faces such faces count, so small grey details
(silver trims, highlights) are kept. --apply moves those faces to the InnerRobe material (procedural silk from
texture_macanh_inner_fabric.py); without it the faces get a red review material copy so they can be checked first.
UVs, geometry and weights are untouched.
"""
import bpy
import json
import sys
import numpy as np
import bmesh

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def arg(name, default, cast=str):
    return cast(argv[argv.index(name) + 1]) if name in argv else default


APPLY = '--apply' in argv
SAVE, REPORT = arg('--save', ''), arg('--report', '')
GREY_SAT, NOISE, MIN_FACES = arg('--grey-sat', 0.10, float), arg('--noise', 0.035, float), arg('--min-faces', 40, int)
RES = 4096

rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects[rig.get('source_body_object', 'Mesh_0.001')]
me = body.data
base_mat = body.material_slots[0].material
img = next(n.image for n in base_mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image and n.image.size[0] >= 8192)
work = img.copy()
work.scale(RES, RES)
pix = np.empty(RES * RES * 4, dtype=np.float32)
work.pixels.foreach_get(pix)
bpy.data.images.remove(work)
pix = pix.reshape(RES, RES, 4)[:, :, :3]


def box(a, r):
    """Mean over a (2r+1)^2 window (edges clamped)."""
    p = np.pad(a, ((r + 1, r), (r + 1, r)) + ((0, 0),) * (a.ndim - 2), mode='edge')
    s = p.cumsum(0).cumsum(1)
    k = 2 * r + 1
    return (s[k:, k:] - s[:-k, k:] - s[k:, :-k] + s[:-k, :-k]) / (k * k)


lum = pix.mean(2)
mx, mn = pix.max(2), pix.min(2)
sat = (mx - mn) / np.maximum(mx, 1e-4)
grain = box(np.abs(lum - box(lum, 1)), 3)
static = (sat < GREY_SAT) & (mx > 0.40) & (mx < 0.88) & (grain > NOISE)
print(f'TEXTURE static texels {static.mean() * 100:.2f} % of the atlas', flush=True)

bm = bmesh.new()
bm.from_mesh(me)
bm.faces.ensure_lookup_table()
uv = bm.loops.layers.uv['UVMap']
flag = np.zeros(len(bm.faces), dtype=bool)
for f in bm.faces:
    if f.material_index > 1:
        continue
    pts = [(l[uv].uv.x, l[uv].uv.y) for l in f.loops]
    samples = pts + [(sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts))]
    hits = 0
    for px, py in samples:
        x = min(RES - 1, max(0, int(px * RES)))
        y = min(RES - 1, max(0, int(py * RES)))
        hits += static[y, x]
    flag[f.index] = hits >= len(samples) * 0.6
# Keep only connected groups of flagged faces.
keep = np.zeros_like(flag)
seen = np.zeros_like(flag)
groups = []
for f0 in bm.faces:
    if not flag[f0.index] or seen[f0.index]:
        continue
    comp, stack = [], [f0]
    seen[f0.index] = True
    while stack:
        f = stack.pop()
        comp.append(f.index)
        for e in f.edges:
            for g in e.link_faces:
                if flag[g.index] and not seen[g.index]:
                    seen[g.index] = True
                    stack.append(g)
    if len(comp) >= MIN_FACES:
        keep[comp] = True
        groups.append(len(comp))
print(f'FACES flagged {int(flag.sum())}, kept in {len(groups)} groups: {int(keep.sum())} faces; groups {sorted(groups, reverse=True)[:15]}',
      flush=True)

names = [s.material.name if s.material else '' for s in body.material_slots]
if APPLY:
    target = next(i for i, n in enumerate(names) if n.endswith('InnerRobe'))
else:
    review = bpy.data.materials.new('Review_StaticTexture')
    review.use_nodes = True
    bs = review.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (1, 0, 0, 1)
    bs.inputs['Emission Color'].default_value = (1, 0, 0, 1)
    bs.inputs['Emission Strength'].default_value = 0.6
    body.data.materials.append(review)
    target = len(body.data.materials) - 1
for i in np.nonzero(keep)[0]:
    bm.faces[int(i)].material_index = target
bm.to_mesh(me)
bm.free()
if REPORT:
    json.dump({'grey_sat': GREY_SAT, 'noise': NOISE, 'min_faces': MIN_FACES, 'flagged': int(flag.sum()),
               'moved': int(keep.sum()), 'groups': sorted(groups, reverse=True), 'applied': APPLY,
               'target_material': names[target] if APPLY else 'Review_StaticTexture'}, open(REPORT, 'w'), indent=1)
if SAVE:
    bpy.ops.wm.save_as_mainfile(filepath=SAVE)
    print('SAVED', SAVE, flush=True)
print('NOISE_DONE', flush=True)
