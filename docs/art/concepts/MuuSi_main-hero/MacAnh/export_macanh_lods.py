"""Export a labelled working LOD and shared actions; original assets stay intact."""
import bpy,sys,os,json,hashlib,shutil
from pathlib import Path
from mathutils import Matrix
root=Path(__file__).resolve().parent;project=root.parents[4]
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
out=project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/LOD';out.mkdir(parents=True,exist_ok=True)
stage=project/'Library/MacAnh3DExport';stage.mkdir(parents=True,exist_ok=True)
source=Path(bpy.data.filepath);before=hashlib.file_digest(open(source,'rb'),'sha256').hexdigest()
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
plate_bones={b.name for b in rig.data.bones if b.name.endswith('ShoulderPlate')}
assert plate_bones in (set(),{'RightShoulderPlate','LeftShoulderPlate'})
assert len(rig.data.bones)==87+len(plate_bones)
max_influences=max(sum(g.weight>0 for g in v.groups) for v in body.data.vertices)
assert max_influences<=4
used=sorted({face.material_index for face in body.data.polygons})
if len(used)<len(body.data.materials):
    materials=[body.data.materials[index] for index in used];remap={old:new for new,old in enumerate(used)}
    assignments=[remap[face.material_index] for face in body.data.polygons]
    body.data.materials.clear()
    for material in materials:body.data.materials.append(material)
    for face,index in zip(body.data.polygons,assignments):face.material_index=index
body.data.calc_loop_triangles();body_triangles=len(body.data.loop_triangles);fan_triangles=0
for obj in bpy.context.scene.objects:
    if obj.type=='MESH' and obj.name.startswith('Fan_'):
        obj.data.calc_loop_triangles();fan_triangles+=len(obj.data.loop_triangles)
for modifier in body.modifiers:modifier.show_viewport=False;modifier.show_render=False
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.data.objects:
    if obj==rig or obj==body or obj.name.startswith(('Fan_','MacAnh_Fan','Socket_')):obj.select_set(True)
bpy.context.view_layer.objects.active=rig
options=dict(use_selection=True,global_scale=1,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',
    use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=False,
    mesh_smooth_type='OFF',path_mode='STRIP',embed_textures=False)
filename='MacAnh_'+label+'Rig.fbx'
bpy.ops.export_scene.fbx(filepath=str(stage/filename),bake_anim=False,**options)
os.replace(stage/filename,out/filename)
if label=='Battle':
    approved={'MacAnh_'+name for name in ('Idle','Walk','Run','Dodge','Attack01','Attack02','Skill01','Skill02','Skill03','Ultimate','DodgeAttack')}
    for action in list(bpy.data.actions):
        if action.name not in approved:bpy.data.actions.remove(action)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in bpy.data.objects:
        if obj.type=='EMPTY' and obj.name.startswith(('MacAnh_Fan','Socket_')):obj.select_set(True)
    filename='MacAnh_LODActions.fbx'
    bpy.ops.export_scene.fbx(filepath=str(stage/filename),bake_anim=True,bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_step=1,**options)
    os.replace(stage/filename,out/filename)
normal=root/'Production/LOD'/('MacAnh_'+label+'_Normal_4096.png')
if normal.exists():shutil.copyfile(normal,out/normal.name)
assert hashlib.file_digest(open(source,'rb'),'sha256').hexdigest()==before
report={'status':'working candidate; not approved for delivery','lod':label,'source':str(source),
    'source_sha256':before,'vertices':len(body.data.vertices),'bones':len(rig.data.bones),'max_influences':max_influences,
    'body_triangles':body_triangles,'fan_triangles':fan_triangles,'total_triangles':body_triangles+fan_triangles}
report['lining_materials']=[material.name for material in body.data.materials if material and 'lining' in material.name]
if 'lod_inner_materials' in body:report['inner_materials']=json.loads(body['lod_inner_materials'])
atlas=root/'Production/LOD'/('Atlas_'+label)
if body.get('lod_dual_uv'):
    report['body_material']=body['lod_body_material'];report['cap_material']=body.get('lod_cap_material','')
    report['dual_uv']=True
if body.get('lod_dual_uv') or any(material and material.name.startswith('MacAnh_'+label+'_AtlasBody') for material in body.data.materials):
    if not body.get('lod_dual_uv'):report['body_material']=next(material.name for material in body.data.materials if material and material.name.startswith('MacAnh_'+label+'_AtlasBody'))
    report['atlas']=True
    for kind in ('BaseColor','Normal','ORM'):
        filename='MacAnh_'+label+'_Atlas'+kind+'.png';shutil.copyfile(atlas/filename,out/filename)
(out/('Export_'+label+'.json')).write_text(json.dumps(report,indent=2))
print('MACANH_LOD_EXPORT_COMPLETE',report,flush=True)
