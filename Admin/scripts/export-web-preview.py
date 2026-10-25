"""Read a production .blend and export a full-resolution GLB derivative. Never saves the source."""
import bpy, json, sys, hashlib
from pathlib import Path
from mathutils import Matrix
argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
hero=argv[argv.index('--hero')+1]
root=Path(__file__).resolve().parents[1]
output=root/'.local/storage/web-preview'/hero
output.mkdir(parents=True,exist_ok=True)
allowed=['Idle','Walk','Run','Attack01','Attack02','Dodge','DodgeAttack','Skill01','Skill02','Skill03','Ultimate'] if hero=='lord_strategist' else ['Idle','Walk','Run','Attack01','Attack02','Victory','Defeat']
source=Path(bpy.data.filepath)
for action in list(bpy.data.actions):
    name=action.name.removeprefix('MacAnh_').removeprefix('LuBu_')
    if name not in allowed:bpy.data.actions.remove(action)
    else:action.name=name
rigs=[o for o in bpy.data.objects if o.type=='ARMATURE']
for rig in rigs:
    if rig.animation_data:rig.animation_data.action=None
    for bone in rig.pose.bones:bone.matrix_basis=Matrix.Identity(4)
for obj in bpy.data.objects:
    obj.hide_set(False)
    obj.hide_viewport=False
    obj.select_set(obj.type in ['MESH','ARMATURE','EMPTY'])
    if obj.type=='MESH':
        if obj.parent and obj.parent.type=='ARMATURE' and obj.parent_type=='BONE':
            parent=obj.parent; bone=obj.parent_bone; world=obj.matrix_world.copy()
            obj.parent_type='OBJECT';obj.parent_bone='';obj.matrix_world=world
            group=obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
            group.add(list(range(len(obj.data.vertices))),1.0,'REPLACE')
            modifier=obj.modifiers.new('WebBoneParentEquivalent','ARMATURE');modifier.object=parent
        obj.modifiers.new('WebTriangles','TRIANGULATE')
        # GLTF uses linear skinning. Preserve source geometry and all source textures/UV channels.
        for modifier in obj.modifiers:
            if modifier.type=='ARMATURE':modifier.use_deform_preserve_volume=False
bpy.context.view_layer.update()
available={p.identifier for p in bpy.ops.export_scene.gltf.get_rna_type().properties}
settings={'filepath':str(output/'model.glb'),'export_format':'GLB','use_selection':True,'export_yup':True,'export_animations':True,'export_animation_mode':'ACTIONS','export_frame_range':False,'export_force_sampling':True,'export_frame_step':1,'export_skins':True,'export_all_influences':True,'export_morph':True,'export_texcoords':True,'export_normals':True,'export_tangents':True,'export_materials':'EXPORT','export_image_format':'AUTO','export_keep_originals':True,'export_draco_mesh_compression_enable':False,'export_optimize_animation_size':False,'export_cameras':False,'export_lights':False}
settings['export_armature_object_remove']=False
settings['export_def_bones']=False
settings['export_keep_originals']=False
settings['export_image_format']='AUTO'
settings['export_jpeg_quality']=100
result=bpy.ops.export_scene.gltf(**{key:value for key,value in settings.items() if key in available})
if 'FINISHED' not in result or not (output/'model.glb').is_file():raise RuntimeError('GLB export failed')
import struct
glb=(output/'model.glb').read_bytes();document=json.loads(glb[20:20+struct.unpack_from('<I',glb,12)[0]].decode('utf8'))
if any(image.get('uri','') and not image['uri'].startswith('data:') for image in document.get('images',[])):raise RuntimeError('GLB must embed every texture')
if sum(1 for material in document.get('materials',[]) if material.get('pbrMetallicRoughness',{}).get('baseColorTexture'))<2:raise RuntimeError('Source base color textures were not exported')
report={'heroId':hero,'sourcePath':str(source.relative_to(root.parent)).replace('\\','/'),'sourceSha256':hashlib.file_digest(source.open('rb'),'sha256').hexdigest(),'sourceBytes':source.stat().st_size,'glbBytes':(output/'model.glb').stat().st_size,'quality':'FULL_SOURCE_NO_DECIMATION','clips':[a.name for a in bpy.data.actions],'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons)} for o in bpy.data.objects if o.type=='MESH'],'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None} for rig in rigs for b in rig.data.bones],'textures':[{'name':i.name,'width':i.size[0],'height':i.size[1]} for i in bpy.data.images if i.source=='FILE'],'note':'Web GLB derivative; existing source deformation is not repaired or claimed QA-passed.'}
(output/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print('ADMIN_WEB_PREVIEW_DONE='+str(output/'model.glb'),flush=True)
