"""Bat Tran Do: build Unity Terrain data from the top-down ground image."""
import json, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

OUT = "BatTran_Terrain"
os.makedirs(OUT, exist_ok=True)

img = Image.open("src.png").convert("RGB")
IW, IH = img.size
A = np.asarray(img).astype(np.float32) / 255
sand_img = np.load("sand_img.npy")

# ---- world mapping: picture covers x -27..27 (54 m), y scaled by aspect
LX = 54.0
LY = round(LX * IH / IW, 2)            # 30.38 m
X0, Y0 = -LX / 2, -LY / 2
PX = IW / LX                           # pixels per metre
BOUND_X, BOUND_Y = 24.0, 13.0          # mapBounds (play area)
BASES = [(-19.5, 0.0), (19.5, 0.0)]
CLEAR_R = 4.0                          # camp clearing radius
RING_R = 5.7                           # radius of pillar / gate centres
WATER = None


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def world_grid(nx, ny):
    """Cell-centre world coords; row 0 = SOUTH (Unity terrain convention)."""
    xs = X0 + (np.arange(nx) + 0.5) * LX / nx
    ys = Y0 + (np.arange(ny) + 0.5) * LY / ny
    return np.meshgrid(xs, ys)


def to_px(x, y):
    return (x - X0) * PX - 0.5, (LY / 2 - y) * PX - 0.5     # image row 0 = north


def sample(arr, x, y, order=1):
    u, v = to_px(x, y)
    if arr.ndim == 2:
        return ndi.map_coordinates(arr.astype(np.float32), [v, u], order=order, mode="nearest")
    return np.stack([ndi.map_coordinates(arr[..., c], [v, u], order=order, mode="nearest")
                     for c in range(arr.shape[2])], -1)


rng = np.random.default_rng(7)

# ---- image-space masks: sand vs. thin path
r_open = 1.6 * PX                                  # areas wider than ~3.2 m count as open sand
core = ndi.distance_transform_edt(sand_img) > r_open
big = (ndi.distance_transform_edt(~core) <= r_open + 2) & sand_img
path_img = sand_img & ~big

# ---- camp clearings painted into the picture (sand transplanted from the diamond)
YY, XX = np.mgrid[0:IH, 0:IW]
wx = X0 + (XX + 0.5) / PX
wy = LY / 2 - (YY + 0.5) / PX
edge_noise = ndi.gaussian_filter(rng.standard_normal((IH, IW)), 10)
edge_noise /= np.abs(edge_noise).max()
clear = np.zeros((IH, IW), np.float32)
painted = A.copy()
for bx, by in BASES:
    d = np.hypot(wx - bx, wy - by) + edge_noise * 0.9
    w = 1 - smoothstep(CLEAR_R - 0.8, CLEAR_R + 0.4, d)
    clear = np.maximum(clear, w)
    shift = int(round((-bx * 0.55) * PX))            # sample sand from toward the centre
    src = np.roll(A, -shift, axis=1)
    painted = painted * (1 - w[..., None]) + src * w[..., None]
Image.fromarray((np.clip(painted, 0, 1) * 255).astype(np.uint8)).resize((2048, int(2048 * IH / IW)), Image.LANCZOS) \
    .save(f"{OUT}/BatTran_Painted.png")

sand_all = np.maximum(sand_img.astype(np.float32), clear)

# ---- heightmap (257 x 257, rows from south)
HR = 257
TH, BASE_OFF = 3.0, 0.5                 # terrain height 3 m, ground (y=0) sits 0.5 m above terrain bottom
GX, GY = world_grid(HR, HR)
out_d = np.maximum(np.abs(GX) - (BOUND_X + 0.4), np.abs(GY) - (BOUND_Y + 0.4))
n1 = ndi.gaussian_filter(rng.standard_normal((HR, HR)), 6); n1 /= np.abs(n1).max()
n2 = ndi.gaussian_filter(rng.standard_normal((HR, HR)), 2); n2 /= np.abs(n2).max()
rim = smoothstep(0.0, 1.8, out_d + 0.6 * n1)           # rises just outside the play area
h = 1.1 * rim * (0.75 + 0.25 * n2)
grass_w = 1 - sample(sand_all, GX, GY)
h += 0.05 * n1 * grass_w * (1 - rim)                     # gentle bumps on grass only
flat = np.zeros_like(h)
for bx, by in BASES + [(0.0, 0.0)]:                      # bases and formation stay perfectly flat
    flat = np.maximum(flat, 1 - smoothstep(5.0, 7.5, np.hypot(GX - bx, GY - by)))
h *= 1 - flat * (1 - rim)
h = 0.25 * (h + h[:, ::-1] + h[::-1, :] + h[::-1, ::-1])  # exact symmetry
hn = np.clip((h + BASE_OFF) / TH, 0, 1)
(hn * 65535).astype("<u2").tofile(f"{OUT}/BatTran_Height.bytes")

# ---- splat map (512 x 512 x 4): grass, sand, path, rock
AR = 512
SX_, SY_ = world_grid(AR, AR)
sand_w = sample(np.maximum(big.astype(np.float32), clear), SX_, SY_)
path_w = sample(path_img.astype(np.float32), SX_, SY_) * (1 - sand_w)
h_a = ndi.map_coordinates(h, [(SY_ - Y0) / LY * HR - 0.5, (SX_ - X0) / LX * HR - 0.5], order=1, mode="nearest")
gy, gx = np.gradient(h, LY / HR, LX / HR)
slope = ndi.map_coordinates(np.hypot(gx, gy), [(SY_ - Y0) / LY * HR - 0.5, (SX_ - X0) / LX * HR - 0.5], order=1, mode="nearest")
rock_w = np.clip(smoothstep(0.35, 0.9, slope) + 0.5 * smoothstep(0.6, 1.0, h_a), 0, 1)
sand_w = ndi.gaussian_filter(sand_w, 1.2) * (1 - rock_w)
path_w = ndi.gaussian_filter(path_w, 1.2) * (1 - rock_w)
grass_w = np.clip(1 - sand_w - path_w - rock_w, 0, 1)
S = np.stack([grass_w, sand_w, path_w, rock_w], -1)
S /= S.sum(-1, keepdims=True)
S = 0.25 * (S + S[:, ::-1] + S[::-1, :] + S[::-1, ::-1])
(np.clip(S, 0, 1) * 255).round().astype(np.uint8).tofile(f"{OUT}/BatTran_Splat.bytes")

# ---- layout (Unity: X = x, Z = y)
markers = []
def mk(name, x, y, rot=0.0, kind=""):
    markers.append({"name": name, "kind": kind, "x": round(float(x), 3), "y": round(float(y), 3), "rotY": round(float(rot), 2)})

mk("Base_A", -19.5, 0, 90, "base"); mk("Base_B", 19.5, 0, -90, "base")
mk("Formation_Center", 0, 0, 0, "altar")
for k in range(8):
    th = np.radians(k * 45)                       # gate 0 faces Base_B (east), gate 4 faces Base_A
    mk(f"Gate_{k}", RING_R * np.cos(th), RING_R * np.sin(th), 90 - k * 45, "gate")
    tp = np.radians(k * 45 + 22.5)
    mk(f"Pillar_{k}", RING_R * np.cos(tp), RING_R * np.sin(tp), 90 - (k * 45 + 22.5), "pillar")
for i, dy in enumerate((-1.5, 0.0, 1.5), 1):
    mk(f"Spawn_A_{i:02d}", -16.0, dy, 90, "spawn"); mk(f"Spawn_B_{i:02d}", 16.0, -dy, -90, "spawn")

def lane(name, pts):
    p = np.array(pts, float)
    L = float(np.sum(np.hypot(*np.diff(p, axis=0).T)))
    return {"name": name, "length": round(L, 2), "points": [{"x": float(a), "y": float(b)} for a, b in p]}

lanes = [
    lane("Lane_Center", [(-19.5, 0), (-RING_R, 0), (0, 0), (RING_R, 0), (19.5, 0)]),
    lane("Lane_North", [(-19.5, 0), (-15.0, 5.0), (-7.5, 8.0), (0, 8.6), (7.5, 8.0), (15.0, 5.0), (19.5, 0)]),
    lane("Lane_South", [(-19.5, 0), (-15.0, -5.0), (-7.5, -8.0), (0, -8.6), (7.5, -8.0), (15.0, -5.0), (19.5, 0)]),
]
layout = {
    "map": "BatTran",
    "terrain": {"sizeX": LX, "height": TH, "sizeZ": LY, "posX": X0, "posY": -BASE_OFF, "posZ": Y0,
                "heightRes": HR, "alphaRes": AR},
    "mapBounds": {"x": -BOUND_X, "y": -BOUND_Y, "w": 2 * BOUND_X, "h": 2 * BOUND_Y},
    "ring": {"radius": RING_R, "gateWidth": 2.5, "pillarWidth": 2.0, "altarDiameter": 4.0},
    "markers": markers,
    "lanes": lanes,
}
json.dump(layout, open(f"{OUT}/BatTran_Layout.json", "w"), indent=2)
np.savez_compressed("world.npz", h=h, S=S)
print("LY", LY, "lanes", [(l["name"], l["length"]) for l in lanes])
print("height range m", float(h.min()), float(h.max()))
