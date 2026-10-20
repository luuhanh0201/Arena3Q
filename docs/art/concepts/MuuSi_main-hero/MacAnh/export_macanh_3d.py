"""Export the existing rig, unchanged geometry and packed maps. NEVER save the blend.
Run: blender -b ../muu-si.blend -P export_macanh_3d.py
Geometry and animation are separate so animation import never duplicates the 3M-triangle mesh.
"""
import bpy, os, json, hashlib, time, sys
from mathutils import Matrix
from pathlib import Path

root = Path(__file__).resolve().parent
project = root.parents[4]
out = project / 'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source'
stage = project / 'Library/MacAnh3DExport'
report_dir = root / 'Production/GraphicsUpgrade'
for p in (out, stage, report_dir): p.mkdir(parents=True, exist_ok=True)
def sha(path): return hashlib.file_digest(open(path,'rb'), 'sha256').hexdigest()
source_hash = sha(bpy.data.filepath)
rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects['Mesh_0.001']
manifest = {'source': bpy.data.filepath, 'source_sha256': source_hash,
            'vertices': len(body.data.vertices), 'triangles': len(body.data.polygons),
            'bones': len(rig.data.bones), 'textures': [], 'materials': [], 'actions': []}
for image in bpy.data.images:
    if not image.packed_file: continue
    data = bytes(image.packed_file.data)
    ext = '.png' if data.startswith(b'\x89PNG') else '.jpg' if data.startswith(b'\xff\xd8') else '.bin'
    path = out / (image.name + ext)
    path.write_bytes(data)
    image.filepath = str(path)
    manifest['textures'].append({'name':image.name, 'path':str(path.relative_to(project)),
        'size':list(image.size), 'sha256':hashlib.sha256(data).hexdigest(), 'colorspace': image.colorspace_settings.name})
for mat in bpy.data.materials:
    if not mat.node_tree: continue
    principled = next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'), None)
    props = {}
    if principled:
        for inp in principled.inputs:
            if inp.name not in ('Base Color','Metallic','Roughness','Alpha','Normal','Emission Color','Emission Strength'): continue
            value = getattr(inp,'default_value',None)
            if value is not None:
                try: value=list(value)
                except TypeError: pass
            props[inp.name]={'value':value,'links':[{'node':l.from_node.name,'socket':l.from_socket.name} for l in inp.links]}
    manifest['materials'].append({'name':mat.name,'properties':props,
        'nodes':[{'name':n.name,'type':n.type,'image':getattr(getattr(n,'image',None),'name',None),
                  'links':[{'to':l.to_node.name,'input':l.to_socket.name,'output':l.from_socket.name} for o in n.outputs for l in o.links]} for n in mat.node_tree.nodes]})

# No evaluation of the dense body during bone baking. FBX skin bindings read
# modifiers independently of their viewport visibility.
for mod in body.modifiers: mod.show_viewport=False; mod.show_render=False
rig.animation_data.action=None
for bone in rig.pose.bones:
    # Source bones use quaternions. Setting only rotation_euler leaves the
    # previous animated quaternion active while authoring the FBX bind pose.
    bone.matrix_basis=Matrix.Identity(4)
bpy.context.scene.frame_set(0)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for ob in bpy.data.objects:
    if ob.type in ('MESH','ARMATURE','EMPTY') and (ob==rig or ob==body or ob.name.startswith(('Fan_','MacAnh_Fan','Socket_'))): ob.select_set(True)
bpy.context.view_layer.objects.active=rig
opts=dict(use_selection=True, global_scale=1., apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
          use_mesh_modifiers=False, add_leaf_bones=False, use_armature_deform_only=False,
          mesh_smooth_type='OFF', path_mode='STRIP', embed_textures=False)
mesh_path=stage/'MacAnh_OriginalRig.fbx'
print('EXPORT_GEOMETRY_START', flush=True)
bpy.ops.export_scene.fbx(filepath=str(mesh_path), bake_anim=False, **opts)
os.replace(mesh_path,out/mesh_path.name)
print('EXPORT_GEOMETRY_DONE', flush=True)
if '--geometry-only' in sys.argv:
    assert sha(bpy.data.filepath)==source_hash
    print('MACANH_GEOMETRY_ONLY_DONE',flush=True)
    sys.exit(0)

bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for ob in bpy.data.objects:
    if ob.type=='EMPTY' and ob.name.startswith(('MacAnh_Fan','Socket_')): ob.select_set(True)
for a in bpy.data.actions:
    manifest['actions'].append({'name':a.name,'frames':list(a.frame_range)})
anim_path=stage/'MacAnh_OriginalActions.fbx'
print('EXPORT_ACTIONS_START', flush=True)
bpy.ops.export_scene.fbx(filepath=str(anim_path), bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0., bake_anim_step=1., **opts)
os.replace(anim_path,out/anim_path.name)
manifest['source_preserved']=sha(bpy.data.filepath)==source_hash
(report_dir/'SourceExport.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
print('MACANH_3D_EXPORT_DONE',manifest['source_preserved'], flush=True)
