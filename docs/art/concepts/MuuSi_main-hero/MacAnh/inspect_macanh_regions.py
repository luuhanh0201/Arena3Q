"""Render labelled garment surfaces and export bind geometry for repair diagnostics."""
import bpy, json, sys
import numpy as np
from pathlib import Path
from mathutils import Vector
r=bpy.data.objects['MacAnh_Rig']; o=bpy.data.objects[r['source_body_object']]
m=o.data
m.calc_loop_triangles()
np.savez_compressed('Logs/MacAnh-bind-regions.npz',co=np.array([v.co[:] for v in m.vertices]),
    faces=np.array([p.vertices[:] for p in m.loop_triangles]),regions=np.array([m.attributes['GarmentRegion'].data[p.polygon_index].value for p in m.loop_triangles]))
Path('Logs/MacAnh-bind-bones.json').write_text(json.dumps({b.name:dict(head=list(b.head_local),tail=list(b.tail_local)) for b in r.data.bones},indent=2))
for color in [(.5,.5,.5,1),(.1,.5,1,1),(1,.3,.1,1),(0,1,.4,1),(1,1,0,1),(.8,0,1,1),(1,0,.5,1)]:
    mat=bpy.data.materials.new('DiagnosticRegion');mat.diffuse_color=color;mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=.8
    m.materials.append(mat)
start=len(m.materials)-7
if '--textured' not in sys.argv:
    for p in m.polygons:p.material_index=start+m.attributes['GarmentRegion'].data[p.index].value
s=bpy.context.scene;s.render.engine='BLENDER_EEVEE_NEXT';s.eevee.taa_render_samples=16
s.render.resolution_x=s.render.resolution_y=900;s.render.resolution_percentage=100
s.world.color=(.3,.3,.3)
for name,pos in [('Key',(-3,-4,4)),('Fill',(3,-2,2))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=500;d.size=4
    light=bpy.data.objects.new(name,d);s.collection.objects.link(light);light.location=pos
    light.rotation_euler=(Vector((0,0,.8))-light.location).to_track_quat('-Z','Y').to_euler()
cam=bpy.data.objects.new('DiagnosticCamera',bpy.data.cameras.new('DiagnosticCamera'));s.collection.objects.link(cam);s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.3
poses=[('Rest',0),('Skill02',30)] if '--gait' not in sys.argv else [('Run',0),('Run',6),('Run',12),('Run',18)]
for clip,f in poses:
    r.animation_data.action=None if clip=='Rest' else bpy.data.actions['MacAnh_'+clip]
    if clip=='Rest':
        for p in r.pose.bones:p.matrix_basis.identity()
    s.frame_set(f);bpy.context.view_layer.update()
    views=[('Front',(0,-4,.2)),('Back',(0,4,.2))] if '--gait' not in sys.argv else [('Front',(0,-4,.3)),('Right',(-4,0,.3))]
    for name,pos in views:
        target=Vector((0,0,.20 if '--gait' in sys.argv else .6))
        cam.data.ortho_scale=.72 if '--gait' in sys.argv else 1.3
        cam.location=target+Vector(pos);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        label=Path(bpy.data.filepath).stem
        s.render.filepath=str(Path('Logs').resolve()/f'{label}-{clip}-{f:03}-{name}.png');bpy.ops.render.render(write_still=True)
print('MACANH_REGION_RENDER_COMPLETE',flush=True)
