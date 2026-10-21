"""Renders Mặc Ảnh's battle VFX as flipbooks for MacAnhVfxPlayer (spec §07–§14, storyboard
images/a_high_detail_game_design_concept_sheet_in_a_dark.png, palette §14: core #F3ECFF/#D6B9FF, band #9657EF,
depth #552C91, trim #E6C47C).

blender -b --factory-startup --python vfx_macanh_flipbooks.py -- [--only WindSlash,Impact] [--preview <dir>]

Output: Assets/Arena3Q/Art/Heroes/MacAnh/VFX/Flipbook/<Effect>/<Effect>_pNN.png + <Effect>.recipe.json, imported by
Arena3Q > Mac Anh > Graphics > Import Flipbook VFX (MacAnhFlipbookBuilder).
Each frame is two Eevee renders combined: RGB = emission on black with a bloom glare (added on top in the game),
A = coverage of the shapes (a dark underlay in the game, so the light still reads on the bright sand).
Views: 'front' = effects drawn in the screen plane (projectiles, impacts; the game rotates them to the flight
direction), 'game' = the hero sprite camera (3/4, ground projection 0.75) for things with height, 'top' = straight
down for ground rings (the game spins and squashes them).
Every effect is a function below that builds its scene for a frame index; numbers live in the EFFECTS table.
"""
import bpy
import bmesh
import json
import math
import os
import random
import sys
import numpy as np
from mathutils import Vector, Matrix

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
ONLY = argv[argv.index('--only') + 1].split(',') if '--only' in argv else None
PREVIEW = argv[argv.index('--preview') + 1] if '--preview' in argv else ''
ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(ROOT, '../../../../..'))
OUT = os.path.abspath(argv[argv.index('--out') + 1]) if '--out' in argv else os.path.join(PROJECT, 'Assets/Arena3Q/Art/Heroes/MacAnh/VFX/Flipbook')
PAGE = 2048
PROJECTION = .75            # same ground projection as export_macanh_sprites.py
# How much the glow halo (not the shapes) darkens what is behind it: lower = the halo lights the sand up.
HALO_COVER = float(argv[argv.index('--halo') + 1]) if '--halo' in argv else .9

CORE = '#F3ECFF'; CORE2 = '#D6B9FF'; BAND = '#9657EF'; DEEP = '#552C91'; GOLD = '#E6C47C'


def lin(hex_colour, a=1.0):
    c = [int(hex_colour[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (a,)


# ---------------------------------------------------------------- scene, camera, compositor, render
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT'
scene.eevee.taa_render_samples = 16
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)
world = bpy.data.worlds.new('Black')
world.color = (0, 0, 0)
scene.world = world
cam = bpy.data.objects.new('VfxCamera', bpy.data.cameras.new('VfxCamera'))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.clip_start = .01
cam.data.clip_end = 100

scene.use_nodes = True
tree = scene.node_tree
for n in list(tree.nodes):
    tree.nodes.remove(n)
rl = tree.nodes.new('CompositorNodeRLayers')
glare = tree.nodes.new('CompositorNodeGlare')
comp = tree.nodes.new('CompositorNodeComposite')
tree.links.new(rl.outputs['Image'], glare.inputs['Image'])
tree.links.new(glare.outputs['Image'], comp.inputs['Image'])


def set_glare(size=7, threshold=.35, strength=1.0):
    for attr, value in (('glare_type', 'FOG_GLOW'), ('quality', 'HIGH'), ('size', size), ('threshold', threshold), ('mix', 0.0)):
        try:
            setattr(glare, attr, value)
        except Exception:
            pass
    for name, value in (('Threshold', threshold), ('Strength', strength), ('Size', size / 9.0)):
        if name in glare.inputs:
            try:
                glare.inputs[name].default_value = value
            except Exception:
                pass


def set_view(kind, width_m, height_m, ppu, centre=(0, 0, 0)):
    """kind: front (looks along +Y at the XZ plane), game (hero sprite camera), top (looks down -Z)."""
    centre = Vector(centre)
    if kind == 'front':
        cam.location = centre + Vector((0, -10, 0))
        cam.rotation_euler = (math.pi / 2, 0, 0)
    elif kind == 'top':
        cam.location = centre + Vector((0, 0, 10))
        cam.rotation_euler = (0, 0, 0)
    else:
        d = Vector((0, -1, PROJECTION / math.sqrt(1 - PROJECTION ** 2))).normalized()
        cam.location = centre + d * 10
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    scene.render.resolution_x = int(round(width_m * ppu / 4)) * 4
    scene.render.resolution_y = int(round(height_m * ppu / 4)) * 4
    cam.data.ortho_scale = max(width_m, height_m)
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1


TMP = os.path.join(bpy.app.tempdir or '.', 'macanh_vfx.png')


def _render(path):
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path, check_existing=False)
    a = np.empty(img.size[0] * img.size[1] * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    shape = (img.size[1], img.size[0], 4)
    bpy.data.images.remove(img)
    return a.reshape(shape)


def render_frame(cover_scale=1.0):
    """RGB: emission on black + glare. A: coverage of the shapes (film transparent, no compositor)."""
    scene.render.film_transparent = False
    scene.render.use_compositing = True
    glow = _render(TMP)
    scene.render.film_transparent = True
    scene.render.use_compositing = False
    cover = _render(TMP)
    out = np.zeros_like(glow)
    out[:, :, :3] = np.clip(glow[:, :, :3], 0, 1)
    # The underlay also follows the glow halo, so the halo tints the sand violet instead of washing it out.
    lum = glow[:, :, :3].max(2)
    out[:, :, 3] = np.clip(np.maximum(cover[:, :, 3] * cover_scale, np.clip(lum * 1.6, 0, 1) * HALO_COVER), 0, 1)
    return out


# ---------------------------------------------------------------- materials
_mats = {}


def glow_material(name, stops, alpha_stops=None, strength=1.0, noise=None, axis='v', tips=True):
    """Emission along a UV axis: stops = [(pos, hex, strength)], alpha_stops = [(pos, alpha)].
    noise = (scale, amount, scroll) streaks along u, scrolled by the 'Phase' value node (0..1 loops seamlessly)."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.surface_render_method = 'BLENDED'
    m.use_backface_culling = False
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    uv = nt.nodes.new('ShaderNodeUVMap')
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    nt.links.new(uv.outputs['UV'], sep.inputs['Vector'])
    coord = sep.outputs['Y' if axis == 'v' else 'X']
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    el = ramp.color_ramp.elements
    while len(el) > 1:
        el.remove(el[-1])
    el[0].position = stops[0][0]
    el[0].color = lin(stops[0][1])
    for pos, colour, _ in stops[1:]:
        e = el.new(pos)
        e.color = lin(colour)
    nt.links.new(coord, ramp.inputs['Fac'])
    sramp = nt.nodes.new('ShaderNodeFloatCurve')
    curve = sramp.mapping.curves[0]
    curve.points[0].location = (stops[0][0], min(1, stops[0][2] / 8))
    curve.points[1].location = (stops[-1][0], min(1, stops[-1][2] / 8))
    for pos, _, s in stops[1:-1]:
        curve.points.new(pos, min(1, s / 8))
    sramp.mapping.update()
    nt.links.new(coord, sramp.inputs['Value'])
    strength_mul = nt.nodes.new('ShaderNodeMath')
    strength_mul.operation = 'MULTIPLY'
    strength_mul.inputs[1].default_value = 8 * strength
    nt.links.new(sramp.outputs['Value'], strength_mul.inputs[0])
    final_strength = strength_mul.outputs[0]
    alpha = None
    if alpha_stops:
        aramp = nt.nodes.new('ShaderNodeFloatCurve')
        ac = aramp.mapping.curves[0]
        ac.points[0].location = alpha_stops[0]
        ac.points[1].location = alpha_stops[-1]
        for p in alpha_stops[1:-1]:
            ac.points.new(*p)
        aramp.mapping.update()
        nt.links.new(coord, aramp.inputs['Value'])
        alpha = aramp.outputs['Value']
    if noise:
        scale, amount, _ = noise
        phase = nt.nodes.new('ShaderNodeValue')
        phase.name = 'Phase'
        phase.outputs[0].default_value = 0.0
        # Periodic streaks: a sine wave along u scrolled by the phase, distorted by fixed noise.
        noise_tex = nt.nodes.new('ShaderNodeTexNoise')
        noise_tex.inputs['Scale'].default_value = scale
        noise_tex.inputs['Detail'].default_value = 2.0
        nt.links.new(uv.outputs['UV'], noise_tex.inputs['Vector'])
        sx = sep.outputs['X']
        wave_in = nt.nodes.new('ShaderNodeMath')
        wave_in.operation = 'MULTIPLY_ADD'
        wave_in.inputs[1].default_value = scale * 0.6
        nt.links.new(sx, wave_in.inputs[0])
        nt.links.new(noise_tex.outputs['Fac'], wave_in.inputs[2])
        add_phase = nt.nodes.new('ShaderNodeMath')
        add_phase.operation = 'MULTIPLY_ADD'
        add_phase.inputs[1].default_value = -2 * math.pi * 3
        nt.links.new(phase.outputs[0], add_phase.inputs[0])
        nt.links.new(wave_in.outputs[0], add_phase.inputs[2])
        sine = nt.nodes.new('ShaderNodeMath')
        sine.operation = 'SINE'
        nt.links.new(add_phase.outputs[0], sine.inputs[0])
        streak = nt.nodes.new('ShaderNodeMapRange')
        streak.inputs['From Min'].default_value = -1
        streak.inputs['From Max'].default_value = 1
        streak.inputs['To Min'].default_value = 1 - amount
        streak.inputs['To Max'].default_value = 1
        nt.links.new(sine.outputs[0], streak.inputs['Value'])
        mul = nt.nodes.new('ShaderNodeMath')
        mul.operation = 'MULTIPLY'
        nt.links.new(final_strength, mul.inputs[0])
        nt.links.new(streak.outputs[0], mul.inputs[1])
        final_strength = mul.outputs[0]
        if alpha is not None:
            amul = nt.nodes.new('ShaderNodeMath')
            amul.operation = 'MULTIPLY'
            nt.links.new(alpha, amul.inputs[0])
            nt.links.new(streak.outputs[0], amul.inputs[1])
            alpha = amul.outputs[0]
    # Tip / edge fade along u (both ends) for ribbons; flat for discs and rings, where u is the angle.
    tip = nt.nodes.new('ShaderNodeFloatCurve')
    tc = tip.mapping.curves[0]
    tc.points[0].location = (0, 0 if tips else 1)
    tc.points[1].location = (1, 0 if tips else 1)
    if tips:
        tc.points.new(.18, 1)
        tc.points.new(.82, 1)
    tip.mapping.update()
    nt.links.new(sep.outputs['X'], tip.inputs['Value'])
    smul = nt.nodes.new('ShaderNodeMath')
    smul.operation = 'MULTIPLY'
    nt.links.new(final_strength, smul.inputs[0])
    nt.links.new(tip.outputs['Value'], smul.inputs[1])
    final_strength = smul.outputs[0]
    gain = nt.nodes.new('ShaderNodeValue')
    gain.name = 'Gain'
    gain.outputs[0].default_value = 1.0
    gmul = nt.nodes.new('ShaderNodeMath')
    gmul.operation = 'MULTIPLY'
    nt.links.new(final_strength, gmul.inputs[0])
    nt.links.new(gain.outputs[0], gmul.inputs[1])
    emis = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(ramp.outputs['Color'], emis.inputs['Color'])
    nt.links.new(gmul.outputs[0], emis.inputs['Strength'])
    mix = nt.nodes.new('ShaderNodeMixShader')
    transp = nt.nodes.new('ShaderNodeBsdfTransparent')
    afac = nt.nodes.new('ShaderNodeMath')
    afac.operation = 'MULTIPLY'
    if alpha is not None:
        nt.links.new(alpha, afac.inputs[0])
    else:
        afac.inputs[0].default_value = 1.0
    nt.links.new(tip.outputs['Value'], afac.inputs[1])
    again = nt.nodes.new('ShaderNodeMath')
    again.operation = 'MULTIPLY'
    nt.links.new(afac.outputs[0], again.inputs[0])
    nt.links.new(gain.outputs[0], again.inputs[1])
    clamp = nt.nodes.new('ShaderNodeClamp')
    nt.links.new(again.outputs[0], clamp.inputs['Value'])
    nt.links.new(clamp.outputs[0], mix.inputs['Fac'])
    nt.links.new(transp.outputs[0], mix.inputs[1])
    nt.links.new(emis.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs['Surface'])
    return m


def set_phase(material, phase):
    n = material.node_tree.nodes.get('Phase') if material.node_tree else None
    if n:
        n.outputs[0].default_value = phase


def set_gain(material, gain):
    material.node_tree.nodes['Gain'].outputs[0].default_value = gain


# ---------------------------------------------------------------- geometry helpers
_objects = []


def clear_effect():
    for o in _objects:
        if o.name in bpy.data.objects:
            bpy.data.objects.remove(o, do_unlink=True)
    _objects.clear()


def ribbon(name, points, widths, normal, material, uv_u=None):
    """Quad strip along points (list of Vector) offset by +-width/2 in the plane spanned with `normal`.
    u runs along the strip (0..1), v across it (0 = outer/front edge, 1 = inner/back edge)."""
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new('UVMap')
    n = len(points)
    rows = []
    for i, p in enumerate(points):
        t = (points[min(i + 1, n - 1)] - points[max(i - 1, 0)]).normalized()
        side = t.cross(normal).normalized()
        rows.append((bm.verts.new(p + side * widths[i] * .5), bm.verts.new(p - side * widths[i] * .5)))
    for i in range(n - 1):
        f = bm.faces.new((rows[i][0], rows[i + 1][0], rows[i + 1][1], rows[i][1]))
        u0 = (uv_u[i] if uv_u else i / (n - 1))
        u1 = (uv_u[i + 1] if uv_u else (i + 1) / (n - 1))
        for loop, (u, v) in zip(f.loops, ((u0, 0), (u1, 0), (u1, 1), (u0, 1))):
            loop[uv].uv = (u, v)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(material)
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    _objects.append(o)
    return o


def disc(name, radius, material, location=(0, 0, 0), normal_axis='Y', segments=24, inner=0.0):
    """A disc/annulus with v = radial (0 centre .. 1 rim) and u = angle; faces the given axis."""
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new('UVMap')
    rings = 8
    verts = []
    for r in range(rings + 1):
        rr = inner + (radius - inner) * r / rings
        row = []
        for s in range(segments + 1):
            a = 2 * math.pi * s / segments
            p = (math.cos(a) * rr, math.sin(a) * rr)
            co = (p[0], 0, p[1]) if normal_axis == 'Y' else (p[0], p[1], 0)
            row.append(bm.verts.new(co))
        verts.append(row)
    for r in range(rings):
        for s in range(segments):
            f = bm.faces.new((verts[r][s], verts[r][s + 1], verts[r + 1][s + 1], verts[r + 1][s]))
            for loop, (u, v) in zip(f.loops, ((s / segments, r / rings), ((s + 1) / segments, r / rings),
                                             ((s + 1) / segments, (r + 1) / rings), (s / segments, (r + 1) / rings))):
                loop[uv].uv = (u, v)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(material)
    o = bpy.data.objects.new(name, me)
    o.location = location
    scene.collection.objects.link(o)
    _objects.append(o)
    return o


# ---------------------------------------------------------------- packing / output
def crop(frame, anchor_px, pad=2):
    a = np.maximum(frame[:, :, 3], frame[:, :, :3].max(2))
    ys, xs = np.nonzero(a > 1.5 / 255)
    if len(xs) == 0:
        ys, xs = np.array([int(anchor_px[1])]), np.array([int(anchor_px[0])])
    x0, x1 = max(0, xs.min() - pad), min(frame.shape[1], xs.max() + 1 + pad)
    y0, y1 = max(0, ys.min() - pad), min(frame.shape[0], ys.max() + 1 + pad)
    c = frame[y0:y1, x0:x1]
    return c, ((anchor_px[0] - x0) / c.shape[1], (anchor_px[1] - y0) / c.shape[0])


class Packer:
    def __init__(self, name):
        self.name = name
        self.pages = []
        self._new()

    def _new(self):
        self.page = np.zeros((PAGE, PAGE, 4), dtype=np.float32)
        self.x = self.y = self.row = self.used = 0
        self.pages.append(self.page)

    def add(self, img):
        h, w = img.shape[:2]
        if self.x + w + 2 > PAGE:
            self.x = 0
            self.y += self.row + 2
            self.row = 0
        if self.y + h + 2 > PAGE:
            self.used_last = self.used
            self._new()
        self.page[self.y:self.y + h, self.x:self.x + w] = img
        rect = (len(self.pages) - 1, self.x, self.y, w, h)
        self.x += w + 2
        self.row = max(self.row, h)
        self.used = max(self.used, self.y + h + 2)
        return rect

    def save(self, folder):
        names = []
        for i, page in enumerate(self.pages):
            used = int(np.nonzero(page.max(axis=(1, 2)) > 0)[0].max() + 3) if page.max() > 0 else 4
            height = min(PAGE, max(4, int(math.ceil(used / 4)) * 4))
            image = bpy.data.images.new('page', width=PAGE, height=height, alpha=True)
            image.pixels.foreach_set(page[:height].ravel())
            name = f'{self.name}_p{i:02d}.png'
            image.filepath_raw = os.path.join(folder, name)
            image.file_format = 'PNG'
            image.save()
            bpy.data.images.remove(image)
            names.append(name)
        return names


def anchor_pixel(world_point):
    from bpy_extras.object_utils import world_to_camera_view
    p = world_to_camera_view(scene, cam, Vector(world_point))
    return (p.x * scene.render.resolution_x, p.y * scene.render.resolution_y)


SAND = (0.86, 0.66, 0.36)   # battlefield sand (sRGB) from Logs/MacAnhBattleQa captures


def preview_sheet(name, layers):
    """Frames composited exactly like the game shader (rgb*glow + bg*(1-a*underlay) + underlayColour*a*underlay)
    over the sand (top row) and a dark night ground (bottom row)."""
    os.makedirs(PREVIEW, exist_ok=True)
    tiles = []
    for l in layers:
        frames = l['_frames']
        step = max(1, len(frames) // 8)
        for img, _ in frames[::step][:8]:
            tiles.append((img, l))
    h = max(t[0].shape[0] for t in tiles)
    w = sum(t[0].shape[1] for t in tiles) + 6 * len(tiles)
    sheet = np.zeros((h * 2 + 6, w, 4), dtype=np.float32)
    sheet[:, :, 3] = 1
    x = 0
    for img, l in tiles:
        for row, bg in ((1, SAND), (0, (0.07, 0.05, 0.1))):
            under = np.array(l['underlayColour'][:3])
            a = img[:, :, 3:4] * l['underlay']
            comp = np.clip(img[:, :, :3] * l['glow'] + np.array(bg) * (1 - a) + under * a, 0, 1)
            y0 = row * (h + 6)
            sheet[y0:y0 + img.shape[0], x:x + img.shape[1], :3] = comp
            sheet[y0 + img.shape[0]:y0 + h, x:x + img.shape[1], :3] = bg
        x += img.shape[1] + 6
    image = bpy.data.images.new('preview', width=sheet.shape[1], height=sheet.shape[0], alpha=True)
    image.pixels.foreach_set(sheet.ravel())
    image.filepath_raw = os.path.join(PREVIEW, f'{name}_xemtruoc_cat-tren_toi-duoi.png')
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


def write_effect(name, layers, reference_radius=1.0):
    if PREVIEW:
        preview_sheet(name, layers)
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    packer = Packer(name)
    out_layers = []
    for layer in layers:
        frames = []
        for img, anchor in layer.pop('_frames'):
            c, pivot = crop(img, anchor)
            page, x, y, w, h = packer.add(c)
            frames.append({'page': page, 'x': x, 'y': y, 'width': w, 'height': h, 'pivot_x': pivot[0], 'pivot_y': pivot[1]})
        layer['frames'] = frames
        out_layers.append(layer)
        if PREVIEW:
            os.makedirs(PREVIEW, exist_ok=True)
    pages = packer.save(folder)
    recipe = {'name': name, 'referenceRadius': reference_radius, 'renderProjection': PROJECTION, 'pages': pages, 'layers': out_layers}
    recipe.update(SCREEN_DIM.get(name, {}))
    with open(os.path.join(folder, name + '.recipe.json'), 'w', encoding='utf-8') as f:
        json.dump(recipe, f, indent=1)
    print('EFFECT', name, 'layers', len(out_layers), 'frames', sum(len(l['frames']) for l in out_layers), 'pages', len(pages), flush=True)


def layer(name, frames, ppu, **kw):
    d = {'name': name, 'ppu': ppu, 'framesPerSecond': 24, 'loop': False, 'plane': 'Upright', 'phase': 'Release',
         'start': 0, 'end': 0, 'fadeIn': .06, 'fadeOut': .15, 'scaleFrom': 1, 'scaleTo': 1, 'spin': 0, 'height': 0,
         'glow': .85, 'underlay': .9, 'underlayColour': [.22, .07, .45, 1], 'tint': [1, 1, 1, 1], 'sortingOffset': 0,
         'highQualityOnly': False, 'stretchToGround': False, 'breath': .45, 'noiseScale': 2.5, 'dissolve': True}
    d.update(kw)
    d['_frames'] = frames
    return d


# ---------------------------------------------------------------- effects
def crescent_points(radius, sweep_deg, centre_x, n=48):
    pts, us = [], []
    for i in range(n):
        t = i / (n - 1)
        a = math.radians(-sweep_deg / 2 + sweep_deg * t)
        pts.append(Vector((centre_x + radius * math.cos(a), 0, radius * math.sin(a))))
        us.append(t)
    return pts, us


def blade_widths(n, width, power=1.3):
    return [width * max(0.04, math.sin(math.pi * i / (n - 1))) ** power for i in range(n)]


def build_wind_slash(scale=1.0, frames_count=12, seed=3):
    """Attack01 Phiến Phong Trảm (and the smaller CounterSlash): crescent wind blade flying towards +X."""
    rnd = random.Random(seed)
    # The blade itself is a solid crescent of light (no streak pattern); the wind behind it is noise smoke, not
    # evenly spaced arcs (Model3D_Rules 28–29, review 2026-10-20).
    core = glow_material('SlashCore', [(0, CORE, 3.6), (.07, CORE2, 2.5), (.28, BAND, 1.55), (.65, DEEP, .6), (1, DEEP, 0)],
                         [(0, 1), (.4, .95), (.75, .55), (1, 0)], 1.0)
    R = .56 * scale
    n = 64
    pts, us = crescent_points(R, 165, -R * .55, n)
    ribbon('Blade', pts, blade_widths(n, .2 * scale), Vector((0, 1, 0)), core, us)
    smoke = smoke_ribbon_material('SlashSmoke', 1.7, contrast=(.34, .7))
    sp, su = crescent_points(R * .9, 158, -R * .55 - .3 * scale, n)
    o = ribbon('Smoke', sp, blade_widths(n, .62 * scale, 1.0), Vector((0, 1, 0)), smoke, su)
    o.location = (0, .02, 0)
    wisp = smoke_ribbon_material('SlashWisp', 1.1, scale=(5.0, 2.0), contrast=(.42, .76))
    wp, wu = crescent_points(R * .78, 140, -R * .55 - .62 * scale, n)
    o = ribbon('Wisp', wp, blade_widths(n, .42 * scale, 1.2), Vector((0, 1, 0)), wisp, wu)
    o.location = (0, .03, 0)
    # Spirit dust: small glowing motes drifting backwards, recycled every loop (seamless).
    mote = glow_material('Mote', [(0, CORE, 6), (.6, CORE2, 3), (1, BAND, 0)], [(0, 1), (.7, .6), (1, 0)], 1.0, tips=False)
    motes = []
    for i in range(18):
        o = disc(f'Mote{i}', rnd.uniform(.008, .02) * scale, mote, segments=10)
        motes.append((o, rnd.uniform(0, 1), rnd.uniform(-.85, .55), rnd.uniform(.25, .7) * scale, rnd.uniform(.6, 1.0)))
    width, height, ppu = 1.95 * scale, 1.45 * scale, 110
    set_view('front', width, height, ppu, (-.4 * scale, 0, 0))
    set_glare(7, .4, 1.0)
    frames = []
    for f in range(frames_count):
        phase = f / frames_count
        for m in bpy.data.materials:
            set_phase(m, phase)
        for o, start, z, travel, gain in motes:
            t = (start + phase) % 1.0
            o.location = (R * .35 - t * travel - .2 * scale, -.01, z * R)
            set_gain(o.data.materials[0], gain * math.sin(math.pi * t))
        frames.append((render_frame(.9), anchor_pixel((0, 0, 0))))
    clear_effect()
    return frames, ppu


def build_impact(frames_count=16, seed=7):
    """Hit flash: violet star burst, expanding ring, glowing feathers scattering and dissolving."""
    rnd = random.Random(seed)
    flash = glow_material('Flash', [(0, CORE, 3.5), (.2, CORE2, 2.2), (.55, BAND, 1.1), (1, DEEP, 0)], [(0, 1), (.5, .6), (1, 0)], 1.0, tips=False)
    ring = glow_material('Ring', [(0, DEEP, 0), (.42, BAND, 1.3), (.55, CORE2, 2.6), (.68, BAND, 1.3), (1, DEEP, 0)],
                         [(0, 0), (.55, .85), (1, 0)], 1.0, tips=False)
    ray = glow_material('Ray', [(0, CORE, 3), (.35, CORE2, 1.8), (1, BAND, 0)], [(0, .9), (1, 0)], 1.0, axis='u')
    feather = glow_material('Feather', [(0, CORE, 2.4), (.4, CORE2, 1.6), (1, BAND, .5)], [(0, .9), (.6, .6), (1, 0)], 1.0)
    f_obj = disc('Flash', .32, flash, segments=32)
    r_obj = disc('Ring', .5, ring, segments=48, inner=.0)
    rays = []
    for i in range(8):
        a = i / 8 * 2 * math.pi + rnd.uniform(-.15, .15)
        length = rnd.uniform(.35, .55)
        d = Vector((math.cos(a), 0, math.sin(a)))
        pts = [d * (length * t) for t in np.linspace(0, 1, 12)]
        o = ribbon(f'Ray{i}', pts, [.05 * (1 - t) + .004 for t in np.linspace(0, 1, 12)], Vector((0, 1, 0)), ray.copy(),
                   list(np.linspace(0, 1, 12)))
        rays.append(o)
    feathers = []
    for i in range(10):
        a = rnd.uniform(0, 2 * math.pi)
        d = Vector((math.cos(a), 0, math.sin(a)))
        side = Vector((-d.z, 0, d.x))
        n = 10
        curve = rnd.uniform(-.04, .04)
        pts = [d * (.09 * t) + side * curve * math.sin(math.pi * t) for t in np.linspace(0, 1, n)]
        o = ribbon(f'Feather{i}', pts, [.035 * math.sin(math.pi * max(.08, t)) for t in np.linspace(0, 1, n)], Vector((0, 1, 0)),
                   feather.copy(), list(np.linspace(0, 1, n)))
        feathers.append((o, d, rnd.uniform(.25, .5), rnd.uniform(-2.5, 2.5)))
    ppu = 150
    set_view('front', 1.3, 1.3, ppu)
    set_glare(7, .35, 1.0)
    frames = []
    for f in range(frames_count):
        t = f / (frames_count - 1)
        flash_t = max(0.0, 1 - t / .35)
        f_obj.scale = Vector((1, 1, 1)) * (.5 + 1.1 * min(1, t / .2))
        set_gain(f_obj.data.materials[0], flash_t ** 1.5)
        r_obj.scale = Vector((1, 1, 1)) * (.25 + 1.0 * (1 - (1 - t) ** 2))
        set_gain(r_obj.data.materials[0], max(0, 1 - t / .7) ** 1.2)
        for o in rays:
            o.scale = Vector((1, 1, 1)) * (.4 + 1.0 * min(1, t / .25))
            set_gain(o.data.materials[0], max(0, 1 - t / .45))
        for o, d, dist, spin in feathers:
            e = 1 - (1 - t) ** 2
            o.location = d * (.08 + dist * e)
            o.rotation_euler = (0, spin * t, 0)
            set_gain(o.data.materials[0], math.sin(math.pi * min(1, t / .9)) * (1 - t) ** .5)
        frames.append((render_frame(.8), anchor_pixel((0, 0, 0))))
    clear_effect()
    return frames, ppu


# ---------------------------------------------------------------- shared pieces for the area effects
def line_material(name, colour, strength, alpha=1.0):
    """Thin glowing line: bright across the middle of the strip, soft edges; no tip fade (rings, star lines)."""
    return glow_material(name, [(0, DEEP, 0), (.5, colour, strength), (1, DEEP, 0)], [(0, 0), (.5, alpha), (1, 0)], 1.0, tips=False)


def fill_material(name, strength=.6):
    return glow_material(name, [(0, BAND, strength), (.65, DEEP, strength * .5), (1, DEEP, 0)], [(0, .8), (.8, .55), (1, 0)], 1.0,
                         tips=False)


def flat_glow(name, hex_colour, strength, alpha=.7):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.surface_render_method = 'BLENDED'
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    emis = nt.nodes.new('ShaderNodeEmission')
    emis.inputs['Color'].default_value = lin(hex_colour)
    emis.inputs['Strength'].default_value = strength
    gain = nt.nodes.new('ShaderNodeValue')
    gain.name = 'Gain'
    gain.outputs[0].default_value = 1.0
    smul = nt.nodes.new('ShaderNodeMath')
    smul.operation = 'MULTIPLY'
    smul.inputs[0].default_value = strength
    nt.links.new(gain.outputs[0], smul.inputs[1])
    nt.links.new(smul.outputs[0], emis.inputs['Strength'])
    amul = nt.nodes.new('ShaderNodeMath')
    amul.operation = 'MULTIPLY'
    amul.inputs[0].default_value = alpha
    nt.links.new(gain.outputs[0], amul.inputs[1])
    clamp = nt.nodes.new('ShaderNodeClamp')
    nt.links.new(amul.outputs[0], clamp.inputs['Value'])
    mix = nt.nodes.new('ShaderNodeMixShader')
    transp = nt.nodes.new('ShaderNodeBsdfTransparent')
    nt.links.new(clamp.outputs[0], mix.inputs['Fac'])
    nt.links.new(transp.outputs[0], mix.inputs[1])
    nt.links.new(emis.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs['Surface'])
    return m


def fresnel_material(name, rim_hex, body_hex, rim_strength=2.2, body_alpha=.08, rim_alpha=.75):
    """Shield shell: faint body, bright rim where the surface turns away from the camera."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.surface_render_method = 'BLENDED'
    m.use_backface_culling = False
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    lw = nt.nodes.new('ShaderNodeLayerWeight')
    lw.inputs['Blend'].default_value = .35
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = lin(body_hex)
    ramp.color_ramp.elements[1].color = lin(rim_hex)
    nt.links.new(lw.outputs['Facing'], ramp.inputs['Fac'])
    power = nt.nodes.new('ShaderNodeMath')
    power.operation = 'POWER'
    power.inputs[1].default_value = 2.2
    nt.links.new(lw.outputs['Facing'], power.inputs[0])
    strength = nt.nodes.new('ShaderNodeMath')
    strength.operation = 'MULTIPLY_ADD'
    strength.inputs[1].default_value = rim_strength
    strength.inputs[2].default_value = .25
    nt.links.new(power.outputs[0], strength.inputs[0])
    gain = nt.nodes.new('ShaderNodeValue')
    gain.name = 'Gain'
    gain.outputs[0].default_value = 1.0
    gs = nt.nodes.new('ShaderNodeMath')
    gs.operation = 'MULTIPLY'
    nt.links.new(strength.outputs[0], gs.inputs[0])
    nt.links.new(gain.outputs[0], gs.inputs[1])
    emis = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(ramp.outputs['Color'], emis.inputs['Color'])
    nt.links.new(gs.outputs[0], emis.inputs['Strength'])
    alpha = nt.nodes.new('ShaderNodeMath')
    alpha.operation = 'MULTIPLY_ADD'
    alpha.inputs[1].default_value = rim_alpha - body_alpha
    alpha.inputs[2].default_value = body_alpha
    nt.links.new(power.outputs[0], alpha.inputs[0])
    ga = nt.nodes.new('ShaderNodeMath')
    ga.operation = 'MULTIPLY'
    nt.links.new(alpha.outputs[0], ga.inputs[0])
    nt.links.new(gain.outputs[0], ga.inputs[1])
    mix = nt.nodes.new('ShaderNodeMixShader')
    transp = nt.nodes.new('ShaderNodeBsdfTransparent')
    nt.links.new(ga.outputs[0], mix.inputs['Fac'])
    nt.links.new(transp.outputs[0], mix.inputs[1])
    nt.links.new(emis.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs['Surface'])
    return m


def ring(name, radius, width, material, z=0.0, segments=128):
    """Flat annulus in the ground plane (XY), v across the line."""
    return disc(name, radius + width / 2, material, location=(0, 0, z), normal_axis='Z', segments=segments,
                inner=max(0.0, radius - width / 2))


def polyline(name, pts, width, material, normal=Vector((0, 0, 1)), steps=6):
    dense = []
    for i in range(len(pts) - 1):
        a, b = Vector(pts[i]), Vector(pts[i + 1])
        for t in np.linspace(0, 1, steps, endpoint=(i == len(pts) - 2)):
            dense.append(a.lerp(b, float(t)))
    return ribbon(name, dense, [width] * len(dense), normal, material)


def glyph(prefix, centre, size, angle, rnd, material):
    """A seal-script-like rune: three to five strokes in a cell, rotated to face out of its ring."""
    rot = Matrix.Rotation(angle, 3, 'Z')
    c = Vector(centre)
    for k in range(rnd.randint(3, 5)):
        r = rnd.random()
        if r < .35:
            y = rnd.uniform(-.38, .38)
            pts = [(rnd.uniform(-.45, -.1), y), (rnd.uniform(.1, .45), y)]
        elif r < .7:
            x = rnd.uniform(-.38, .38)
            pts = [(x, rnd.uniform(-.45, -.1)), (x, rnd.uniform(.1, .45))]
        elif r < .9:
            x = rnd.uniform(-.25, .25)
            pts = [(x, .42), (x, -.15), (x + rnd.choice((-.3, .3)), -.42)]
        else:
            x, y = rnd.uniform(-.3, .3), rnd.uniform(-.3, .3)
            pts = [(x, y), (x + .14, y + .1)]
        world = [c + rot @ Vector((p[0] * size, p[1] * size, 0)) for p in pts]
        polyline(f'{prefix}_{k}', world, size * .11, material)


def rune_band(prefix, radius, height, count, rnd, material, z=0.0):
    for i in range(count):
        a = 2 * math.pi * i / count
        glyph(f'{prefix}{i}', (math.cos(a) * radius, math.sin(a) * radius, z), height, a - math.pi / 2, rnd, material)


def star_polygon(prefix, radius, points, step, material, width, z=0.0):
    """{points/step} star (6/2 = hexagram, 8/3 = octagram) as straight glowing lines."""
    verts = [(math.cos(2 * math.pi * i / points + math.pi / 2) * radius, math.sin(2 * math.pi * i / points + math.pi / 2) * radius, z)
             for i in range(points)]
    for i in range(points):
        polyline(f'{prefix}{i}', [verts[i], verts[(i + step) % points]], width, material)


def ticks(prefix, r0, r1, count, material, width):
    for i in range(count):
        a = 2 * math.pi * i / count
        d = Vector((math.cos(a), math.sin(a), 0))
        polyline(f'{prefix}{i}', [d * r0, d * r1], width, material, steps=3)


def render_static(view, size_m, ppu, cover=.8):
    set_view(view, size_m, size_m, ppu)
    return [(render_frame(cover), anchor_pixel((0, 0, 0)))]


def ribbon_sides(name, points, sides, widths, material):
    """Like ribbon() but with a given width direction per point (helix bands that stay upright)."""
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new('UVMap')
    n = len(points)
    rows = [(bm.verts.new(p + s * w * .5), bm.verts.new(p - s * w * .5)) for p, s, w in zip(points, sides, widths)]
    for i in range(n - 1):
        f = bm.faces.new((rows[i][0], rows[i + 1][0], rows[i + 1][1], rows[i][1]))
        for loop, (u, v) in zip(f.loops, ((i / (n - 1), 0), ((i + 1) / (n - 1), 0), ((i + 1) / (n - 1), 1), (i / (n - 1), 1))):
            loop[uv].uv = (u, v)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(material)
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    _objects.append(o)
    return o


# ---------------------------------------------------------------- Attack02, Skill01, Skill02, Skill03, Ultimate
def build_feather_blade(frames_count=12, seed=11):
    """Attack02 Tam Vũ Liên Kích: a pointed spirit feather flying towards +X (shaft, angled barbs, wind tail)."""
    rnd = random.Random(seed)
    vane = glow_material('Vane', [(0, DEEP, .3), (.22, BAND, 1.25), (.5, CORE2, 1.9), (.78, BAND, 1.25), (1, DEEP, .3)],
                         [(0, .3), (.5, 1), (1, .3)], 1.0, noise=(7, .3, 1))
    barb = glow_material('Barb', [(0, CORE2, 1.8), (1, BAND, .8)], [(0, .9), (1, .1)], 1.0)
    tail = glow_material('FeatherTail', [(0, BAND, 1.1), (1, DEEP, .1)], [(0, .6), (1, 0)], 1.0, noise=(10, .8, 1))
    n = 40
    length = .95
    pts = [Vector((-length / 2 + length * i / (n - 1), 0, 0)) for i in range(n)]
    widths = [.2 * max(.04, math.sin(math.pi * (i / (n - 1)) ** .65)) for i in range(n)]
    ribbon('Vane', pts, widths, Vector((0, 1, 0)), vane)
    for k in range(9):
        x = -length / 2 + .12 + k * .075
        for side in (-1, 1):
            w = .2 * math.sin(math.pi * ((x + length / 2) / length) ** .65) * .5
            polyline(f'Barb{k}{side}', [Vector((x, -.01, 0)), Vector((x - .07, -.01, side * w))], .011, barb, normal=Vector((0, 1, 0)))
    for k, z in enumerate((-.05, .04, .0)):
        p = [Vector((-length / 2 - .55 * t, 0, z * (1 + t))) for t in np.linspace(0, 1, 20)]
        o = ribbon(f'Tail{k}', p, [(.05 - .015 * k) * (1 - t * .7) for t in np.linspace(0, 1, 20)], Vector((0, 1, 0)), tail.copy())
        set_gain(o.data.materials[0], .9 - .25 * k)
    mote = glow_material('FMote', [(0, CORE, 4), (1, BAND, 0)], [(0, 1), (1, 0)], 1.0, tips=False)
    motes = [(disc(f'FMote{i}', rnd.uniform(.008, .016), mote, segments=8), rnd.random(), rnd.uniform(-.12, .12)) for i in range(10)]
    ppu = 120
    set_view('front', 1.8, .7, ppu, (-.3, 0, 0))
    set_glare(6, .4, 1.0)
    frames = []
    for f in range(frames_count):
        phase = f / frames_count
        for m in bpy.data.materials:
            set_phase(m, phase)
        for o, start, z in motes:
            t = (start + phase) % 1
            o.location = (-.3 - t * .7, -.02, z)
            set_gain(o.data.materials[0], math.sin(math.pi * t))
        frames.append((render_frame(.9), anchor_pixel((0, 0, 0))))
    clear_effect()
    return frames, ppu


def build_magic_array(seed=21):
    """Skill01 Huyền Cơ Trận: outer rune ring, inner hexagram ring, soft fill (top view, radius 1);
    energy spikes erupting around the circle (game view)."""
    rnd = random.Random(seed)
    ppu_top = 320
    gold = line_material('Gold', GOLD, 2.0)
    gold_thin = line_material('GoldThin', GOLD, 1.6, .9)
    violet = line_material('Violet', BAND, 1.4)
    pale = line_material('Pale', CORE2, 1.5)
    set_glare(6, .45, .8)
    # Outer ring.
    ring('R1', 1.0, .028, gold)
    ring('R2', .965, .008, gold_thin)
    ring('R3', .86, .012, pale)
    rune_band('Rune', .912, .075, 18, rnd, gold_thin)
    ticks('Tick', .79, .83, 48, violet, .008)
    outer = render_static('top', 2.25, ppu_top)
    clear_effect()
    # Inner ring.
    ring('I1', .62, .02, violet)
    ring('I2', .30, .012, gold)
    star_polygon('Hex', .62, 6, 2, pale, .014)
    for i in range(6):
        a = 2 * math.pi * i / 6 + math.pi / 2
        o = ring(f'Node{i}', .045, .01, gold)
        o.location = (math.cos(a) * .62, math.sin(a) * .62, 0)
    glyph('Centre', (0, 0, 0), .22, 0, rnd, gold)
    inner = render_static('top', 1.45, ppu_top)
    clear_effect()
    disc('Fill', 1.08, fill_material('Fill', .55), normal_axis='Z', segments=64)
    fill = render_static('top', 2.3, 120, cover=1.0)
    clear_effect()
    # Qi bursting up out of the circle (game view). Not shards or rays (user 2026-10-21): soft columns of density
    # that swell as they rise, sway, thin out and dissolve from the top; random places (not on a circle), sizes,
    # leans and timing (Model3D_Rules 29).
    setup_volumes()
    glow_m = glow_material('QiFoot', [(0, CORE2, .9), (.5, BAND, .6), (1, DEEP, 0)], [(0, .6), (1, 0)], 1.0, tips=False)
    columns = []
    for i in range(16):
        centre = i == 0
        a = rnd.uniform(0, 2 * math.pi)
        r = 0.0 if centre else math.sqrt(rnd.uniform(.03, 1.0)) * .9
        base = Vector((math.cos(a) * r, math.sin(a) * r, 0))
        h = 2.7 if centre else rnd.uniform(1.3, 2.3) * (1.1 - .35 * r)
        r0 = .13 if centre else rnd.uniform(.06, .1)
        r1 = .38 if centre else rnd.uniform(.16, .27)
        lean = 0.0 if centre else rnd.uniform(.06, .32)
        m = qi_column_material(f'Qi{i}', h, r0, r1, lean, seed=i + 1, density_scale=4.5 if centre else 5.5, emission=1.1)
        cage = volume_cage(f'QiCage{i}', m, r1 * 1.9 + lean + .35, -.05, h + .9)
        cage.location = base
        cage.rotation_euler = (0, 0, a)                      # +x of the column (its lean) points outwards
        foot = disc(f'QiFoot{i}', .4 if centre else rnd.uniform(.18, .28), glow_m.copy(), location=base + Vector((0, -.01, .02)),
                    segments=24)
        delay = 0.0 if centre else rnd.uniform(0, .17)
        columns.append((cage, m, foot, h, delay, rnd.uniform(.17, .27), rnd.uniform(.3, .38), rnd.uniform(1.7, 2.6)))
    frames_count = 24
    ppu = 120
    set_view('game', 2.8, 4.0, ppu, (0, 0, 1.3))           # roomy: the taller qi must not touch the frame edge
    set_glare(7, .45, .9)
    spike_frames = []
    for f in range(frames_count):
        t = f / 30.0
        for cage, m, foot, h, delay, rise, life, speed in columns:
            age = t - delay
            grow = 1 - (1 - min(1.0, max(0.0, age / rise))) ** 2      # eases out: the burst slows as it rises
            after = max(0.0, age - rise)
            fade = 0.0 if age < 0 else (1 - min(1.0, after / life)) ** 1.3
            # The qi leaves the ground once it has risen: the foot lifts and the top keeps drifting up.
            set_values(m, Low=after * speed * .55, High=h * grow + after * speed * .35, Flow=max(0.0, age) * speed,
                       Gain=fade * min(1.0, max(0.0, age) / .05))
            cage.hide_render = fade <= 0
            foot.scale = Vector((1, 1, 1)) * (.6 + 1.0 * min(1, max(0, age) / .18))
            set_gain(foot.data.materials[0], 0 if age < 0 else max(0, 1 - age / .4) ** 1.5)
        spike_frames.append((render_frame(.8), anchor_pixel((0, 0, 0))))
    clear_effect()
    return outer, inner, fill, spike_frames, ppu_top, ppu


class Graph:
    """Small helper to wire shader math: arguments are sockets or numbers."""

    def __init__(self, tree):
        self.nt = tree

    def _in(self, socket_in, value):
        if isinstance(value, (int, float)):
            socket_in.default_value = value
        else:
            self.nt.links.new(value, socket_in)

    def math(self, op, a, b=0.0, c=None):
        n = self.nt.nodes.new('ShaderNodeMath')
        n.operation = op
        self._in(n.inputs[0], a)
        self._in(n.inputs[1], b)
        if c is not None:
            self._in(n.inputs[2], c)
        return n.outputs[0]

    def vec(self, op, a, b=None):
        n = self.nt.nodes.new('ShaderNodeVectorMath')
        n.operation = op
        self.nt.links.new(a, n.inputs[0])
        if b is not None:
            if isinstance(b, (tuple, list)):
                n.inputs[1].default_value = b
            else:
                self.nt.links.new(b, n.inputs[1])
        return n.outputs['Value'] if op in ('LENGTH', 'DOT_PRODUCT', 'DISTANCE') else n.outputs['Vector']

    def rotate_z(self, vector, angle):
        n = self.nt.nodes.new('ShaderNodeVectorRotate')
        n.rotation_type = 'Z_AXIS'
        self.nt.links.new(vector, n.inputs['Vector'])
        self._in(n.inputs['Angle'], angle)
        return n.outputs['Vector']

    def noise(self, vector, scale, detail=4.0, roughness=.55, distortion=0.0):
        n = self.nt.nodes.new('ShaderNodeTexNoise')
        n.inputs['Scale'].default_value = scale
        n.inputs['Detail'].default_value = detail
        n.inputs['Roughness'].default_value = roughness
        n.inputs['Distortion'].default_value = distortion
        self.nt.links.new(vector, n.inputs['Vector'])
        return n.outputs['Fac']

    def remap(self, value, a, b, c=0.0, d=1.0, smooth=True):
        n = self.nt.nodes.new('ShaderNodeMapRange')
        n.interpolation_type = 'SMOOTHSTEP' if smooth else 'LINEAR'
        n.clamp = True
        self._in(n.inputs['Value'], value)
        for name, v in (('From Min', a), ('From Max', b), ('To Min', c), ('To Max', d)):
            self._in(n.inputs[name], v)
        return n.outputs['Result']

    def xyz(self, vector):
        n = self.nt.nodes.new('ShaderNodeSeparateXYZ')
        self.nt.links.new(vector, n.inputs['Vector'])
        return n.outputs['X'], n.outputs['Y'], n.outputs['Z']

    def ramp(self, value, stops):
        n = self.nt.nodes.new('ShaderNodeValToRGB')
        el = n.color_ramp.elements
        el[0].position, el[0].color = stops[0][0], lin(stops[0][1])
        el[1].position, el[1].color = stops[-1][0], lin(stops[-1][1])
        for pos, colour in stops[1:-1]:
            el.new(pos).color = lin(colour)
        self._in(n.inputs['Fac'], value)
        return n.outputs['Color']


def cyclone_volume_material(height=2.5, r0=.16, r1=.9, lean=.3, twist=2.6, density_scale=7.5):
    """Glowing wind as a density field (Model3D_Rules 28–29): no ribbons, no periodic helix. Two noise fields twisted
    with height and turned about the axis at different speeds (1 and 2 turns per loop, so the loop is seamless but
    the pattern changes); funnel radius varies with height, angle and noise; the axis leans and precesses; the crown
    and the foot erode with noise instead of a flat cut. The 'Phase' value (0..1) drives one loop."""
    m = bpy.data.materials.new('CycloneVolume')
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    g = Graph(nt)
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    phase_n = nt.nodes.new('ShaderNodeValue')
    phase_n.name = 'Phase'
    phase = phase_n.outputs[0]
    co = nt.nodes.new('ShaderNodeTexCoord').outputs['Object']
    x, y, z = g.xyz(co)
    t = g.math('DIVIDE', z, height)                                  # 0 at the foot, 1 at the crown
    tc = g.math('MINIMUM', g.math('MAXIMUM', t, 0.0), 1.0)
    turn1 = g.math('MULTIPLY', phase, -2 * math.pi)
    turn2 = g.math('MULTIPLY', phase, -4 * math.pi)
    # Leaning, precessing axis: offset grows with height and turns once per loop.
    lean_v = nt.nodes.new('ShaderNodeCombineXYZ')
    g._in(lean_v.inputs['X'], g.math('MULTIPLY', g.math('POWER', tc, 1.4), lean))
    axis = g.rotate_z(lean_v.outputs['Vector'], g.math('MULTIPLY_ADD', tc, 1.3, g.math('MULTIPLY', phase, 2 * math.pi)))
    rel = g.vec('SUBTRACT', co, axis)
    flat = g.vec('MULTIPLY', rel, (1, 1, 0))
    d = g.vec('LENGTH', flat)
    # Two twisted, turning noise fields.
    p1 = g.rotate_z(co, g.math('MULTIPLY_ADD', z, twist, turn1))
    p2 = g.rotate_z(co, g.math('MULTIPLY_ADD', z, twist * 1.35, turn2))
    n1 = g.noise(g.vec('MULTIPLY', p1, (1, 1, .55)), 1.6, 5, .6, .3)
    n2 = g.noise(g.vec('MULTIPLY', p2, (1, 1, .8)), 4.2, 3, .5, .2)
    # Funnel radius: widens with height, breathes with the large noise.
    radius = g.math('MULTIPLY_ADD', g.math('POWER', tc, 1.25), r1 - r0, r0)
    radius = g.math('MULTIPLY', radius, g.math('MULTIPLY_ADD', n1, .55, .72))
    width = g.math('MULTIPLY_ADD', tc, .2, .06)
    off = g.math('DIVIDE', g.math('SUBTRACT', d, radius), width)
    wall = g.math('EXPONENT', g.math('MULTIPLY', g.math('POWER', g.math('ABSOLUTE', off), 2.0), -1.0))
    core = g.math('MULTIPLY', g.remap(g.math('DIVIDE', d, radius), 1.0, 0.0), .1)
    shape = g.math('MAXIMUM', wall, core)
    # Streaky wisps: contrast on the product of the two fields.
    wisps = g.remap(g.math('MULTIPLY_ADD', n2, .45, g.math('MULTIPLY', n1, .75)), .52, .82)
    # Foot and crown eroded with noise.
    erode = g.math('MULTIPLY_ADD', n2, .35, -.17)
    foot = g.remap(g.math('ADD', t, erode), 0.0, .12)
    crown = g.remap(g.math('ADD', t, erode), 1.02, .72)
    density = g.math('MULTIPLY', g.math('MULTIPLY', shape, wisps), g.math('MULTIPLY', foot, crown))
    density = g.math('MULTIPLY', density, density_scale)
    colour = g.ramp(g.math('MULTIPLY_ADD', wisps, .7, g.math('MULTIPLY', wall, .3)),
                    [(0, DEEP), (.45, BAND), (.8, CORE2), (1, CORE)])
    vol = nt.nodes.new('ShaderNodeVolumePrincipled')
    vol.inputs['Color'].default_value = lin(DEEP)
    nt.links.new(density, vol.inputs['Density'])
    nt.links.new(colour, vol.inputs['Emission Color'])
    nt.links.new(g.math('MULTIPLY', density, .85), vol.inputs['Emission Strength'])
    nt.links.new(vol.outputs['Volume'], out.inputs['Volume'])
    return m


def swirl_surface_material(name, strength=1.6):
    """Ground vortex: noise swirled by radius (polar twist) - soft smoky arms, no drawn spirals."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.surface_render_method = 'BLENDED'
    nt = m.node_tree
    nt.nodes.clear()
    g = Graph(nt)
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    co = nt.nodes.new('ShaderNodeTexCoord').outputs['Object']
    r = g.vec('LENGTH', g.vec('MULTIPLY', co, (1, 1, 0)))
    p = g.rotate_z(co, g.math('MULTIPLY', r, 3.2))
    n1 = g.noise(g.vec('MULTIPLY', p, (1.0, 2.6, 1)), 2.4, 5, .6, .4)
    n2 = g.noise(co, 7.0, 3, .5, 0)
    arms = g.remap(g.math('MULTIPLY_ADD', n2, .3, g.math('MULTIPLY', n1, .85)), .45, .8)
    ringmask = g.math('MULTIPLY', g.remap(r, .05, .35), g.remap(r, 1.05, .7))
    a = g.math('MULTIPLY', arms, ringmask)
    colour = g.ramp(arms, [(0, DEEP), (.5, BAND), (.85, CORE2), (1, CORE)])
    emis = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(colour, emis.inputs['Color'])
    nt.links.new(g.math('MULTIPLY', a, strength), emis.inputs['Strength'])
    mix = nt.nodes.new('ShaderNodeMixShader')
    transp = nt.nodes.new('ShaderNodeBsdfTransparent')
    nt.links.new(g.math('MINIMUM', g.math('MULTIPLY', a, 1.2), 1.0), mix.inputs['Fac'])
    nt.links.new(transp.outputs[0], mix.inputs[1])
    nt.links.new(emis.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs['Surface'])
    return m


def smoke_ribbon_material(name, strength=1.4, scale=(3.2, 1.3), contrast=(.42, .78), lead=1.0):
    """Wind smoke for ribbons: 4D noise over the UVs, looped by cross-fading W (no streaks at fixed intervals).
    lead: the v of the bright leading edge (0 or 1, depends on the strip direction); u fades at both tips.
    'Phase' (0..1) drives one loop."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.surface_render_method = 'BLENDED'
    m.use_backface_culling = False
    nt = m.node_tree
    nt.nodes.clear()
    g = Graph(nt)
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    phase_n = nt.nodes.new('ShaderNodeValue')
    phase_n.name = 'Phase'
    phase = phase_n.outputs[0]
    uv = nt.nodes.new('ShaderNodeUVMap').outputs['UV']
    u, v, _ = g.xyz(uv)
    p = g.vec('MULTIPLY', uv, (scale[0], scale[1], 1))

    def noise4(w):
        n = nt.nodes.new('ShaderNodeTexNoise')
        n.noise_dimensions = '4D'
        n.inputs['Scale'].default_value = 1.0
        n.inputs['Detail'].default_value = 5.0
        n.inputs['Roughness'].default_value = .6
        n.inputs['Distortion'].default_value = .35
        nt.links.new(p, n.inputs['Vector'])
        g._in(n.inputs['W'], w)
        return n.outputs['Fac']
    # Seamless loop: blend the field at W = phase and W = phase - 1 with weights (1 - phase, phase).
    a = noise4(g.math('MULTIPLY', phase, 1.2))
    b = noise4(g.math('MULTIPLY', g.math('SUBTRACT', phase, 1.0), 1.2))
    n = g.math('ADD', g.math('MULTIPLY', a, g.math('SUBTRACT', 1.0, phase)), g.math('MULTIPLY', b, phase))
    smoke = g.remap(n, contrast[0], contrast[1])
    # lead None: soft on both long edges (free-floating smoke); else strongest at the leading edge, gone at the back.
    edge = g.remap(g.math('ABSOLUTE', g.math('SUBTRACT', v, .5)), .5, .08) if lead is None else g.remap(v, 1.0 - lead, .05 + .9 * lead)
    tips = g.math('MULTIPLY', g.remap(u, 0.0, .2), g.remap(u, 1.0, .8))
    mask = g.math('MULTIPLY', g.math('MULTIPLY', smoke, edge), tips)
    colour = g.ramp(g.math('MULTIPLY', smoke, edge), [(0, DEEP), (.45, BAND), (.8, CORE2), (1, CORE)])
    emis = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(colour, emis.inputs['Color'])
    nt.links.new(g.math('MULTIPLY', mask, strength), emis.inputs['Strength'])
    mix = nt.nodes.new('ShaderNodeMixShader')
    transp = nt.nodes.new('ShaderNodeBsdfTransparent')
    nt.links.new(g.math('MINIMUM', g.math('MULTIPLY', mask, 1.3), 1.0), mix.inputs['Fac'])
    nt.links.new(transp.outputs[0], mix.inputs[1])
    nt.links.new(emis.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs['Surface'])
    return m


def qi_column_material(name, height, r0, r1, lean=0.0, bulge=0.0, seed=0, density_scale=5.0, emission=1.0):
    """Qi rising (or pouring) as a density field (user 2026-10-21: rising streams are soft qi, never rays or lines):
    no bright core line, no hard edge, no straight bar. Object coordinates in metres, origin at the foot, z up.
    The width grows from r0 at the foot to r1 at `height` (bulge > 0 swells the middle instead), breathes with a large
    noise and the axis leans by `lean` towards +x and sways; the density is uneven puffs, thicker near the foot.
    Live values (set_values): 'Low' / 'High' = the metres of the column that hold qi (both edges eroded by noise),
    'Flow' = how far the noise has travelled up (negative = down), 'Gain' = density and light."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    g = Graph(nt)
    out = nt.nodes.new('ShaderNodeOutputMaterial')

    def live(label, value):
        n = nt.nodes.new('ShaderNodeValue')
        n.name = label
        n.outputs[0].default_value = value
        return n.outputs[0]
    low, high, flow, gain = live('Low', 0.0), live('High', height), live('Flow', 0.0), live('Gain', 1.0)
    co = nt.nodes.new('ShaderNodeTexCoord').outputs['Object']
    x, y, z = g.xyz(co)
    t = g.math('MINIMUM', g.math('MAXIMUM', g.math('DIVIDE', z, height), 0.0), 1.0)
    # The noise travels with the qi (up, or down for a pouring column); each column has its own part of the field.
    shift = nt.nodes.new('ShaderNodeCombineXYZ')
    g._in(shift.inputs['Z'], flow)
    p = g.vec('ADD', g.vec('SUBTRACT', co, shift.outputs['Vector']), (seed * 7.31, seed * 3.17, seed * 1.93))
    big = g.noise(g.vec('MULTIPLY', p, (1.2, 1.2, .7)), 1.3, 3, .5, .2)
    big2 = g.noise(g.vec('ADD', g.vec('MULTIPLY', p, (1.2, 1.2, .7)), (11.3, 4.1, 2.7)), 1.3, 3, .5, .2)
    # Wisps drawn out along the flow (not round beads), still broken up across.
    puffs = g.remap(g.noise(g.vec('MULTIPLY', p, (1, 1, .55)), 4.2, 5, .62, .45), .3, .76)
    # Leaning, swaying axis; the sway grows with height (the foot stays where the qi comes out).
    sway = g.math('MULTIPLY', t, .32)
    ax = g.math('MULTIPLY_ADD', g.math('SUBTRACT', big, .5), sway, g.math('MULTIPLY', g.math('POWER', t, 1.4), lean))
    ay = g.math('MULTIPLY', g.math('SUBTRACT', big2, .5), sway)
    d = g.math('SQRT', g.math('ADD', g.math('POWER', g.math('SUBTRACT', x, ax), 2.0), g.math('POWER', g.math('SUBTRACT', y, ay), 2.0)))
    radius = g.math('MULTIPLY_ADD', g.math('POWER', t, .8), r1 - r0, r0)
    if bulge:
        swell = g.math('MULTIPLY_ADD', g.math('SINE', g.math('MULTIPLY', t, math.pi)), bulge, 1.0 - bulge * .6)
        radius = g.math('MULTIPLY', radius, swell)
    radius = g.math('MULTIPLY', radius, g.math('MULTIPLY_ADD', big, .6, .7))
    radial = g.math('EXPONENT', g.math('MULTIPLY', g.math('POWER', g.math('DIVIDE', d, radius), 2.0), -2.2))
    # Where the column holds qi: soft, noise-eroded edges at both ends (no flat cut, no pointed tip).
    erode = g.math('MULTIPLY', g.math('SUBTRACT', puffs, .5), .35)
    ze = g.math('ADD', z, erode)
    lower = g.remap(ze, g.math('SUBTRACT', low, .04), g.math('ADD', low, .22))
    upper = g.remap(ze, g.math('ADD', high, .04), g.math('SUBTRACT', high, .55))
    ground = g.remap(z, -.02, .07)
    thin = g.math('MULTIPLY_ADD', t, -.45, 1.0)
    shape = g.math('MULTIPLY', g.math('MULTIPLY', radial, puffs), g.math('MULTIPLY', g.math('MULTIPLY', lower, upper), ground))
    dens = g.math('MULTIPLY', g.math('MULTIPLY', shape, thin), g.math('MULTIPLY', gain, density_scale))
    tone = g.math('MULTIPLY', g.math('MULTIPLY', radial, puffs), g.math('MULTIPLY_ADD', t, -.35, 1.0))
    colour = g.ramp(tone, [(0, DEEP), (.4, BAND), (.78, CORE2), (1, CORE)])
    vol = nt.nodes.new('ShaderNodeVolumePrincipled')
    vol.inputs['Color'].default_value = lin(DEEP)
    nt.links.new(dens, vol.inputs['Density'])
    nt.links.new(colour, vol.inputs['Emission Color'])
    nt.links.new(g.math('MULTIPLY', dens, emission), vol.inputs['Emission Strength'])
    nt.links.new(vol.outputs['Volume'], out.inputs['Volume'])
    return m


def set_values(material, **values):
    for label, value in values.items():
        material.node_tree.nodes[label].outputs[0].default_value = value


def volume_cage(name, material, half_width, z0, z1):
    """Box for a volume that reads Object coordinates in metres with the origin at the foot; larger than the density
    it holds (Model3D_Rules: no flat cut)."""
    bpy.ops.mesh.primitive_cube_add(size=1)
    cage = bpy.context.active_object
    cage.name = name
    for v in cage.data.vertices:
        v.co.x *= 2 * half_width
        v.co.y *= 2 * half_width
        v.co.z = z0 if v.co.z < 0 else z1
    cage.data.materials.append(material)
    _objects.append(cage)
    return cage


def setup_volumes():
    e = scene.eevee
    for attr, value in (('volumetric_tile_size', '1'), ('volumetric_samples', 160), ('volumetric_start', 6.0),
                        ('volumetric_end', 14.0), ('use_volumetric_shadows', False), ('volumetric_sample_distribution', .6)):
        try:
            setattr(e, attr, value)
        except Exception:
            pass


def build_cyclone(frames_count=24, seed=31):
    """Skill02 Phong Vũ Khốn Trận: a glowing wind funnel made of noise in a volume (no ribbons), with feathers and a
    few golden runes caught in it at random radii and heights (game view, seamless loop); a smoky swirl on the ground
    (top view, the game spins it)."""
    rnd = random.Random(seed)
    disc('Swirl', 1.1, swirl_surface_material('SwirlM'), normal_axis='Z', segments=96)
    set_glare(6, .45, .8)
    vortex = render_static('top', 2.3, 260)
    clear_effect()
    setup_volumes()
    height = 2.5
    vol_m = cyclone_volume_material(height=height)
    bpy.ops.mesh.primitive_cube_add(size=1)
    cage = bpy.context.active_object
    cage.name = 'CycloneCage'
    cage.scale = (3.8, 3.8, 3.2)                # cage clearly larger than the density (rule: no flat cut)
    cage.location = (0, 0, 1.25)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    cage.data.materials.append(vol_m)
    _objects.append(cage)
    # The material reads Object coordinates; keep them metric with the origin on the ground.
    cage.location = (0, 0, 0)
    for v in cage.data.vertices:
        v.co.z += 1.25
    feather_m = glow_material('CFeather', [(0, CORE, 2.4), (.5, CORE2, 1.5), (1, BAND, .5)], [(0, .9), (1, .1)], 1.0)
    rune_m = flat_glow('CRune', GOLD, 1.6, .85)
    # Caught debris: random height, radius jitter, speed (1 or 2 turns per loop keeps the loop seamless) and tilt.
    orbiters = [(rnd.uniform(.25, 2.3), rnd.random(), rnd.choice((1, 1, 2)), rnd.random() < .22, rnd.uniform(-.18, .22),
                 rnd.uniform(-.6, .6)) for _ in range(22)]
    ppu = 150
    set_view('game', 3.3, 4.3, ppu, (0, 0, 1.45))
    set_glare(7, .4, 1.0)
    frames = []
    for f in range(frames_count):
        phase = f / frames_count
        set_phase(vol_m, phase)
        debris = [o for o in _objects if o.name.startswith(('Feather', 'Rune'))]
        _objects[:] = [o for o in _objects if o not in debris]
        for o in debris:
            bpy.data.objects.remove(o, do_unlink=True)
        for i, (z, start, revs, is_rune, dr, tilt) in enumerate(orbiters):
            r = .16 + .74 * (z / height) ** 1.25 + dr
            a = 2 * math.pi * (start + revs * phase)
            p = Vector((math.cos(a) * r, math.sin(a) * r, z))
            if is_rune:
                glyph(f'Rune{i}', (0, 0, 0), .09, 0, random.Random(i), rune_m)
                for o in _objects[-5:]:
                    if o.name.startswith(f'Rune{i}'):
                        o.rotation_euler = (math.radians(90), 0, 0)
                        o.location = p
            else:
                d = Vector((-math.sin(a), math.cos(a), tilt)).normalized() * .075
                polyline(f'Feather{i}', [p - d, p + d], .024, feather_m, normal=Vector((0, 1, 0)), steps=6)
        frames.append((render_frame(.85), anchor_pixel((0, 0, 0))))
    clear_effect()
    return vortex, frames, ppu


def build_shield(frames_count=16):
    """Skill03 Thiên Mưu Hộ Trận: translucent violet dome with a bright rim, geodesic (hexagon-like) lattice, a
    shimmer ring rising through it, golden base ring (game view, loop); rune ring on the ground (top view)."""
    rnd = random.Random(41)
    ring('SR1', 1.05, .02, line_material('SR1M', GOLD, 1.25))
    ring('SR2', .93, .01, line_material('SR2M', CORE2, 1.3))
    rune_band('SRune', .99, .07, 16, rnd, line_material('SRuneM', GOLD, 1.1, .9))
    disc('SFill', 1.1, fill_material('SFill', .35), normal_axis='Z', segments=64)
    set_glare(6, .45, .8)
    ground = render_static('top', 2.35, 260)
    clear_effect()
    # Dome shell.
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=48, v_segments=24, radius=1.0)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -.01], context='VERTS')
    me = bpy.data.meshes.new('Dome')
    bm.to_mesh(me)
    bm.free()
    me.materials.append(fresnel_material('DomeM', CORE2, BAND, rim_strength=2.4, body_alpha=.28, rim_alpha=.9))
    dome = bpy.data.objects.new('Dome', me)
    scene.collection.objects.link(dome)
    _objects.append(dome)
    # Geodesic lattice.
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.005)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -.02], context='VERTS')
    me = bpy.data.meshes.new('Lattice')
    bm.to_mesh(me)
    bm.free()
    lattice_m = flat_glow('LatticeM', CORE2, 1.5, .55)
    me.materials.append(lattice_m)
    lattice = bpy.data.objects.new('Lattice', me)
    wire = lattice.modifiers.new('Wire', 'WIREFRAME')
    wire.thickness = .012
    wire.use_replace = True
    scene.collection.objects.link(lattice)
    _objects.append(lattice)
    base = ring('Base', 1.0, .035, line_material('BaseM', GOLD, 1.5))
    # Front emblem (storyboard "Lá chắn (Front)"): octagram in a circle, standing at the front of the dome.
    emblem_m = line_material('EmblemM', GOLD, 1.8, .9)
    before = len(_objects)
    star_polygon('Emb', .28, 8, 3, emblem_m, .012)
    ring('EmbRing', .3, .012, emblem_m, segments=64)
    ring('EmbCore', .1, .02, line_material('EmbCoreM', CORE2, 1.8), segments=48)
    for o in _objects[before:]:
        o.rotation_euler = (math.radians(90), 0, 0)
        o.location = (0, -.86, .5)
    shimmer_m = line_material('Shimmer', CORE, 2.6, .9)
    ppu = 110
    set_view('game', 2.4, 2.2, ppu, (0, 0, .55))
    set_glare(7, .4, 1.0)
    frames = []
    for f in range(frames_count):
        phase = f / frames_count
        z = .02 + .95 * phase
        r = math.sqrt(max(0.0, 1 - z * z))
        shimmer = ring('ShimmerRing', r, .03, shimmer_m, z=z, segments=96)
        set_gain(shimmer_m, math.sin(math.pi * phase))
        set_gain(lattice_m, .75 + .25 * math.sin(2 * math.pi * phase))
        frames.append((render_frame(.7), anchor_pixel((0, 0, 0))))
        bpy.data.objects.remove(shimmer, do_unlink=True)
        _objects.remove(shimmer)
    clear_effect()
    return ground, frames, ppu


def build_ultimate(seed=51):
    """Ultimate Vạn Tượng Thiên Cơ: two-tier sky formation (top view, drawn above the target), rain of feather beams
    with ground impacts (game view, one-shot), shockwave and target ring (top view)."""
    rnd = random.Random(seed)
    gold = line_material('UGold', GOLD, 1.25)
    pale = line_material('UPale', CORE2, 1.5)
    violet = line_material('UViolet', BAND, 1.4)
    set_glare(6, .45, .8)
    ring('U1', 1.0, .03, gold)
    ring('U2', .94, .01, pale)
    rune_band('URune', .87, .08, 20, rnd, line_material('URuneM', GOLD, 1.1, .9))
    ring('U3', .8, .014, violet)
    for i in range(12):
        a = 2 * math.pi * i / 12
        d = Vector((math.cos(a), math.sin(a), 0))
        s = Vector((-d.y, d.x, 0)) * .05
        polyline(f'URay{i}', [d * .8 + s, d * 1.12, d * .8 - s], .012, pale, steps=4)
    sky_outer = render_static('top', 2.4, 260)
    clear_effect()
    ring('V1', .62, .02, violet)
    star_polygon('Oct', .62, 8, 3, gold, .012)
    ring('V2', .34, .014, pale)
    glyph('UCentre', (0, 0, 0), .26, 0, rnd, gold)
    sky_inner = render_static('top', 1.4, 260)
    clear_effect()
    disc('UFill', 1.1, fill_material('UFill', .7), normal_axis='Z', segments=64)
    sky_fill = render_static('top', 2.35, 110, cover=1.0)
    clear_effect()
    ring('Wave', .9, .2, glow_material('WaveM', [(0, DEEP, 0), (.6, CORE2, 2.8), (.8, BAND, 1.4), (1, DEEP, 0)],
                                       [(0, 0), (.65, .95), (1, 0)], 1.0, tips=False))
    ring('Wave2', .72, .06, line_material('Wave2M', GOLD, 1.4, .8))
    shock = render_static('top', 2.1, 220)
    clear_effect()
    ring('T1', 1.0, .025, violet)
    ring('T2', .97, .008, pale)
    target = render_static('top', 2.2, 220)
    clear_effect()
    # Qi pouring down from the formation (spec §12 "tia năng lượng đổ xuống"), soft streams and not rays (user
    # 2026-10-21): each is a density column swollen in the middle like a feather, its noise flowing down; the front
    # falls, the tail drains into the ground and it dissolves. Own lean, width, strength and landing time (in loose
    # waves, Model3D_Rules 29); a soft flash where it lands.
    setup_volumes()
    hit_m = glow_material('BeamHit', [(0, CORE2, 1.2), (.45, BAND, .8), (1, DEEP, 0)], [(0, .65), (1, 0)], 1.0, tips=False)
    top_z = 2.4
    beams = []
    for i in range(26):
        r = math.sqrt(rnd.random()) * 1.1
        a = rnd.uniform(0, 2 * math.pi)
        x, y = math.cos(a) * r, math.sin(a) * r
        width = rnd.uniform(.09, .17)
        m = qi_column_material(f'Pour{i}', top_z, width * .8, width, rnd.uniform(-.12, .12), bulge=.5, seed=40 + i,
                               density_scale=5.0, emission=1.15)
        cage = volume_cage(f'PourCage{i}', m, width * 2.4 + .4, -.05, top_z + .6)
        cage.location = (x, y, 0)
        cage.rotation_euler = (rnd.uniform(-.1, .1), rnd.uniform(-.13, .13), rnd.uniform(0, 2 * math.pi))
        h = disc(f'Hit{i}', rnd.uniform(.3, .5), hit_m.copy(), location=(x, y - .01, .03), segments=24)
        wave = rnd.choice((0.0, .12, .26, .4))
        beams.append((cage, m, h, wave + rnd.uniform(0, .1), rnd.uniform(.75, 1.0), rnd.uniform(4.5, 6.5)))
    ppu = 100
    set_view('game', 3.0, 3.6, ppu, (0, 0, 1.15))
    set_glare(7, .45, .9)
    frames = []
    fall_time = .16
    for f in range(30):
        t = f / 30.0
        for cage, m, h, delay, gain, speed in beams:
            age = t - delay
            fall = 1 - (1 - min(1.0, max(0.0, age / fall_time))) ** 1.6
            land = age - fall_time
            fade = 0.0 if age < 0 else (1 - min(1.0, max(0.0, land) / .32)) ** 1.2
            # Front falls from the formation; once landed the tail drains down into the ground.
            set_values(m, Low=top_z * (1 - fall) - .1, High=top_z + .2 - max(0.0, land) * speed * .6,
                       Flow=-max(0.0, age) * speed, Gain=gain * fade)
            cage.hide_render = fade <= 0
            h.scale = Vector((1, 1, 1)) * (.8 + .9 * min(1, max(0, land) / .2))
            # A soft splash that swells in (no bright point at the moment of landing).
            set_gain(h.data.materials[0], 0 if land < 0 else min(1.0, land / .07) * max(0, 1 - land / .35))
        frames.append((render_frame(.75), anchor_pixel((0, 0, 0))))
    clear_effect()
    return sky_outer, sky_inner, sky_fill, shock, target, frames, ppu


def build_ultimate_aura(frames_count=24, seed=61):
    """Ultimate f31–45 (spec §12 "vòng ma pháp xuất hiện phía sau"): an upright rune circle behind Mặc Ảnh, drawn
    in the screen plane. Formation geometry may be regular (Model3D_Rules 29); it turns 90° per loop (a common
    period of its 20 runes and the octagram), so the loop is seamless."""
    rnd = random.Random(seed)
    gold = line_material('AGold', GOLD, 1.6)
    pale = line_material('APale', CORE2, 1.7)
    violet = line_material('AViolet', BAND, 1.5)
    before = len(_objects)
    ring('AR1', .75, .022, gold)
    ring('AR2', .7, .008, pale)
    rune_band('ARune', .64, .07, 20, rnd, line_material('ARuneM', GOLD, 1.3, .9))
    ring('AR3', .56, .012, violet)
    star_polygon('AOct', .56, 8, 3, pale, .01)
    ring('AR4', .24, .014, gold)
    disc('AFill', .8, fill_material('AFill', .22), normal_axis='Z', segments=64)
    parts = _objects[before:]
    pivot = bpy.data.objects.new('AuraPivot', None)
    scene.collection.objects.link(pivot)
    _objects.append(pivot)
    for o in parts:
        o.parent = pivot
    pivot.rotation_euler = (math.radians(90), 0, 0)      # stand the circle up, facing the front camera
    ppu = 110
    set_view('front', 1.7, 1.7, ppu)
    set_glare(6, .45, .9)
    frames = []
    for f in range(frames_count):
        pivot.rotation_euler = (math.radians(90), math.radians(90) * f / frames_count, 0)
        frames.append((render_frame(.75), anchor_pixel((0, 0, 0))))
    clear_effect()
    return frames, ppu


def build_dodge_streak(frames_count=12, seed=71):
    """DodgeAttack f8–13 (spec §13 "vệt thân lướt"): a short smoky violet streak trailing the dash (+X = the
    direction of travel's opposite, the game rotates it), with a few motes. Noise smoke, no drawn lines."""
    rnd = random.Random(seed)
    smoke = smoke_ribbon_material('StreakSmoke', 1.6, scale=(2.6, 1.6), contrast=(.34, .7), lead=None)
    n = 40
    pts = [Vector((-.1 + 1.1 * t, 0, .5 + .08 * math.sin(math.pi * t))) for t in np.linspace(0, 1, n)]
    ribbon('Streak', pts, [.85 * math.sin(math.pi * min(1.0, .15 + t * .95)) ** .8 * (1 - .55 * t) + .04 for t in np.linspace(0, 1, n)], Vector((0, 1, 0)), smoke)
    wisp = smoke_ribbon_material('StreakWisp', 1.1, scale=(4.0, 2.2), contrast=(.42, .76), lead=None)
    pts2 = [Vector((.1 + 1.0 * t, -.01, .78 - .1 * t)) for t in np.linspace(0, 1, n)]
    ribbon('Wisp', pts2, [.42 * math.sin(math.pi * min(1.0, .1 + t)) + .03 for t in np.linspace(0, 1, n)], Vector((0, 1, 0)), wisp)
    mote = glow_material('SMote', [(0, CORE, 5), (1, BAND, 0)], [(0, 1), (1, 0)], 1.0, tips=False)
    motes = [(disc(f'SMote{i}', rnd.uniform(.01, .022), mote, segments=8), rnd.random(), rnd.uniform(.15, .95), rnd.uniform(.2, .9))
             for i in range(12)]
    ppu = 110
    set_view('front', 1.5, 1.2, ppu, (.45, 0, .5))
    set_glare(6, .4, 1.0)
    frames = []
    for f in range(frames_count):
        phase = f / frames_count
        for m in bpy.data.materials:
            set_phase(m, phase)
        for o, start, z, travel in motes:
            t = (start + phase) % 1
            o.location = (t * travel + .05, -.02, z)
            set_gain(o.data.materials[0], math.sin(math.pi * t))
        frames.append((render_frame(.85), anchor_pixel((0, 0, .5))))
    clear_effect()
    return frames, ppu


def magic_array_layers():
    outer, inner, fill, spikes, ppu_top, ppu = build_magic_array()
    # One pixels-per-unit per effect (importer rule): the top views were drawn at ppu_top, so their layer scale
    # carries the ratio and the importer uses the game-view ppu for every page.
    k = ppu / ppu_top
    common = dict(plane='Ground', loop=False, framesPerSecond=1)
    return [
        layer('ChargeFill', fill, ppu, phase='Charge', fadeIn=.25, fadeOut=0, glow=.7, underlay=.85, sortingOffset=0,
              scaleFrom=1.2 * ppu / 120, scaleTo=.95 * ppu / 120, **common),
        layer('ChargeOuter', outer, ppu, phase='Charge', fadeIn=.25, fadeOut=0, spin=60, scaleFrom=1.25 * k, scaleTo=.95 * k,
              sortingOffset=1, **common),
        layer('ChargeInner', inner, ppu, phase='Charge', fadeIn=.3, fadeOut=0, spin=-90, scaleFrom=1.3 * k, scaleTo=.9 * k,
              sortingOffset=2, **common),
        layer('Fill', fill, ppu, fadeIn=.05, fadeOut=.35, glow=.7, underlay=.85, scaleFrom=ppu / 120, scaleTo=ppu / 120,
              sortingOffset=0, **common),
        layer('Outer', outer, ppu, fadeIn=.05, fadeOut=.35, spin=18, scaleFrom=.95 * k, scaleTo=1 * k, sortingOffset=1, **common),
        layer('Inner', inner, ppu, fadeIn=.05, fadeOut=.35, spin=-28, scaleFrom=.9 * k, scaleTo=1 * k, sortingOffset=2, **common),
        layer('Spikes', spikes, ppu, framesPerSecond=30, start=0, end=.82, fadeIn=0, fadeOut=.15, stretchToGround=True, glow=1.2,
              underlay=.7, sortingOffset=3),
    ]


def cyclone_layers():
    vortex, column, ppu = build_cyclone()
    k = ppu / 260
    return [
        layer('Vortex', vortex, ppu, plane='Ground', framesPerSecond=1, spin=-240, fadeIn=.2, fadeOut=.5, scaleFrom=.7 * k,
              scaleTo=1 * k, glow=1.0, underlay=.7),
        layer('Column', column, ppu, framesPerSecond=20, loop=True, fadeIn=.35, fadeOut=.5, glow=1.25, underlay=.95, sortingOffset=1),
    ]


def shield_layers():
    ground, dome, ppu = build_shield()
    k = ppu / 260
    return [
        layer('Runes', ground, ppu, plane='Ground', framesPerSecond=1, spin=22, fadeIn=.2, fadeOut=.25, scaleFrom=1 * k,
              scaleTo=1 * k, underlay=.8),
        layer('Dome', dome, ppu, framesPerSecond=16, loop=True, fadeIn=.25, fadeOut=.25, glow=1.0, underlay=.8, sortingOffset=1),
    ]


def ultimate_layers():
    sky_outer, sky_inner, sky_fill, shock, target, beams, ppu = build_ultimate()
    k260, k110, k220 = ppu / 260, ppu / 110, ppu / 220
    sky = 2.4 * math.sqrt(1 - PROJECTION ** 2)      # where the beams start on screen, above the target point
    g = dict(plane='Ground', framesPerSecond=1)
    return [
        layer('ChargeTarget', target, ppu, phase='Charge', fadeIn=.3, fadeOut=0, spin=12, scaleFrom=1 * k220, scaleTo=1 * k220,
              underlay=.75, **g),
        layer('ChargeSkyFill', sky_fill, ppu, phase='Charge', height=sky, fadeIn=.4, fadeOut=0, scaleFrom=1.3 * k110 * 1.2,
              scaleTo=1 * k110 * 1.2, glow=.8, underlay=.85, sortingOffset=10, **g),
        layer('ChargeSkyOuter', sky_outer, ppu, phase='Charge', height=sky, fadeIn=.4, fadeOut=0, spin=40, scaleFrom=1.35 * k260 * 1.2,
              scaleTo=1 * k260 * 1.2, sortingOffset=11, **g),
        layer('ChargeSkyInner', sky_inner, ppu, phase='Charge', height=sky, fadeIn=.5, fadeOut=0, spin=-70, scaleFrom=1.4 * k260 * 1.2,
              scaleTo=1 * k260 * 1.2, sortingOffset=12, **g),
        layer('SkyFill', sky_fill, ppu, height=sky, end=1.0, fadeIn=0, fadeOut=.5, scaleFrom=1 * k110 * 1.2, scaleTo=1.1 * k110 * 1.2, glow=.8,
              underlay=.5, sortingOffset=10, **g),
        layer('SkyOuter', sky_outer, ppu, height=sky, end=1.0, fadeIn=0, fadeOut=.5, spin=40, scaleFrom=1 * k260 * 1.2, scaleTo=1.1 * k260 * 1.2,
              sortingOffset=11, **g),
        layer('SkyInner', sky_inner, ppu, height=sky, end=1.0, fadeIn=0, fadeOut=.5, spin=-70, scaleFrom=1 * k260 * 1.2, scaleTo=1.1 * k260 * 1.2,
              sortingOffset=12, **g),
        layer('Beams', beams, ppu, framesPerSecond=30, end=1.0, fadeIn=0, fadeOut=.2, stretchToGround=True, glow=1.15, underlay=.7,
              sortingOffset=13),
        layer('Shockwave', shock, ppu, start=.35, end=1.0, fadeIn=.04, fadeOut=.4, scaleFrom=.25 * k220, scaleTo=1.45 * k220,
              underlay=.5, **g),
        layer('Target', target, ppu, end=0, fadeIn=0, fadeOut=.4, spin=12, scaleFrom=1 * k220, scaleTo=1 * k220, underlay=.75, **g),
    ]


EFFECTS = {
    'TripleFeather': lambda: [layer('Feather', *build_feather_blade(), framesPerSecond=24, loop=True, fadeIn=.04, fadeOut=.08,
                                    glow=1.15, underlay=.85)],
    'MagicArray': magic_array_layers,
    # Upright behind the caster: height lifts the circle to the chest (1 m of height = 0.66 on screen in the 3/4 view).
    'UltimateAura': lambda: [layer('Circle', *build_ultimate_aura(), framesPerSecond=12, loop=True, fadeIn=.35, fadeOut=.5,
                                   height=.66, glow=1.0, underlay=.9, scaleFrom=.6, scaleTo=1.0)],
    'DodgeStreak': lambda: [layer('Streak', *build_dodge_streak(), framesPerSecond=24, loop=True, fadeIn=.03, fadeOut=.22,
                                  height=.33, glow=1.1, underlay=.8)],
    'Cyclone': cyclone_layers,
    'Shield': shield_layers,
    'Ultimate': ultimate_layers,
    'WindSlash': lambda: [layer('Blade', *build_wind_slash(2.2), framesPerSecond=24, loop=True, fadeIn=.05, fadeOut=.08,
                                glow=1.15, underlay=.85)],
    'CounterSlash': lambda: [layer('Blade', *build_wind_slash(1.5, seed=5), framesPerSecond=24, loop=True, fadeIn=.04,
                                   fadeOut=.08, glow=1.15, underlay=.85)],
    'Impact': lambda: [layer('Burst', *build_impact(), framesPerSecond=30, loop=False, fadeIn=0, fadeOut=.12, underlay=.6)],
}

# Radius the battle passes for 'scale 1' of each effect (Impact is spawned at 0.5: draw it at 1.6x).
REFERENCE_RADIUS = {'Impact': .3, 'Shield': .55, 'UltimateAura': .62, 'DodgeStreak': .5}
# Cinematic dim of the battlefield while the effect plays (see MacAnhVfxRecipe.screenDim); 0 / missing = off.
SCREEN_DIM = {'Ultimate': {'screenDim': .6, 'dimFadeIn': .35, 'dimFadeOut': .5, 'dimColour': [.06, .02, .12, 1]}}

for name, build in EFFECTS.items():
    if ONLY and name not in ONLY:
        continue
    print('BUILD', name, flush=True)
    layers = build()
    # ppu travels with each layer for the importer.
    write_effect(name, layers, REFERENCE_RADIUS.get(name, 1.0))
print('VFX_DONE', flush=True)
