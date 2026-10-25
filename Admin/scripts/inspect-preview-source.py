import bpy, json, os
from pathlib import Path
root=Path(__file__).resolve().parents[1]
report={"source":bpy.data.filepath,"meshes":[{"name":o.name,"vertices":len(o.data.vertices),"polygons":len(o.data.polygons),"materials":[m.name for m in o.data.materials if m]} for o in bpy.data.objects if o.type=='MESH'],"armatures":[{"name":o.name,"bones":len(o.data.bones)} for o in bpy.data.objects if o.type=='ARMATURE'],"actions":[{"name":a.name,"range":list(a.frame_range)} for a in bpy.data.actions],"images":[{"name":i.name,"size":list(i.size),"path":i.filepath,"packed":bool(i.packed_file)} for i in bpy.data.images if i.source=='FILE']}
report['materials']=[{'name':m.name,'nodes':[{'name':n.name,'type':n.type,'image':n.image.name if n.type=='TEX_IMAGE' and n.image else None,'inputs':{s.name:([{'node':link.from_node.name,'socket':link.from_socket.name}for link in s.links] if s.is_linked else str(s.default_value) if hasattr(s,'default_value') else None) for s in n.inputs if s.name in ['Base Color','Color','Normal','Surface','Vector']}}for n in m.node_tree.nodes]}for m in bpy.data.materials if m.use_nodes]
out=root/'.local'/('source-'+Path(bpy.data.filepath).stem+'.json');out.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print('ADMIN_SOURCE_REPORT='+str(out))
