import bpy,numpy as np
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(20)
ids={i for p,a in zip(m.polygons,m.attributes['GarmentRegion'].data) if a.value==3 for i in p.vertices}
edges=[tuple(e.vertices) for e in m.edges if any(i in ids for i in e.vertices)]
for step in range(20):
 bpy.context.view_layer.update();ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();bad=[]
 for a,b in edges:
  rest=(m.vertices[a].co-m.vertices[b].co).length;posed=(mesh.vertices[a].co-mesh.vertices[b].co).length
  if rest>1e-7 and ((posed/rest>1.32 and posed-rest>.0058) or (posed/rest>2.8 and posed-rest>.0018) or (posed/rest<.68 and rest-posed>.0058)):bad.append((a,b))
 ev.to_mesh_clear();print('RELAX',step,len(bad),flush=True)
 if not bad:break
 for a,b in bad:
  wa={g.group:g.weight for g in m.vertices[a].groups};wb={g.group:g.weight for g in m.vertices[b].groups}
  avg={g:(wa.get(g,0)+wb.get(g,0))/2 for g in wa.keys()|wb.keys()}
  top=sorted(avg.items(),key=lambda x:x[1],reverse=True)[:4];total=sum(w for g,w in top)
  for i in (a,b):
   for g in list(m.vertices[i].groups):o.vertex_groups[g.group].remove([i])
   for g,w in top:o.vertex_groups[g].add([i],w/total,'REPLACE')
 m.update()
bpy.ops.wm.save_as_mainfile(filepath='D:/UnityProjects/Arena3Q/Library/MacAnh_HandRelaxed.blend',compress=True)
