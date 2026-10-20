"""Build only MacAnh's fan in a NEW scene. Does not open/edit any character file.
Blender 4.5 LTS: --background --python create_macanh_fan.py
Dimensions derive from Production/MacAnh_HandMeasurements.json. Front is -Y,
up is +Z, grip pivot is the origin. glTF conversion is performed by the exporter.
"""
import bpy, bmesh, os, json, math, numpy as np
from mathutils import Vector

ROOT=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(ROOT,'Production','Weapon')
os.makedirs(os.path.join(OUT,'Textures'),exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
measurements=json.load(open(os.path.join(ROOT,'Production','MacAnh_HandMeasurements.json'),encoding='utf-8'))
DESIGN=measurements['grip_design']

def linear(x): return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
def rgba(hex):
    return tuple(linear(int(hex[i:i+2],16)/255) for i in (0,2,4))+(1,)
def material(name,color,metal=0,rough=.5,emission=0):
    mat=bpy.data.materials.new(name); mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=rgba(color)
    shader.inputs['Metallic'].default_value=metal
    shader.inputs['Roughness'].default_value=rough
    shader.inputs['Emission Color'].default_value=rgba(color)
    shader.inputs['Emission Strength'].default_value=emission
    mat.diffuse_color=rgba(color)
    return mat
feather=material('MacAnh_Feather_PBR','F3ECFF',.02,.57)
gold=material('MacAnh_Gold_PBR','E6C47C',.82,.28)
crystal=material('MacAnh_Crystal_PBR','7032BF',.15,.19,.10)
crystal.node_tree.nodes['Principled BSDF'].inputs['Emission Color'].default_value=rgba('9657EF')
grip=material('MacAnh_Grip_PBR','21152E',.02,.48)
tassel=material('MacAnh_Tassel_PBR','552C91',.03,.52)

# Separate feather texture means gold never bleeds into white feather UVs.
size=512
u=np.linspace(0,1,size,dtype=np.float32)[None,:]
v=np.linspace(0,1,size,dtype=np.float32)[:,None]
t=np.clip((v-.48)/.48,0,1)
ivory=np.array(rgba('F3ECFF')[:3]); lavender=np.array(rgba('C5A5E8')[:3]); purple=np.array(rgba('552C91')[:3])
color=ivory[None,None,:]*(1-np.clip(t*1.7,0,1))[:,:,None]+lavender[None,None,:]*np.clip(t*1.7,0,1)[:,:,None]
edge=np.clip((t-.50)/.50,0,1)
color=color*(1-edge[:,:,None])+purple[None,None,:]*edge[:,:,None]
color=np.broadcast_to(color,(size,size,3)).copy()
barb=.965+.035*np.cos((v-abs(u-.5)*.44)*math.tau*36)
color*=barb[:,:,None]
vein=np.exp(-((u-.5)/.014)**2)*.32
color=color*(1-vein[:,:,None])+np.array(rgba('E6C47C')[:3])[None,None,:]*vein[:,:,None]
pixels=np.ones((size,size,4),dtype=np.float32); pixels[:,:,:3]=color
texture=bpy.data.images.new('MacAnh_Feathers_BaseColor',width=size,height=size,alpha=False)
texture.colorspace_settings.name='sRGB'; texture.pixels.foreach_set(pixels.ravel())
texture.filepath_raw=os.path.join(OUT,'Textures','MacAnh_Feathers_BaseColor.png')
texture.file_format='PNG'; texture.save(); texture.pack()
texnode=feather.node_tree.nodes.new('ShaderNodeTexImage'); texnode.image=texture
feather.node_tree.links.new(texnode.outputs['Color'],feather.node_tree.nodes['Principled BSDF'].inputs['Base Color'])

root=bpy.data.objects.new('MacAnh_Fan',None); scene.collection.objects.link(root)
root['unit']='meter'; root['grip_pivot']='origin'; root['front_axis']='-Y'; root['up_axis']='+Z'
root['character_source_untouched']=True
root['hand_fit_status']='static measurement; final fit pending rig approval'
groups={name:[] for name in ('Fan_Handle','Fan_GoldFrame','Fan_Feathers','Fan_Crystal','Fan_Tassels','Fan_Ornaments')}

def mesh_obj(name,vertices,faces,mat,group,uv=None):
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(vertices,[],faces); mesh.update()
    bm=bmesh.new(); bm.from_mesh(mesh); bmesh.ops.recalc_face_normals(bm,faces=bm.faces); bm.to_mesh(mesh); bm.free()
    obj=bpy.data.objects.new(name,mesh); scene.collection.objects.link(obj); obj.parent=root
    mesh.materials.append(mat)
    if uv:
        layer=mesh.uv_layers.new(name='UVMap')
        for poly in mesh.polygons:
            for li in poly.loop_indices: layer.data[li].uv=uv[mesh.loops[li].vertex_index]
    else:
        layer=mesh.uv_layers.new(name='UVMap')
        low=[min(p[i] for p in vertices) for i in range(3)]
        high=[max(p[i] for p in vertices) for i in range(3)]
        axes=sorted(range(3),key=lambda i:high[i]-low[i],reverse=True)[:2]
        for loop in mesh.loops:
            p=vertices[loop.vertex_index]
            layer.data[loop.index].uv=tuple(.04+.92*(p[i]-low[i])/max(.00001,high[i]-low[i]) for i in axes)
    groups[group].append(obj)
    return obj

def tube(name,points,radius,mat,group,sides=5):
    points=[Vector(p) for p in points]; verts=[]
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        reference=Vector((0,1,0)) if abs(tangent.y)<.9 else Vector((1,0,0))
        x=tangent.cross(reference).normalized(); y=tangent.cross(x).normalized()
        r=radius[i] if isinstance(radius,list) else radius
        for k in range(sides): verts.append(tuple(p+r*(x*math.cos(k*math.tau/sides)+y*math.sin(k*math.tau/sides))))
    faces=[]
    for i in range(len(points)-1):
        for k in range(sides):
            a=i*sides+k; b=i*sides+(k+1)%sides; c=b+sides; d=a+sides
            faces.append((a,b,c,d))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+k for k in range(sides))])
    obj=mesh_obj(name,verts,faces,mat,group)
    for poly in obj.data.polygons: poly.use_smooth=True
    return obj

def sphere(name,center,radius,mat,group):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=radius,location=center)
    obj=bpy.context.object; obj.name=name; obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    obj.parent=root; groups[group].append(obj)
    return obj

# Thirteen separate solid leaf forms, joined by logical group at the end.
layers=[(-62+i*124/8,1,0) for i in range(9)]+[(a,.76,-.018) for a in (-42,-14,14,42)]
for i,(degrees,length_factor,depth_offset) in enumerate(layers):
    angle=math.radians(degrees)
    direction=Vector((math.sin(angle),0,math.cos(angle)))
    lateral=Vector((math.cos(angle),0,-math.sin(angle)))
    length=(.20+.23*math.cos(angle))*length_factor
    width=.071+.018*math.cos(angle)
    origin=Vector((.021*math.sin(angle),depth_offset+.005*abs(degrees)/62,.113))
    vertices=[]; uv=[]; rows=12; cols=4
    for back in (False,True):
        for row in range(rows+1):
            t=row/rows
            center=origin+direction*(length*t)
            for col in range(cols+1):
                s=col/cols*2-1
                # Recessed barbs give a feather silhouette rather than a flat paddle.
                notch=1-.07*(row%2)*abs(s)
                spread=width*.5*max(.008,math.sin(math.pi*t)**.63)*(.90+.10*t)*notch
                p=center+lateral*s*spread
                # All feathers follow the same depth envelope by WORLD height.
                # Using each feather's normalized length made short front tips
                # emerge through the back layer during side/back inspection.
                height=(p.z-.113)/.43
                p.y+=.012*height-.013*math.sin(math.pi*height)
                p.y+=(.003 if back else -.003)-(.004*(1-abs(s)) if not back else 0)
                vertices.append(tuple(p)); uv.append((.04+.92*(col/cols),.025+.95*t))
    faces=[]; half=(rows+1)*(cols+1)
    for back in (0,1):
        for row in range(rows):
            for col in range(cols):
                a=back*half+row*(cols+1)+col
                faces.append((a,a+1,a+cols+2,a+cols+1))
    perimeter=list(range(cols+1))+[r*(cols+1)+cols for r in range(1,rows+1)]+[rows*(cols+1)+c for c in range(cols-1,-1,-1)]+[r*(cols+1) for r in range(rows-1,0,-1)]
    for j,a in enumerate(perimeter):
        b=perimeter[(j+1)%len(perimeter)]; faces.append((a,b,b+half,a+half))
    leaf=mesh_obj('Feather_%02d'%i,vertices,faces,feather,'Fan_Feathers',uv)
    for poly in leaf.data.polygons: poly.use_smooth=True

def gold_leaf(name,points,y):
    # Open filigree, rather than a flat filled plate, keeps the feathers visible.
    outline=[(x,y,z) for x,z in points]+[(points[0][0],y,points[0][1])]
    tube(name,outline,.0028,gold,'Fan_GoldFrame',4)
    a=Vector(outline[0]); b=Vector(outline[3])
    tube(name+'_Vein',[a,(a+b)*.5,b],.0018,gold,'Fan_GoldFrame',4)

# Sculptural metalwork on both sides of the fan. The rear is intentionally finished.
for back in (False,True):
    y=.022 if back else -.042
    for side in (-1,1):
        for level in range(3):
            x=.04+level*.032; z=.105+level*.027
            points=[(side*(x-.015),z-.025),(side*(x+.039),z+.027),(side*(x+.018),z+.019),
                    (side*(x+.012),z+.063),(side*(x-.010),z+.030),(side*(x-.025),z+.008)]
            gold_leaf(('Rear' if back else 'Front')+'Wing_%d_%d'%(side,level),points,y)
        for level in (0,1):
            cx=side*(.072+level*.031); cz=.101+level*.037
            points=[]
            for k in range(15):
                t=k/14; angle=-.5*math.pi+t*math.pi*2.05
                r=.038*(1-.78*t)
                points.append((cx+side*r*math.cos(angle),y-.002,cz+r*math.sin(angle)))
            tube('Gold_Scroll',points,[.0048*(1-.60*k/14) for k in range(15)],gold,'Fan_GoldFrame',4)
    # Diamond bezel follows outside of the crystal, leaving its facets visible.
    corners=[(0,y,.252),(.049,y,.186),(0,y,.121),(-.049,y,.186),(0,y,.252)]
    tube('Gold_DiamondBezel',corners,.0055,gold,'Fan_GoldFrame',5)

def gem(name,center,width,height,depth,mat,group):
    x,y,z=center
    outline=[(0,.5),(.23,.26),(.5,0),(.23,-.26),(0,-.5),(-.23,-.26),(-.5,0),(-.23,.26)]
    verts=[]
    for scale,offset in [(1,0),(.62,-depth*.42),(.62,depth*.34)]:
        verts.extend([(x+px*width*scale,y+offset,z+pz*height*scale) for px,pz in outline])
    verts.extend([(x,y-depth*.64,z),(x,y+depth*.5,z)])
    faces=[]
    for i in range(8):
        j=(i+1)%8
        faces.extend([(i,j,8+j,8+i),(i,16+i,16+j,j),(24,8+i,8+j),(25,16+j,16+i)])
    return mesh_obj(name,verts,faces,mat,group)
gem('Front_Amethyst',(0,-.043,.186),.077,.105,.028,crystal,'Fan_Crystal')
gem('Rear_Amethyst',(0,.018,.186),.062,.090,.022,crystal,'Fan_Crystal')

# Hand-calibrated grip, its centre is exactly the attachment pivot (0,0,0).
grip_length=DESIGN['length_m']; grip_radius=DESIGN['diameter_m']/2
tube('Black_Grip',[(0,0,-grip_length/2),(0,0,grip_length/2)],grip_radius,grip,'Fan_Handle',12)
for z in (-grip_length/2,grip_length/2):
    tube('Grip_GoldFerrule',[(0,0,z-.006),(0,0,z+.006)],grip_radius+.0025,gold,'Fan_Handle',12)
tube('Grip_Neck',[(0,0,.045),(0,0,.112)],.009,gold,'Fan_Handle',8)
for offset in (0,math.pi):
    pts=[]
    for k in range(19):
        t=k/18; a=offset+t*math.tau*1.25
        pts.append((grip_radius*1.035*math.cos(a),grip_radius*1.035*math.sin(a),-.040+t*.080))
    tube('Grip_GoldWrap',pts,.0014,gold,'Fan_Handle',4)
gem('Pommel_Amethyst',(0,-.002,-.065),.023,.028,.025,crystal,'Fan_Crystal')

# Two bead chains and ribbons hang independently from the decorative frame.
for side in (-1,1):
    anchor=Vector((side*.135,-.022,.106))
    chain=[]
    for k in range(6): chain.append(tuple(anchor+Vector((side*.012*math.sin(k*.5),.002*k,-.022*k))))
    tube('BeadChain',chain,.0019,gold,'Fan_Ornaments',4)
    for k in range(1,5):
        center=Vector(chain[k]); sphere('Gold_Bead',center,.0075,gold,'Fan_Ornaments')
        if k in (2,4): gem('Small_Amethyst',center,.012,.018,.010,crystal,'Fan_Crystal')
    start=Vector(chain[-1])+Vector((0,0,-.008))
    tube('Tassel_GoldCap',[start,start+Vector((0,0,-.020))],[.013,.016],gold,'Fan_Ornaments',8)
    for strand in range(5):
        pts=[]; radii=[]
        for k in range(10):
            t=k/9
            p=start+Vector((side*(.008+strand*.006)*math.sin(t*math.pi*.9),
                            (strand-1.5)*.005+.008*math.sin(t*math.pi),-.018-.123*t))
            pts.append(tuple(p)); radii.append(.0008+.0075*math.sin(math.pi*(.08+.92*t))**.65)
        tube('Purple_Tassel',pts,radii,tassel,'Fan_Tassels',5)

# Check that the shorter front leaves do not cut through the rear layer.
from mathutils.bvhtree import BVHTree
leaf_objects=groups['Fan_Feathers']
leaf_trees=[BVHTree.FromPolygons([v.co for v in obj.data.vertices],[tuple(p.vertices) for p in obj.data.polygons]) for obj in leaf_objects]
layer_intersections=sum(len(leaf_trees[i].overlap(leaf_trees[j])) for i in range(9) for j in range(9,13))
assert layer_intersections==0,('Front/back feather layers intersect',layer_intersections)

# Join logical pieces, keeping every object origin at the common grip pivot.
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join(); obj=bpy.context.object; obj.name=name
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    obj['logical_part']=name
    if not obj.data.uv_layers:
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(island_margin=.04); bpy.ops.object.mode_set(mode='OBJECT')

# Match the measured silhouette width on the fan crown only (never scale the character or grip).
feathers=bpy.data.objects['Fan_Feathers']
width=max(v.co.x for v in feathers.data.vertices)-min(v.co.x for v in feathers.data.vertices)
x_factor=DESIGN['fan_width_target_m']/width
for name in ('Fan_Feathers','Fan_GoldFrame','Fan_Crystal','Fan_Tassels','Fan_Ornaments'):
    for vertex in bpy.data.objects[name].data.vertices: vertex.co.x*=x_factor

for name,location in [('Socket_Grip',(0,0,0)),('Socket_Fan_Tip',(0,-.01,max(v.co.z for v in feathers.data.vertices)))]:
    socket=bpy.data.objects.new(name,None); scene.collection.objects.link(socket); socket.parent=root; socket.location=location
    socket.empty_display_type='ARROWS'; socket.empty_display_size=.04

def stats():
    meshes=[o for o in root.children if o.type=='MESH']; points=[]; count=0; parts=[]
    for obj in meshes:
        obj.data.calc_loop_triangles(); tris=len(obj.data.loop_triangles); count+=tris
        points.extend([obj.matrix_world @ v.co for v in obj.data.vertices])
        parts.append({'name':obj.name,'vertices':len(obj.data.vertices),'triangles':tris,
                      'material_slots':[m.name for m in obj.data.materials],'uv_layers':[u.name for u in obj.data.uv_layers]})
    low=[min(p[i] for p in points) for i in range(3)]; high=[max(p[i] for p in points) for i in range(3)]
    return {'triangles':count,'parts':parts,'min':low,'max':high,'dimensions':[high[i]-low[i] for i in range(3)]}

report={'asset':'MacAnh_Fan','blender':bpy.app.version_string,'units':'meters','grip_origin':[0,0,0],
        'blender_axes':{'front':'-Y','up':'+Z'},'feather_count':13,'feather_layer_intersections':layer_intersections,'grip':DESIGN,'geometry':stats(),
        'character_fit':'static hand measured; rig/posed grip not yet approved or validated',
        'character_source_modified':False,'closed_variant':False}
assert 3500<=report['geometry']['triangles']<=8000,report['geometry']['triangles']
assert abs(report['geometry']['dimensions'][0]-.60)<.015
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
camera=bpy.data.objects.new('FanPreviewCamera',bpy.data.cameras.new('FanPreviewCamera'))
scene.collection.objects.link(camera); scene.camera=camera; camera.data.type='ORTHO'; camera.data.ortho_scale=.85

# Export selected asset only; camera and studio lights are not part of the GLB.
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True)
for obj in root.children: obj.select_set(True)
bpy.context.view_layer.objects.active=root
glb=os.path.join(OUT,'MacAnh_Fan.glb')
bpy.ops.export_scene.gltf(filepath=glb,export_format='GLB',use_selection=True,export_yup=True,export_animations=False)
blend=os.path.join(OUT,'MacAnh_Fan.blend')
camera.location=target+Vector((-.65,-1,.35)); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=blend,compress=True)
for name,offset in [('Front',(0,-2,0)),('Back',(0,2,0)),('Left',(-2,0,0)),('Right',(2,0,0)),('ThreeQuarter',(-1.2,-2,.5))]:
    camera.location=target+Vector(offset); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=os.path.join(OUT,'MacAnh_Fan_'+name+'.png'); bpy.ops.render.render(write_still=True)
    print('MACANH_FAN_RENDERED',name,flush=True)
with open(os.path.join(OUT,'MacAnh_Fan_Report.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2)
print('MACANH_FAN_BUILD_SUCCESS',json.dumps({'triangles':report['geometry']['triangles'],'dimensions':report['geometry']['dimensions']}),flush=True)
