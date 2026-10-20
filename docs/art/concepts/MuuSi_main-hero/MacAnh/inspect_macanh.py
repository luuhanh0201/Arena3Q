"""Read-only preflight of the supplied character. Never saves the source blend."""
import bpy, os, json, hashlib, numpy as np
from mathutils import Vector

root=os.path.dirname(os.path.abspath(__file__))
output=os.path.join(root,'Production')
os.makedirs(output,exist_ok=True)
def sha(data): return hashlib.sha256(data).hexdigest()
def numbers(value): return [round(float(v),7) for v in value]
def mesh_signature(mesh):
    coords=np.empty(len(mesh.vertices)*3,dtype=np.float32); mesh.vertices.foreach_get('co',coords)
    indices=np.empty(len(mesh.loops),dtype=np.int32); mesh.loops.foreach_get('vertex_index',indices)
    uv={}
    for layer in mesh.uv_layers:
        values=np.empty(len(layer.data)*2,dtype=np.float32); layer.data.foreach_get('uv',values)
        uv[layer.name]=sha(values.tobytes())
    weights=hashlib.sha256()
    for vertex in mesh.vertices:
        for group in vertex.groups:
            weights.update(('%d:%d:%.9g;'%(vertex.index,group.group,group.weight)).encode())
    return {'vertices':len(mesh.vertices),'polygons':len(mesh.polygons),'loops':len(mesh.loops),
            'coordinates_sha256':sha(coords.tobytes()),'topology_sha256':sha(indices.tobytes()),
            'uv_sha256':uv,'weights_sha256':weights.hexdigest(),
            'materials':[m.name if m else None for m in mesh.materials]}
report={'source':bpy.data.filepath,'blender':bpy.app.version_string,
        'units':{'system':bpy.context.scene.unit_settings.system,'scale_length':bpy.context.scene.unit_settings.scale_length},
        'objects':[],'armatures':[],'actions':[],'images':[],'materials':[]}
all_points=[]
for obj in bpy.data.objects:
    entry={'name':obj.name,'type':obj.type,'parent':obj.parent.name if obj.parent else None,
           'location':numbers(obj.location),'rotation_euler':numbers(obj.rotation_euler),'scale':numbers(obj.scale),
           'matrix_world':[numbers(row) for row in obj.matrix_world],
           'modifiers':[{'name':m.name,'type':m.type,'target':getattr(getattr(m,'object',None),'name',None)} for m in obj.modifiers]}
    if obj.type=='MESH':
        points=[obj.matrix_world @ Vector(p) for p in obj.bound_box]
        all_points.extend(points)
        entry['world_min']=[min(p[i] for p in points) for i in range(3)]
        entry['world_max']=[max(p[i] for p in points) for i in range(3)]
        entry['world_dimensions']=[entry['world_max'][i]-entry['world_min'][i] for i in range(3)]
        entry['mesh']=mesh_signature(obj.data)
        entry['vertex_groups']=[g.name for g in obj.vertex_groups]
    if obj.type=='ARMATURE':
        report['armatures'].append({'name':obj.name,'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None,
           'head_world':numbers(obj.matrix_world @ b.head_local),'tail_world':numbers(obj.matrix_world @ b.tail_local)} for b in obj.data.bones]})
    report['objects'].append(entry)
if all_points:
    low=[min(p[i] for p in all_points) for i in range(3)]; high=[max(p[i] for p in all_points) for i in range(3)]
    report['mesh_world_bounds']={'min':low,'max':high,'dimensions':[high[i]-low[i] for i in range(3)]}
for action in bpy.data.actions:
    report['actions'].append({'name':action.name,'frame_range':numbers(action.frame_range),'users':action.users})
for image in bpy.data.images:
    report['images'].append({'name':image.name,'path':image.filepath,'size':list(image.size),
        'packed':bool(image.packed_file),'packed_sha256':sha(bytes(image.packed_file.data)) if image.packed_file else None})
for material in bpy.data.materials:
    nodes=[]
    if material.node_tree:
        for node in material.node_tree.nodes:
            values={}
            for socket in node.inputs:
                if not hasattr(socket,'default_value'): continue
                value=socket.default_value
                if isinstance(value,(float,int,str,bool)): values[socket.name]=value
                else:
                    try: values[socket.name]=list(value)
                    except TypeError: pass
            nodes.append({'name':node.name,'type':node.type,'image':getattr(getattr(node,'image',None),'name',None),'inputs':values})
    report['materials'].append({'name':material.name,'nodes':nodes})
with open(os.path.join(output,'MacAnh_Preflight.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2,ensure_ascii=False)
summary={'objects':[(o['name'],o['type'],o.get('world_dimensions'),o.get('mesh',{}).get('vertices')) for o in report['objects']],
         'bounds':report.get('mesh_world_bounds'),'rigs':len(report['armatures']),'bones':[len(r['bones']) for r in report['armatures']],
         'actions':report['actions'],'images':report['images'],'units':report['units']}
print('MACANH_PREFLIGHT',json.dumps(summary,ensure_ascii=False),flush=True)
