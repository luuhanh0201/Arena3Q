import bpy, sys, os, glob, math
import numpy as np
from mathutils import Vector, Matrix
a = sys.argv[sys.argv.index("--")+1:]; root, outdir = a[0], a[1]; only = a[2].split(",") if len(a) > 2 else None
os.makedirs(outdir, exist_ok=True)
VIEWS = [(45, 50), (0, 12), (90, 12), (225, 30)]
S = 420
def render_views(path, objs):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT"; sc.eevee.taa_render_samples = 16; sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True; sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.57, 0.6, 1); sc.world.node_tree.nodes["Background"].inputs[1].default_value = 1.0; sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sun.data.energy = 3.0; sun.rotation_euler = (0.7, 0.2, 0.8); sc.collection.objects.link(sun)
    sc.render.resolution_x = sc.render.resolution_y = S; sc.render.film_transparent = False
    sc.world = sc.world or bpy.data.worlds.new("W")
    cs = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector([min(c[i] for c in cs) for i in range(3)]); hi = Vector([max(c[i] for c in cs) for i in range(3)])
    ctr = (lo + hi) / 2; rad = max((hi - lo).length / 2, 1e-4)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = "ORTHO"; cam.data.ortho_scale = rad * 2.1; cam.data.clip_start = rad * 0.01; cam.data.clip_end = rad * 10
    sheet = np.zeros((S, S * len(VIEWS), 4), dtype=np.float32)
    for k, (yaw, el) in enumerate(VIEWS):
        y, e = math.radians(yaw), math.radians(el)
        d = Vector((math.sin(y) * math.cos(e), -math.cos(y) * math.cos(e), math.sin(e)))
        cam.location = ctr + d * rad * 4
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        f = os.path.join(outdir, "_tmp.png"); sc.render.filepath = f; bpy.ops.render.render(write_still=True)
        im = bpy.data.images.load(f); px = np.array(im.pixels[:], dtype=np.float32).reshape(S, S, 4)
        sheet[:, k * S:(k + 1) * S] = px; bpy.data.images.remove(im)
    out = bpy.data.images.new("sheet", S * len(VIEWS), S, alpha=True); out.pixels = sheet.ravel()
    out.filepath_raw = path; out.file_format = "PNG"; out.save(); bpy.data.images.remove(out)
    bpy.data.objects.remove(cam)
for d in sorted(glob.glob(os.path.join(root, "SF[0-9]*"))):
    code = os.path.basename(d)
    if only and code not in only: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(d, "scene.gltf")); bpy.context.view_layer.update()
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    render_views(os.path.join(outdir, code + ".png"), objs)
    print("PREVIEW", code)
print("PREVIEW_DONE")
