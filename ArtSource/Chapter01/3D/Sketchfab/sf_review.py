"""Render identical-camera before/after sheets from the normalization QA blends.
Four view pairs: left before reduction/resize, right export geometry/materials; 1.8 m figure.
"""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT = Path(__file__).resolve().parents[4]
QA = ROOT/'ArtSource/Chapter01/3D/Renders/Sketchfab'
OUT = QA/'Comparison'; OUT.mkdir(parents=True, exist_ok=True)
VIEWS = [(45,50),(0,12),(90,12),(225,30)]; S = 360

def bounds(obs):
    pts = [o.matrix_world @ Vector(c) for o in obs for c in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])

def figure(x, y, z):
    mat = bpy.data.materials.new('Scale figure'); mat.diffuse_color = (.08,.25,.45,1)
    parts = []
    for center, scale in [((0,0,1.65),(.15,.15,.15)),((0,0,1.1),(.22,.12,.4)),((-.13,0,.36),(.09,.09,.36)),((.13,0,.36),(.09,.09,.36)),((-.29,0,1.05),(.075,.075,.34)),((.29,0,1.05),(.075,.075,.34))]:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,location=(x+center[0],y+center[1],z+center[2]))
        o=bpy.context.object; o.scale=scale; o.data.materials.append(mat); parts.append(o)
    return parts

def render(code):
    bpy.ops.wm.open_mainfile(filepath=str(QA/(code+'_qa.blend')))
    sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE_NEXT'; sc.eevee.taa_render_samples=16
    sc.render.resolution_x=sc.render.resolution_y=S; sc.render.resolution_percentage=100
    sc.world=bpy.data.worlds.new('ReviewWorld'); sc.world.use_nodes=True
    sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.57,.6,1)
    sc.world.node_tree.nodes['Background'].inputs[1].default_value=.8
    sun=bpy.data.objects.new('ReviewSun',bpy.data.lights.new('ReviewSun','SUN')); sc.collection.objects.link(sun)
    sun.data.energy=3; sun.rotation_euler=(.7,.2,.8)
    cam=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera')); sc.collection.objects.link(cam); sc.camera=cam
    cam.data.type='ORTHO'
    pieces=sorted(set(o['piece'] for o in sc.objects if 'piece' in o))
    for piece in pieces:
        objs=[o for o in sc.objects if o.get('piece')==piece]
        lo,hi=bounds(objs); humans=figure(hi.x+.45,(lo.y+hi.y)/2,lo.z)
        human_base=Vector((hi.x+.45,(lo.y+hi.y)/2,lo.z))
        offsets=[o.location-human_base for o in humans]
        l,h=bounds(objs+humans); ctr=(l+h)/2; radius=max((h-l).length/2,.1)
        cam.data.ortho_scale=radius*2.15; cam.data.clip_start=.01; cam.data.clip_end=max(100,radius*10)
        sheet=np.zeros((S*2,S*4,4),dtype=np.float32)
        for row,state in enumerate(['before','after']):
            for o in objs: o.hide_render=o['state']!=state
            for col,(yaw,elevation) in enumerate(VIEWS):
                y,e=math.radians(yaw),math.radians(elevation); d=Vector((math.sin(y)*math.cos(e),-math.cos(y)*math.cos(e),math.sin(e)))
                right=Vector((math.cos(y),math.sin(y),0))
                base=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))+right*(Vector((hi.x-lo.x,hi.y-lo.y,0)).length/2+.45)
                for human,offset in zip(humans,offsets): human.location=base+offset
                cam.location=ctr+d*radius*4; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
                tmp=OUT/'_frame.png'; sc.render.filepath=str(tmp); bpy.ops.render.render(write_still=True)
                im=bpy.data.images.load(str(tmp)); pixels=np.array(im.pixels[:],dtype=np.float32).reshape(S,S,4)
                pair_row=col//2; pair_col=(col%2)*2+row
                sheet[(1-pair_row)*S:(2-pair_row)*S,pair_col*S:(pair_col+1)*S]=pixels; bpy.data.images.remove(im)
        image=bpy.data.images.new('Comparison',S*4,S*2,alpha=True); image.pixels=sheet.ravel()
        image.filepath_raw=str(OUT/(code+'_'+piece+'.png')); image.file_format='PNG'; image.save(); bpy.data.images.remove(image)
        for o in objs: o.hide_render=True
        for o in humans: bpy.data.objects.remove(o,do_unlink=True)
        print('REVIEW',code,piece,flush=True)

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
codes=args[0].split(',') if args else [m['code'] for m in json.loads((QA/'normalization.json').read_text(encoding='utf-8'))]
for code in codes: render(code)
print('REVIEW_DONE',len(codes),flush=True)
