import numpy as np
from PIL import Image
from scipy import ndimage as ndi
a = np.asarray(Image.open('src.png').convert('RGB')).astype(np.float32)
R, G, B = a[..., 0], a[..., 1], a[..., 2]
lum = 0.3 * R + 0.59 * G + 0.11 * B
var = ndi.gaussian_filter((lum - ndi.gaussian_filter(lum, 2)) ** 2, 6)
bsm = ndi.gaussian_filter(B, 6)
score = (bsm - 66) / 14 - (np.sqrt(var) - 9) / 6     # >0 -> sand
sand = ndi.gaussian_filter(score, 3) > 0
# mirror-symmetrise (left/right and top/bottom)
s = sand.astype(np.float32)
s = (s + s[:, ::-1] + s[::-1, :] + s[::-1, ::-1]) / 4 > 0.5
s = ndi.binary_opening(s, iterations=2); s = ndi.binary_closing(s, iterations=3)
np.save('sand_img.npy', s)
print('sand frac', s.mean().round(3), 'var range', np.percentile(np.sqrt(var), [10, 50, 90]).round(1))
vis = a.copy(); vis[s] = vis[s] * 0.35 + np.array([0, 90, 255]) * 0.65
Image.fromarray(vis.astype(np.uint8)).save('dbg_sand.png')
