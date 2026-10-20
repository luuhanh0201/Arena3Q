"""Audit actual skinned surface edges, not just finite bone transforms.

blender -b ../muu-si.blend -P audit_macanh_deformation.py -- --stride 5
The compressed cache is disposable; the JSON report is the review artifact.
"""
import bpy, sys, json, hashlib, time
from pathlib import Path
import numpy as np

root = Path(__file__).resolve().parent
project = root.parents[4]
cache = project / 'Library/MacAnhDeformation'
if '--cache-name' in sys.argv:
    cache = cache / sys.argv[sys.argv.index('--cache-name')+1]
out = root / 'Production/DeformationFix'
cache.mkdir(parents=True, exist_ok=True)
out.mkdir(parents=True, exist_ok=True)
rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects[rig['source_body_object']]
for mod in body.modifiers:
    mod.show_viewport = False
    mod.show_render = False
mesh = body.data
co = np.empty(len(mesh.vertices)*3, np.float32)
mesh.vertices.foreach_get('co', co)
co = co.reshape(-1, 3)
edges = np.empty(len(mesh.edges)*2, np.int32)
mesh.edges.foreach_get('vertices', edges)
edges = edges.reshape(-1, 2)
names = [g.name for g in body.vertex_groups]
indices = np.zeros((len(co), 16), np.int16)
weights = np.zeros((len(co), 16), np.float32)
for v in mesh.vertices:
    groups = sorted([(g.group,g.weight) for g in v.groups if g.weight>0], key=lambda x:-x[1])
    assert len(groups) <= 16
    for j,(i,w) in enumerate(groups):
        indices[v.index,j] = i
        weights[v.index,j] = w
np.savez_compressed(cache/'weights.npz', co=co, indices=indices, weights=weights, names=names)
np.save(cache/'edges.npy', edges)
if '--weights' in sys.argv:
    candidate = np.load(cache/sys.argv[sys.argv.index('--weights')+1])
    assert list(candidate['names']) == names
    indices = candidate['indices']
    weights = candidate['weights']
    assert candidate['co'].shape == co.shape and np.allclose(candidate['co'],co,atol=1e-7)
max_influences=int((weights>0).sum(axis=1).max())
active_columns=np.flatnonzero(np.any(weights>0,axis=0))
influence_limit=int(sys.argv[sys.argv.index('--max-influences')+1]) if '--max-influences' in sys.argv else 16
height=float(np.ptp(co[:,2]))
absolute_limit=.004*height
def region(vertex):
    name=names[int(indices[vertex,weights[vertex].argmax()])]
    for family in ('Hair','Sleeve','Robe','Ribbon','Thumb','Index','Middle','Ring','Little','Hand','Arm','Foot','Toe','Leg','Head'):
        if family in name:return family
    return 'Torso'
rest = np.linalg.norm(co[edges[:,0]]-co[edges[:,1]], axis=1)
valid = rest > .00002
edges = edges[valid]
rest = rest[valid]
rest_inv = np.array([rig.data.bones[n].matrix_local.inverted() for n in names],np.float32)
homo = np.c_[co, np.ones(len(co), np.float32)]
stride = int(sys.argv[sys.argv.index('--stride')+1]) if '--stride' in sys.argv else 1
label = sys.argv[sys.argv.index('--label')+1] if '--label' in sys.argv else 'Before'
report = dict(source_sha256=hashlib.file_digest(open(bpy.data.filepath,'rb'),'sha256').hexdigest(),
              vertices=len(co), edges=len(edges), stride=stride, clips=[],
              height_m=height,max_influences=max_influences,influence_limit=influence_limit,
              weight_sum_error=float(abs(weights.sum(axis=1)-1).max()))
worst = np.ones(len(edges),np.float32)
started = time.time()
for action in sorted(bpy.data.actions, key=lambda a:a.name):
    if not action.name.startswith('MacAnh_'): continue
    rig.animation_data_create()
    rig.animation_data.action = action
    lo,hi = map(int,action.frame_range)
    frames = sorted(set(range(lo,hi+1,stride)) | {hi})
    if '--release-subframes' in sys.argv:
        frames = sorted(set(frames) | {m.frame+offset for m in action.pose_markers for offset in (-.5,.5) if lo<=m.frame+offset<=hi})
    if '--half-all' in sys.argv:
        frames=sorted(set(frames)|{f+.5 for f in range(lo,hi)})
    peak = 0
    record = None
    counts = []
    max_added = 0
    max_strain_count=0; max_compressed_count=0; minimum_ratio=1.; samples=[]; region_counts={}
    for frame in frames:
        bpy.context.scene.frame_set(int(frame),subframe=float(frame%1))
        bpy.context.view_layer.update()
        matrices = np.array([rig.pose.bones[n].matrix for n in names],np.float32) @ rest_inv
        posed = np.zeros((len(co),3),np.float32)
        for j in active_columns:
            posed += np.einsum('nij,nj->ni',matrices[indices[:,j],:3,:],homo)*weights[:,j,None]
        length = np.linalg.norm(posed[edges[:,0]]-posed[edges[:,1]],axis=1)
        ratio = length/rest
        assert np.isfinite(posed).all()
        worst = np.maximum(worst,ratio)
        bad = (ratio>3)&(length-rest>.002)
        delta=length-rest
        stretched=(ratio>1.35)&(delta>absolute_limit)
        compressed=(ratio<.65)&(-delta>absolute_limit)
        unacceptable=bad|stretched|compressed
        max_strain_count=max(max_strain_count,int(stretched.sum()))
        max_compressed_count=max(max_compressed_count,int(compressed.sum()))
        minimum_ratio=min(minimum_ratio,float(ratio.min()))
        failing=np.flatnonzero(unacceptable)
        if len(failing):
            chosen=failing[np.argsort(abs(delta[failing]))[-20:]]
            for edge_id in chosen:
                a,b=map(int,edges[edge_id]);part=region(a)
                samples.append(dict(frame=frame,vertices=[a,b],region=part,ratio=float(ratio[edge_id]),
                    delta_m=float(delta[edge_id]),rest_m=float(rest[edge_id]),coordinates=co[[a,b]].tolist()))
            families={}
            for edge_id in failing:
                part=region(int(edges[edge_id,0]));families[part]=families.get(part,0)+1
            for part,count in families.items():region_counts[part]=max(region_counts.get(part,0),count)
        max_added = max(max_added,float((length-rest).max()))
        counts.append(int(bad.sum()))
        k = int(ratio.argmax())
        if ratio[k]>peak:
            peak = float(ratio[k])
            a,b = map(int,edges[k])
            record = dict(frame=frame,vertices=[a,b],rest_m=float(rest[k]),posed_m=float(length[k]),
                          coordinates=co[[a,b]].tolist(),
                          weights=[[(names[i],float(w)) for i,w in zip(indices[v],weights[v]) if w>0] for v in (a,b)])
    samples.sort(key=lambda item:abs(item['delta_m']),reverse=True)
    result = dict(name=action.name,frames=len(frames),max_ratio=peak,min_ratio=minimum_ratio,
        max_bad_edges=max(counts),max_stretched_edges=max_strain_count,max_compressed_edges=max_compressed_count,
        max_edge_added_m=max_added,worst=record,worst20=samples[:20],failing_regions=region_counts)
    report['clips'].append(result)
    print('DEFORMATION',label,result['name'],'ratio',round(peak,3),'bad_edges',max(counts),'frame',record['frame'],'vertices',record['vertices'],flush=True)
np.save(cache/('edge_ratio_'+label+'.npy'),worst)
report['elapsed_seconds']=time.time()-started
report['criteria']=dict(spike_ratio_above=3,spike_added_length_above_m=.002,
    stretch_ratio_above=1.35,compression_ratio_below=.65,absolute_delta_above_m=absolute_limit)
report['passed']=report['weight_sum_error']<1e-5 and max_influences<=influence_limit and all(
    c['max_bad_edges']==c['max_stretched_edges']==c['max_compressed_edges']==0 for c in report['clips'])
(out/('SurfaceAudit_'+label+'.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DEFORMATION_COMPLETE',label,report['elapsed_seconds'],flush=True)
