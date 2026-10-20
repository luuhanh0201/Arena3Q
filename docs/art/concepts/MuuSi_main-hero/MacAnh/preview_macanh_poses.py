"""Render representative TRUE skinned poses and inspect the grip without saving."""
import bpy,os,sys,json
from mathutils import Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
OUT=sys.argv[sys.argv.index('--out')+1] if '--out' in sys.argv else os.path.join(ROOT,'Production','Poses'); os.makedirs(OUT,exist_ok=True)
OUT=os.path.abspath(OUT)
rig=bpy.data.objects['MacAnh_Rig']; scene=bpy.context.scene
if '--clearance-prototype' in sys.argv:
    sys.path.insert(0,ROOT)
    from macanh_fan_clearance import apply_pose,sweep_tassels
    sweep_tassels(.04)
body=bpy.data.objects[rig['source_body_object']]
if '--source-reference' in sys.argv:
    from pathlib import Path
    candidate=body
    with bpy.data.libraries.load(str(Path(ROOT).parent/'muu-si.blend'),link=False) as (available,loaded):loaded.objects=[rig['source_body_object']]
    body=loaded.objects[0];scene.collection.objects.link(body);body.parent=None;body.matrix_world.identity();body.modifiers.clear()
    modifier=body.modifiers.new('Read-only appearance reference','ARMATURE');modifier.object=rig;modifier.use_deform_preserve_volume=True
    candidate.hide_render=True
for modifier in body.modifiers:
    if modifier.type=='ARMATURE':
        modifier.show_viewport=True;modifier.show_render=True;modifier.use_deform_preserve_volume='--source-reference' in sys.argv
if '--regions' in sys.argv:
    palette=[(.2,.2,.2,1),(1,.1,.1,1),(.1,.3,1,1),(1,.7,.1,1),(.1,1,.5,1),(1,.1,1,1),(.1,1,1,1)]
    body.data.materials.clear()
    for i,colour in enumerate(palette):
        mat=bpy.data.materials.new('Inspection region '+str(i));mat.diffuse_color=colour;mat.use_nodes=True
        mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=colour
        mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.85
        body.data.materials.append(mat)
    attribute=body.data.attributes['GarmentRegion']
    for face in body.data.polygons:face.material_index=attribute.data[face.index].value
if '--plain-caps' in sys.argv:
    for slot in list(body.material_slots)[1:]:
        mat=bpy.data.materials.new('Inspection plain inner fabric');mat.use_nodes=True
        surface=mat.node_tree.nodes['Principled BSDF'];surface.inputs['Base Color'].default_value=(.065,.025,.12,1)
        surface.inputs['Roughness'].default_value=.85;slot.material=mat
if '--no-normal' in sys.argv:
    for slot in body.material_slots:
        if slot.material is None or not slot.material.use_nodes:continue
        material=slot.material.copy();slot.material=material
        surface=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        for link in list(surface.inputs['Normal'].links):material.node_tree.links.remove(link)
if '--white-body-caps' in sys.argv:
    material=bpy.data.materials.new('Inspection white inner robe');material.use_nodes=True
    surface=material.node_tree.nodes['Principled BSDF'];surface.inputs['Base Color'].default_value=(.62,.59,.68,1)
    surface.inputs['Roughness'].default_value=.85
    index=len(body.data.materials);body.data.materials.append(material)
    region=body.data.attributes['GarmentRegion']
    for face in body.data.polygons:
        if face.material_index>0 and region.data[face.index].value==0:face.material_index=index
scene.render.engine='BLENDER_EEVEE_NEXT'; scene.eevee.taa_render_samples=16
scene.render.resolution_x=scene.render.resolution_y=800; scene.render.resolution_percentage=100
if '--phone' in sys.argv:scene.render.resolution_x=1440;scene.render.resolution_y=810
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.render.film_transparent=True
for name,location,energy,size in [('Key',(2,-3,4),350,3),('Fill',(-3,-2,2),180,3),('Rim',(1,3,3),400,2)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=location
    obj.rotation_euler=(Vector((0,0,.85))-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('PoseInspectionCamera',bpy.data.cameras.new('PoseInspectionCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'
poses=sys.argv[sys.argv.index('--poses')+1].split(',') if '--poses' in sys.argv else ['Idle:0','Run:6','Attack01:9','Attack01:14','Attack01:24']
checks=[]
for label in poses:
    name,frame=label.split(':'); frame=int(frame)
    rig.animation_data.action=bpy.data.actions['MacAnh_'+name]; scene.frame_set(frame); bpy.context.view_layer.update()
    if '--clearance-prototype' in sys.argv:apply_pose(rig,bpy.data.objects['MacAnh_Fan'])
    views=[('Front',(0,-4,.0)),('ThreeQuarter',(-3,-4,1.5))]
    if '--all-views' in sys.argv:views+=[('Back',(0,4,0)),('Right',(-4,0,0)),('Left',(4,0,0)),('Top',(0,-1,5))]
    if '--views' in sys.argv:
        names=sys.argv[sys.argv.index('--views')+1].split(',');views=[v for v in views if v[0] in names]
    for view,offset in views:
        target=Vector((0,0,.85)); camera.location=target+Vector(offset); camera.data.ortho_scale=4.0 if '--phone' in sys.argv else 2.5
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=os.path.join(OUT,f'{name}_{frame:03d}_{view}.png'); bpy.ops.render.render(write_still=True)
    grip=rig.matrix_world @ rig.pose.bones['Socket_Hand_R'].head
    distance=(bpy.data.objects['MacAnh_Fan'].matrix_world.to_translation()-grip).length
    checks.append({'action':name,'frame':frame,'grip_error_m':distance,
        'max_bone_scale_error':max(abs(s-1) for p in rig.pose.bones for s in p.scale)})
    if name=='Idle':
        target=grip; camera.location=target+Vector((-1.5,-3,1.3)); camera.data.ortho_scale=.32
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=os.path.join(OUT,'Idle_Grip_Closeup.png'); bpy.ops.render.render(write_still=True)
    print('MACANH_PREVIEWED',label,checks[-1],flush=True)
with open(os.path.join(OUT,'PoseChecks.json'),'w',encoding='utf-8') as f: json.dump(checks,f,indent=2)
assert max(c['grip_error_m'] for c in checks)<.0001
assert max(c['max_bone_scale_error'] for c in checks)<.0001
print('MACANH_POSE_PREVIEW_SUCCESS',flush=True)
