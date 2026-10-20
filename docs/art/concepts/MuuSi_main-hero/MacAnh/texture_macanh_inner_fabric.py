"""Gives Mặc Ảnh's added inner cloth (InnerRobe, sleeve linings, wrist / boot interiors) a fabric look.

blender -b <Portrait>.blend --python texture_macanh_inner_fabric.py -- [--save <out>.blend]

Why: the under-layers that close holes (material slots 2..8) are flat grey Principled materials; when an arm lifts
(Ultimate, Skill02) they read as blank patches beside the textured silk. The source atlas is too fragmented to borrow
a calm silk window for pieces this size (tried 2026-10-20: largest clean window ~88 texels), so each inner material
becomes a procedural silk in the concept's colours (images/a_vertical_character_concept_sheet_game_artwork.png):
white silk under the arms, deep purple sleeve lining, darker wrist interiors, black boot interiors. The look is a
soft two-tone noise (no hard pattern); folds darken through the renderer's own shading. No AO node and no weave
bump: at the sprite exporter's 8 Eevee samples the AO node rendered as grain and the weave as moiré (tested
2026-10-20, Logs/ClaudeMacAnh/InnerFabric/MauThu_8-64-anLot.png). Geometry, weights, UVs and the original body
material are untouched.
Colours are sRGB hex here (concept palette) and converted to linear for the shader.
"""
import bpy
import sys

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
SAVE = argv[argv.index('--save') + 1] if '--save' in argv else ''

# material name suffix -> (light colour, shadow colour, roughness, sheen)
LOOKS = {
    'InnerRobe': ('#EEEAF3', '#C9C0D8', 0.55, 0.35),
    'RightSleeveLining': ('#6B4A9E', '#3E2763', 0.6, 0.3),
    'LeftSleeveLining': ('#6B4A9E', '#3E2763', 0.6, 0.3),
    'RightWristInterior': ('#4A3270', '#24173A', 0.7, 0.2),
    'LeftWristInterior': ('#4A3270', '#24173A', 0.7, 0.2),
    'RightBootInterior': ('#26222B', '#100E13', 0.8, 0.0),
    'LeftBootInterior': ('#26222B', '#100E13', 0.8, 0.0),
}


def linear(hex_colour):
    c = [int(hex_colour[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)


rig = bpy.data.objects['MacAnh_Rig']
body = bpy.data.objects[rig.get('source_body_object', 'Mesh_0.001')]
changed = []
for slot in body.material_slots[2:]:
    m = slot.material
    if not m:
        continue
    key = next((k for k in LOOKS if m.name.endswith(k)), None)
    if key is None:
        print('SKIP', m.name, flush=True)
        continue
    light, shadow, rough, sheen = LOOKS[key]
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    for n in [n for n in nt.nodes if n.name.startswith('Fabric_')]:
        nt.nodes.remove(n)

    def node(kind, name, x, y):
        n = nt.nodes.new(kind)
        n.name = 'Fabric_' + name
        n.location = (bsdf.location.x + x, bsdf.location.y + y)
        return n

    coord = node('ShaderNodeTexCoord', 'Coord', -1400, 0)
    noise = node('ShaderNodeTexNoise', 'Noise', -1150, 150)
    noise.inputs['Scale'].default_value = 18.0
    noise.inputs['Detail'].default_value = 3.0
    noise.inputs['Roughness'].default_value = 0.45
    nt.links.new(coord.outputs['Object'], noise.inputs['Vector'])
    ramp = node('ShaderNodeValToRGB', 'Tone', -900, 150)
    ramp.color_ramp.elements[0].position = 0.3
    ramp.color_ramp.elements[0].color = linear(shadow)
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[1].color = linear(light)
    nt.links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = rough
    if 'Sheen Weight' in bsdf.inputs:
        bsdf.inputs['Sheen Weight'].default_value = sheen
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.3
    changed.append(m.name)
    print('FABRIC', m.name, light, shadow, flush=True)

if SAVE:
    bpy.ops.wm.save_as_mainfile(filepath=SAVE)
    print('SAVED', SAVE, flush=True)
print('INNER_FABRIC_DONE', len(changed), flush=True)
