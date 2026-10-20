"""Selected-to-active tangent normals from immutable original to matching LOD UV.

Geometry-only bake artifact; skinning approval is a separate mandatory gate.
"""
import bpy,sys,json,time,hashlib
from pathlib import Path
root=Path(__file__).resolve().parent;out=root/'Production/LOD'
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
source=root.parent/'muu-si.blend'
source_hash=hashlib.file_digest(open(source,'rb'),'sha256').hexdigest()
rig=bpy.data.objects['MacAnh_Rig'];target=bpy.data.objects[rig['source_body_object']]
rig.animation_data.action=None
for p in rig.pose.bones:p.matrix_basis.identity()
for mod in target.modifiers:mod.show_viewport=False;mod.show_render=False
bpy.context.view_layer.update()
with bpy.data.libraries.load(str(source),link=False) as (available,loaded):
    loaded.objects=[rig['source_body_object']]
high=loaded.objects[0];bpy.context.scene.collection.objects.link(high)
high.parent=None;high.matrix_world.identity();high.animation_data_clear()
high.modifiers.clear()
for obj in bpy.context.scene.objects:obj.hide_render=obj not in (high,target)
high.hide_set(False);target.hide_set(False)
bpy.ops.object.select_all(action='DESELECT')
high.select_set(True);target.select_set(True);bpy.context.view_layer.objects.active=target
image=bpy.data.images.new('MacAnh_'+label+'_BakedNormal',width=4096,height=4096,alpha=False,float_buffer=False)
image.colorspace_settings.name='Non-Color'
for slot in target.material_slots:
    material=slot.material.copy();slot.material=material;material.use_nodes=True
    node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;material.node_tree.nodes.active=node
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_selected_to_active=True
scene.render.bake.cage_extrusion=.015;scene.render.bake.max_ray_distance=.03
scene.render.bake.margin=12
scene.render.bake.normal_space='TANGENT'
scene.render.bake.normal_r='POS_X';scene.render.bake.normal_g='POS_Y';scene.render.bake.normal_b='POS_Z'
started=time.time();print('LOD_NORMAL_BAKE_BEGIN',label,flush=True)
bpy.ops.object.bake(type='NORMAL')
image.filepath_raw=str(out/('MacAnh_'+label+'_Normal_4096.png'));image.file_format='PNG';image.save()
assert hashlib.file_digest(open(source,'rb'),'sha256').hexdigest()==source_hash
report={'lod':label,'method':'Cycles selected-to-active tangent-space normal, source material normals included',
    'resolution':[4096,4096],'margin_pixels':12,'cage_extrusion_m':.015,'max_ray_distance_m':.03,
    'source_sha256':source_hash,'image':image.filepath_raw,'seconds':time.time()-started,
    'status':'baked; inspect seams, silhouette and ray projection before approval'}
(out/('NormalBake_'+label+'.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
print('LOD_NORMAL_BAKE_COMPLETE',report,flush=True)
