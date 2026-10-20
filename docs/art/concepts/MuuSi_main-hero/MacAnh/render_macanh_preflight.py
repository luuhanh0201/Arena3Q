"""Render the existing character for inspection without saving or editing its mesh."""
import bpy,os,math
from mathutils import Vector
root=os.path.dirname(os.path.abspath(__file__))
output=os.path.join(root,'Production','Preflight')
os.makedirs(output,exist_ok=True)
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE_NEXT'
scene.eevee.taa_render_samples=32
scene.render.resolution_x=scene.render.resolution_y=800
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGBA'
scene.render.film_transparent=True
for name,location,energy,size in [('Key',(2,-3,4),350,3),('Fill',(-3,-2,2),180,3),('Rim',(1,3,3),400,2)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=location
    obj.rotation_euler=(Vector((0,0,.85))-obj.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('InspectionCamera',bpy.data.cameras.new('InspectionCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'; camera.data.ortho_scale=2.05
for name,offset in [('Front',(0,-4,.0)),('Back',(0,4,0)),('Right',(-4,0,0)),('Left',(4,0,0)),('ThreeQuarter',(-3,-4,1.5))]:
    target=Vector((0,0,.85)); camera.location=target+Vector(offset)
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=os.path.join(output,'MacAnh_ExistingModel_'+name+'.png')
    bpy.ops.render.render(write_still=True)
print('MACANH_EXISTING_MODEL_RENDER_SUCCESS',flush=True)
