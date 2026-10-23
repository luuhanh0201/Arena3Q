"""Measure actual deformed digit surfaces against the unchanged handle."""
import bpy,json,sys,hashlib
from pathlib import Path
from mathutils.bvhtree import BVHTree
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];h=bpy.data.objects['Fan_Handle']
digits=('Thumb','Index','Middle','Ring','Little');groups={d:[] for d in digits}
for v in o.data.vertices:
 tag=o.data.attributes.get('GripDigit')
 if tag is not None:
  value=tag.data[v.index].value
  if 1<=value<=5:groups[digits[value-1]].append(v.index)
  continue
 weights={d:sum(g.weight for g in v.groups if o.vertex_groups[g.group].name.startswith('Right'+d)) for d in digits}
 d=max(weights,key=weights.get)
 if weights[d]>.30:groups[d].append(v.index)
report={'source_sha256':hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
 'method':'Distinct digit vertices from GripDigit anatomical tags, or strongest digit influence >0.30; nearest deformed surface vertex to handle triangles.',
 'limit_m':.004,'digit_vertices':{d:len(v) for d,v in groups.items()},'clips':[]}
for a in sorted(bpy.data.actions,key=lambda a:a.name):
 if not a.name.startswith('MacAnh_'):continue
 r.animation_data.action=a;row={'clip':a.name,'samples':0,'max_gap_m':{d:0 for d in digits}}
 for half in range(int(a.frame_range[0]*2),int(a.frame_range[1]*2)+1):
  bpy.context.scene.frame_set(half//2,subframe=(half%2)/2);bpy.context.view_layer.update()
  tree=BVHTree.FromPolygons([h.matrix_world@v.co for v in h.data.vertices],[list(p.vertices) for p in h.data.polygons])
  ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh()
  for d,ids in groups.items():
   gap=min((tree.find_nearest(o.matrix_world@m.vertices[i].co)[3] for i in ids),default=1)
   row['max_gap_m'][d]=max(row['max_gap_m'][d],gap)
  ev.to_mesh_clear();row['samples']+=1
 report['clips'].append(row);print('GRIP_CONTACT',row,flush=True)
report['passed']=all(v<=report['limit_m'] for row in report['clips'] for v in row['max_gap_m'].values())
out=Path(sys.argv[sys.argv.index('--output')+1]);out.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('GRIP_CONTACT_COMPLETE',report['passed'],flush=True)
