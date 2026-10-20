"""Keep source UV0 and colour/ORM; add UV1 for baked normals and new inner patches."""
import bpy,sys,math,json,time,hashlib
import numpy as np
from pathlib import Path
root=Path(__file__).resolve().parent
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
out=root/'Production/LOD'/('Atlas_'+label);out.mkdir(parents=True,exist_ok=True)
source=root.parent/'muu-si.blend';source_hash=hashlib.file_digest(open(source,'rb'),'sha256').hexdigest()
rig=bpy.data.objects['MacAnh_Rig'];target=bpy.data.objects[rig['source_body_object']]
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
for modifier in target.modifiers:modifier.show_viewport=False;modifier.show_render=False
bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT')
target.select_set(True);bpy.context.view_layer.objects.active=target
uv0=target.data.uv_layers[0]
original_uv_name=uv0.name;normal_uv_name='NormalBakeUV'
original_uv=np.empty(len(uv0.data)*2,np.float32);uv0.data.foreach_get('uv',original_uv)
uv0_hash=hashlib.sha256(original_uv.tobytes()).hexdigest()
if target.data.uv_layers.get(normal_uv_name) is None:target.data.uv_layers.new(name=normal_uv_name)
target.data.uv_layers.active_index=target.data.uv_layers.find(normal_uv_name)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.004,area_weight=.1)
bpy.ops.object.mode_set(mode='OBJECT')
uv_check=np.empty(len(target.data.uv_layers[original_uv_name].data)*2,np.float32)
target.data.uv_layers[original_uv_name].data.foreach_get('uv',uv_check)
assert hashlib.sha256(uv_check.tobytes()).hexdigest()==uv0_hash,'Original UV changed during unwrap'
target.data.uv_layers[original_uv_name].active_render=True
objects_before=set(bpy.data.objects);actions_before=set(bpy.data.actions)
with bpy.data.libraries.load(str(source),link=False) as (available,loaded):loaded.objects=[rig['source_body_object']]
high=loaded.objects[0];bpy.context.scene.collection.objects.link(high)
high.parent=None;high.matrix_world.identity();high.animation_data_clear();high.modifiers.clear()
for obj in bpy.context.scene.objects:obj.hide_render=obj not in (target,high)
high.hide_set(False);target.hide_set(False)
source_materials=[slot.material for slot in high.material_slots]
target_materials=[]
has_inner_materials='lod_inner_materials' in target
for index,slot in enumerate(target.material_slots):
    if index==0 or not has_inner_materials:slot.material=slot.material.copy()
    target_materials.append(slot.material)
bpy.ops.object.select_all(action='DESELECT');high.select_set(True);target.select_set(True);bpy.context.view_layer.objects.active=target
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.004
scene.render.bake.max_ray_distance=.02;scene.render.bake.margin=12
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
scene.render.bake.normal_space='TANGENT'
images={};started=time.time()
passes=[('Normal','NORMAL')] if has_inner_materials else [('BaseColor','EMIT'),('Normal','NORMAL'),('ORM','EMIT')]
for kind,bake_type in passes:
    # Tangent-space normal baking must derive its target tangent frame from
    # the same UV set that will sample the resulting map in Blender/Unity.
    target.data.uv_layers[normal_uv_name].active_render=True
    image=bpy.data.images.new('MacAnh_'+label+'_Atlas'+kind,width=4096,height=4096,alpha=True)
    image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
    image.generated_color=(.5,.5,1,1) if kind=='Normal' else (0,0,0,0)
    for material in target_materials:
        node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;material.node_tree.nodes.active=node
    for slot,material in zip(high.material_slots,source_materials):slot.material=material
    if kind=='BaseColor':
        # Diffuse colour bakes suppress metallic albedo. Route the untouched
        # source base-colour input through emission so crown/gold stay gold.
        for slot,material in zip(high.material_slots,source_materials):
            replacement=material.copy();nodes=replacement.node_tree.nodes;links=replacement.node_tree.links
            principled=next(node for node in nodes if node.type=='BSDF_PRINCIPLED')
            emission=nodes.new('ShaderNodeEmission');output=next(node for node in nodes if node.type=='OUTPUT_MATERIAL')
            colour=principled.inputs['Base Color']
            if colour.is_linked:links.new(colour.links[0].from_socket,emission.inputs['Color'])
            else:emission.inputs['Color'].default_value=colour.default_value
            links.new(emission.outputs[0],output.inputs['Surface']);slot.material=replacement
    if kind=='ORM':
        for slot,material in zip(high.material_slots,source_materials):
            replacement=bpy.data.materials.new('Temporary source ORM');replacement.use_nodes=True
            nodes=replacement.node_tree.nodes;nodes.clear()
            tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.get('Image_1')
            if tex.image is None:tex.image=next(i for i in bpy.data.images if i.name.startswith('Image_1'))
            emission=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial')
            replacement.node_tree.links.new(tex.outputs['Color'],emission.inputs['Color'])
            replacement.node_tree.links.new(emission.outputs[0],output.inputs['Surface']);slot.material=replacement
    print('LOD_ATLAS_BAKE_BEGIN',label,kind,flush=True);bpy.ops.object.bake(type=bake_type,uv_layer=normal_uv_name)
    image.filepath_raw=str(out/('MacAnh_'+label+'_Atlas'+kind+'.png'));image.file_format='PNG';image.save();images[kind]=image
    print('LOD_ATLAS_BAKE_PASS_COMPLETE',label,kind,flush=True)
for slot,material in zip(high.material_slots,source_materials):slot.material=material
for image in images.values():image.pack()
material=None
if not has_inner_materials:
    material=bpy.data.materials.new('MacAnh_'+label+'_CapAtlas');material.use_nodes=True
    nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
    surface=nodes.new('ShaderNodeBsdfPrincipled');output=nodes.new('ShaderNodeOutputMaterial');links.new(surface.outputs[0],output.inputs['Surface'])
    textures={}
    uv_node=nodes.new('ShaderNodeUVMap');uv_node.uv_map=normal_uv_name
    for kind,image in images.items():
        tex=nodes.new('ShaderNodeTexImage');tex.image=image;links.new(uv_node.outputs[0],tex.inputs['Vector']);textures[kind]=tex
    links.new(textures['BaseColor'].outputs['Color'],surface.inputs['Base Color'])
    normal=nodes.new('ShaderNodeNormalMap');normal.uv_map=normal_uv_name;links.new(textures['Normal'].outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],surface.inputs['Normal'])
    separate=nodes.new('ShaderNodeSeparateColor');links.new(textures['ORM'].outputs['Color'],separate.inputs[0]);links.new(separate.outputs['Green'],surface.inputs['Roughness']);links.new(separate.outputs['Blue'],surface.inputs['Metallic'])
body_material=target_materials[0];body_material.name='MacAnh_'+label+'_DualUvBody'
nodes=body_material.node_tree.nodes;links=body_material.node_tree.links
surface=next(node for node in nodes if node.type=='BSDF_PRINCIPLED')
uv_node=nodes.new('ShaderNodeUVMap');uv_node.uv_map=normal_uv_name
texture=nodes.new('ShaderNodeTexImage');texture.image=images['Normal'];links.new(uv_node.outputs[0],texture.inputs['Vector'])
normal=nodes.new('ShaderNodeNormalMap');normal.uv_map=normal_uv_name
links.new(texture.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],surface.inputs['Normal'])
target.material_slots[0].material=body_material
if not has_inner_materials:
    for index in range(1,len(target.material_slots)):target.material_slots[index].material=material
target['lod_body_material']=body_material.name;target['lod_dual_uv']=True
target['lod_cap_material']=material.name if len(target.material_slots)>1 and not has_inner_materials else ''
after_uv=np.empty(len(target.data.uv_layers[original_uv_name].data)*2,np.float32);target.data.uv_layers[original_uv_name].data.foreach_get('uv',after_uv)
assert hashlib.sha256(after_uv.tobytes()).hexdigest()==uv0_hash,'Source UV0 changed'
target.data.uv_layers.active_index=0;target.data.uv_layers[0].active_render=True
mesh=high.data;bpy.data.objects.remove(high,do_unlink=True)
for obj in list(bpy.data.objects):
    if obj not in objects_before:bpy.data.objects.remove(obj,do_unlink=True)
for action in list(bpy.data.actions):
    if action not in actions_before:bpy.data.actions.remove(action)
for obj in bpy.context.scene.objects:obj.hide_render=False
for modifier in target.modifiers:
    if modifier.type=='ARMATURE':modifier.show_viewport=True;modifier.show_render=True;modifier.use_deform_preserve_volume=False
rig.animation_data.action=bpy.data.actions['MacAnh_Idle'];scene.frame_set(0)
tag=sys.argv[sys.argv.index('--tag')+1] if '--tag' in sys.argv else 'DualUvTrial'
destination=root/'Production/LOD'/('MacAnh_'+label+'_'+tag+'.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
assert hashlib.file_digest(open(source,'rb'),'sha256').hexdigest()==source_hash
report={'status':'trial; colour/normal/seam comparisons pending','uv':'original UV0 unchanged; added NormalBakeUV UV1',
    'resolution':[4096,4096],'maps':{k:i.filepath_raw for k,i in images.items()},'body_material':body_material.name,'uv0_sha256':uv0_hash,
    'source_sha256':source_hash,'cage_extrusion_m':.004,'max_ray_distance_m':.02,'seconds':time.time()-started}
(out/'BakeReport.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('LOD_ATLAS_BAKE_COMPLETE',report,flush=True)
