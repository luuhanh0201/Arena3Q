"""Restore hand surface detail from the higher-resolution authored mesh."""
import bpy,bmesh,json,sys,numpy as np
from pathlib import Path
from mathutils.bvhtree import BVHTree
from mathutils import Vector
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
root=Path(__file__).resolve().parent
tree=BVHTree.FromPolygons([v.co for v in m.vertices],[list(p.vertices) for p in m.polygons])
regions=[a.value for a in m.attributes['GarmentRegion'].data]
image=bpy.data.images['Image_0'];w,h=image.size
pixels=np.empty(w*h*4,dtype=np.float32);image.pixels.foreach_get(pixels)
def skin_colour(uv):
 x=int(uv.x*w)%w;y=int(uv.y*h)%h;red,green,blue=pixels[(y*w+x)*4:(y*w+x)*4+3]
 return red-blue>.05 and red-green>.03 and red>.35 and blue>red*.65 and green>red*.65
def hand_box(c):return -.49<c.x<-.30 and -.26<c.y<-.06 and .53<c.z<.68
extra=set()
for p in m.polygons:
 if p.material_index!=0 or not hand_box(p.center):continue
 uv=sum((m.uv_layers[0].data[i].uv for i in p.loop_indices),Vector((0,0)))/len(p.loop_indices)
 if skin_colour(uv):extra.add(p.index)
source=root/'Production/LOD/MacAnh_Portrait_Working.blend'
with bpy.data.libraries.load(str(source),link=False) as (available,loaded):loaded.objects=[r['source_body_object']]
hand=loaded.objects[0];bpy.context.scene.collection.objects.link(hand);hand.parent=None;hand.matrix_world.identity();hand.modifiers.clear()
bm=bmesh.new();bm.from_mesh(hand.data);remove=[];uv_layer=bm.loops.layers.uv[0]
for f in bm.faces:
 c=f.calc_center_median()
 if not(-.49<c.x<-.30 and -.26<c.y<-.04 and .51<c.z<.70):remove.append(f);continue
 nearest=tree.find_nearest(c)
 uv=sum((loop[uv_layer].uv for loop in f.loops),Vector((0,0)))/len(f.loops)
 accepted=(nearest[3]<.02 and regions[nearest[2]]==3 and m.polygons[nearest[2]].material_index==0) or (hand_box(c) and skin_colour(uv))
 if not accepted:remove.append(f)
bmesh.ops.delete(bm,geom=remove,context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
reg=bm.faces.layers.int.get('GarmentRegion') or bm.faces.layers.int.new('GarmentRegion')
for f in bm.faces:f[reg]=3;f.material_index=0;f.smooth=True
bm.to_mesh(hand.data);bm.free();hand.data.update();hand.vertex_groups.clear()
hand.vertex_groups.new(name='RightHand').add(list(range(len(hand.data.vertices))),1,'REPLACE')
mat=bpy.data.materials.new('MacAnh_DetailedHand');mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['Image_0']
bs=mat.node_tree.nodes['Principled BSDF'];bs.inputs['Roughness'].default_value=.65
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
hand.data.materials.clear();hand.data.materials.append(mat)
bm=bmesh.new();bm.from_mesh(m);reg=bm.faces.layers.int['GarmentRegion']
bm.faces.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f[reg]==3 and f.material_index==0) or f.index in extra],context='FACES')
bm.to_mesh(m);bm.free();m.update()
print('RESTORED_HAND_DETAIL',len(hand.data.vertices),len(hand.data.polygons),flush=True)
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);hand.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.join()
metadata=json.loads(o.get('lod_inner_materials','[]'));metadata.append(dict(name=mat.name,colour=[1,1,1,1],roughness=.65,base_texture='Battle_BaseColor.jpg'));o['lod_inner_materials']=json.dumps(metadata)
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
