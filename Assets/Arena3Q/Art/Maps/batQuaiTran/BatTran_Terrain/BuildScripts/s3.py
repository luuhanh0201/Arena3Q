import json, numpy as np
from PIL import Image
from scipy import ndimage as ndi
import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
O="BatTran_Terrain"; L=json.load(open(f"{O}/BatTran_Layout.json")); T=L["terrain"]
HR, AR = T["heightRes"], T["alphaRes"]
h = np.fromfile(f"{O}/BatTran_Height.bytes", "<u2").reshape(HR, HR) / 65535 * T["height"] + T["posY"]
S = np.fromfile(f"{O}/BatTran_Splat.bytes", np.uint8).reshape(AR, AR, 4) / 255
P = np.asarray(Image.open(f"{O}/BatTran_Painted.png").convert("RGB")) / 255
ext = [T["posX"], T["posX"] + T["sizeX"], T["posZ"], T["posZ"] + T["sizeZ"]]
# shade painted texture with the heightmap
hz = ndi.zoom(h[::-1], (P.shape[0] / HR, P.shape[1] / HR), order=1)
gy, gx = np.gradient(hz, T["sizeZ"] / P.shape[0], T["sizeX"] / P.shape[1])
n = np.dstack([-gx, gy, np.ones_like(hz)]); n /= np.linalg.norm(n, axis=2, keepdims=True)
Ld = np.array([-0.5, 0.5, 0.7]); Ld /= np.linalg.norm(Ld)
rock = np.array([0.62, 0.42, 0.25])
rk = np.clip((hz - 0.25) / 0.6, 0, 1)[..., None]
col = np.clip((P * (1 - rk) + rock * rk) * np.clip(n @ Ld * 0.8 + 0.35, 0, 1.3)[..., None], 0, 1)
fig, axs = plt.subplots(1, 2, figsize=(18, 5.6), dpi=100)
ax = axs[0]; ax.imshow(col, extent=ext)
b = L["mapBounds"]; ax.add_patch(plt.Rectangle((b["x"], b["y"]), b["w"], b["h"], fill=False, ec="white", ls="--"))
colors = {"base": "red", "altar": "gold", "gate": "cyan", "pillar": "grey", "spawn": "white"}
for m in L["markers"]:
    c = colors.get(m["kind"], "k")
    if m["kind"] == "pillar": ax.add_patch(plt.Circle((m["x"], m["y"]), 1.0, fc=(0.5, 0.5, 0.5, 0.8), ec="k"))
    elif m["kind"] == "gate":
        th = np.radians(90 - m["rotY"]); tx, ty = -np.sin(th), np.cos(th)
        ax.plot([m["x"] - 1.25 * tx, m["x"] + 1.25 * tx], [m["y"] - 1.25 * ty, m["y"] + 1.25 * ty], color="cyan", lw=3)
    elif m["kind"] == "altar": ax.add_patch(plt.Circle((0, 0), 2.0, fc=(1, 0.85, 0.3, 0.7), ec="k"))
    elif m["kind"] == "base": ax.add_patch(plt.Circle((m["x"], m["y"]), 3.0, fill=False, ec=c, lw=2))
    else: ax.plot(m["x"], m["y"], "o", color=c, ms=3)
for ln, cc in zip(L["lanes"], ["yellow", "lime", "lime"]):
    p = np.array([[q["x"], q["y"]] for q in ln["points"]]); ax.plot(p[:, 0], p[:, 1], ls=":", color=cc, lw=1.5)
    ax.text(p[len(p)//2, 0], p[len(p)//2, 1] + 0.6, f'{ln["name"]} {ln["length"]} m', color=cc, ha="center", fontsize=8)
ax.set_title("Bat Tran — painted ground + markers (top-down)")
ax2 = axs[1]; ax2.imshow(S[::-1, :, :3] * 0.8 + S[::-1, :, 3:4] * np.array([0.5, 0.5, 0.5]), extent=ext)
ax2.set_title("Splat: R=dry grass  G=sand  B=dirt path  grey=rock rim")
fig.tight_layout(); fig.savefig(f"{O}/Preview_BatTran.png")
print("ok")
