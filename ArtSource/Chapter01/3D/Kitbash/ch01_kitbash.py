# Chapter 1 (Loạn Khăn Vàng) kitbash models that no free pack provides in a matching style:
# village gate, large camp gate, soldier tent, war drum, cliff pieces, altar plateau and stone stairs.
# Own geometry only (no third-party mesh), metres, Blender Z-up; exported as FBX for Unity (Y-up, baked).
# UVs are world-scale box projections (1 UV = TILE[material] metres) so every model shares the texel density
# of the textures the Unity builder assigns by material name (Chapter01Kitbash.cs).
#
# Run: blender -b --factory-startup --python ch01_kitbash.py -- <output folder>
import bpy, bmesh, math, random, sys, os
from mathutils import Matrix, Vector, noise

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.dirname(__file__)
TILE = {"M_Wood": 1.6, "M_WoodDark": 1.6, "M_Lacquer": 1.6, "M_Roof": 1.4, "M_RoofDark": 1.4, "M_Ridge": 1.0,
        "M_Cloth": 2.0, "M_Dark": 2.0, "M_Rope": 1.0, "M_Iron": 1.0, "M_Brass": 1.0, "M_Skin": 1.2,
        "M_Stone": 2.0, "M_StoneBlock": 2.0, "M_Rock": 4.0, "M_GroundTop": 4.0}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name):
    return bpy.data.materials.get(name) or bpy.data.materials.new(name)


def mesh_object(name, bm, mat):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    me.materials.append(material(mat))
    return ob


def box(name, size, centre, mat, rot=(0, 0, 0), bevel=0.02):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    ob = mesh_object(name, bm, mat)
    ob.rotation_euler = [math.radians(a) for a in rot]; ob.location = centre
    if bevel > 0:
        m = ob.modifiers.new("bevel", "BEVEL"); m.width = bevel; m.segments = 1; m.limit_method = "ANGLE"
    return ob


def beam(name, a, b, thickness, mat):
    """Square beam from point a to point b."""
    a, b = Vector(a), Vector(b); d = b - a
    ob = box(name, (thickness, thickness, d.length), (a + b) / 2, mat, bevel=0.015)
    ob.rotation_mode = "QUATERNION"; ob.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
    return ob


def cylinder(name, radius, depth, centre, mat, axis="Z", segments=12):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=radius, radius2=radius, depth=depth)
    ob = mesh_object(name, bm, mat); ob.location = centre
    if axis == "X": ob.rotation_euler = (0, math.radians(90), 0)
    if axis == "Y": ob.rotation_euler = (math.radians(90), 0, 0)
    return ob


def roof(name, width, depth, ridge_z, rise, lift, mat, centre=(0, 0), yaw=0.0, thickness=0.1, flare=0.06, nu=18, nv=7):
    """Gable roof with a concave profile (steep at the ridge, flat at the eave) and upturned corners."""
    bm = bmesh.new()
    for side in (-1, 1):
        rows = []
        for j in range(nv + 1):
            v = j / nv; row = []
            for i in range(nu + 1):
                u = -1 + 2 * i / nu
                x = u * width / 2 * (1 + flare * v)
                y = side * v * depth
                z = ridge_z - rise * (1 - (1 - v) ** 1.7) + lift * (v ** 2) * (abs(u) ** 4)
                row.append(bm.verts.new((x, y, z)))
            rows.append(row)
        for j in range(nv):
            for i in range(nu):
                q = [rows[j][i], rows[j][i + 1], rows[j + 1][i + 1], rows[j + 1][i]]
                bm.faces.new(q if side > 0 else list(reversed(q)))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    ob = mesh_object(name, bm, mat)
    m = ob.modifiers.new("solid", "SOLIDIFY"); m.thickness = thickness; m.offset = -1
    ob.location = (centre[0], centre[1], 0); ob.rotation_euler = (0, 0, math.radians(yaw))
    parts = [ob]
    # Ridge with raised ends, the light stone ridge of the CS Studio houses.
    rx, ry = centre
    ridge = box(name + "_ridge", (width * 0.92, 0.22, 0.22), (0, 0, ridge_z + 0.08), "M_Ridge")
    ends = [box(name + "_ridgeEnd%d" % k, (0.22, 0.26, 0.42), (s * width * 0.46, 0, ridge_z + 0.22), "M_Ridge", rot=(0, s * 20, 0))
            for k, s in enumerate((-1, 1))]
    for p in [ridge] + ends:
        p.location = Vector(p.location); p.location.rotate(ob.rotation_euler); p.location += Vector((rx, ry, 0))
        p.rotation_euler = (p.rotation_euler[0], p.rotation_euler[1], p.rotation_euler[2] + math.radians(yaw))
        parts.append(p)
    return parts


def gable(name, width, height, base_z, x, mat, thickness=0.1):
    """Triangular gable wall under a roof end, in the YZ plane at x."""
    bm = bmesh.new()
    pts = [(-width / 2, base_z), (width / 2, base_z), (0, base_z + height)]
    for sx in (-thickness / 2, thickness / 2):
        for y, z in pts: bm.verts.new((sx, y, z))
    bm.verts.ensure_lookup_table(); v = bm.verts
    bm.faces.new([v[0], v[1], v[2]]); bm.faces.new([v[5], v[4], v[3]])
    for a, b in ((0, 1), (1, 2), (2, 0)): bm.faces.new([v[a], v[a + 3], v[b + 3], v[b]])
    ob = mesh_object(name, bm, mat); ob.location = (x, 0, 0)
    return ob


def displace_rock(ob, amplitude, frequency, seed, keep_bottom=True, keep_top_z=None, cuts=7):
    """Lumpy faceted rock: push vertices along their normals with noise; the ground line and a flat top stay put."""
    me = ob.data
    # Work in placed coordinates: the ground line is z = 0 and the top is keep_top_z only after the object offset.
    me.transform(ob.matrix_basis); ob.matrix_basis = Matrix.Identity(4)  # matrix_world is stale until a depsgraph update
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    bm.normal_update()
    offset = Vector((seed * 13.1, seed * 7.7, seed * 3.3))
    for vert in bm.verts:
        p = vert.co
        if keep_bottom and p.z < 0.02:
            vert.co += Vector((vert.normal.x, vert.normal.y, 0)) * amplitude * 0.3; continue
        n = noise.noise(p * frequency + offset) + 0.5 * noise.noise(p * frequency * 2.3 + offset)
        strata = 0.35 * math.sin(p.z * 3.1 + seed)
        d = vert.normal.copy()
        if keep_top_z is not None and abs(p.z - keep_top_z) < 0.02:
            d.z = 0
        vert.co += d * amplitude * (n + strata * 0.4)
    if keep_top_z is not None:  # walkable top stays flat
        for vert in bm.verts: vert.co.z = min(vert.co.z, keep_top_z)
    bm.to_mesh(me); bm.free()
    for poly in me.polygons: poly.use_smooth = False


def finish(name, parts, top_material=None, top_z=None):
    """Applies modifiers, joins parts, assigns box UVs per material and exports the FBX."""
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True); bpy.context.view_layer.objects.active = p
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        for m in list(p.modifiers): bpy.ops.object.modifier_apply(modifier=m.name)
        p.select_set(False)
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = name; ob.data.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    me = ob.data
    if top_material:
        # Flat walkable top of a plateau gets the ground material.
        if top_material not in [m.name for m in me.materials]: me.materials.append(material(top_material))
        top = [m.name for m in me.materials].index(top_material); top_z = top_z if top_z is not None else max(v.co.z for v in me.vertices)
        for poly in me.polygons:
            if poly.normal.z > 0.7 and poly.center.z > top_z - 0.5: poly.material_index = top
    uv = me.uv_layers.new(name="UV0")
    for poly in me.polygons:
        tile = TILE.get(me.materials[poly.material_index].name, 2.0); n = poly.normal
        ax = max(range(3), key=lambda k: abs(n[k]))
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            if ax == 2: u, v = co.x, co.y
            elif ax == 0: u, v = co.y, co.z
            else: u, v = co.x, co.z
            uv.data[li].uv = (u / tile, v / tile)
    # Pivot: ground centre of the footprint.
    xs = [v.co.x for v in me.vertices]; ys = [v.co.y for v in me.vertices]; zmin = min(v.co.z for v in me.vertices)
    shift = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, zmin))
    for v in me.vertices: v.co -= shift
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             apply_scale_options="FBX_SCALE_UNITS", mesh_smooth_type="FACE", add_leaf_bones=False)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    print("[Kitbash] %s %d tris, %.1fx%.1fx%.1f m" % (name, tris, max(xs) - min(xs), max(ys) - min(ys), max(v.co.z for v in me.vertices)))


# ---------------------------------------------------------------------------------------------- models

def village_gate():
    """Cổng làng gỗ một gian (Ải 1): two posts on stone plinths, lintels, plaque, small tiled roof."""
    reset(); p = []
    for s in (-1, 1):
        x = s * 1.9
        p.append(box("plinth", (0.7, 0.7, 0.5), (x, 0, 0.25), "M_Stone"))
        p.append(box("post", (0.32, 0.32, 4.1), (x, 0, 2.05), "M_WoodDark"))
        for f in (-1, 1):  # raking braces that hold each post, front and back
            p.append(beam("brace", (x, f * 0.95, 0.0), (x, f * 0.14, 1.5), 0.14, "M_WoodDark"))
    p.append(box("lintel", (4.9, 0.36, 0.42), (0, 0, 3.72), "M_WoodDark"))
    p.append(box("beam", (4.2, 0.24, 0.26), (0, 0, 2.95), "M_WoodDark"))
    p.append(box("plaque", (1.2, 0.12, 0.5), (0, 0, 3.32), "M_Lacquer"))
    for i in range(5):
        p.append(box("bracket", (0.3, 0.5, 0.22), (-1.8 + i * 0.9, 0, 4.04), "M_Wood"))
    p += roof("roof", 5.7, 1.15, 4.95, 0.72, 0.35, "M_Roof")
    finish("CH01_VillageGate", p)


def camp_gate():
    """Cổng đại doanh (Ải 6): two plank towers with tiled roofs and a double plank gate under its own roof."""
    reset(); p = []
    for s in (-1, 1):
        cx = s * 5.0
        for dx in (-1.5, 1.5):
            for dy in (-1.5, 1.5):
                p.append(box("post", (0.36, 0.36, 7.4), (cx + dx, dy, 3.7), "M_WoodDark"))
        for dy in (-1.5, 1.5):  # cross braces on the long faces, two levels
            for z0, z1 in ((0.3, 2.6), (2.6, 4.9)):
                p.append(beam("brace", (cx - 1.5, dy, z0), (cx + 1.5, dy, z1), 0.14, "M_WoodDark"))
                p.append(beam("brace", (cx + 1.5, dy, z0), (cx - 1.5, dy, z1), 0.14, "M_WoodDark"))
        p.append(box("floor", (3.5, 3.5, 0.22), (cx, 0, 5.2), "M_Wood"))
        for side in range(4):  # plank parapet
            a = side * math.pi / 2
            p.append(box("parapet", (3.5, 0.12, 1.0), (cx + math.sin(a) * 1.72, math.cos(a) * 1.72, 5.8), "M_Wood", rot=(0, 0, math.degrees(a))))
        p += roof("towerRoof", 4.1, 2.0, 8.55, 1.15, 0.4, "M_RoofDark", centre=(cx, 0))
        for gx in (-1.95, 1.95):
            p.append(gable("towerGable", 3.4, 1.0, 7.4, cx + gx, "M_Wood"))
    for s in (-1, 1):
        p.append(box("gatePost", (0.42, 0.42, 6.2), (s * 3.45, 0, 3.1), "M_WoodDark"))
        p.append(box("door", (3.2, 0.16, 4.7), (s * 1.66, 0, 2.38), "M_Wood"))
        for z in (0.8, 2.4, 4.0):
            p.append(box("band", (3.0, 0.2, 0.14), (s * 1.66, 0, z), "M_Iron", bevel=0.0))
    p.append(box("gateLintel", (7.6, 0.42, 0.5), (0, 0, 5.55), "M_WoodDark"))
    p.append(box("gateBeam", (7.0, 0.3, 0.3), (0, 0, 4.92), "M_WoodDark"))
    p.append(box("gatePlaque", (1.6, 0.14, 0.62), (0, 0, 6.05), "M_Lacquer"))
    p += roof("gateRoof", 8.2, 1.45, 7.05, 0.95, 0.45, "M_RoofDark")
    finish("CH01_CampGate", p)


def soldier_tent():
    """Lều lính Khăn Vàng: A-frame cloth tent with ridge pole, end poles, a dark doorway and guy ropes."""
    reset(); p = []
    length, half, height = 3.2, 1.3, 2.1
    bm = bmesh.new(); nu, nv = 8, 5; rows = {}
    for side in (-1, 1):
        for j in range(nv + 1):
            v = j / nv
            for i in range(nu + 1):
                u = -0.5 + i / nu
                sag = 0.09 * math.sin(math.pi * v) * (1 - (2 * u) ** 2)
                rows[(side, j, i)] = bm.verts.new((u * length, side * v * half, height - v * (height - 0.12) - sag))
        for j in range(nv):
            for i in range(nu):
                q = [rows[(side, j, i)], rows[(side, j, i + 1)], rows[(side, j + 1, i + 1)], rows[(side, j + 1, i)]]
                bm.faces.new(q if side > 0 else list(reversed(q)))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    cloth = mesh_object("cloth", bm, "M_Cloth"); s = cloth.modifiers.new("solid", "SOLIDIFY"); s.thickness = 0.03; p.append(cloth)
    for x in (-length / 2, length / 2):
        p.append(gable("end", half * 2, height - 0.12, 0.12, x, "M_Cloth", thickness=0.03))
    p.append(gable("door", 1.0, 1.35, 0.0, -length / 2 - 0.03, "M_Dark", thickness=0.02))
    p.append(cylinder("ridgePole", 0.05, length + 0.5, (0, 0, height + 0.02), "M_WoodDark", axis="X", segments=8))
    for x in (-length / 2 - 0.18, length / 2 + 0.18):
        p.append(cylinder("pole", 0.05, height + 0.15, (x, 0, (height + 0.15) / 2), "M_WoodDark", segments=8))
        d = 1 if x > 0 else -1
        p.append(beam("rope", (x, 0, height + 0.05), (x + d * 1.0, 0, 0.02), 0.025, "M_Rope"))
        p.append(box("stake", (0.07, 0.07, 0.3), (x + d * 1.0, 0, 0.1), "M_WoodDark", bevel=0.0))
    finish("CH01_SoldierTent", p)


def war_drum():
    """Trống trận: lacquered barrel drum with brass stud rings and hide skins on a crossed wooden stand."""
    reset(); p = []
    radius, length, cz = 0.55, 0.82, 1.35
    bm = bmesh.new(); seg = 18; rings = []
    for k in range(7):
        t = -1 + 2 * k / 6; x = t * length / 2; r = radius * (1 + 0.1 * (1 - t * t))
        rings.append([bm.verts.new((x, r * math.cos(a), cz + r * math.sin(a))) for a in [2 * math.pi * i / seg for i in range(seg)]])
    for k in range(6):
        for i in range(seg):
            j = (i + 1) % seg; bm.faces.new([rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]])
    p.append(mesh_object("body", bm, "M_Lacquer"))
    for s in (-1, 1):
        p.append(cylinder("skin", radius * 0.97, 0.04, (s * length / 2, 0, cz), "M_Skin", axis="X", segments=seg))
        p.append(cylinder("studs", radius * 1.02, 0.05, (s * length * 0.42, 0, cz), "M_Brass", axis="X", segments=seg))
        x = s * 0.62
        for f in (-1, 1):
            p.append(beam("leg", (x, f * 0.75, 0.0), (x, -f * 0.32, cz + 0.25), 0.11, "M_WoodDark"))
        p.append(box("cradle", (0.12, 0.9, 0.12), (x, 0, cz - radius * 0.55), "M_WoodDark"))
    for f in (-1, 1):
        p.append(box("rail", (1.5, 0.1, 0.1), (0, f * 0.7, 0.06), "M_WoodDark"))
    finish("CH01_WarDrum", p)


def cliff_wall(name, seed, size=(6.0, 1.8, 3.2)):
    reset()
    ob = box("rock", size, (0, 0, size[2] / 2), "M_Rock", bevel=0)
    displace_rock(ob, 0.32, 0.55, seed)
    finish(name, [ob])


def cliff_outcrop(name, seed):
    reset()
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=9, radius1=2.6, radius2=1.8, depth=3.4)
    ob = mesh_object("rock", bm, "M_Rock"); ob.location = (0, 0, 1.7)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.transform_apply(location=True); ob.select_set(False)
    displace_rock(ob, 0.38, 0.5, seed)
    finish(name, [ob])


def altar_plateau():
    """Nền cao của tế đàn (Ải 7): rocky sides, flat walkable top."""
    reset(); h = 2.6
    ob = box("rock", (14.0, 14.0, h), (0, 0, h / 2), "M_Rock", bevel=0)
    displace_rock(ob, 0.45, 0.35, 5, keep_top_z=h, cuts=11)
    finish("CH01_AltarPlateau", [ob], top_material="M_GroundTop", top_z=h)


def stone_stairs():
    """Bậc đá lên tế đàn: 11 steps up 2.6 m with sloped cheek walls."""
    reset(); p = []; steps, rise, run, width = 11, 2.6 / 11, 0.42, 3.6
    for i in range(steps):
        p.append(box("step", (width, run + 0.04, rise * (i + 1)), (0, i * run, rise * (i + 1) / 2), "M_StoneBlock", bevel=0.015))
    for s in (-1, 1):
        bm = bmesh.new(); L = steps * run; t = 0.45; x0 = s * (width / 2 + t / 2)
        pts = [(-run / 2, 0), (L - run / 2, 0), (L - run / 2, 2.6 + 0.4), (-run / 2, 0.55)]
        vs = [[bm.verts.new((x0 + dx, y, z)) for y, z in pts] for dx in (-t / 2, t / 2)]
        bm.faces.new(vs[0][::-1]); bm.faces.new(vs[1])
        for k in range(4):
            j = (k + 1) % 4; bm.faces.new([vs[0][k], vs[0][j], vs[1][j], vs[1][k]])
        p.append(mesh_object("cheek", bm, "M_Stone"))
    finish("CH01_StoneStairs", p)


def wood_bridge():
    """Cầu gỗ (Ải 1 to 4 river crossings): arched plank deck along X, stringers, posts and rails."""
    reset(); p = []; length, width, n = 8.0, 3.0, 16
    deck = lambda x: 0.12 + 0.55 * math.cos(math.pi * x / length)  # 0.12 m at the ends, 0.67 m mid-span
    for i in range(n):
        x = -length / 2 + (i + 0.5) * length / n
        p.append(box("plank", (length / n - 0.04, width, 0.1), (x, 0, deck(x)), "M_Wood", rot=(0, -math.degrees(math.atan(-0.55 * math.pi / length * math.sin(math.pi * x / length))), 0), bevel=0.01))
    for s in (-1, 1):
        y = s * (width / 2 - 0.15)
        for k in range(4):
            x0 = -length / 2 + k * length / 4; x1 = x0 + length / 4
            p.append(beam("stringer", (x0, y, deck(x0) - 0.14), (x1, y, deck(x1) - 0.14), 0.18, "M_WoodDark"))
        y = s * (width / 2 + 0.05)
        posts = [-length / 2 + 0.2, -length / 6, length / 6, length / 2 - 0.2]
        for x in posts:
            p.append(box("post", (0.16, 0.16, 1.05), (x, y, deck(x) + 0.47), "M_WoodDark", bevel=0.01))
        for a, b in zip(posts, posts[1:]):
            p.append(beam("rail", (a, y, deck(a) + 0.95), (b, y, deck(b) + 0.95), 0.1, "M_WoodDark"))
    finish("CH01_WoodBridge", p)


def barracks():
    """Doanh trại lính (Ải 6): long plank hall on a stone base, dark tiled gable roof, slit windows."""
    reset(); p = []; L, W, H = 10.0, 5.0, 2.8
    p.append(box("base", (L + 0.4, W + 0.4, 0.35), (0, 0, 0.175), "M_Stone"))
    for s in (-1, 1):
        p.append(box("wallLong", (L, 0.15, H), (0, s * W / 2, 0.35 + H / 2), "M_Wood") if s > 0 else
                 box("wallFrontL", (L / 2 - 0.8, 0.15, H), (-(L / 4 + 0.4), -W / 2, 0.35 + H / 2), "M_Wood"))
        p.append(box("wallEnd", (0.15, W, H), (s * L / 2, 0, 0.35 + H / 2), "M_Wood"))
        for x in (-L / 2, L / 2):
            p.append(box("corner", (0.3, 0.3, H + 0.1), (x, s * W / 2, 0.35 + H / 2), "M_WoodDark"))
        for k in range(4):
            x = -L / 2 + 1.2 + k * (L - 2.4) / 3
            if s < 0 and abs(x) < 1.2: continue
            p.append(box("slit", (0.7, 0.05, 0.35), (x, s * (W / 2 + 0.08), 0.35 + H * 0.72), "M_Dark", bevel=0))
    p.append(box("wallFrontR", (L / 2 - 0.8, 0.15, H), (L / 4 + 0.4, -W / 2, 0.35 + H / 2), "M_Wood"))
    p.append(box("lintel", (1.8, 0.2, 0.5), (0, -W / 2, 0.35 + H - 0.25), "M_WoodDark"))
    p.append(box("doorway", (1.5, 0.05, H - 0.5), (0, -W / 2 + 0.1, 0.35 + (H - 0.5) / 2), "M_Dark", bevel=0))
    p += roof("roof", L + 1.2, W / 2 + 0.7, 0.35 + H + 1.7, 1.75, 0.3, "M_RoofDark")
    for x in (-L / 2, L / 2):
        p.append(gable("gable", W, 1.6, 0.35 + H, x, "M_Wood"))
    finish("CH01_Barracks", p)


def campfire():
    """Đống lửa trại: ring of stones and crossed logs (the fire itself is VFX)."""
    reset(); p = []
    for k in range(9):
        a = 2 * math.pi * k / 9
        p.append(box("stone", (0.32, 0.26, 0.22), (math.cos(a) * 0.62, math.sin(a) * 0.62, 0.1), "M_Rock", rot=(0, 0, math.degrees(a) + 17 * k), bevel=0.04))
    for k in range(4):
        a = math.pi * k / 4
        p.append(beam("log", (math.cos(a) * 0.5, math.sin(a) * 0.5, 0.06), (-math.cos(a) * 0.12, -math.sin(a) * 0.12, 0.42), 0.12, "M_WoodDark"))
    finish("CH01_Campfire", p)


os.makedirs(OUT, exist_ok=True)
random.seed(1)
village_gate(); camp_gate(); soldier_tent(); war_drum()
cliff_wall("CH01_CliffWall_A", 1); cliff_wall("CH01_CliffWall_B", 2, (4.5, 1.6, 2.4)); cliff_outcrop("CH01_CliffOutcrop", 3)
altar_plateau(); stone_stairs(); wood_bridge(); barracks(); campfire()
print("[Kitbash] done")
