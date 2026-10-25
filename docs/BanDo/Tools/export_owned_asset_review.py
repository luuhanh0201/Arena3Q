"""Export read-only FBX derivatives for asset approval. Does not build a map or save source FBX/blend files."""
import bpy, json, sys, hashlib
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[3]
review = root / 'NghiemThu/Battlefield/XemTruoc_Assets_20261025'
plan = json.loads((Path(__file__).parent / 'asset-review/owned-export-plan.json').read_text(encoding='utf8'))
for item in plan:
    for quality in ['budget', 'source']:
        target = review / 'models' / (item['id'] + '-' + quality + '.glb')
        if target.exists():
            continue
        source = root / item[quality]['model']
        before = hashlib.file_digest(source.open('rb'), 'sha256').hexdigest()
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(source))
        meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
        if not meshes:
            raise RuntimeError('No source geometry: '+str(source))
        base = bpy.data.images.load(str(root / item[quality]['baseColor']), check_existing=True)
        normal = bpy.data.images.load(str(root / item[quality]['normal']), check_existing=True)
        normal.colorspace_settings.name='Non-Color'
        material=bpy.data.materials.new('SourceMaterial');material.use_nodes=True
        nodes=material.node_tree.nodes; shader=nodes.get('Principled BSDF')
        shader.inputs['Roughness'].default_value=.82
        shader.inputs['Metallic'].default_value=0
        tex=nodes.new('ShaderNodeTexImage');tex.image=base
        bump=nodes.new('ShaderNodeTexImage');bump.image=normal
        normal_node=nodes.new('ShaderNodeNormalMap');normal_node.inputs['Strength'].default_value=.45
        material.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
        material.node_tree.links.new(bump.outputs['Color'],normal_node.inputs['Color'])
        material.node_tree.links.new(normal_node.outputs['Normal'],shader.inputs['Normal'])
        for obj in meshes:
            obj.data.materials.clear();obj.data.materials.append(material)
        bpy.context.view_layer.update()
        corners=[obj.matrix_world@Vector(corner) for obj in meshes for corner in obj.bound_box]
        low=Vector(tuple(min(v[axis] for v in corners) for axis in range(3)))
        high=Vector(tuple(max(v[axis] for v in corners) for axis in range(3)))
        size=high-low
        factor=item['size']/ (size.z if item['heightSized'] else max(size.x,size.y))
        origin=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
        wrapper=bpy.data.objects.new(item['id'],None);bpy.context.collection.objects.link(wrapper)
        for obj in list(bpy.context.scene.objects):
            if obj!=wrapper and obj.parent is None:
                obj.parent=wrapper
        wrapper.scale=Vector((factor,)*3);wrapper.location=-origin*factor
        target.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',export_animations=False,
            export_materials='EXPORT',export_image_format='AUTO',export_keep_originals=False,
            export_jpeg_quality=100,export_texcoords=True,export_normals=True,
            export_cameras=False,export_lights=False)
        if hashlib.file_digest(source.open('rb'),'sha256').hexdigest()!=before:
            raise RuntimeError('Source modified unexpectedly')
        print('ASSET_REVIEW_EXPORT '+item['id']+' '+quality+' '+str(target.stat().st_size),flush=True)
print('ASSET_REVIEW_EXPORT_DONE',flush=True)
