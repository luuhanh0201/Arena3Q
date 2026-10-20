"""Repacks the exported Lu Bu atlases into pages of at most PAGE x PAGE pixels (default 2048), without re-rendering.

blender -b --factory-startup --python RepackLuBuPages.py -- [--page 2048] [--preview <dir>]

Why: ExportSmoothSprites.py used to pack each clip on one 4096-wide page; 2048 pages are what every Android/iOS GPU
takes and match Mac Anh. Sprites keep their pixels, size and pivot; only their page and position change. Each moved
sprite is read back from the written page and compared with the original crop (must match to 1/255). frames.json and
animations.json are rewritten; the source_stamp is kept, so the manifest still matches the blend that rendered it.
"""
import bpy
import json
import math
import os
import sys
import numpy as np

sys.stdout.reconfigure(line_buffering=True)
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def arg(name, default, cast=str):
    return cast(argv[argv.index(name) + 1]) if name in argv else default


PAGE = arg('--page', 2048, int)
PREVIEW = arg('--preview', '')
GAP = 4
root = os.path.dirname(os.path.abspath(__file__))
project = os.path.abspath(os.path.join(root, '../../../..'))
art = os.path.join(project, 'Assets/Arena3Q/Art/Heroes/LuBu/Smooth')
manifest_path = os.path.join(art, 'animations.json')
manifest = json.load(open(manifest_path, encoding='utf-8'))


def load(path):
    image = bpy.data.images.load(path, check_existing=False)
    w, h = image.size
    a = np.empty(w * h * 4, dtype=np.float32)
    image.pixels.foreach_get(a)
    bpy.data.images.remove(image)
    return a.reshape(h, w, 4)


def save(pixels, path):
    h, w = pixels.shape[:2]
    image = bpy.data.images.new('Repack', width=w, height=h, alpha=True)
    image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw = path
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


previews = {}
for clip in manifest['clips']:
    name = clip['name']
    clip_path = os.path.join(art, name + '.frames.json')
    entry = json.load(open(clip_path, encoding='utf-8'))
    old_pages = [load(os.path.join(art, p)) for p in entry['pages']]
    if all(max(p.shape[:2]) <= PAGE for p in old_pages):
        print('REPACK_SKIP', name, 'already within', PAGE, flush=True)
        continue
    crops = [old_pages[s['page']][s['y']:s['y'] + s['height'], s['x']:s['x'] + s['width']].copy() for s in entry['sprites']]
    # Shelf packing in the original order (direction-major), the same rules as the exporter.
    layout, pages_used = [], []
    page_index = x = y = row = used = 0
    for s in entry['sprites']:
        w, h = s['width'], s['height']
        if x + w + GAP > PAGE:
            x = 0; y += row + GAP; row = 0
        if y + h + GAP > PAGE:
            pages_used.append(used); page_index += 1; x = y = row = used = 0
        layout.append((page_index, x, y))
        x += w + GAP; row = max(row, h); used = max(used, y + h + GAP)
    pages_used.append(used)
    new_pages = [np.zeros((min(PAGE, max(4, int(math.ceil(u / 4)) * 4)), PAGE, 4), dtype=np.float32) for u in pages_used]
    for s, crop, (p, px, py) in zip(entry['sprites'], crops, layout):
        new_pages[p][py:py + s['height'], px:px + s['width']] = crop
    names = [f'{name}_p{i:02d}.png' for i in range(len(new_pages))]
    for pixels, file in zip(new_pages, names):
        save(pixels, os.path.join(art, file))
    # Read back and compare every sprite with its original crop.
    written = [load(os.path.join(art, f)) for f in names]
    worst = 0.0
    for s, crop, (p, px, py) in zip(entry['sprites'], crops, layout):
        worst = max(worst, float(np.abs(written[p][py:py + s['height'], px:px + s['width']] - crop).max()))
    if worst > 1.5 / 255:
        raise RuntimeError(f'{name}: repacked pixels differ by {worst * 255:.1f}/255')
    for stale in set(entry['pages']) - set(names):
        for f in (stale, stale + '.meta'):
            if os.path.exists(os.path.join(art, f)):
                os.remove(os.path.join(art, f))
    old_spots = [(s['page'], s['x'], s['y'], s['width'], s['height']) for s in entry['sprites']]
    for s, (p, px, py) in zip(entry['sprites'], layout):
        s['page'], s['x'], s['y'] = p, px, py
    old_shapes = [p.shape[1::-1] for p in old_pages]
    entry['pages'] = names
    temporary = clip_path + '.tmp'
    with open(temporary, 'w', encoding='utf-8') as f:
        json.dump(entry, f, indent=2)
    os.replace(temporary, clip_path)
    clip.update(entry)
    previews[name] = (old_pages, written, old_spots, layout)
    print('REPACKED', name, 'pages', [f'{w}x{h}' for w, h in old_shapes], '->',
          [f'{p.shape[1]}x{p.shape[0]}' for p in written], 'max pixel diff', round(worst * 255, 2), flush=True)

temporary = manifest_path + '.tmp'
with open(temporary, 'w', encoding='utf-8') as f:
    json.dump(manifest, f, indent=2)
os.replace(temporary, manifest_path)

if PREVIEW and previews:
    # Before / after sheet per clip: old page(s) on the left, new pages on the right, all at the same scale.
    os.makedirs(PREVIEW, exist_ok=True)
    scale = 4
    for name, (old, new, old_spots, layout) in previews.items():
        def tile(pages):
            small = [p[::scale, ::scale] for p in pages]
            h = max(p.shape[0] for p in small)
            out = np.zeros((h, sum(p.shape[1] for p in small) + 16 * (len(small) - 1), 4), dtype=np.float32)
            x = 0
            for p in small:
                out[h - p.shape[0]:, x:x + p.shape[1]] = p
                out[h - p.shape[0]:, x + p.shape[1]:x + p.shape[1] + 16] = (1, 0.2, 0.2, 1)  # page boundary
                x += p.shape[1] + 16
            return out[:, :max(1, x - 16)]
        left, right = tile(old), tile(new)
        h = max(left.shape[0], right.shape[0])
        sheet = np.full((h, left.shape[1] + 48 + right.shape[1], 4), (0.12, 0.12, 0.14, 1), dtype=np.float32)
        for img, x0 in ((left, 0), (right, left.shape[1] + 48)):
            region = sheet[h - img.shape[0]:, x0:x0 + img.shape[1]]
            a = img[:, :, 3:4]
            region[:, :, :3] = img[:, :, :3] * a + region[:, :, :3] * (1 - a)
        save(sheet, os.path.join(PREVIEW, f'Repack_{name}_trang_truoc-trai_sau-phai.png'))
        # Same sprites cut from the old page (top row) and the new page (bottom row), 2x, one per direction.
        picks = [d * (len(layout) // 8) for d in range(8)]
        cells = []
        for i in picks:
            op, ox, oy, w, h = old_spots[i]
            np_, nx, ny = layout[i]
            cells.append((old[op][oy:oy + h, ox:ox + w], new[np_][ny:ny + h, nx:nx + w]))
        cw, ch = max(c[0].shape[1] for c in cells) * 2 + 8, max(c[0].shape[0] for c in cells) * 2 + 8
        grid = np.full((ch * 2, cw * len(cells), 4), (0.2, 0.2, 0.22, 1), dtype=np.float32)
        for k, (a_img, b_img) in enumerate(cells):
            for row, img in ((1, a_img), (0, b_img)):   # row 1 = top (images are bottom-up)
                big = img.repeat(2, 0).repeat(2, 1)
                region = grid[row * ch + 4:row * ch + 4 + big.shape[0], k * cw + 4:k * cw + 4 + big.shape[1]]
                a = big[:, :, 3:4]
                region[:, :, :3] = big[:, :, :3] * a + region[:, :, :3] * (1 - a)
        save(grid, os.path.join(PREVIEW, f'Repack_{name}_sprite_tren-truoc_duoi-sau.png'))
print('LUBU_REPACK_DONE', flush=True)
