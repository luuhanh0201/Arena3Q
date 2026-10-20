"""Measure visible right-hand landmarks on the supplied static mesh; no rig is added."""
import bpy,os,json
from mathutils import Vector
root=os.path.dirname(os.path.abspath(__file__))
output=os.path.join(root,'Production','Preflight')
body=next(o for o in bpy.data.objects if o.type=='MESH')
# Front orthographic image coordinates, recorded so these selections are reviewable.
pixels={'wrist':(262,485),'palm':(249,499),'middle_tip':(232,514),'thumb_tip':(271,509)}
points={}
for name,(x,y) in pixels.items():
    origin=Vector(((x-400)*2.05/800,-4,.85+(400-y)*2.05/800))
    ok,local,normal,face=body.ray_cast(body.matrix_world.inverted() @ origin,Vector((0,1,0)))
    if ok: points[name]=list(body.matrix_world @ local)
report={'method':'manual visible landmarks on front orthographic source render; not joint centers',
        'front_render_pixels':pixels,'landmarks_world':points,
        'wrist_to_middle_tip_m':(Vector(points['wrist'])-Vector(points['middle_tip'])).length,
        'palm_to_thumb_tip_m':(Vector(points['palm'])-Vector(points['thumb_tip'])).length,
        'grip_design':{'length_m':.095,'diameter_m':.026,'fan_width_target_m':.60},
        'status':'provisional static-mesh fit; final grip/socket requires approved rig and posed hand'}
with open(os.path.join(root,'Production','MacAnh_HandMeasurements.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE_NEXT'; scene.eevee.taa_render_samples=32
scene.render.resolution_x=scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.render.film_transparent=True
for name,location,energy in [('Key',(1,-3,3),300),('Fill',(-3,-2,2),170)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.size=3
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=location
    obj.rotation_euler=(Vector((-.37,-.15,.61))-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('HandInspectionCamera',bpy.data.cameras.new('HandInspectionCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'; camera.data.ortho_scale=.23
target=Vector((-.37,-.1,.61))
for name,offset in [('Front',(0,-3,0)),('Side',(-3,0,0))]:
    camera.location=target+Vector(offset); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=os.path.join(output,'MacAnh_RightHand_'+name+'.png'); bpy.ops.render.render(write_still=True)
print('MACANH_HAND_MEASUREMENTS',json.dumps(report),flush=True)
