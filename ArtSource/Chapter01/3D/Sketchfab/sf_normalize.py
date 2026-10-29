"""Blender 4.5: immutable glTF sources -> metre FBXs, material manifest and QA blends.
Usage: blender -b --factory-startup --python-exit-code 1 --python this.py -- [SF01,SF02]
"""
import bpy, bmesh, json, math, re, shutil, sys, hashlib
from pathlib import Path
from mathutils import Matrix, Vector
import numpy as np

ROOT = Path(__file__).resolve().parents[4]
CONFIG = json.loads(Path(__file__).with_suffix('.json').read_text(encoding='utf-8'))
SOURCES = ROOT / 'ArtSource/Chapter01/3D/Sources/Sketchfab'
OUT = ROOT / 'Assets/ThirdParty/Chapter01/Sketchfab'
QA = ROOT / 'ArtSource/Chapter01/3D/Renders/Sketchfab'

def bounds(obs):
    points = [o.matrix_world @ v.co for o in obs for v in o.data.vertices]
    if not points: raise ValueError('Empty selection')
    return Vector([min(p[i] for p in points) for i in range(3)]), Vector([max(p[i] for p in points) for i in range(3)])

def signature(o): return o.get('source_name', o.name) + '|' + '|'.join(m.name for m in o.data.materials if m)
def tris(obs): return sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in obs)
def copy_object(o, suffix):
    n = o.copy(); n.data = o.data.copy(); n.name = o.name + suffix
    bpy.context.scene.collection.objects.link(n)
    return n

def split_pieces(obs, config):
    if 'cluster' not in config:
        result = []
        for spec in config['pieces']:
            selected = [o for o in obs if re.search(spec.get('include', '.*'), signature(o)) and not re.search(spec.get('exclude', '(?!)'), signature(o))]
            if not selected: raise ValueError('No meshes for ' + spec['name'])
            result.append((spec, selected))
        claimed = [o.name for _, objects in result for o in objects]
        if len(claimed) != len(set(claimed)): raise ValueError('Piece selectors overlap')
        return result
    # glTF parent nodes already identify complete stems with their branch/leaf primitives.
    # Canopy AABBs overlap adjacent examples: merging them would join separate trees.
    groups = {}
    for o in obs: groups.setdefault(o['source_parent'], []).append(o)
    result, counts = [], {}
    for parent, objects in sorted(groups.items()):
        lo, hi = bounds(objects)
        kind = 'Tall' if hi.z-lo.z > config['cluster']['tall'] else 'Clump' if any(re.search(config['cluster']['leaves'], signature(o)) for o in objects) else 'Stem'
        counts[kind] = counts.get(kind, 0) + 1
        if counts[kind] > config['cluster']['max_per_kind'][kind]: continue
        result.append(({'name': kind + '%02d' % counts[kind]}, objects))
    return result

def image_for(socket, seen=None):
    seen = set() if seen is None else seen
    for link in socket.links:
        node = link.from_node
        if node in seen: continue
        seen.add(node)
        if node.type == 'TEX_IMAGE': return node.image
        for inp in node.inputs:
            found = image_for(inp, seen)
            if found: return found
    return None

def save_textures(code, materials):
    folder = OUT / code / 'Textures'; folder.mkdir(parents=True, exist_ok=True)
    source_materials = {m.get('name',''): m for m in json.loads((SOURCES/code/'scene.gltf').read_text(encoding='utf-8')).get('materials',[])}
    paths, report = {}, []
    images = set(n.image for m in materials if m.use_nodes for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image)
    for index, im in enumerate(sorted(images, key=lambda i: i.name)):
        src = Path(bpy.path.abspath(im.filepath)); before = list(im.size)
        name = re.sub(r'[^\w.-]', '_', im.name)
        if not Path(name).suffix: name += '.png'
        dest = folder / ('%02d_' % index + name)
        if max(before) > CONFIG['max_texture']:
            factor = CONFIG['max_texture'] / max(before)
            w,h = max(1, round(before[0]*factor)), max(1, round(before[1]*factor))
            im.scale(w,h)
            resized = bpy.data.images.new(im.name + '_1024', w, h, alpha=True)
            resized.colorspace_settings.name = im.colorspace_settings.name
            resized.pixels = im.pixels[:]
            dest = dest.with_suffix('.png'); resized.filepath_raw = str(dest); resized.file_format = 'PNG'; resized.save()
            for mat in materials:
                for node in mat.node_tree.nodes:
                    if node.type == 'TEX_IMAGE' and node.image == im: node.image = resized
            im = resized
        elif src.is_file(): shutil.copyfile(src, dest)
        else:
            dest = dest.with_suffix('.png'); im.filepath_raw = str(dest); im.file_format = 'PNG'; im.save()
        if max(im.size) > CONFIG['max_texture']: raise ValueError('Texture size was not reduced: ' + im.name)
        paths[im] = dest.relative_to(ROOT).as_posix()
        report.append({'image': im.name, 'before': before, 'after': list(im.size), 'path': paths[im]})
    result = []
    for m in sorted(materials, key=lambda m: m.name):
        p = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if m.use_nodes else None
        if not p: raise ValueError('Missing PBR node: ' + m.name)
        base = image_for(p.inputs['Base Color']); normal = image_for(p.inputs['Normal'])
        emission = image_for(p.inputs['Emission Color'])
        authored = source_materials.get(m.name, {})
        pbr = authored.get('pbrMetallicRoughness', {})
        specgloss = authored.get('extensions', {}).get('KHR_materials_pbrSpecularGlossiness', {})
        color = pbr.get('baseColorFactor', specgloss.get('diffuseFactor', [1,1,1,1]))
        metal = image_for(p.inputs['Metallic']); rough = image_for(p.inputs['Roughness'])
        alpha = p.inputs['Alpha'].is_linked or p.inputs['Alpha'].default_value < .999
        packed_path = ''; packed_audit = None
        metal_override = next((value for regex,value in CONFIG['sources'][code].get('metallic_override',{}).items() if re.search(regex,m.name)),None)
        if metal or rough:
            sample = metal or rough; w, h = sample.size
            px = np.ones((h*w, 4), dtype=np.float32)
            px[:, 0] = p.inputs['Metallic'].default_value
            px[:, 3] = 1-p.inputs['Roughness'].default_value
            # glTF metallic B / roughness G -> Unity metallic R / smoothness A.
            if metal: px[:, 0] = np.asarray(metal.pixels[:]).reshape(-1, 4)[:, 2] * pbr.get('metallicFactor',1)
            if rough:
                if list(rough.size) != [w,h]: raise ValueError('PBR map dimensions differ')
                px[:, 3] = 1-np.asarray(rough.pixels[:]).reshape(-1, 4)[:, 1] * pbr.get('roughnessFactor',1)
            if metal_override is not None: px[:,0] = metal_override
            packed = bpy.data.images.new('Unity_' + m.name, w, h, alpha=True)
            packed.colorspace_settings.name = 'Non-Color'; packed.pixels = px.ravel()
            dest = folder / (re.sub(r'[^\w.-]', '_', m.name) + '_MetalSmooth.png')
            packed.filepath_raw = str(dest); packed.file_format = 'PNG'; packed.save()
            packed_path = dest.relative_to(ROOT).as_posix(); bpy.data.images.remove(packed)
            readback = bpy.data.images.load(str(dest),check_existing=False);readback.colorspace_settings.name='Non-Color'
            actual=np.asarray(readback.pixels[:]).reshape(-1,4)
            error=float(np.max(np.abs(actual[:,[0,3]]-px[:,[0,3]])))
            if error>1.1/255: raise ValueError('Packed material channels differ from glTF factors: '+m.name)
            packed_audit={'metallicMean':float(actual[:,0].mean()),'smoothnessMean':float(actual[:,3].mean()),'maxQuantizationError':error,
                          'sourceMetallicFactor':pbr.get('metallicFactor',1),'sourceRoughnessFactor':pbr.get('roughnessFactor',1),'metallicOverride':metal_override}
            bpy.data.images.remove(readback)
        if metal_override is not None:
            for link in list(p.inputs['Metallic'].links): m.node_tree.links.remove(link)
            p.inputs['Metallic'].default_value=metal_override
        result.append({'name': m.name, 'baseMap': paths.get(base, ''), 'normalMap': paths.get(normal, ''), 'metalMap': packed_path,
                       'color': color, 'metallic': float(p.inputs['Metallic'].default_value),
                       'emissionMap': paths.get(emission,''), 'emission': authored.get('emissiveFactor',[0,0,0]),
                       'smoothness': 1-float(p.inputs['Roughness'].default_value), 'cutout': alpha,
                       'cutoff': authored.get('alphaCutoff',.5), 'doubleSided': not m.use_backface_culling, 'packedAudit': packed_audit})
    return result, report

def normalize(code, config):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source = SOURCES/code/'scene.gltf'
    bpy.ops.import_scene.gltf(filepath=str(source)); bpy.context.view_layer.update()
    obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in obs:
        o['source_name'] = o.name; o['source_parent'] = o.parent.name if o.parent else o.name
        world = o.matrix_world.copy(); o.data = o.data.copy(); o.data.transform(world)
        o.parent = None; o.matrix_world = Matrix.Identity(4)
    before_tris = tris(obs); ground_removed = 0
    original_ground = {o: o.data.copy() for o in obs} if config.get('drop_ground') else {}
    if config.get('drop_ground'):
        lo, hi = bounds(obs); ground = config['drop_ground']
        ceiling = lo.z + (hi.z-lo.z)*ground['height_fraction']
        center = (lo+hi)/2; half = (hi-lo)/2
        for o in obs:
            bm = bmesh.new(); bm.from_mesh(o.data)
            faces = [f for f in bm.faces if all(v.co.z <= ceiling for v in f.verts)
                     and any(abs(v.co.x-center.x) >= half.x*ground['outer_fraction'] or abs(v.co.y-center.y) >= half.y*ground['outer_fraction'] for v in f.verts)]
            ground_removed += len(faces); bmesh.ops.delete(bm, geom=faces, context='FACES')
            bm.to_mesh(o.data); bm.free(); o.data.update()
        if ground_removed != ground['expected_faces']: raise ValueError('Unexpected ground face count: ' + str(ground_removed))
    pieces = split_pieces(obs, config)
    ref = next((objects for spec, objects in pieces if spec['name'] == config['scale'].get('ref')), pieces[0][1])
    lo, hi = bounds(ref); size = hi-lo; scaling = config['scale']
    scale = scaling.get('factor', scaling.get('height', 1)/max(size.z, 1e-12))
    if 'footprint' in scaling: scale = scaling['footprint']/max(size.x, size.y)
    used = {o for _, objects in pieces for o in objects}
    omitted = [o.name for o in obs if o not in used]
    output = OUT/code; output.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(SOURCES/code/'license.txt', output/'license.txt')
    material_set = {m for o in used for m in o.data.materials if m}
    # Preserve original full resolution material/image state in the Before collection.
    before_mats = {}
    for m in material_set:
        clone = m.copy(); before_mats[m] = clone
        for node in clone.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                node.image = node.image.copy(); node.image.pack()
    rows = []; baselines = []
    for spec, objects in pieces:
        lo, hi = bounds(objects); pivot = Vector(((lo.x+hi.x)/2, (lo.y+hi.y)/2, hi.z if spec.get('pivot') == 'top' else lo.z))
        for o in objects: o.data.transform(Matrix.Scale(scale,4) @ Matrix.Translation(-pivot)); o.data.update()
        baselines_for_piece = []
        for o in objects:
            baseline = copy_object(o, '_before'); baseline['piece'] = spec['name']; baseline['state'] = 'before'
            if o in original_ground:
                baseline.data = original_ground[o].copy()
                baseline.data.transform(Matrix.Scale(scale,4) @ Matrix.Translation(-pivot))
            for i, m in enumerate(baseline.data.materials): baseline.data.materials[i] = before_mats[m]
            baselines_for_piece.append(baseline)
        baselines += baselines_for_piece
        n_before = tris(objects); removed_degenerate = 0
        for o in objects:
            for regex, ratio in spec.get('decimate', {}).items():
                if re.search(regex, signature(o)):
                    bpy.context.view_layer.objects.active = o
                    mod = o.modifiers.new('Mobile reduction', 'DECIMATE'); mod.ratio = ratio
                    bpy.ops.object.modifier_apply(modifier=mod.name)
            bm = bmesh.new(); bm.from_mesh(o.data)
            # Use double precision cross products, as in the export audit: BMesh's single
            # precision area can leave exactly collinear source triangles behind.
            bad = []
            for f in bm.faces:
                points = np.asarray([v.co[:] for v in f.verts], dtype=np.float64)
                area = sum(np.linalg.norm(np.cross(points[i]-points[0],points[i+1]-points[0])) for i in range(1,len(points)-1))
                if area < 1e-12: bad.append(f)
            removed_degenerate += len(bad)
            bmesh.ops.delete(bm, geom=bad, context='FACES')
            bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
            bm.to_mesh(o.data); bm.free(); o.data.update()
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects: o.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        if len(objects) > 1: bpy.ops.object.join()
        mesh = bpy.context.object; mesh.name = code+'_'+spec['name']; mesh['piece'] = spec['name']; mesh['state'] = 'after'
        lo, hi = bounds([mesh]); mesh.data.calc_loop_triangles()
        coords = np.array([v.co[:] for v in mesh.data.vertices]); faces = np.array([t.vertices[:] for t in mesh.data.loop_triangles])
        area = np.linalg.norm(np.cross(coords[faces[:,1]]-coords[faces[:,0]], coords[faces[:,2]]-coords[faces[:,0]]),axis=1)
        degenerate = int(np.count_nonzero(area < 1e-12))
        if not np.isfinite(coords).all(): raise ValueError('Nonfinite vertices')
        row = {'code': code, 'group': config['group'], 'name': mesh.name, 'piece': spec['name'], 'scale': scale, 'usage': spec.get('usage','CANDIDATE_REQUIRES_APPROVAL'),
               'pivot': spec.get('pivot','bottom'), 'trianglesBefore': n_before, 'triangles': tris([mesh]),
               'boundsMin': list(lo), 'boundsMax': list(hi), 'size': list(hi-lo), 'degenerate': degenerate,
               'finite': True, 'removedDegenerate': removed_degenerate, 'uvLayers': len(mesh.data.uv_layers), 'materials': [m.name for m in mesh.data.materials if m]}
        rows.append(row)
    # Remove unused meshes only in this generated scene, never source files.
    for o in list(bpy.context.scene.objects):
        if o.type != 'MESH' or 'state' not in o: bpy.data.objects.remove(o, do_unlink=True)
    materials, textures = save_textures(code, material_set)
    for row in rows:
        mesh = bpy.data.objects[row['name']]
        bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); bpy.context.view_layer.objects.active = mesh
        path = output/(row['name']+'.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                                 bake_space_transform=True, apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
                                 mesh_smooth_type='OFF', path_mode='RELATIVE', bake_anim=False)
        row['path'] = path.relative_to(ROOT).as_posix()
        row['cutout'] = any(m['cutout'] and m['name'] in row['materials'] for m in materials)
    QA.mkdir(parents=True, exist_ok=True)
    for o in bpy.context.scene.objects: o.hide_render = True
    bpy.ops.wm.save_as_mainfile(filepath=str(QA/(code+'_qa.blend')))
    report = {'code': code, 'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'sourceTriangles': before_tris,
              'groundFacesRemoved': ground_removed, 'omittedObjects': omitted, 'pieces': rows, 'materials': materials, 'textures': textures}
    (output/'manifest.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print('NORMALIZED', code, len(rows), flush=True)
    return report

def main():
    args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    only = args[0].split(',') if args else CONFIG['sources']
    for code in only: normalize(code, CONFIG['sources'][code])
    manifests = [json.loads((OUT/code/'manifest.json').read_text(encoding='utf-8')) for code in CONFIG['sources'] if (OUT/code/'manifest.json').exists()]
    rows = [p for m in manifests for p in m['pieces']]
    (OUT/'pieces.tsv').write_text('group\tname\tpath\tcutout\tusage\n' + ''.join(f"{p['group']}\t{p['name']}\t{p['path']}\t{str(p['cutout']).lower()}\t{p.get('usage','CANDIDATE_REQUIRES_APPROVAL')}\n" for p in rows), encoding='utf-8')
    (QA/'normalization.json').write_text(json.dumps(manifests,ensure_ascii=False,indent=2),encoding='utf-8')
    print('NORMALIZE_DONE', len(manifests), len(rows), flush=True)

if __name__ == '__main__': main()
