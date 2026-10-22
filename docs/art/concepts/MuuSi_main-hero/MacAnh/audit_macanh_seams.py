"""Measure separation of initially neighbouring surface points, including disconnected islands."""
import bpy, sys, json
import numpy as np
from pathlib import Path
root = Path(__file__).resolve().parent
project = root.parents[4]
sys.path.insert(0, str(project / 'Library/MacAnhTools/Python'))
from scipy.spatial import cKDTree
rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects[rig['source_body_object']]
mesh = body.data
co = np.array([v.co[:] for v in mesh.vertices])
names = [g.name for g in body.vertex_groups]
weights = np.zeros((len(co), len(names)))
for v in mesh.vertices:
    for g in v.groups: weights[v.index, g.group] = g.weight
rest_inv = np.array([rig.data.bones[n].matrix_local.inverted() for n in names])
pairs = cKDTree(co).query_pairs(.008, output_type='ndarray')
rest = np.linalg.norm(co[pairs[:, 0]] - co[pairs[:, 1]], axis=1)
homo = np.c_[co, np.ones(len(co))]
report = []
for action in bpy.data.actions:
    if not action.name.startswith('MacAnh_'): continue
    rig.animation_data.action = action
    worst = np.zeros(len(pairs)); times = np.zeros(len(pairs))
    for frame in np.arange(action.frame_range[0], action.frame_range[1]+.1, .5):
        bpy.context.scene.frame_set(int(frame), subframe=float(frame % 1))
        pose = np.array([rig.pose.bones[n].matrix for n in names]) @ rest_inv
        skin = np.einsum('vb,bij,vj->vi', weights, pose, homo, optimize=True)[:, :3]
        gap = np.linalg.norm(skin[pairs[:,0]] - skin[pairs[:,1]], axis=1) - rest
        times[gap > worst] = frame; worst = np.maximum(worst, gap)
    entries = []
    for i in np.argsort(worst)[-15:][::-1]:
        a,b = pairs[i]
        entries.append(dict(vertices=[int(a),int(b)],rest=float(rest[i]),growth=float(worst[i]),frame=float(times[i]),
            positions=[co[a].tolist(),co[b].tolist()],
            weights=[{names[j]:float(weights[v,j]) for j in np.flatnonzero(weights[v])} for v in (a,b)]))
    report.append(dict(action=action.name,over2cm=int((worst>.02).sum()),worst=entries))
    print('SEAM',action.name,int((worst>.02).sum()),entries[0],flush=True)
out = project / 'Logs/MacAnh-seams.json'
out.write_text(json.dumps(report,indent=2))
print('SEAM_AUDIT_COMPLETE',flush=True)
