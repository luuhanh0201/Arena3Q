"""Read-only audit of the MAIN character after rigging, weighting and baking."""
import bpy,os,json,hashlib,numpy as np
ROOT=os.path.dirname(os.path.abspath(__file__)); OUT=os.path.join(ROOT,'Production')
baseline=json.load(open(os.path.join(OUT,'MacAnh_Preflight.json')))
expected=baseline['objects'][0]['mesh']; rig=bpy.data.objects['MacAnh_Rig']; body=bpy.data.objects[rig['source_body_object']]
mesh=body.data
def sha(data): return hashlib.sha256(data).hexdigest()
co=np.empty(len(mesh.vertices)*3,dtype=np.float32); mesh.vertices.foreach_get('co',co)
indices=np.empty(len(mesh.loops),dtype=np.int32); mesh.loops.foreach_get('vertex_index',indices)
uv=np.empty(len(mesh.uv_layers.active.data)*2,dtype=np.float32); mesh.uv_layers.active.data.foreach_get('uv',uv)
report={'source':bpy.data.filepath,'blender':bpy.app.version_string,'original_vertices':len(mesh.vertices),
    'original_triangles':len(mesh.polygons),'bones':len(rig.data.bones),
    'coordinates_unchanged':sha(co.tobytes())==expected['coordinates_sha256'],
    'topology_unchanged':sha(indices.tobytes())==expected['topology_sha256'],
    'uv_unchanged':sha(uv.tobytes())==expected['uv_sha256']['UVMap'],
    'original_materials_unchanged':[m.name for m in mesh.materials]==expected['materials'],
    'source_scale_one':all(abs(v-1)<1e-7 for v in body.scale) and all(abs(v-1)<1e-7 for v in rig.scale),
    'height_including_crown_m':float(np.ptp(co.reshape(-1,3)[:,2])),'original_images':[]}
report['original_material_nodes_unchanged']=True
for original in baseline['materials']:
    material=bpy.data.materials[original['name']]; nodes=[]
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
    report['original_material_nodes_unchanged'] &= nodes==original['nodes']
report['parent_inverse_identity']=max(abs(body.matrix_parent_inverse[i][j]-(1 if i==j else 0)) for i in range(4) for j in range(4))<1e-6
for old in baseline['images']:
    image=bpy.data.images[old['name']]
    unchanged=bool(image.packed_file) and sha(bytes(image.packed_file.data))==old['packed_sha256']
    report['original_images'].append({'name':old['name'],'unchanged':unchanged})
max_influences=0; max_error=0; unweighted=0
for vertex in mesh.vertices:
    values=[g.weight for g in vertex.groups if g.weight>0]
    max_influences=max(max_influences,len(values)); max_error=max(max_error,abs(sum(values)-1))
    if not values: unweighted+=1
report.update(max_influences=max_influences,max_weight_sum_error=max_error,unweighted=unweighted)
for m in body.modifiers:
    if m.type=='ARMATURE': m.show_viewport=False
scene=bpy.context.scene; clips=json.load(open(os.path.join(OUT,'MacAnh_AnimationManifest.json')))['clips']; checks=[]
for clip in clips:
    name=clip['name']; action=bpy.data.actions['MacAnh_'+name]; rig.animation_data.action=action
    max_grip=max_scale=max_root=0; finite=True; count=0
    for frame in range(clip['last']+1):
        scene.frame_set(frame); bpy.context.view_layer.update(); count+=1
        grip=rig.matrix_world @ rig.pose.bones['Socket_Hand_R'].head
        max_grip=max(max_grip,(bpy.data.objects['MacAnh_Fan'].matrix_world.to_translation()-grip).length)
        max_scale=max(max_scale,max(abs(v-1) for p in rig.pose.bones for v in p.scale))
        max_root=max(max_root,rig.pose.bones['Root'].location.length)
        finite=finite and all(np.isfinite(list(p.rotation_quaternion)).all() for p in rig.pose.bones)
    checks.append({'name':name,'frames_checked':count,'max_grip_error_m':max_grip,'max_scale_error':max_scale,
        'max_root_translation_m':max_root,'finite_rotations':finite,
        'event_markers':[{'name':m.name,'frame':m.frame} for m in action.pose_markers]})
report['actions']=checks
report['passed']=all(report[k] for k in ('coordinates_unchanged','topology_unchanged','uv_unchanged','original_materials_unchanged','original_material_nodes_unchanged','parent_inverse_identity','source_scale_one')) and \
    all(i['unchanged'] for i in report['original_images']) and unweighted==0 and max_influences<=16 and max_error<1e-5 and \
    len(checks)==11 and all(c['max_grip_error_m']<.0001 and c['max_scale_error']<.0001 and c['max_root_translation_m']<.0001 and c['finite_rotations'] for c in checks)
with open(os.path.join(OUT,'MacAnh_Rig_QA.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print('MACANH_RIG_QA',json.dumps(report),flush=True)
assert report['passed']
