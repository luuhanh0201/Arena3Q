"""Battle-budget copy of a Meshy map prop (docs/KeHoach/ChuyenSang3D.md P2).

blender -b --factory-startup -P decimate_prop.py -- <source.fbx> <out.fbx> <target triangles>

Imports the source with Blender's default FBX axes (so the result keeps the source's orientation in Unity), joins the
meshes, decimates to the triangle budget (collapse, keeps UVs), triangulates, and writes a mesh-only FBX (one UV set,
normals, no materials, no textures). Sizing, yaw and the ground pivot are applied in Unity, like PropSpriteRenderer.
"""
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:]
source, target, budget = args[0], args[1], int(args[2])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
for o in bpy.context.scene.objects:
    o.select_set(o in meshes)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
mesh_object = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


before = triangles(mesh_object)
if before > budget:
    modifier = mesh_object.modifiers.new("Budget", "DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = budget / before
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
triangulate = mesh_object.modifiers.new("Triangles", "TRIANGULATE")
bpy.ops.object.modifier_apply(modifier=triangulate.name)
# Keep the first UV set only; drop vertex colours and materials (Unity assigns the prop material).
while len(mesh_object.data.uv_layers) > 1:
    mesh_object.data.uv_layers.remove(mesh_object.data.uv_layers[-1])
for attribute in [a for a in mesh_object.data.color_attributes]:
    mesh_object.data.color_attributes.remove(attribute)
mesh_object.data.materials.clear()
bpy.ops.object.shade_smooth()
after = triangles(mesh_object)

for o in list(bpy.context.scene.objects):
    if o is not mesh_object:
        bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=target, use_selection=True, object_types={"MESH"}, mesh_smooth_type="OFF",
                         use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode="STRIP",
                         embed_textures=False)
print(f"[Arena3Q] {source} : {before} -> {after} triangles -> {target}")
