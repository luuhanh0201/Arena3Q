"""Recover complete boots, instead of animating only the old narrow foot cuts.

Writes a separate candidate; never modifies the loaded source. The upper boot
boundary is separated from the robe so a stride cannot pull the hem with it.
"""
import bpy, bmesh, sys, json
from pathlib import Path
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']]
action=r.animation_data.action;r.animation_data.action=None
for p in r.pose.bones:p.matrix_basis.identity()
bpy.context.view_layer.update()
bm=bmesh.new();bm.from_mesh(o.data);region=bm.faces.layers.int['GarmentRegion'];deform=bm.verts.layers.deform.verify()
groups={g.name:g.index for g in o.vertex_groups};counts={}
for side,sign,label in [('Right',-1,5),('Left',1,6)]:
    selected=[]
    for f in bm.faces:
        x,y,z=f.calc_center_median()
        if f[region]==label or (.058<x*sign<.235 and y<.045 and z<.135):selected.append(f)
    selected=set(selected)
    # Split the upper attachment only. Existing hand/sleeve segmentation stays intact.
    boundary=[e for e in bm.edges if any(f in selected for f in e.link_faces) and any(f not in selected for f in e.link_faces)]
    bmesh.ops.split_edges(bm,edges=boundary)
    for f in selected:f[region]=label
    verts={v for f in selected for v in f.verts}
    for v in verts:
        v[deform].clear();v[deform][groups[side+'Foot']]=1
    counts[side]=dict(faces=len(selected),vertices=len(verts),split_edges=len(boundary))
bm.normal_update();bm.to_mesh(o.data);bm.free();o.data.update()
r.animation_data.action=action;bpy.context.scene.frame_set(0)
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve()
assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
Path('Logs/MacAnh-boot-repair.json').write_text(json.dumps(counts,indent=2))
print('MACANH_BOOT_REGIONS_COMPLETE',counts,flush=True)
