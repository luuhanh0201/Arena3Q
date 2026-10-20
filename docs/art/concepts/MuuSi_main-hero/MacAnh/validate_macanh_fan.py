"""GLB round-trip QA. Run against MacAnh_Fan.blend; never opens character source."""
import bpy,os,json,math,numpy as np
from mathutils import Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(ROOT,'Production','Weapon')
expected=json.load(open(os.path.join(OUT,'MacAnh_Fan_Report.json'),encoding='utf-8'))

def geometry():
    points=[]; tris=0; meshes={}; positions={}; issues=[]
    for obj in bpy.data.objects:
        if obj.type!='MESH': continue
        mesh=obj.data; mesh.calc_loop_triangles()
        if not mesh.uv_layers: issues.append(obj.name+': no UV')
        array=np.array([tuple(obj.matrix_world @ v.co) for v in mesh.vertices],dtype=np.float32)
        if not np.isfinite(array).all(): issues.append(obj.name+': non-finite coordinate')
        points.extend(array.tolist())
        tris+=len(mesh.loop_triangles)
        meshes[obj.name]={'triangles':len(mesh.loop_triangles),'uv_layers':len(mesh.uv_layers),
                          'materials':[m.name for m in mesh.materials]}
        # Export may split vertices for hard normals/UVs; compare unique positions instead.
        positions[obj.name]=sorted(set(tuple(round(float(n),6) for n in p) for p in array))
        zero_area=0
        for triangle in mesh.loop_triangles:
            a,b,c=[Vector(array[i]) for i in triangle.vertices]
            if (b-a).cross(c-a).length<1e-12: zero_area+=1
        if zero_area: issues.append(obj.name+': zero-area triangles '+str(zero_area))
    low=[min(p[i] for p in points) for i in range(3)]; high=[max(p[i] for p in points) for i in range(3)]
    return {'triangles':tris,'meshes':meshes,'bounds':{'min':low,'max':high,'dimensions':[high[i]-low[i] for i in range(3)]},'issues':issues},positions

before,positions_before=geometry()
source_materials={m.name:{'color':list(m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value),
                         'metallic':m.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value,
                         'roughness':m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value} for m in bpy.data.materials}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(OUT,'MacAnh_Fan.glb'))
bpy.context.view_layer.update()
after,positions_after=geometry()
checks={
 'triangle_count':before['triangles']==after['triangles']==expected['geometry']['triangles'],
 'six_logical_meshes':set(before['meshes'])==set(after['meshes'])=={'Fan_Handle','Fan_GoldFrame','Fan_Feathers','Fan_Crystal','Fan_Tassels','Fan_Ornaments'},
 'bounds_meters':max(abs(before['bounds']['dimensions'][i]-after['bounds']['dimensions'][i]) for i in range(3))<.00001,
 'unique_vertex_positions':positions_before==positions_after,
 'no_geometry_issues':not before['issues'] and not after['issues'],
 'five_pbr_materials':set(source_materials)==set(m.name for m in bpy.data.materials),
 'grip_pivot_origin':bpy.data.objects.get('Socket_Grip') is not None and bpy.data.objects['Socket_Grip'].matrix_world.to_translation().length<.000001,
 'fan_tip_retained':bpy.data.objects.get('Socket_Fan_Tip') is not None,
 'feather_image_embedded':any(tuple(i.size)==(512,512) and i.packed_file for i in bpy.data.images)
}
material_errors=[]
for name,values in source_materials.items():
    mat=bpy.data.materials.get(name)
    if not mat: material_errors.append(name+': missing'); continue
    shader=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if not shader: material_errors.append(name+': no PBR shader'); continue
    for key,socket in [('metallic','Metallic'),('roughness','Roughness')]:
        if abs(shader.inputs[socket].default_value-values[key])>.00001: material_errors.append(name+': '+key)
    if name!='MacAnh_Feather_PBR' and max(abs(shader.inputs['Base Color'].default_value[i]-values['color'][i]) for i in range(4))>.00001:
        material_errors.append(name+': base color')
checks['pbr_factors_preserved']=not material_errors
report={'blender':bpy.app.version_string,'checks':checks,'source':before,'reimported':after,
        'material_errors':material_errors,'result':'PASS' if all(checks.values()) else 'FAIL',
        'unity_runtime_tested':False,'posed_hand_fit_tested':False}
with open(os.path.join(OUT,'MacAnh_Fan_RoundTrip.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print('MACANH_FAN_ROUNDTRIP',json.dumps(checks),flush=True)
assert all(checks.values()),'See MacAnh_Fan_RoundTrip.json'

# Render the imported GLB, rather than the original Blender objects, for visual QA.
scene=bpy.context.scene; scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
scene.render.engine='BLENDER_EEVEE_NEXT'; scene.eevee.taa_render_samples=64
scene.render.resolution_x=scene.render.resolution_y=960; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='AgX'
world=bpy.data.worlds.new('FanStudio'); world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.055,.035,.08,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.35; scene.world=world
target=Vector((0,0,.20))
for name,location,energy,size in [('Key',(1.1,-1.7,2),130,2),('Fill',(-1.6,-.5,1),80,1.5),('Rim',(.5,1.4,1.4),180,1.5)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=location
    obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('RoundTripCamera',bpy.data.cameras.new('RoundTripCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'; camera.data.ortho_scale=.85
camera.location=target+Vector((0,-2,0)); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=os.path.join(OUT,'MacAnh_Fan_GLB_Reimport.png'); bpy.ops.render.render(write_still=True)
arrays=[]
for name in ('MacAnh_Fan_Front.png','MacAnh_Fan_GLB_Reimport.png'):
    image=bpy.data.images.load(os.path.join(OUT,name),check_existing=False)
    values=np.empty(960*960*4,dtype=np.float32); image.pixels.foreach_get(values)
    arrays.append(values.reshape(960,960,4)); bpy.data.images.remove(image)
mask=(arrays[0][:,:,3]>.1)|(arrays[1][:,:,3]>.1)
error=float(np.abs(arrays[0][mask]-arrays[1][mask]).mean())
report['render_mean_absolute_error']=error
report['checks']['render_roundtrip_matches']=error<.015
report['result']='PASS' if all(report['checks'].values()) else 'FAIL'
with open(os.path.join(OUT,'MacAnh_Fan_RoundTrip.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
assert error<.015,('Rendered GLB differs from source',error)
print('MACANH_FAN_ROUNDTRIP_SUCCESS',flush=True)
