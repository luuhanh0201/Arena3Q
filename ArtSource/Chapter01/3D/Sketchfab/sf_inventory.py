import bpy, sys, os, glob, json
from mathutils import Vector
root = sys.argv[sys.argv.index("--")+1]; out = sys.argv[sys.argv.index("--")+2]
res = {}
for d in sorted(glob.glob(os.path.join(root, "SF[0-9]*"))):
    code = os.path.basename(d)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(d, "scene.gltf"))
    bpy.context.view_layer.update()
    items = []
    for ob in bpy.context.scene.objects:
        if ob.type != "MESH": continue
        cs = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
        lo = [min(c[i] for c in cs) for i in range(3)]; hi = [max(c[i] for c in cs) for i in range(3)]
        tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
        items.append({"name": ob.name, "parent": ob.parent.name if ob.parent else None, "tris": tris,
                      "mats": [m.name for m in ob.data.materials if m],
                      "lo": [round(v, 3) for v in lo], "hi": [round(v, 3) for v in hi]})
    res[code] = items
json.dump(res, open(out, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print("INVENTORY_DONE", len(res))
