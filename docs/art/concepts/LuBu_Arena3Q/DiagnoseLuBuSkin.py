"""Which bone pairs meet across the stretched edges of the skinned body (read-only diagnosis).

blender -b LuBu_Arena3Q_Smooth.blend --python DiagnoseLuBuSkin.py -- <out.json>
For every clip's worst frame: the bad edges (stretch > 1.35 or squash < 0.65 with > 0.4 % of the height) are
grouped by the dominant deform bone of their two vertices; also per region, how much weight comes from bones that
should not move that region (arm / shoulder bones on the lower robe, ...).
"""
import bpy
import json
import os
import sys
import numpy as np

sys.stdout.reconfigure(line_buffering=True)
out_path = sys.argv[sys.argv.index('--') + 1]
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
W = np.zeros((n, len(names)), dtype=np.float32)
for v in me.vertices:
    for g in v.groups:
        W[v.index, g.group] = g.weight
dominant = W.argmax(1)
edges = np.empty(len(me.edges) * 2, dtype=np.int64)
me.edges.foreach_get('vertices', edges)
edges = edges.reshape(-1, 2)
rest = np.empty(n * 3)
me.vertices.foreach_get('co', rest)
rest = rest.reshape(-1, 3)
height = float(np.ptp(rest[:, 2]))
zmin = float(rest[:, 2].min())
L0 = np.linalg.norm(rest[edges[:, 0]] - rest[edges[:, 1]], axis=1)
valid = L0 > 1e-6 * height


def coords():
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    ev = body.evaluated_get(dg)
    m = ev.to_mesh()
    a = np.empty(n * 3)
    m.vertices.foreach_get('co', a)
    ev.to_mesh_clear()
    return a.reshape(-1, 3)


report = {'groups': names, 'height': height, 'clips': {}}
# Bone hierarchy families, to say "arm weight on the robe".
families = {}
for b in rig.data.bones:
    chain = [b.name]
    p = b.parent
    while p:
        chain.append(p.name)
        p = p.parent
    families[b.name] = chain
arm_like = [i for i, g in enumerate(names) if any(k in g for k in ('Shoulder', 'UpperArm', 'LowerArm', 'Hand', 'Thumb', 'Index', 'Middle', 'Ring', 'Little'))]
cape_like = [i for i, g in enumerate(names) if g.startswith('Cape') or 'Skirt' in g or 'Robe' in g]
# Height bands (fraction of height above the feet) and the arm-bone weight found there.
bands = {}
for lo in np.arange(0, 1.0, 0.1):
    sel = (rest[:, 2] - zmin) / height
    m = (sel >= lo) & (sel < lo + 0.1)
    if m.any():
        bands[f'{lo:.1f}-{lo + 0.1:.1f}'] = {'vertices': int(m.sum()), 'arm_weight_mean': float(W[m][:, arm_like].sum(1).mean()),
                                             'vertices_with_arm_weight>0.2': int((W[m][:, arm_like].sum(1) > 0.2).sum())}
report['arm_weight_by_height'] = bands
low = (rest[:, 2] - zmin) / height < 0.35
offenders = {}
for i in arm_like:
    c = int((low & (W[:, i] > 0.05)).sum())
    if c:
        offenders[names[i]] = c
report['arm_bones_on_lower_35pct'] = dict(sorted(offenders.items(), key=lambda kv: -kv[1]))

for name in ns['EXPECTED_ACTIONS']:
    ns['play'](name)
    action = rig.animation_data.action
    first, last = map(int, action.frame_range)
    best = None
    for frame in range(first, last + 1, 2):
        bpy.context.scene.frame_set(frame)
        p = coords()
        L = np.linalg.norm(p[edges[:, 0]] - p[edges[:, 1]], axis=1)
        r = np.where(valid, L / np.where(valid, L0, 1), 1.0)
        bad = valid & (((r > 1.35) & (L - L0 > 0.004 * height)) | ((r < 0.65) & (L0 - L > 0.004 * height)))
        if best is None or bad.sum() > best[1].sum():
            best = (frame, bad)
    frame, bad = best
    pairs = {}
    for a, b in edges[bad]:
        ga, gb = names[dominant[a]], names[dominant[b]]
        key = ' | '.join(sorted((ga, gb)))
        pairs[key] = pairs.get(key, 0) + 1
    report['clips'][name] = {'frame': frame, 'bad_edges': int(bad.sum()),
                             'top_pairs': dict(sorted(pairs.items(), key=lambda kv: -kv[1])[:15])}
    print('CLIP', name, frame, int(bad.sum()), list(report['clips'][name]['top_pairs'].items())[:6], flush=True)

json.dump(report, open(out_path, 'w', encoding='utf-8'), indent=1)
print('ARM ON LOWER BODY', list(report['arm_bones_on_lower_35pct'].items())[:10], flush=True)
print('BANDS', json.dumps(bands), flush=True)
print('DIAGNOSE_DONE', flush=True)
