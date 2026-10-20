# =====================================================================
#  Arena3Q - AUTO RIG cho tướng chibi (Unity Humanoid + ngón tay + socket
#            + xương lông trĩ + xương áo choàng)
#  Blender 4.x / 5.x  -  chạy trong tab Scripting, bấm nút ▶ (Run Script)
#
#  CÁCH DÙNG: đổi STEP rồi bấm chạy, theo thứ tự:
#    STEP = 1  -> chuẩn hóa model + dựng bộ xương tự động
#                 (sau đó bạn kiểm tra / kéo lại khớp nếu lệch)
#    STEP = 2  -> gắn da vào xương (tự tính weight, sửa ngón tay)
#    STEP = "test"  -> đặt tư thế thử (đưa tay, nhấc gối, quay đầu, nắm tay)
#    STEP = "walk"  -> tạo animation đi bộ thử, bấm Space để xem
#    STEP = "reset" -> trả về tư thế gốc (bỏ animation thử)
#    STEP = "anim"  -> tạo animation (1 hoặc 2 tay theo TWO_HAND): Idle, Walk, Run, Attack01 (chém quét), Attack02 (đâm)
#                      + cây kích tạm để xem. Đổi PLAY ở đầu file rồi chạy STEP = "play" để xem từng cái
#    STEP = "buildall" -> tạo BẢN SAO MỚI từ lubu.blend, nạp texture sạch mép UV, rig + weight + kích thật + 7 animation + PlayAll
#    STEP = "finishall" -> tiếp tục bản kết quả đã rig nếu lần trước dừng giữa chừng
#    STEP = "coloronly" -> chỉ làm sạch mép UV trên một bản sao, không dựng rig
#    STEP = "addanims" -> chỉ thêm Victory + Defeat nếu chưa có
#    STEP = "fixdefeat" -> sửa riêng Defeat: rơi kích -> khuỵu cả 2 gối -> ngã sấp; giữ Action Defeat cũ làm backup
#    STEP = "playall" -> xem 7 animation liên tiếp, mỗi animation 5 giây (tổng 35 giây)
#    STEP = "slim"  -> giảm lưới cho model ĐÃ gắn xương (nặng, xem animation bị giật), rồi chạy lại STEP = 2
#    STEP = "weapon"-> ghép vũ khí thật từ WEAPON_FILE vào 2 tay + làm lại animation cho khớp cây thương thật
#    STEP = 3  -> xuất FBX cho Unity (cạnh file .blend)
# =====================================================================
# BẢN MỚI: dữ liệu gốc có 98.598 vertices, UV atlas 4096x4096. Không giảm lưới/gộp UV seams.
# CHÚ Ý: Blender phải mở lubu.blend (trong gói đã giải nén). Sau khi chạy: kết quả LuBu_Arena3Q_7Anims.blend.
# PHIÊN BẢN: v11  (bảng thông báo khi chạy phải ghi đúng số phiên bản này)
STEP = "buildall"  # Mở lubu.blend, chạy 1 lần. Tạo file KẾT QUẢ RIÊNG; không ghi đè model gốc.

HERO_NAME      = "LuBu"     # tên file xuất ra
HEAD_TOP_Z     = 0          # 0 = tự tìm đỉnh mũ/đầu. Nếu tự tìm sai: nhập Z của đỉnh mũ (đo bằng phím N)
TARGET_HEAD_TOP = 1.08      # đỉnh mũ sau khi chuẩn hóa (người gốc: đỉnh đầu = 1.0 m)
DECIMATE_TO    = 0      # giảm lưới nếu nhiều hơn số tam giác này (0 = không giảm)
ADD_FEATHERS   = True       # xương 2 lông trĩ trên mũ
ADD_CAPE       = True       # xương áo choàng (3 dải)
FINGER_CURL    = 75         # độ nắm tay ở STEP "test"
WEAPON_FILE    = ""  # tự phát hiện phuong-tien-hoa-kich.glb cạnh file .blend         # đường dẫn file 3D vũ khí (.glb / .fbx / .obj), ví dụ r"D:\Arena3Q\ArtSource\Weapons\Kich.glb"
WEAPON_LENGTH  = 1.35        # chiều dài cả cây vũ khí (m). Lữ Bố cao ~1.16 m
WEAPON_GRIP    = 0.30        # chỗ tay PHẢI cầm trên CÁN, tính từ đuôi (0 = đuôi cán, 1 = chỗ bắt đầu lưỡi)
WEAPON_ROLL    = 0           # xoay vũ khí quanh cán nếu lưỡi bị quay sai (độ: 90, 180...)
WEAPON_FLIP    = False       # True nếu vũ khí bị cầm ngược (lưỡi nằm phía tay)
WEAPON_TRIS    = 0        # giảm lưới vũ khí còn khoảng này
WEAPON_IN_CHARACTER = True        # xuất vũ khí nằm sẵn trong tay nhân vật (trong file _Rigged.fbx)
EXPORT_WEAPON_SEPARATE = True     # xuất thêm file vũ khí riêng (gốc tọa độ ở chỗ tay phải cầm)
TWO_HAND       = False       # False = cầm thương 1 tay (tay phải). True = cầm 2 tay
PLAY           = "Defeat" # STEP="play" để xem Defeat mới; STEP="playall" để xem cả 7
PLAYALL_SECONDS = 5.0        # mỗi animation phát trong 5 giây (30 fps = 150 frame)
PLAYALL_ORDER   = ("Idle", "Walk", "Run", "Attack01", "Attack02", "Victory", "Defeat")
PLAYALL_TRACK   = "Arena3Q_PlayAll"
PLAYALL_MARKER  = "PlayAll | "
# =====================================================================

import bpy, bmesh, math
from mathutils import Vector, Matrix, Quaternion

RIG_NAME = HERO_NAME + "_Rig"
BODY_NAME = HERO_NAME + "_Body"
BASE = [
    ["Hips", "", [0.0, 0.01, 0.31], [0.0, 0.01, 0.38], 0, 1],
    ["Spine", "Hips", [0.0, 0.01, 0.38], [0.0, 0.01, 0.45], 1, 1],
    ["Chest", "Spine", [0.0, 0.01, 0.45], [0.0, 0.01, 0.555], 1, 1],
    ["Neck", "Chest", [0.0, 0.005, 0.555], [0.0, 0.005, 0.615], 0, 1],
    ["Head", "Neck", [0.0, 0.005, 0.615], [0.0, 0.005, 0.95], 1, 1],
    ["Socket_Head", "Head", [0.0, 0.005, 1.0], [0.0, 0.005, 1.07], 0, 0],
    ["LeftShoulder", "Chest", [0.03, 0.01, 0.535], [0.135, 0.012, 0.53], 0, 1],
    ["LeftUpperArm", "LeftShoulder", [0.135, 0.012, 0.53], [0.19, 0.008, 0.478], 1, 1],
    ["LeftLowerArm", "LeftUpperArm", [0.19, 0.008, 0.478], [0.243, -0.015, 0.423], 1, 1],
    ["LeftHand", "LeftLowerArm", [0.243, -0.015, 0.423], [0.2791, -0.0339, 0.385], 1, 1],
    ["LeftIndexProximal", "LeftHand", [0.2823, -0.0481, 0.385], [0.2977, -0.0512, 0.3623], 0, 1],
    ["LeftIndexIntermediate", "LeftIndexProximal", [0.2977, -0.0512, 0.3623], [0.3079, -0.0532, 0.3472], 1, 1],
    ["LeftIndexDistal", "LeftIndexIntermediate", [0.3079, -0.0532, 0.3472], [0.3165, -0.0549, 0.3346], 1, 1],
    ["LeftMiddleProximal", "LeftHand", [0.2791, -0.0339, 0.385], [0.2992, -0.0325, 0.3596], 0, 1],
    ["LeftMiddleIntermediate", "LeftMiddleProximal", [0.2992, -0.0325, 0.3596], [0.3126, -0.0315, 0.3427], 1, 1],
    ["LeftMiddleDistal", "LeftMiddleIntermediate", [0.3126, -0.0315, 0.3427], [0.3238, -0.0307, 0.3286], 1, 1],
    ["LeftRingProximal", "LeftHand", [0.2831, -0.0155, 0.385], [0.2984, -0.0126, 0.359], 0, 1],
    ["LeftRingIntermediate", "LeftRingProximal", [0.2984, -0.0126, 0.359], [0.3086, -0.0107, 0.3416], 1, 1],
    ["LeftRingDistal", "LeftRingIntermediate", [0.3086, -0.0107, 0.3416], [0.317, -0.0091, 0.3272], 1, 1],
    ["LeftLittleProximal", "LeftHand", [0.279, 0.0096, 0.385], [0.2918, 0.0099, 0.3599], 0, 1],
    ["LeftLittleIntermediate", "LeftLittleProximal", [0.2918, 0.0099, 0.3599], [0.3004, 0.0101, 0.3432], 1, 1],
    ["LeftLittleDistal", "LeftLittleIntermediate", [0.3004, 0.0101, 0.3432], [0.3075, 0.0103, 0.3292], 1, 1],
    ["LeftThumbProximal", "LeftHand", [0.2536, -0.0394, 0.4097], [0.2591, -0.0481, 0.3875], 0, 1],
    ["LeftThumbIntermediate", "LeftThumbProximal", [0.2591, -0.0481, 0.3875], [0.2638, -0.0555, 0.3688], 1, 1],
    ["LeftThumbDistal", "LeftThumbIntermediate", [0.2638, -0.0555, 0.3688], [0.2681, -0.0624, 0.3513], 1, 1],
    ["Socket_Hand_L", "LeftHand", [0.2545, -0.0393, 0.3863], [0.2545, -0.0993, 0.3863], 0, 0],
    ["RightShoulder", "Chest", [-0.03, 0.01, 0.535], [-0.135, 0.012, 0.53], 0, 1],
    ["RightUpperArm", "RightShoulder", [-0.135, 0.012, 0.53], [-0.19, 0.008, 0.478], 1, 1],
    ["RightLowerArm", "RightUpperArm", [-0.19, 0.008, 0.478], [-0.243, -0.015, 0.423], 1, 1],
    ["RightHand", "RightLowerArm", [-0.243, -0.015, 0.423], [-0.2795, -0.0352, 0.385], 1, 1],
    ["RightIndexProximal", "RightHand", [-0.2846, -0.0484, 0.385], [-0.2987, -0.0517, 0.3621], 0, 1],
    ["RightIndexIntermediate", "RightIndexProximal", [-0.2987, -0.0517, 0.3621], [-0.3081, -0.0539, 0.3469], 1, 1],
    ["RightIndexDistal", "RightIndexIntermediate", [-0.3081, -0.0539, 0.3469], [-0.3159, -0.0557, 0.3342], 1, 1],
    ["RightMiddleProximal", "RightHand", [-0.2795, -0.0352, 0.385], [-0.2996, -0.0333, 0.3597], 0, 1],
    ["RightMiddleIntermediate", "RightMiddleProximal", [-0.2996, -0.0333, 0.3597], [-0.3129, -0.0319, 0.3428], 1, 1],
    ["RightMiddleDistal", "RightMiddleIntermediate", [-0.3129, -0.0319, 0.3428], [-0.3241, -0.0308, 0.3287], 1, 1],
    ["RightRingProximal", "RightHand", [-0.2823, -0.0158, 0.385], [-0.2981, -0.013, 0.359], 0, 1],
    ["RightRingIntermediate", "RightRingProximal", [-0.2981, -0.013, 0.359], [-0.3086, -0.0111, 0.3417], 1, 1],
    ["RightRingDistal", "RightRingIntermediate", [-0.3086, -0.0111, 0.3417], [-0.3174, -0.0096, 0.3273], 1, 1],
    ["RightLittleProximal", "RightHand", [-0.2787, 0.0083, 0.385], [-0.2915, 0.0095, 0.3594], 0, 1],
    ["RightLittleIntermediate", "RightLittleProximal", [-0.2915, 0.0095, 0.3594], [-0.2999, 0.0103, 0.3423], 1, 1],
    ["RightLittleDistal", "RightLittleIntermediate", [-0.2999, 0.0103, 0.3423], [-0.307, 0.011, 0.328], 1, 1],
    ["RightThumbProximal", "RightHand", [-0.2521, -0.0387, 0.4134], [-0.2583, -0.0477, 0.3897], 0, 1],
    ["RightThumbIntermediate", "RightThumbProximal", [-0.2583, -0.0477, 0.3897], [-0.2635, -0.0552, 0.3698], 1, 1],
    ["RightThumbDistal", "RightThumbIntermediate", [-0.2635, -0.0552, 0.3698], [-0.2684, -0.0623, 0.3511], 1, 1],
    ["Socket_Hand_R", "RightHand", [-0.2547, -0.0402, 0.3863], [-0.2547, -0.1002, 0.3863], 0, 0],
    ["Socket_Back", "Chest", [0.0, 0.12, 0.47], [0.0, 0.12, 0.57], 0, 0],
    ["LeftUpperLeg", "Hips", [0.085, 0.01, 0.305], [0.09, -0.005, 0.205], 0, 1],
    ["LeftLowerLeg", "LeftUpperLeg", [0.09, -0.005, 0.205], [0.1, 0.015, 0.072], 1, 1],
    ["LeftFoot", "LeftLowerLeg", [0.1, 0.015, 0.072], [0.115, -0.065, 0.022], 1, 1],
    ["LeftToes", "LeftFoot", [0.115, -0.065, 0.022], [0.12, -0.11, 0.022], 1, 1],
    ["RightUpperLeg", "Hips", [-0.085, 0.01, 0.305], [-0.09, -0.005, 0.205], 0, 1],
    ["RightLowerLeg", "RightUpperLeg", [-0.09, -0.005, 0.205], [-0.1, 0.015, 0.072], 1, 1],
    ["RightFoot", "RightLowerLeg", [-0.1, 0.015, 0.072], [-0.115, -0.065, 0.022], 1, 1],
    ["RightToes", "RightFoot", [-0.115, -0.065, 0.022], [-0.12, -0.11, 0.022], 1, 1],
    ["Socket_Hip_L", "Hips", [0.13, 0.0, 0.33], [0.13, 0.0, 0.26], 0, 0],
]   # bộ xương Base_Male: [tên, cha, đầu, đuôi, nối liền, deform]


VERSION = "v11"
LOG = []


def log(*a):
    msg = " ".join(str(x) for x in a)
    LOG.append(msg); print("[AutoRig]", msg)


def popup(title, icon):
    if bpy.app.background: return
    lines = list(LOG)
    def draw(self, ctx):
        for l in lines: self.layout.label(text=l)
    try: bpy.context.window_manager.popup_menu(draw, title=title, icon=icon)
    except Exception: pass


def view3d_override():
    wm = bpy.context.window_manager
    for win in getattr(wm, "windows", []):
        for area in win.screen.areas:
            if area.type == 'VIEW_3D':
                reg = next((r for r in area.regions if r.type == 'WINDOW'), None)
                return dict(window=win, area=area, region=reg)
    return None


def body_obj():
    o = bpy.data.objects.get(BODY_NAME)
    if o: return o
    sel = [o for o in bpy.context.selected_objects if o.type == 'MESH']
    if not sel: sel = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not sel: raise Exception("Không tìm thấy model (Mesh) trong scene")
    return sel


def to_object_mode():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')


def select_only(*objs, active=None):
    for o in bpy.context.view_layer.objects: o.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[-1]


# ------------------------------------------------------------ STEP 1
def step1():
    to_object_mode()
    parts = body_obj()
    if isinstance(parts, list):
        select_only(*parts, active=parts[0])
        if len(parts) > 1: bpy.ops.object.join()
        body = bpy.context.view_layer.objects.active
    else:
        body = parts
    select_only(body)
    if body.parent:
        mw = body.matrix_world.copy(); body.parent = None; body.matrix_world = mw
    for m in list(body.modifiers):
        if m.type == 'ARMATURE': body.modifiers.remove(m)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    body.name = BODY_NAME
    me = body.data

    # Lưới UV texture atlas cần GIỮ nguyên seam vertices (không Merge by Distance).
    # Gộp điểm trùng có UV khác nhau có thể gây nhem màu và méo đường ráp UV.
    if me.uv_layers:
        log("Giữ nguyên UV seams: bỏ remove_doubles nhằm tránh lem màu")
    else:
        bm = bmesh.new(); bm.from_mesh(me)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(me); bm.free()
    ntri = sum(len(p.vertices) - 2 for p in me.polygons)
    if DECIMATE_TO and ntri > DECIMATE_TO:
        mod = body.modifiers.new("dec", 'DECIMATE'); mod.ratio = DECIMATE_TO / ntri
        bpy.ops.object.modifier_apply(modifier=mod.name)
        log("giảm lưới", ntri, "->", len(me.polygons))

    # chân chạm sàn, căn giữa
    co = [v.co for v in me.vertices]
    zmin = min(c.z for c in co)
    xs = sorted(c.x for c in co); ys = sorted(c.y for c in co)
    source_height = max(c.z for c in co) - zmin
    feet = [c for c in co if c.z < zmin + 0.06 * source_height]
    cx = (min(c.x for c in feet) + max(c.x for c in feet)) / 2
    cy = (min(c.y for c in feet) + max(c.y for c in feet)) / 2
    for v in me.vertices: v.co -= Vector((cx, cy, zmin))
    H = max(v.co.z for v in me.vertices)

    # đỉnh mũ: điểm cao nhất ở dải giữa (không tính lông trĩ ở hai bên)
    top = HEAD_TOP_Z
    if not top:
        band = [v.co.z for v in me.vertices if abs(v.co.x) < 0.035 * H]
        top = max(band) if band else H
    s = TARGET_HEAD_TOP / top
    for v in me.vertices: v.co *= s
    me.update()
    log(f"chiều cao gốc {H:.3f}, đỉnh mũ {top:.3f} -> tỉ lệ {s:.3f}; cao mới {H * s:.3f} m")

    build_rig(body)


def P(body):
    return [v.co.copy() for v in body.data.vertices]


def build_rig(body):
    pts = P(body)
    old = bpy.data.objects.get(RIG_NAME)
    if old: bpy.data.objects.remove(old, do_unlink=True)
    ad = bpy.data.armatures.new(RIG_NAME)
    rig = bpy.data.objects.new(RIG_NAME, ad)
    bpy.context.scene.collection.objects.link(rig)
    rig.show_in_front = True
    ad.display_type = 'STICK'

    J = {n: [Vector(h), Vector(t)] for n, p, h, t, c, d in BASE}

    # --- tay: tìm đầu ngón tay (điểm xa nhất sang ngang ở độ cao bàn tay, phía trước áo choàng)
    for side, sg in (("Left", 1), ("Right", -1)):
        cand = [c for c in pts if 0.24 < c.z < 0.50 and c.x * sg > 0.15 and c.y < 0.05]
        if not cand: continue
        tip = max(cand, key=lambda c: c.x * sg)
        A = J[side + "UpperArm"][0].copy()
        B = J[side + "MiddleDistal"][1].copy()
        rot = (B - A).rotation_difference(tip - A)
        k = (tip - A).length / (B - A).length
        ARM = ("Arm", "Hand", "Thumb", "Index", "Middle", "Ring", "Little")
        for n in J:
            if n.startswith(side) and any(x in n for x in ARM):
                for i in (0, 1): J[n][i] = A + rot @ ((J[n][i] - A) * k)
        tag = "L" if sg > 0 else "R"
        for i in (0, 1): J["Socket_Hand_" + tag][i] = A + rot @ ((J["Socket_Hand_" + tag][i] - A) * k)
        J[side + "Shoulder"][1] = J[side + "UpperArm"][0].copy()
        log(side, "đầu ngón tay", tuple(round(x, 3) for x in tip), "tỉ lệ tay", round(k, 2))

    # --- chân: lấy tâm ống chân thật (bỏ áo choàng phía sau)
    for side, sg in (("Left", 1), ("Right", -1)):
        sl = [c for c in pts if 0.10 < c.z < 0.16 and c.x * sg > 0.02 and c.y < 0.02]
        if len(sl) < 5: continue
        xs = sorted(c.x * sg for c in sl)
        cx = (xs[int(len(xs) * 0.1)] + xs[int(len(xs) * 0.9)]) / 2 * sg
        dx = cx - J[side + "LowerLeg"][1].x
        for n in ("UpperLeg", "LowerLeg", "Foot", "Toes"):
            for i in (0, 1):
                if not (n == "UpperLeg" and i == 0): J[side + n][i].x += dx
            if n == "UpperLeg": J[side + n][0].x += dx * 0.5
        toes = [c for c in pts if c.z < 0.06 and c.x * sg > 0.02]
        if toes:
            ty = min(c.y for c in toes) + 0.01
            J[side + "Toes"][1].y = ty
        log(side, "chân x", round(cx, 3))

    bpy.context.view_layer.objects.active = rig
    for o in bpy.context.view_layer.objects: o.select_set(False)
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    eb = ad.edit_bones
    for n, p, h, t, c, d in BASE:
        b = eb.new(n); b.head = J[n][0]; b.tail = J[n][1]; b.use_deform = bool(d)
    for n, p, h, t, c, d in BASE:
        if p:
            eb[n].parent = eb[p]; eb[n].use_connect = bool(c)
    for b in eb:
        if any(x in b.name for x in ("Thumb", "Index", "Middle", "Ring", "Little")):
            b.align_roll(Vector((-1 if b.name.startswith("Left") else 1, 0, 0)))
        elif "Arm" in b.name or "Shoulder" in b.name or "Foot" in b.name or "Toes" in b.name:
            b.align_roll(Vector((0, 0, 1)))
        else:
            b.align_roll(Vector((0, -1, 0)))

    if ADD_FEATHERS: add_feathers(eb, pts)
    if ADD_CAPE: add_cape(eb, pts)
    ad.use_mirror_x = True
    bpy.ops.object.mode_set(mode='OBJECT')
    log("ĐÃ DỰNG XƯƠNG:", len(ad.bones), "xương. Kiểm tra bằng Alt+Z, Tab để chỉnh khớp, rồi chạy STEP = 2")


def chain(eb, name, pts, parent):
    prev = parent
    for i in range(len(pts) - 1):
        b = eb.new(f"{name}_{i + 1:02d}" if "." not in name else name.replace(".", f"_{i + 1:02d}."))
        b.head = pts[i]; b.tail = pts[i + 1]
        b.parent = eb[prev]; b.use_connect = i > 0
        prev = b.name


def add_feathers(eb, pts, body=None):
    # ước lượng khối đầu: lát cắt rộng nhất ở phần trên người -> bán kính, tâm đầu
    best_w, best_z = 0.0, 0.8
    for i in range(30):
        z = 0.62 + i * 0.012
        xs = sorted(abs(c.x) for c in pts if abs(c.z - z) < 0.006)
        if len(xs) > 10:
            w = xs[0]
            for x in xs:                      # dừng ở khoảng trống đầu tiên (bỏ lông trĩ bay ngoài)
                if x - w > 0.03: break
                w = x
            if w > best_w: best_w, best_z = w, z
    R = best_w
    head_c = Vector((0, 0, max(best_z, TARGET_HEAD_TOP - R * 1.05)))
    log('đầu: tâm z', round(head_c.z, 3), 'bán kính', round(R, 3))
    me = bpy.data.objects[BODY_NAME].data
    nbr = [[] for _ in me.vertices]
    for e in me.edges:
        a, b = e.vertices; nbr[a].append(b); nbr[b].append(a)
    for side, sg in ((".L", 1), (".R", -1)):
        ids = {v.index for v in me.vertices if v.co.z > head_c.z - R * 0.6 and v.co.x * sg > 0.015
               and (v.co - head_c).length > R * 1.12}
        # cụm liền nhau lớn nhất = lông trĩ (bỏ các chi tiết nhỏ trên mũ)
        comp, seen = [], set()
        for i0 in ids:
            if i0 in seen: continue
            stack, cur = [i0], []
            seen.add(i0)
            while stack:
                i = stack.pop(); cur.append(i)
                for j in nbr[i]:
                    if j in ids and j not in seen: seen.add(j); stack.append(j)
            comp.append(cur)
        F = [me.vertices[i].co.copy() for i in max(comp, key=len)] if comp else []
        log('lông', side, 'điểm', len(ids), 'cụm', len(comp), 'lớn nhất', len(F))
        if len(F) < 20:
            log("không thấy lông trĩ bên", side, "- tạo xương mặc định, hãy tự kéo vào đúng chỗ")
            ch = [Vector((0.06 * sg, 0.0, 1.0)), Vector((0.12 * sg, 0.0, 1.10)),
                  Vector((0.22 * sg, 0.0, 1.12)), Vector((0.30 * sg, 0.0, 1.0)), Vector((0.36 * sg, 0.0, 0.85))]
        else:
            root = min(F, key=lambda c: (c - head_c).length)
            # đi dọc lông từ gốc: mỗi bước lấy tâm các điểm ở vành kế tiếp
            span = max((c - root).length for c in F)
            step = max(span / 14, 0.015)
            path, cur, pool = [root], root, list(F)
            for _ in range(60):
                pool = [c for c in pool if (c - cur).length >= step * 0.5]
                ring = [c for c in pool if (c - cur).length < step * 1.5]
                if not ring: break
                cur = sum(ring, Vector()) / len(ring)
                path.append(cur)
            L = [0.0]
            for i in range(1, len(path)): L.append(L[-1] + (path[i] - path[i - 1]).length)
            ch = []
            for k in range(5):
                tgt = L[-1] * k / 4
                j = min(range(len(L)), key=lambda i: abs(L[i] - tgt))
                ch.append(path[j].copy())
            ch[0] = root.lerp(head_c, 0.1)
            log("lông trĩ", side, "dài", round(L[-1], 2), "m")
        chain(eb, "Feather" + side, ch, "Head")


def add_cape(eb, pts):
    def back_y(x, z):
        s = [c.y for c in pts if abs(c.x - x) < 0.035 and abs(c.z - z) < 0.025]
        return (max(s) - 0.015) if s else None
    for nm, x in (("Cape_Mid", 0.0), ("Cape.L", 0.12), ("Cape.R", -0.12)):
        zs = [0.52, 0.36, 0.21, 0.07]
        ch = []
        for z in zs:
            y = back_y(x * (1 + (0.52 - z)), z)
            if y is None or y < 0.04: y = 0.09 + (0.52 - z) * 0.1
            ch.append(Vector((x * (1 + (0.52 - z)), y, z)))
        chain(eb, nm, ch, "Chest")


# ------------------------------------------------------------ STEP 2
def seg_dist(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length


def step2():
    to_object_mode()
    body = bpy.data.objects[BODY_NAME]; rig = bpy.data.objects[RIG_NAME]
    for m in list(body.modifiers):
        if m.type == 'ARMATURE': body.modifiers.remove(m)
    body.vertex_groups.clear()
    body.parent = None
    # phóng to x10 để tính weight ổn định với model nhỏ
    for o in (rig, body): o.scale = (10, 10, 10)
    bpy.context.view_layer.update()
    select_only(body, rig, active=rig)
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    for o in (rig, body): o.scale = (1, 1, 1)
    body.matrix_parent_inverse = Matrix.Identity(4)
    bpy.context.view_layer.update()

    me = body.data
    deform = [b for b in rig.data.bones if b.use_deform]
    for b in deform:
        if b.name not in body.vertex_groups: body.vertex_groups.new(name=b.name)
    # đỉnh nào chưa có weight -> gán cho xương gần nhất
    miss = [v for v in me.vertices if sum(g.weight for g in v.groups) < 1e-4]
    for v in miss:
        b = min(deform, key=lambda b: seg_dist(v.co, b.head_local, b.tail_local))
        body.vertex_groups[b.name].add([v.index], 1.0, 'REPLACE')
    log("đỉnh thiếu weight (đã gán xương gần nhất):", len(miss))

    fix_fingers(body, rig)
    fix_regions(body, rig)

    select_only(body)
    bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
    bpy.ops.object.vertex_group_smooth(group_select_mode='BONE_DEFORM', factor=0.35, repeat=1)
    bpy.ops.object.vertex_group_limit_total(group_select_mode='BONE_DEFORM', limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='BONE_DEFORM', lock_active=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    log("GẮN DA XONG. Chạy STEP = \"test\" để thử cử động")


def fix_fingers(body, rig):
    """mỗi đỉnh ở bàn tay chỉ theo 1 ngón gần nhất (không kéo dính ngón bên cạnh)"""
    FING = ["Thumb", "Index", "Middle", "Ring", "Little"]; SEG = ["Proximal", "Intermediate", "Distal"]
    me = body.data
    for side in ("Left", "Right"):
        fam = [side + "Hand"] + [f"{side}{f}{s}" for f in FING for s in SEG]
        if any(n not in body.vertex_groups for n in fam): continue
        gi = {body.vertex_groups[n].index: j for j, n in enumerate(fam)}
        segs = [(rig.data.bones[n].head_local, rig.data.bones[n].tail_local) for n in fam]
        for v in me.vertices:
            W = [0.0] * len(fam)
            for g in v.groups:
                if g.group in gi: W[gi[g.group]] = g.weight
            tot = sum(W)
            if tot < 1e-3: continue
            D = [max(seg_dist(v.co, a, b), 1e-4) for a, b in segs]
            best = min(range(5), key=lambda k: min(D[1 + 3 * k:4 + 3 * k]))
            cols = [0] + [1 + 3 * best + s for s in range(3)]
            w = {c: D[c] ** -8 for c in cols}; sw = sum(w.values())
            for j, n in enumerate(fam):
                nw = tot * w.get(j, 0) / sw
                vg = body.vertex_groups[n]
                if nw > 1e-3: vg.add([v.index], nw, 'REPLACE')
                else: vg.remove([v.index])


def fix_regions(body, rig):
    """Lông trĩ chỉ theo Head + xương lông; áo choàng chỉ theo xương áo + thân (không bị tay/chân kéo)."""
    import numpy as np
    me = body.data
    deform = [b for b in rig.data.bones if b.use_deform]
    names = [b.name for b in deform]
    V = np.array([v.co for v in me.vertices])
    D = np.zeros((len(V), len(deform)))
    for j, b in enumerate(deform):
        a = np.array(b.head_local); ab = np.array(b.tail_local) - a
        t = np.clip(((V - a) @ ab) / (ab @ ab), 0, 1)
        D[:, j] = np.linalg.norm(V - (a + t[:, None] * ab), axis=1)
    near = D.argmin(1)
    torso = [n for n in ("Hips", "Spine", "Chest") if n in names]
    groups = {}
    for side in (".L", ".R"):
        fb = [n for n in names if n.startswith("Feather") and n.endswith(side)]
        if fb: groups["F" + side] = (set(fb), ["Head"] + fb, 6.0)
    cb = [n for n in names if n.startswith("Cape")]
    if cb: groups["C"] = (set(cb), cb + torso, 4.0)
    idx = {n: j for j, n in enumerate(names)}
    # đầu chibi cứng: mọi đỉnh trên cổ (trừ lông trĩ) chỉ theo Head -> tai, tóc, mũ không bị kéo méo
    hz = rig.data.bones["Head"].head_local.z + 0.03
    hj = idx.get("Head")
    feather_cols = [idx[n] for n in names if n.startswith("Feather")]
    if hj is not None:
        nh = 0
        for i in np.where(V[:, 2] > hz)[0]:
            if feather_cols and near[i] in feather_cols and D[i, near[i]] < 0.05: continue
            for g in list(me.vertices[i].groups):
                body.vertex_groups[g.group].remove([int(i)])
            body.vertex_groups["Head"].add([int(i)], 1.0, 'REPLACE'); nh += 1
        log("đầu cứng:", nh, "đỉnh chỉ theo Head")
    count = 0
    for key, (trigger, allowed, pw) in groups.items():
        cols = [idx[n] for n in allowed if n in idx]
        sel = np.where(np.isin(near, [idx[n] for n in trigger]))[0]
        if key.startswith("F"):          # chỉ phần thân lông (sát xương lông), không lấy tai/tóc gần đó
            sel = sel[D[sel, near[sel]] < 0.05]
        for i in sel:
            d = np.maximum(D[i, cols], 1e-4) ** -pw
            order = np.argsort(-d)[:3]; w = d[order] / d[order].sum()
            for g in list(me.vertices[i].groups):
                body.vertex_groups[g.group].remove([int(i)])
            for o, ww in zip(order, w):
                if ww > 1e-3: body.vertex_groups[names[cols[o]]].add([int(i)], float(ww), 'REPLACE')
        count += len(sel)
        log("vùng", key, "-", len(sel), "đỉnh chỉ theo:", ", ".join(allowed[:4]) + ("..." if len(allowed) > 4 else ""))


# ------------------------------------------------------------ TEST / RESET
def reset():
    rig = bpy.data.objects[RIG_NAME]
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0); pb.scale = (1, 1, 1)


def rot(name, axis, deg):
    rig = bpy.data.objects[RIG_NAME]
    pb = rig.pose.bones.get(name)
    if not pb: return
    ax = (pb.bone.matrix_local.to_3x3().inverted() @ Vector(axis)).normalized()
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = Quaternion(ax, math.radians(deg)) @ pb.rotation_quaternion


# Hướng chuẩn (nhân vật nhìn về -Y):
def leg_fwd(side, deg):  rot(side + "UpperLeg", (1, 0, 0), -deg)     # đưa đùi ra trước
def knee(side, deg):     rot(side + "LowerLeg", (1, 0, 0), deg)      # gập gối (cẳng chân ra sau)
def ankle(side, deg):    rot(side + "Foot", (1, 0, 0), -deg)         # nhón / gập cổ chân
def arm_fwd(side, deg):  rot(side + "UpperArm", (1, 0, 0), -deg)     # vung tay ra trước
def arm_out(side, deg):  rot(side + "UpperArm", (0, 1, 0), -deg if side == "Left" else deg)  # dang tay ngang
def elbow(side, deg):    rot(side + "LowerArm", (1, 0, 0), -deg)     # gập khuỷu (cẳng tay ra trước)


def curl(side, deg):
    rig = bpy.data.objects[RIG_NAME]; s = 1 if side == "Left" else -1
    for f in ["Index", "Middle", "Ring", "Little"]:
        for seg in ["Proximal", "Intermediate", "Distal"]:
            b = rig.data.bones.get(f"{side}{f}{seg}")
            if b:
                d = (b.tail_local - b.head_local).normalized()
                rot(b.name, d.cross(Vector((-s, 0, 0))), deg)


def test_pose():
    stop_playall()
    rig = bpy.data.objects[RIG_NAME]
    if rig.animation_data:
        rig.animation_data.action = None
    reset()
    arm_fwd("Left", 70); elbow("Left", 60)                 # tay trái đưa ra trước, gập khuỷu
    arm_out("Right", 60); elbow("Right", 30)               # tay phải dang ngang
    leg_fwd("Left", 55); knee("Left", 80); ankle("Left", 10)   # nhấc gối trái
    knee("Right", 10)
    rot("Head", (0, 0, 1), 25); rot("Spine", (0, 0, 1), -10)
    curl("Left", FINGER_CURL); curl("Right", FINGER_CURL)
    log("Tư thế thử: tay trái đưa trước, tay phải dang, nhấc gối trái, quay đầu, nắm tay. STEP = \"reset\" để về gốc")


def walk():
    """Chu kỳ đi bộ 24 khung (lặp). Bấm Space để xem."""
    stop_playall()
    rig = bpy.data.objects[RIG_NAME]; sc = bpy.context.scene
    reset()
    act = bpy.data.actions.get(HERO_NAME + "_WalkPreview")
    if act: bpy.data.actions.remove(act)
    rig.animation_data_create()
    rig.animation_data.action = bpy.data.actions.new(HERO_NAME + "_WalkPreview")
    sc.frame_start, sc.frame_end = 1, 24
    sc.render.fps = 24
    hips = rig.pose.bones["Hips"]
    # (khung, chân trái trước?, là khung tiếp đất?)
    keys = [(1, 1, True), (7, 1, False), (13, -1, True), (19, -1, False), (25, 1, True)]
    for f, lead, contact in keys:
        reset()
        A, B = ("Left", "Right") if lead > 0 else ("Right", "Left")
        if contact:      # chân trước chạm gót, chân sau đẩy mũi
            leg_fwd(A, 25); knee(A, 5); ankle(A, -15)
            leg_fwd(B, -20); knee(B, 15); ankle(B, 20)
            arm_fwd(B, 22); arm_fwd(A, -18)
            hips.location = (0, -0.012, 0)
        else:            # chân trước đỡ người, chân sau co lên bước qua
            leg_fwd(A, 0); knee(A, 8)
            leg_fwd(B, 25); knee(B, 55); ankle(B, -10)
            hips.location = (0, 0.006, 0)
        for sd in ("Left", "Right"):
            arm_out(sd, -8); elbow(sd, 25); curl(sd, 35)
        rot("Spine", (0, 0, 1), 6 * lead if contact else 0)
        rot("Chest", (0, 0, 1), -8 * lead if contact else 0)
        for side in (".L", ".R"):     # lông trĩ đung đưa trễ nhịp
            pb = rig.pose.bones.get("Feather_02" + side)
            if pb: rot(pb.name, (1, 0, 0), 6 if contact else -6)
        for pb in rig.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f)
            if pb.name == "Hips": pb.keyframe_insert("location", frame=f)
    sc.frame_set(1)
    log("Đã tạo animation đi bộ thử (khung 1-24). Di chuột vào khung 3D, bấm Space để xem")


def reset_all():
    stop_playall()
    rig = bpy.data.objects[RIG_NAME]
    if rig.animation_data: rig.animation_data.action = None
    reset()


# ------------------------------------------------------------ ANIMATION (Idle / Run / Attack)
def wrist(side, axis, deg): rot(side + "Hand", axis, deg)
def lean(bone, deg):        rot(bone, (1, 0, 0), deg)       # + = cúi ra trước
def twist(bone, deg):       rot(bone, (0, 0, 1), deg)       # + = xoay người sang trái nhân vật
def side_bend(bone, deg):   rot(bone, (0, 1, 0), deg)
def hips_y(v):              bpy.data.objects[RIG_NAME].pose.bones["Hips"].location = (0, v, 0)   # lên/xuống


AIM_LOG = []


def aim(side, d):
    """Xoay cổ tay để mũi vũ khí chĩa đúng hướng d (hướng thế giới, nhân vật nhìn về -Y)."""
    rig = bpy.data.objects[RIG_NAME]
    bpy.context.view_layer.update()
    sock = rig.pose.bones["Socket_Hand_" + ("R" if side == "Right" else "L")]
    hand = rig.pose.bones[side + "Hand"]
    cur = (sock.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
    q = cur.rotation_difference(Vector(d).normalized())
    AIM_LOG.append(round(math.degrees(q.angle)))
    M = hand.matrix.copy()
    hand.matrix = Matrix.Translation(M.to_translation()) @ (q.to_matrix() @ M.to_3x3()).to_4x4()
    bpy.context.view_layer.update()


def feathers(deg):
    rig = bpy.data.objects[RIG_NAME]
    for side in (".L", ".R"):
        for i, k in ((1, 0.15), (2, 0.3), (3, 0.4)):
            pb = rig.pose.bones.get(f"Feather_{i:02d}{side}")
            if pb: rot(pb.name, (1, 0, 0), deg * k)


def cape(deg):
    rig = bpy.data.objects[RIG_NAME]
    for pb in rig.pose.bones:
        if pb.name.startswith("Cape"):
            rot(pb.name, (1, 0, 0), -deg * (0.5 if pb.name.endswith(("_01", "_01.L", "_01.R")) else 1.0))


def grip(side="Right"):          # nắm cán kích
    for pb in bpy.data.objects[RIG_NAME].pose.bones:
        if pb.name.startswith(side) and any(d in pb.name for d in ("Thumb", "Index", "Middle", "Ring", "Little")):
            pb.rotation_quaternion = (1, 0, 0, 0)
    curl(side, 85)


GRIP_GAP = 0.17                  # khoảng cách 2 bàn tay trên cán (m)
TWO_HAND_ERR = []


def _empty(name):
    o = bpy.data.objects.get(name) or bpy.data.objects.new(name, None)
    if o.name not in bpy.context.scene.collection.objects: bpy.context.scene.collection.objects.link(o)
    return o


POLE_ANGLE = {}


def _ik_arm(side, wrist_goal):
    """IK 2 đốt cho tay, khuỷu chĩa xuống - ra ngoài, rồi 'nướng' kết quả thành xoay thường."""
    rig = bpy.data.objects[RIG_NAME]; vl = bpy.context.view_layer
    s = 1 if side == "Left" else -1
    up, lo = rig.pose.bones[side + "UpperArm"], rig.pose.bones[side + "LowerArm"]
    up.ik_stretch = 0.0; lo.ik_stretch = 0.0
    tgt, pole = _empty("_IK_T_" + side), _empty("_IK_P_" + side)
    tgt.location = wrist_goal
    sh = up.matrix.to_translation()
    pole.location = sh + Vector((0.3 * s, 0.12, -0.3))
    best = None
    cands = (POLE_ANGLE[side],) if side in POLE_ANGLE else (-90, 90, 0, 180)
    for ang in cands:
        c = lo.constraints.new('IK'); c.target = tgt; c.chain_count = 2
        c.use_stretch = False
        c.pole_target = pole; c.pole_angle = math.radians(ang)
        vl.update()
        elbow = lo.matrix.to_translation()
        sc = (elbow - pole.location).length
        res = (sc, up.matrix.copy(), lo.matrix.copy(), ang)
        lo.constraints.remove(c); vl.update()
        if best is None or res[0] < best[0]: best = res
    _, Mu, Ml, ang = best
    POLE_ANGLE.setdefault(side, ang)          # dùng 1 góc cố định cho cả bộ animation -> khuỷu tay không lật qua lại
    up.matrix = Mu; up.scale = (1, 1, 1); vl.update()
    lo.matrix = Ml; lo.scale = (1, 1, 1); vl.update()


CLEAR_MARGIN = 0.025            # thương phải cách thân/giáp ít nhất 2.5 cm
CLEAR_LOG = []


def _edge_dir(d):
    """Hướng bản rộng của lưỡi: ưu tiên quay ra trước/sau (không quét vào đầu, vai); nếu cán chĩa thẳng trước thì dựng đứng."""
    f = Vector((0, -1, 0))
    e = f - d * f.dot(d)
    if e.length < 0.35:
        u = Vector((0, 0, 1)); e = u - d * u.dot(d)
    return e.normalized()


def _place(P_off, d_local, gap):
    rig = bpy.data.objects[RIG_NAME]; vl = bpy.context.view_layer
    vl.update()
    ch = rig.pose.bones["Chest"]
    R = ch.matrix.to_3x3() @ ch.bone.matrix_local.to_3x3().inverted()
    P = ch.matrix.to_translation() + R @ Vector(P_off)
    d = (R @ Vector(d_local)).normalized()
    hands = [("Right", P - d * gap / 2), ("Left", P + d * gap / 2)] if TWO_HAND else [("Right", P)]
    for side, goal in hands:
        hb = rig.pose.bones[side + "Hand"]
        sock = rig.pose.bones["Socket_Hand_" + ("R" if side == "Right" else "L")]
        wg = goal.copy()
        shb = rig.pose.bones[side + "Shoulder"]
        for _ in range(7):
            for n in ("Shoulder", "UpperArm", "LowerArm", "Hand"):
                rig.pose.bones[side + n].rotation_quaternion = (1, 0, 0, 0)
                rig.pose.bones[side + n].scale = (1, 1, 1)
            vl.update()
            # vai (xương đòn) xoay nhẹ về phía cán (tối đa 25°) -> với xa hơn, tự nhiên như người
            h = shb.matrix.to_translation(); tdir = (shb.tail - shb.head)
            q = tdir.rotation_difference(wg - h)
            ang = min(q.angle, math.radians(25)) * 0.6
            if ang > 1e-3:
                qq = Quaternion(q.axis, ang)
                M = shb.matrix.copy()
                shb.matrix = Matrix.Translation(M.to_translation()) @ (qq.to_matrix() @ M.to_3x3()).to_4x4()
                shb.scale = (1, 1, 1)
                vl.update()
            _ik_arm(side, wg)
            cur = (sock.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
            q = cur.rotation_difference(d)
            M = hb.matrix.copy()
            hb.matrix = Matrix.Translation(M.to_translation()) @ (q.to_matrix() @ M.to_3x3()).to_4x4()
            hb.scale = (1, 1, 1)
            vl.update()
            if side == "Right":                       # xoay quanh cán: bản lưỡi quay theo hướng 'edge'
                e = _edge_dir(d)
                cx = sock.matrix.to_3x3() @ Vector((1, 0, 0))
                cx = (cx - d * cx.dot(d)).normalized(); e2 = (e - d * e.dot(d)).normalized()
                ang = math.atan2(cx.cross(e2).dot(d), cx.dot(e2))
                if abs(ang) > 1e-3:
                    M = hb.matrix.copy()
                    rq = Quaternion(d, ang)
                    hb.matrix = Matrix.Translation(M.to_translation()) @ (rq.to_matrix() @ M.to_3x3()).to_4x4()
                    hb.scale = (1, 1, 1)
                    vl.update()
            err = goal - sock.matrix.to_translation()
            if err.length < 0.003: break
            wg += err
        grip(side)
        TWO_HAND_ERR.append(round(err.length * 100, 1))
    vl.update()
    return R


def _weapon_obj():
    return bpy.data.objects.get(HERO_NAME + "_Weapon") or bpy.data.objects.get(HERO_NAME + "_WeaponPreview")


_BODY_CACHE = {}


def _body_polys(body):
    """Các mặt của THÂN (bỏ áo choàng và lông trĩ - vải/lông được phép chạm thương)."""
    key = (body.name, len(body.data.polygons), TWO_HAND)
    if key in _BODY_CACHE: return _BODY_CACHE[key]
    hold_parts = ("RightLowerArm", "RightHand", "RightThumb", "RightIndex", "RightMiddle", "RightRing", "RightLittle")
    if TWO_HAND: hold_parts += tuple(x.replace("Right", "Left") for x in hold_parts)
    # vải/lông + cẳng tay & bàn tay đang cầm (tự nhiên là áp vào cán) không tính là va chạm
    soft = {g.index for g in body.vertex_groups if g.name.startswith(("Cape", "Feather") + hold_parts)}
    me = body.data
    skip = set()
    for v in me.vertices:
        if sum(g.weight for g in v.groups if g.group in soft) > 0.5: skip.add(v.index)
    polys = [tuple(p.vertices) for p in me.polygons if not any(i in skip for i in p.vertices)]
    _BODY_CACHE[key] = polys
    return polys


def _clearance(gap):
    """Trả về vector cần đẩy cây thương ra khỏi thân (None nếu đã không chạm)."""
    from mathutils.bvhtree import BVHTree
    import numpy as np
    w = _weapon_obj(); body = bpy.data.objects.get(BODY_NAME)
    if not w or not body: return None
    dg = bpy.context.evaluated_depsgraph_get()
    be = body.evaluated_get(dg); me = be.to_mesh()
    co = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get("co", co)
    verts = [Vector(x) for x in co.reshape(-1, 3)]
    be.to_mesh_clear()
    tree = BVHTree.FromPolygons(verts, _body_polys(body))
    inv = body.matrix_world.inverted(); Mw = w.matrix_world; R3 = body.matrix_world.to_3x3()
    vs = w.data.vertices; step = max(1, len(vs) // 1200)
    rays = (Vector((1, 0.13, 0.07)).normalized(), Vector((-0.1, -1, 0.2)).normalized(), Vector((0.05, 0.1, 1)).normalized())
    push = Vector(); n = 0; worst = 0.0
    for i in range(0, len(vs), step):
        lc = vs[i].co
        if abs(lc.y) < 0.07 or (TWO_HAND and abs(lc.y - gap) < 0.07): continue   # chỗ 2 tay nắm
        pl = inv @ (Mw @ lc)
        loc, nrm, _, dist = tree.find_nearest(pl)
        if loc is None: continue
        inside = 0
        for r in rays:
            h = tree.ray_cast(pl, r)
            if h[0] is not None and h[1].dot(r) > 0: inside += 1
        if inside >= 2:
            pen = dist + CLEAR_MARGIN; dv = nrm
        elif dist < CLEAR_MARGIN:
            pen = CLEAR_MARGIN - dist; dv = (pl - loc).normalized() if dist > 1e-5 else nrm
        else:
            continue
        push += (R3 @ dv).normalized() * pen; n += 1; worst = max(worst, pen)
    if n == 0: return None
    return push.normalized() * (min(worst, 0.06) + 0.005)


def _reach_ok(P_off, d_local, gap, R):
    """Dời tâm 2 tay để tay nào cũng với tới cán. Trả về (P_off mới, đã ổn chưa)."""
    rig = bpy.data.objects[RIG_NAME]
    ch = rig.pose.bones["Chest"]
    P = ch.matrix.to_translation() + R @ P_off
    d = (R @ Vector(d_local)).normalized()
    ok = True
    sides = [("Right", -0.5), ("Left", 0.5)] if TWO_HAND else [("Right", 0.0)]
    for side, k in sides:
        b = rig.data.bones
        reach = (b[side + "UpperArm"].length + b[side + "LowerArm"].length +
                 (b["Socket_Hand_" + side[0]].head_local - b[side + "Hand"].head_local).length) * 0.97
        sh = rig.pose.bones[side + "UpperArm"].matrix.to_translation()
        goal = P + d * gap * k
        dist = (goal - sh).length
        if dist > reach:
            P = P - (goal - sh) * (1 - reach / dist); ok = False
    return R.inverted() @ (P - ch.matrix.to_translation()), ok


def weapon(P_off, d_local, gap=None):
    """Đặt cây thương theo khung ngực: P_off = tâm 2 tay so với gốc xương Chest, d_local = hướng mũi.
       - 2 tay tự bám cán (IK), vai xoay nhẹ cho với tới, cổ tay xoay theo cán, ngón nắm chặt.
       - Thương chạm thân/giáp -> tự đẩy ra; tay với không tới -> kéo lại gần; vẫn kẹt -> 2 tay nắm sát nhau hơn."""
    gap = GRIP_GAP if gap is None else gap
    P0 = Vector(P_off)
    while True:
        P_off = P0.copy(); clean = False
        for it in range(10):
            R = _place(P_off, d_local, gap)
            push = _clearance(gap)
            if push is not None: P_off = P_off + R.inverted() @ push
            P_off, reach_ok = _reach_ok(P_off, d_local, gap, R)
            if push is None and reach_ok:
                clean = True; break
        R = _place(P_off, d_local, gap)
        if clean or not TWO_HAND or gap <= 0.11:
            if clean: CLEAR_LOG.append(0)
            else:
                global CLEAR_MARGIN
                m0 = CLEAR_MARGIN; CLEAR_MARGIN = 0.0
                hit = _clearance(gap) is not None
                CLEAR_MARGIN = m0
                CLEAR_LOG.append(-1 if hit else -2)       # -1 = xuyên vào thân, -2 = chỉ hơi sát
            return
        gap -= 0.02


def _left_off_shaft():
    rig = bpy.data.objects[RIG_NAME]
    sR = rig.pose.bones["Socket_Hand_R"]; sL = rig.pose.bones["Socket_Hand_L"]
    o = sR.matrix.to_translation(); d = (sR.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
    v = sL.matrix.to_translation() - o
    return (v - d * v.dot(d)).length


def fix_inbetweens(act, key_frames):
    """Kiểm tra TỪNG khung giữa các khung chính: nếu thương chạm thân hoặc tay trái rời cán
       -> thêm khung chính tại đó (đẩy thương ra, tay bám lại)."""
    rig = bpy.data.objects[RIG_NAME]; sc = bpy.context.scene; vl = bpy.context.view_layer
    if not _weapon_obj(): return
    f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
    added = 0
    for _pass in range(3):
        keys = set(key_frames)
        for f in range(f0 + 1, f1):
            if f in keys: continue
            rig.animation_data.action = act; ensure_playback(); sc.frame_set(f)
            pose = {pb.name: (pb.rotation_quaternion.copy(), pb.location.copy()) for pb in rig.pose.bones}
            rig.animation_data.action = None
            for pb in rig.pose.bones:
                pb.rotation_quaternion, pb.location = pose[pb.name]
            vl.update()
            need = _clearance(GRIP_GAP) is not None or (TWO_HAND and _left_off_shaft() > 0.02)
            if not need: continue
            ch = rig.pose.bones["Chest"]
            R = ch.matrix.to_3x3() @ ch.bone.matrix_local.to_3x3().inverted()
            sR = rig.pose.bones["Socket_Hand_R"]
            d = (sR.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
            P = sR.matrix.to_translation() + d * (GRIP_GAP / 2 if TWO_HAND else 0)
            weapon(R.inverted() @ (P - ch.matrix.to_translation()), R.inverted() @ d)
            rig.animation_data.action = act
            for pb in rig.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f)
                if pb.name == "Hips": pb.keyframe_insert("location", frame=f)
            key_frames = list(key_frames) + [f]; keys.add(f); added += 1
    rig.animation_data.action = act
    if added: INBETWEEN_LOG.append((act.name, added))


INBETWEEN_LOG = []


def make_action(name, keys, loop):
    """keys: [(khung, hàm đặt tư thế)]. Tạo/ghi đè action rồi trả về action."""
    rig = bpy.data.objects[RIG_NAME]
    full = f"{HERO_NAME}_{name}"
    old = bpy.data.actions.get(full)
    if old: bpy.data.actions.remove(old)
    rig.animation_data_create()
    act = bpy.data.actions.new(full); act.use_fake_user = True
    for f, fn in keys:
        rig.animation_data.action = None
        reset(); fn()
        for pb in rig.pose.bones: pb.scale = (1, 1, 1)
        rig.animation_data.action = act
        for pb in rig.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=f)
            pb.keyframe_insert("scale", frame=f)
            if pb.name == "Hips": pb.keyframe_insert("location", frame=f)
    act.frame_range = (keys[0][0], keys[-1][0])
    try: act.use_cyclic = loop
    except Exception: pass
    fix_inbetweens(act, [k[0] for k in keys])
    for o in [o for o in bpy.data.objects if o.name.startswith("_IK_")]:
        bpy.data.objects.remove(o, do_unlink=True)
    return act


def hold(two, one, left=None):
    """two/one = (P_off, hướng mũi[, gap]) cho kiểu 2 tay / 1 tay. left() = tư thế tay trái tự do (kiểu 1 tay)."""
    if TWO_HAND:
        weapon(*two)
    else:
        if left: left()
        weapon(*one)


def free_left(fwd=5, out=-5, elb=35, fist=55):
    arm_fwd("Left", fwd); arm_out("Left", out); elbow("Left", elb); curl("Left", fist)


def stance(breath=0.0):
    """Thế đứng. 2 tay: kích chéo trước người. 1 tay: kích dựng đứng bên phải, tay trái nắm hờ."""
    for sd in ("Left", "Right"): knee(sd, 14 if TWO_HAND else 10); ankle(sd, -6)
    leg_fwd("Left", 14 if TWO_HAND else 8); leg_fwd("Right", -10 if TWO_HAND else -4)
    if TWO_HAND: twist("Spine", 10); twist("Chest", 8)
    hips_y(-0.012 + breath)
    lean("Chest", 3 + breath * 120)
    hold(((0.0, -0.13, 0.02), (0.45, -0.5, 0.75)),
         ((-0.19, -0.12, -0.02), (0.05, -0.12, 1.0)),
         lambda: free_left(5, -5, 35 + breath * 300, 55))


def anim_idle():
    def a(): stance(0.0); feathers(2); cape(1)
    def b(): stance(0.006); feathers(-2); cape(-1); rot("Head", (1, 0, 0), -2)
    return make_action("Idle", [(1, a), (21, b), (41, a)], True)


def anim_walk():
    def p(lead, contact):
        def fn():
            A, B = ("Left", "Right") if lead > 0 else ("Right", "Left")
            if contact:
                leg_fwd(A, 25); knee(A, 5); ankle(A, -15); leg_fwd(B, -20); knee(B, 15); ankle(B, 20); hips_y(-0.012)
            else:
                knee(A, 8); leg_fwd(B, 25); knee(B, 55); ankle(B, -10); hips_y(0.006)
            twist("Spine", (8 if TWO_HAND else 0) + (4 * lead if contact else 0)); twist("Chest", 6 if TWO_HAND else 0)
            feathers(6 if contact else -6); cape(4 if contact else 8)
            hold(((0.0, -0.13, 0.02 + (0.008 if not contact else 0)), (0.45, -0.5, 0.75)),
                 ((-0.19, -0.12, -0.02 + (0.006 if not contact else 0)), (0.05, -0.15, 1.0)),
                 lambda: free_left(-18 * lead if contact else 0, -5, 30, 45))     # tay trái đánh theo nhịp bước
        return fn
    return make_action("Walk", [(1, p(1, 1)), (7, p(1, 0)), (13, p(-1, 1)), (19, p(-1, 0)), (25, p(1, 1))], True)


def anim_run():
    def p(lead, contact):
        def fn():
            A, B = ("Left", "Right") if lead > 0 else ("Right", "Left")
            lean("Spine", 12); lean("Chest", 6); rot("Head", (1, 0, 0), -14)
            if contact:
                leg_fwd(A, 40); knee(A, 20); ankle(A, -10); leg_fwd(B, -35); knee(B, 35); ankle(B, 25); hips_y(-0.02)
            else:
                knee(A, 30); leg_fwd(A, -5); leg_fwd(B, 55); knee(B, 100); ankle(B, -15); hips_y(0.015)
            twist("Spine", (10 if TWO_HAND else 0) + (5 * lead if contact else 0)); twist("Chest", 6 if TWO_HAND else -4 * lead if contact else 0)
            feathers(18 if contact else 10); cape(25 if contact else 35)
            hold(((0.02, -0.11, -0.04), (0.4, -0.85, 0.35)),
                 ((-0.17, -0.13, -0.04), (0.1, -0.75, 0.65)),                 # kích chĩa về trước - lên
                 lambda: free_left(-40 * lead if contact else 0, -5, 80, 70))    # tay trái đánh mạnh theo nhịp chạy
        return fn
    return make_action("Run", [(1, p(1, 1)), (5, p(1, 0)), (9, p(-1, 1)), (13, p(-1, 0)), (17, p(1, 1))], True)


def anim_attack_sweep():
    """Chém quét ngang: vung kích ra sau bên phải -> quét mạnh sang trái. Trúng đòn khung 11."""
    def idle(): stance(0.0)
    def lift():
        twist("Spine", -10); twist("Chest", -12); lean("Spine", -2)
        leg_fwd("Left", 12); knee("Left", 16); leg_fwd("Right", -12); knee("Right", 20); hips_y(-0.015)
        feathers(-6)
        hold(((-0.01, -0.13, 0.03), (-0.15, -0.1, 1.0), 0.15),
             ((-0.2, -0.16, 0.11), (-0.3, 0.0, 1.0)),
             lambda: free_left(20, -5, 40, 70))
    def windup():
        twist("Spine", -25); twist("Chest", -35 if TWO_HAND else -25); lean("Spine", -4); rot("Head", (0, 0, 1), 20)
        leg_fwd("Left", 15); knee("Left", 20); leg_fwd("Right", -15); knee("Right", 25); hips_y(-0.02)
        feathers(-12); cape(6)
        hold(((-0.03, -0.10, 0.02), (-0.35, 0.55, 0.75), 0.15),
             ((-0.22, -0.01, 0.07), (-0.75, 0.5, 0.42)),                     # kích vung ra sau bên phải
             lambda: free_left(35, -5, 40, 70))                              # tay trái đưa ra trước lấy thăng bằng
    def mid():                                                     # giữa cú vung: lưỡi vòng ra bên phải - phía trước, không quét qua người
        twist("Spine", -5); twist("Chest", -5); lean("Spine", 4)
        leg_fwd("Left", 25); knee("Left", 32); leg_fwd("Right", -22); knee("Right", 18); hips_y(-0.028)
        feathers(5); cape(12)
        hold(((-0.06, -0.12, 0.05), (-0.5, -0.85, 0.1)),
             ((-0.2, -0.12, 0.05), (-0.6, -0.8, 0.08)),
             lambda: free_left(10, 10, 30, 75))
    def strike():
        twist("Spine", 25); twist("Chest", 30 if TWO_HAND else 28); lean("Spine", 12); rot("Head", (0, 0, 1), -12)
        leg_fwd("Left", 35); knee("Left", 45); ankle("Left", -15); leg_fwd("Right", -30); knee("Right", 10); ankle("Right", 20)
        hips_y(-0.035); feathers(20); cape(20)
        hold(((0.03, -0.12, 0.04), (0.1, -1.0, 0.0)),
             ((-0.1, -0.17, 0.02), (0.75, -0.65, 0.05)),                     # lưỡi quét ngang trước mặt
             lambda: free_left(-30, 25, 20, 80))                             # tay trái vung ra sau
    def follow():
        twist("Spine", 38 if TWO_HAND else 30); twist("Chest", 40 if TWO_HAND else 35); lean("Spine", 8)
        leg_fwd("Left", 30); knee("Left", 40); leg_fwd("Right", -25); knee("Right", 12); hips_y(-0.03)
        feathers(8); cape(15)
        hold(((0.05, -0.11, -0.05), (0.6, -0.8, -0.05)),
             ((0.0, -0.17, 0.02), (0.97, -0.2, 0.05)),
             lambda: free_left(-25, 20, 25, 80))
    def recover():
        twist("Spine", 20 if TWO_HAND else 10); twist("Chest", 18 if TWO_HAND else 8); lean("Spine", 4)
        for sd in ("Left", "Right"): knee(sd, 18)
        leg_fwd("Left", 18); leg_fwd("Right", -12); hips_y(-0.02)
        hold(((0.02, -0.14, 0.0), (0.55, -0.6, 0.45)),
             ((-0.18, -0.13, -0.02), (0.12, -0.25, 1.0)),
             lambda: free_left(0, 5, 35, 65))
    return make_action("Attack01", [(1, idle), (4, lift), (7, windup), (9, mid), (11, strike), (15, follow), (21, recover), (28, idle)], False)


def anim_attack_thrust():
    """Đâm thẳng: rút kích về hông phải -> lao người đâm tới. Trúng đòn khung 10."""
    def idle(): stance(0.0)
    def pull():
        twist("Spine", -10); twist("Chest", -20 if TWO_HAND else -15); lean("Spine", -6)
        leg_fwd("Left", 10); knee("Left", 25); leg_fwd("Right", -10); knee("Right", 25); hips_y(-0.025)
        feathers(-10)
        hold(((-0.01, -0.09, -0.05), (0.1, -1.0, 0.1), 0.15),
             ((-0.19, -0.03, -0.04), (0.05, -1.0, 0.18)),                      # kích rút về hông phải, mũi chĩa trước
             lambda: free_left(40, -5, 60, 70))                               # tay trái thủ trước ngực
    def thrust():
        twist("Spine", 15); twist("Chest", 12 if TWO_HAND else 18); lean("Spine", 16); rot("Head", (1, 0, 0), -10)
        leg_fwd("Left", 40); knee("Left", 50); ankle("Left", -15); leg_fwd("Right", -35); knee("Right", 5); ankle("Right", 25)
        hips_y(-0.04); feathers(25); cape(25)
        hold(((0.04, -0.12, -0.03), (0.05, -1.0, -0.03), 0.15),
             ((-0.09, -0.21, 0.0), (0.0, -1.0, 0.28)),
             lambda: free_left(-35, 20, 30, 80))
    def back():
        twist("Spine", 5); twist("Chest", 5); lean("Spine", 6)
        for sd in ("Left", "Right"): knee(sd, 18)
        leg_fwd("Left", 20); leg_fwd("Right", -15); hips_y(-0.02)
        hold(((0.01, -0.14, 0.0), (0.3, -0.85, 0.35)),
             ((-0.16, -0.15, -0.03), (0.1, -0.6, 0.8)),
             lambda: free_left(0, -5, 40, 65))
    return make_action("Attack02", [(1, idle), (4, lambda: stance(0.0)), (7, pull), (10, thrust), (16, thrust), (22, back), (28, idle)], False)


def weapon_preview():
    """Cây kích tạm (chỉ để xem trong Blender, KHÔNG xuất ra FBX)."""
    rig = bpy.data.objects[RIG_NAME]
    name = HERO_NAME + "_WeaponPreview"
    old = bpy.data.objects.get(name)
    if old: bpy.data.objects.remove(old, do_unlink=True)
    bm = bmesh.new()
    def cyl(r, y0, y1, seg=10):
        bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=y1 - y0,
                              matrix=Matrix.Translation((0, (y0 + y1) / 2, 0)) @ Matrix.Rotation(math.radians(-90), 4, 'X'))
    cyl(0.011, -0.45, 0.80)                                         # cán (gốc ở tay cầm)
    bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.02, radius2=0.0, depth=0.16,
                          matrix=Matrix.Translation((0, 0.88, 0)) @ Matrix.Rotation(math.radians(-90), 4, 'X'))   # mũi
    for sg in (1, -1):                                              # 2 lưỡi trăng khuyết
        pts = [(sg * (0.02 + 0.10 * math.sin(math.pi * t)), 0.70 + 0.16 * t) for t in [i / 8 for i in range(9)]]
        vs = []
        for x, y in pts:
            vs.append((bm.verts.new((x, y, -0.004)), bm.verts.new((x, y, 0.004))))
        for i in range(len(vs) - 1):
            bm.faces.new((vs[i][0], vs[i + 1][0], vs[i + 1][1], vs[i][1]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    w = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(w)
    w.parent = rig; w.parent_type = 'BONE'; w.parent_bone = "Socket_Hand_R"
    w.matrix_parent_inverse = Matrix.Identity(4)
    w.location = (0, -rig.data.bones["Socket_Hand_R"].length, 0)
    w.color = (0.75, 0.6, 0.3, 1)
    w.hide_render = False
    return w


def ensure_playback():
    """Bảo đảm da đi theo xương và animation được phát (Blender 4.4+ / 5.x)."""
    rig = bpy.data.objects[RIG_NAME]; body = bpy.data.objects.get(BODY_NAME)
    rig.data.pose_position = 'POSE'
    if body:
        mods = [m for m in body.modifiers if m.type == 'ARMATURE']
        if not mods:
            m = body.modifiers.new("Armature", 'ARMATURE'); mods = [m]
            log("Đã thêm lại bộ điều chỉnh Khung Xương (Armature) cho", body.name)
        for m in mods:
            m.object = rig; m.show_viewport = True; m.show_render = True
        if not any(g.name == "Hips" for g in body.vertex_groups):
            log("CẢNH BÁO: da chưa có weight - hãy chạy STEP = 2")
    ad = rig.animation_data
    if ad and ad.action and hasattr(ad, "action_slot") and getattr(ad, "action_slot", None) is None:
        slots = getattr(ad.action, "slots", None)
        if slots: ad.action_slot = slots[0]


def play(name=None):
    stop_playall()   # tắt NLA test để Action riêng không bị chồng
    rig = bpy.data.objects[RIG_NAME]; sc = bpy.context.scene
    act = bpy.data.actions.get(f"{HERO_NAME}_{name or PLAY}")
    if not act: raise Exception(f"Chưa có animation {name or PLAY} - chạy STEP = \"anim\" trước")
    rig.animation_data_create(); rig.animation_data.action = act
    ensure_playback()
    _defeat_weapon_play(name or PLAY)  # Drop chỉ phát trong Defeat; animation khác giữ đúng vị trí cầm.
    sc.frame_start, sc.frame_end = int(act.frame_range[0]), int(act.frame_range[1])
    sc.render.fps = 30; sc.frame_set(sc.frame_start)
    log(f"Đang xem: {act.name} (khung {sc.frame_start}-{sc.frame_end}). Di chuột vào khung 3D, bấm Space")


def make_all_anims():
    remove_playall()  # NLA cũ còn tham chiếu Action sắp được tạo lại
    to_object_mode()
    unhide_all()
    ensure_playback()
    old = bpy.data.actions.get(HERO_NAME + "_WalkPreview")
    if old: bpy.data.actions.remove(old)
    if not bpy.data.objects.get(HERO_NAME + "_Weapon"): weapon_preview()
    CLEAR_LOG.clear(); TWO_HAND_ERR.clear(); INBETWEEN_LOG.clear(); POLE_ANGLE.clear()
    made = [anim_idle(), anim_walk(), anim_run(), anim_attack_sweep(), anim_attack_thrust()]
    log("Đã tạo:", ", ".join(a.name for a in made), "(thương:", _weapon_obj().name + ")")
    bad = sum(1 for x in CLEAR_LOG if x == -1)
    close = sum(1 for x in CLEAR_LOG if x == -2)
    moved = sum(1 for x in CLEAR_LOG if x > 0)
    log(f"Kiểm tra va chạm: {len(CLEAR_LOG)} tư thế - {bad} tư thế thương XUYÊN vào thân, {close} tư thế hơi sát (dưới {int(CLEAR_MARGIN * 1000) / 10} cm, không xuyên)")
    if INBETWEEN_LOG: log("Đã thêm khung sửa va chạm giữa chừng:", ", ".join(f"{n} +{k}" for n, k in INBETWEEN_LOG))
    log("Sát thương: Attack01 ở khung 11, Attack02 ở khung 10 (đặt Animation Event OnHit trong Unity)")
    play('Idle')


# ------------------------------------------------------------ SLIM: giảm lưới cho model đã gắn xương
def slim():
    """Giảm tam giác cho LuBu_Body (giữ chi tiết bàn tay). Sau đó chạy lại STEP = 2."""
    import numpy as np
    to_object_mode(); reset_all()
    body = bpy.data.objects[BODY_NAME]; rig = bpy.data.objects.get(RIG_NAME)
    me = body.data
    n0 = sum(len(p.vertices) - 2 for p in me.polygons)
    target = DECIMATE_TO or 20000
    if n0 <= target * 1.1:
        log(f"Lưới đã nhẹ ({n0} tam giác), không cần giảm"); return
    # vùng bàn tay (giữ nhiều chi tiết hơn để ngón gập đẹp)
    V = np.array([v.co for v in me.vertices])
    hand = np.zeros(len(V), bool)
    if rig:
        for side in ("Left", "Right"):
            b = rig.data.bones.get(side + "Hand")
            if b:
                c = (np.array(b.head_local) + np.array(b.tail_local)) / 2
                hand |= np.linalg.norm(V - c, axis=1) < b.length * 1.6
    for vg in [g for g in body.vertex_groups if g.name == "_protect"]: body.vertex_groups.remove(vg)
    vg = body.vertex_groups.new(name="_protect")
    vg.add([int(i) for i in np.where(hand)[0]], 1.0, 'REPLACE')
    select_only(body)
    nh = sum(1 for p in me.polygons if hand[p.vertices[0]])
    hand_t = min(4000, max(nh, 1))
    # lượt 1: giảm phần thân (tay giữ nguyên)
    m = body.modifiers.new("_dec1", 'DECIMATE')
    m.ratio = min(1.0, (target + nh) / len(me.polygons)); m.vertex_group = "_protect"
    m.vertex_group_factor = 1000; m.invert_vertex_group = True
    while body.modifiers[0].name != m.name: bpy.ops.object.modifier_move_up(modifier=m.name)
    bpy.ops.object.modifier_apply(modifier=m.name)
    # lượt 2: giảm bàn tay còn khoảng 4000 tam giác
    nh1 = sum(1 for p in me.polygons if any(g.group == body.vertex_groups["_protect"].index for g in me.vertices[p.vertices[0]].groups))
    if nh1 > hand_t:
        m = body.modifiers.new("_dec2", 'DECIMATE')
        m.ratio = (len(me.polygons) - nh1 + hand_t) / len(me.polygons); m.vertex_group = "_protect"
        m.vertex_group_factor = 1000; m.invert_vertex_group = False
        while body.modifiers[0].name != m.name: bpy.ops.object.modifier_move_up(modifier=m.name)
        bpy.ops.object.modifier_apply(modifier=m.name)
    body.vertex_groups.remove(body.vertex_groups["_protect"])
    n1 = sum(len(p.vertices) - 2 for p in me.polygons)
    log(f"Đã giảm lưới: {n0} -> {n1} tam giác. Giờ chạy STEP = 2 để gắn da lại")


# ------------------------------------------------------------ WEAPON: ghép vũ khí thật
def unhide_all():
    """Hiện lại xương / thân / vũ khí nếu đang bị ẩn (ẩn thì script không chọn được)."""
    for n in (RIG_NAME, BODY_NAME, HERO_NAME + "_Weapon"):
        o = bpy.data.objects.get(n)
        if o:
            try: o.hide_set(False)
            except Exception: pass
            o.hide_viewport = False; o.hide_select = False


def weapon_attach():
    import numpy as np, os
    if not WEAPON_FILE or not os.path.exists(WEAPON_FILE):
        raise Exception(f"Không thấy file vũ khí: '{WEAPON_FILE}'. Điền WEAPON_FILE ở đầu script (giữ chữ r trước dấu ngoặc kép)")
    to_object_mode()
    rig = bpy.data.objects[RIG_NAME]
    unhide_all()
    before = set(bpy.data.objects)
    ext = os.path.splitext(WEAPON_FILE)[1].lower()
    if ext in (".glb", ".gltf"): bpy.ops.import_scene.gltf(filepath=WEAPON_FILE)
    elif ext == ".fbx": bpy.ops.import_scene.fbx(filepath=WEAPON_FILE)
    elif ext == ".obj": bpy.ops.wm.obj_import(filepath=WEAPON_FILE)
    else: raise Exception("Chỉ hỗ trợ .glb .gltf .fbx .obj")
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == 'MESH']
    if not meshes: raise Exception("File vũ khí không có lưới (mesh)")
    for o in new:                                  # bỏ cha (empty/armature) của file nhập
        if o.type == 'MESH' and o.parent:
            mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    select_only(*meshes, active=meshes[0])
    if len(meshes) > 1: bpy.ops.object.join()
    w = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in new:
        if o != w and o.name in bpy.data.objects and o.type != 'MESH': bpy.data.objects.remove(o, do_unlink=True)
    for old in (HERO_NAME + "_Weapon", HERO_NAME + "_WeaponPreview"):
        ob = bpy.data.objects.get(old)
        if ob and ob != w: bpy.data.objects.remove(ob, do_unlink=True)
    w.name = HERO_NAME + "_Weapon"
    me = w.data
    n = len(me.polygons)                            # giảm lưới TRƯỚC cho nhanh (file Meshy rất nặng)
    if WEAPON_TRIS and n > WEAPON_TRIS * 1.1:
        select_only(w)
        m = w.modifiers.new("dec", 'DECIMATE'); m.ratio = WEAPON_TRIS / n
        bpy.ops.object.modifier_apply(modifier=m.name)
        log(f"Giảm lưới vũ khí: {n} -> {len(me.polygons)} mặt")
    V = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get("co", V)
    V = V.reshape(-1, 3).astype(np.float64); c = V.mean(0)
    _, _, vt = np.linalg.svd(V - c, full_matrices=False)
    a = vt[0]                                       # trục dài = cán
    t = (V - c) @ a; L = t.max() - t.min()
    def spread(mask):
        Q = V[mask] - c; Q = Q - np.outer(Q @ a, a)
        return np.sqrt((Q ** 2).sum(1)).mean() if mask.any() else 0
    lo_end = spread(t < t.min() + 0.2 * L); hi_end = spread(t > t.max() - 0.2 * L)
    if lo_end > hi_end: a = -a; t = -t           # đầu to (lưỡi) về phía +Y
    if WEAPON_FLIP: a = -a; t = -t
    tip = t > t.max() - 0.25 * L                    # hướng bản rộng của lưỡi -> trục X
    Q = V[tip] - c; Q = Q - np.outer(Q @ a, a)
    _, _, vt2 = np.linalg.svd(Q - Q.mean(0), full_matrices=False)
    wdir = vt2[0] - a * (vt2[0] @ a); wdir /= np.linalg.norm(wdir)
    zdir = np.cross(wdir, a)
    # tìm đoạn CÁN (phần mảnh liên tục tính từ đuôi) để 2 tay chỉ nắm trên cán, không nắm vào lưỡi/tua
    butt = V[t < t.min() + 0.08 * L].mean(0)
    perp = (V - butt) - np.outer((V - butt) @ a, a)
    rr = np.sqrt((perp ** 2).sum(1))
    nb = 60; edges = np.linspace(t.min(), t.max(), nb + 1)
    rad = np.array([np.percentile(rr[(t >= edges[i]) & (t <= edges[i + 1])], 80) if ((t >= edges[i]) & (t <= edges[i + 1])).sum() > 2 else 0 for i in range(nb)])
    r0 = np.percentile(rad[rad > 0], 20)            # bán kính cán
    thr = r0 * 1.8
    thin = (rad > 0) & (rad <= thr)
    best_run, cur = (0, 0), None                    # đoạn cán trơn dài nhất (không vòng trang trí, tua, lưỡi)
    for i in range(nb + 1):
        if i < nb and thin[i]:
            cur = i if cur is None else cur
        elif cur is not None:
            if i - cur > best_run[1] - best_run[0]: best_run = (cur, i)
            cur = None
    s0, s1 = edges[best_run[0]], edges[best_run[1]]
    shaft_len = s1 - s0
    gap_src = GRIP_GAP / (WEAPON_LENGTH / L) if TWO_HAND else 0
    tg = s0 + 0.06 * L * 0 + max(WEAPON_GRIP * shaft_len, 0.05 * L)    # cách vòng trang trí phía dưới ít nhất 5%
    tg = min(tg, s1 - gap_src - 0.05 * L)                                 # tay (trái) vẫn nằm trên cán trơn
    ring = (np.abs(t - tg) < 0.02 * L) & (rr < thr)
    g = V[ring].mean(0) if ring.sum() > 3 else butt + a * ((t.min() + (tg - t.min())) - (butt - c) @ a)
    g = g - a * ((g - c) @ a - tg)
    log(f"Đoạn cán trơn: {int((s0 - t.min()) / L * 100)}%-{int((s1 - t.min()) / L * 100)}% chiều dài; tay phải cầm ở {int((tg - t.min()) / L * 100)}% từ đuôi")
    M = np.stack([wdir, a, zdir])                   # hàng: X, Y, Z mới
    k = WEAPON_LENGTH / L
    roll = np.array(Matrix.Rotation(math.radians(WEAPON_ROLL), 3, 'Y'))
    P2 = ((V - g) @ M.T) * k @ roll.T
    me.vertices.foreach_set("co", P2.astype(np.float32).ravel())
    me.update()
    w.parent = rig; w.parent_type = 'BONE'; w.parent_bone = "Socket_Hand_R"
    w.matrix_parent_inverse = Matrix.Identity(4)
    w.location = (0, -rig.data.bones["Socket_Hand_R"].length, 0)
    w.rotation_euler = (0, 0, 0); w.scale = (1, 1, 1)
    log(f"Đã ghép {w.name}: dài {WEAPON_LENGTH} m, {len(me.polygons)} mặt")
    log("Lưỡi quay sai: WEAPON_ROLL = 90. Cầm ngược: WEAPON_FLIP = True. Tay cầm lệch: đổi WEAPON_GRIP")
    select_only(rig)
    make_all_anims()          # làm lại 5 animation theo đúng cây thương thật (tránh va chạm)


# ------------------------------------------------------------ STEP 3
def step3():
    to_object_mode(); reset_all()
    body = bpy.data.objects[BODY_NAME]; rig = bpy.data.objects[RIG_NAME]
    folder = bpy.path.abspath("//")
    if not folder:
        raise Exception("Hãy LƯU file .blend trước (Ctrl+S), FBX sẽ được xuất vào cùng thư mục")
    for img in bpy.data.images:          # texture đóng gói trong .blend -> ghi ra thư mục textures/
        if img.packed_file:
            try: img.unpack(method='WRITE_LOCAL')
            except Exception as e: log("không tách được texture", img.name, e)
    path = folder + HERO_NAME + "_Rigged.fbx"
    anims = [a for a in bpy.data.actions if a.name.startswith(HERO_NAME + "_") and not a.name.endswith("WalkPreview")]
    bpy.context.scene.render.fps = 30
    wpn = bpy.data.objects.get(HERO_NAME + "_Weapon")
    prev = bpy.data.objects.get(HERO_NAME + "_WeaponPreview")
    if wpn and EXPORT_WEAPON_SEPARATE:
        mw = wpn.matrix_world.copy(); par = (wpn.parent, wpn.parent_type, wpn.parent_bone, wpn.matrix_basis.copy())
        wpn.parent = None; wpn.matrix_world = Matrix.Identity(4)
        select_only(wpn)
        wp = folder + HERO_NAME + "_Weapon.fbx"
        bpy.ops.export_scene.fbx(filepath=wp, use_selection=True, object_types={'MESH'}, apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
            mesh_smooth_type='FACE', bake_anim=False, path_mode='COPY', embed_textures=True)
        wpn.parent, wpn.parent_type, wpn.parent_bone = par[0], par[1], par[2]
        wpn.matrix_parent_inverse = Matrix.Identity(4); wpn.matrix_basis = par[3]
        log("ĐÃ XUẤT vũ khí riêng:", wp)
    sel = [body, rig] + ([wpn] if wpn and WEAPON_IN_CHARACTER else [])
    select_only(*sel, active=rig)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        bake_space_transform=True, mesh_smooth_type='FACE', add_leaf_bones=False,
        use_armature_deform_only=False, primary_bone_axis='Y', secondary_bone_axis='X',
        bake_anim=bool(anims), bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        path_mode='COPY', embed_textures=True)
    if anims: log("kèm animation:", ", ".join(a.name for a in anims))
    log("ĐÃ XUẤT:", path)



# ------------------------------------------------------------ PLAYALL (7 animation x 5 giây)
def stop_playall():
    """Tạm tắt NLA PlayAll; giữ nguyên tất cả Action và strip để lần sau mở lại."""
    rig = bpy.data.objects.get(RIG_NAME)
    _defeat_weapon_stop()  # tắt track weapon drop test (nếu có)
    if not rig or not rig.animation_data:
        return
    track = rig.animation_data.nla_tracks.get(PLAYALL_TRACK)
    if track is not None:
        track.is_solo = False
        track.mute = True


def remove_playall():
    """Xóa track/marker thử cũ trước khi tạo lại Action; KHÔNG xóa Action gốc."""
    rig = bpy.data.objects.get(RIG_NAME)
    if rig and rig.animation_data:
        track = rig.animation_data.nla_tracks.get(PLAYALL_TRACK)
        if track is not None:
            track.is_solo = False
            rig.animation_data.nla_tracks.remove(track)
    # Xóa NLA thử của weapon; không xóa Action rơi.
    w = _weapon_obj()
    if w and w.animation_data:
        tw = w.animation_data.nla_tracks.get(DEFEAT_WEAPON_NLA_TRACK)
        if tw:
            tw.is_solo = False
            w.animation_data.nla_tracks.remove(tw)
    for marker in list(bpy.context.scene.timeline_markers):
        if marker.name.startswith(PLAYALL_MARKER):
            bpy.context.scene.timeline_markers.remove(marker)


def playall():
    """Xem toàn bộ 7 Action trong 35 giây bằng NLA, không sửa keyframe Action gốc.

    Từng Action tự lặp toàn chu kỳ cho đủ 5 giây; không cắt ngang động tác.
    Chỉ tạo lại NLA thử, không tác động trực tiếp tới rig hay vũ khí.
    """
    to_object_mode()
    rig = bpy.data.objects.get(RIG_NAME)
    if rig is None or rig.type != 'ARMATURE':
        raise RuntimeError('Không tìm thấy Armature ' + RIG_NAME + '. Hãy chạy STEP = 1, 2 trước.')

    actions = []
    for name in PLAYALL_ORDER:
        act = bpy.data.actions.get(f'{HERO_NAME}_{name}')
        if act is None:
            raise RuntimeError(f'Thiếu {HERO_NAME}_{name}. Chạy "addanims" để thêm Victory/Defeat, hoặc "anim" nếu thiếu 5 Action gốc.')
        actions.append((name, act))

    sc = bpy.context.scene
    sc.render.fps = 30
    sc.render.fps_base = 1.0
    span = round(PLAYALL_SECONDS * sc.render.fps / sc.render.fps_base)
    if span < 1:
        raise ValueError('PLAYALL_SECONDS phải lớn hơn 0')

    # Kích hoạt pose và skinning trước khi gỡ Action khỏi AnimData.
    rig.animation_data_create()
    ensure_playback()
    remove_playall()
    ad = rig.animation_data
    ad.action = None
    track = ad.nla_tracks.new()
    track.name = PLAYALL_TRACK
    track.mute = False

    for i, (name, act) in enumerate(actions):
        start = 1 + i * span
        a0, a1 = map(float, act.frame_range)
        cycle = a1 - a0
        if cycle <= 0:
            raise ValueError('Animation rỗng hoặc chỉ có một frame: ' + act.name)

        strip = track.strips.new(name, start, act)
        # Blender 4.4+ / 5.x: NLA cần đúng Action Slot của Armature.
        if hasattr(strip, 'action_slot') and hasattr(act, 'slots'):
            compatible = [s for s in act.slots
                          if getattr(s, 'target_id_type', None) in ('OBJECT', 'UNSPECIFIED')]
            if compatible:
                try:
                    strip.action_slot = compatible[0]
                except (TypeError, ValueError, AttributeError):
                    log('Không đặt được Action Slot cho', act.name)

        strip.action_frame_start = a0
        strip.action_frame_end = a1
        loops = max(1, round(span / cycle))
        strip.repeat = float(loops)
        strip.scale = span / (cycle * loops)
        strip.blend_type = 'REPLACE'
        strip.extrapolation = 'NOTHING'
        strip.blend_in = 0
        strip.blend_out = 0
        strip.mute = False

        expected = start + span
        if abs(strip.frame_end - expected) > 0.25:
            log('CẢNH BÁO: NLA', name, 'kết thúc ở', round(strip.frame_end, 2),
                'thay vì', expected)
        sc.timeline_markers.new(
            name=f'{PLAYALL_MARKER}{name} ({i * PLAYALL_SECONDS:g}-{(i+1) * PLAYALL_SECONDS:g}s)',
            frame=start)
        log(f'{name}: frame {start}-{expected-1}, {PLAYALL_SECONDS:g}s, lặp {loops} chu kỳ')

    # Chỉ NLA PlayAll chạy, không ghi đè các Action gốc.
    track.is_solo = True
    _defeat_weapon_playall(span)
    sc.frame_start = 1
    sc.frame_end = span * len(actions)
    sc.frame_set(1)
    bpy.context.view_layer.update()
    log(f'PlayAll: {len(actions)} animation x {PLAYALL_SECONDS:g}s '
        f'= {len(actions)*PLAYALL_SECONDS:g}s, 30 FPS, frame 1-{sc.frame_end}')
    log('Di chuột vào Viewport hoặc Timeline rồi bấm Space để phát. '
        'Đổi STEP = "play" để xem animation riêng.')
    return track



# ------------------------------------------------------------ BONUS ANIMATIONS: Victory / Defeat
# Chỉ ghi keyframe vào 2 Action MỚI. Không tạo rig, sửa mesh, parent hoặc weight.
# Không gọi make_action()/make_all_anims() vì các hàm đó ghi đè Action cũ.
BONUS_NAMES = ("Victory", "Defeat")
ORIGINAL_NAMES = ("Idle", "Walk", "Run", "Attack01", "Attack02")


def _bonus_action_curves(act):
    """Lấy curves để kiểm tra fingerprint cho Action cũ (cả legacy và layered)."""
    try:
        if not getattr(act, "is_action_layered", False):
            return list(act.fcurves)
    except (AttributeError, RuntimeError):
        pass
    found = []
    try:
        for layer in act.layers:
            for strip in layer.strips:
                for slot in act.slots:
                    try:
                        bag = strip.channelbag(slot, ensure=False)
                    except (TypeError, RuntimeError, AttributeError):
                        continue
                    if bag:
                        found.extend(bag.fcurves)
    except (AttributeError, RuntimeError):
        pass
    return found


def _bonus_action_fingerprint(act):
    """Mã kiểm tra nội dung keyframe Action gốc, không thay đổi dữ liệu."""
    import hashlib
    h = hashlib.sha256()
    curves = _bonus_action_curves(act)
    if not curves:
        # Trường hợp Blender API đổi cấu trúc: vẫn bảo vệ danh tính và frame range.
        return (act.as_pointer(), tuple(round(float(x), 4) for x in act.frame_range), None)
    for fc in sorted(curves, key=lambda c: (c.data_path, c.array_index)):
        h.update(repr((fc.data_path, fc.array_index)).encode('utf-8'))
        for kp in fc.keyframe_points:
            h.update(repr((tuple(round(float(x), 8) for x in kp.co),
                           str(kp.interpolation),
                           tuple(round(float(x), 8) for x in kp.handle_left),
                           tuple(round(float(x), 8) for x in kp.handle_right))).encode('utf-8'))
    return (act.as_pointer(), tuple(round(float(x), 4) for x in act.frame_range),
            len(curves), h.hexdigest())


def _bonus_model_signature():
    """Chụp trạng thái cấu trúc, dữ liệu model/rig/vũ khí trước-sau; không sửa gì."""
    body = bpy.data.objects.get(BODY_NAME)
    rig = bpy.data.objects.get(RIG_NAME)
    w = _weapon_obj()
    def obj_sig(o):
        if o is None:
            return None
        return (o.as_pointer(), o.data.as_pointer() if o.data else None,
                o.parent.as_pointer() if o.parent else None,
                o.parent_type, o.parent_bone,
                tuple(round(float(x), 8) for row in o.matrix_basis for x in row))
    body_sig = None
    if body and body.type == 'MESH':
        import array, hashlib, struct
        mesh = body.data
        coords = array.array('f', [0.0]) * (3 * len(mesh.vertices))
        mesh.vertices.foreach_get('co', coords)
        coords_hash = hashlib.sha256(coords.tobytes()).hexdigest()
        weights = hashlib.sha256()
        # Toàn bộ weight thực tế, không chỉ tên vertex group.
        for v in mesh.vertices:
            for g in v.groups:
                weights.update(struct.pack('<IIf', v.index, g.group, float(g.weight)))
        body_sig = (obj_sig(body), len(mesh.vertices), len(mesh.edges),
                    len(mesh.polygons), coords_hash, weights.hexdigest(),
                    tuple((vg.name, vg.index) for vg in body.vertex_groups),
                    tuple((m.name, m.type, m.object.as_pointer() if m.type == 'ARMATURE' and m.object else None)
                          for m in body.modifiers))
    rig_sig = None
    if rig and rig.type == 'ARMATURE':
        rig_sig = (obj_sig(rig), tuple((b.name, b.parent.name if b.parent else None,
                    bool(b.use_deform),
                    tuple(round(float(x), 8) for x in b.head_local),
                    tuple(round(float(x), 8) for x in b.tail_local),
                    tuple(round(float(x), 8) for row in b.matrix_local for x in row))
                    for b in rig.data.bones))
    original_sig = tuple((n, _bonus_action_fingerprint(bpy.data.actions.get(HERO_NAME + '_' + n)))
                         for n in ORIGINAL_NAMES)
    return (body_sig, rig_sig, obj_sig(w), original_sig)


def _bonus_pose_capture(rig):
    state = {}
    for pb in rig.pose.bones:
        state[pb.name] = (pb.rotation_mode, pb.rotation_quaternion.copy(),
                          pb.rotation_euler.copy(), tuple(pb.rotation_axis_angle),
                          pb.location.copy(), pb.scale.copy())
    return state


def _bonus_pose_apply(rig, state, force_quaternion=False):
    for pb in rig.pose.bones:
        mode, q, euler, axis_angle, loc, scale = state[pb.name]
        pb.rotation_mode = 'QUATERNION' if force_quaternion else mode
        pb.rotation_quaternion = q.copy()
        pb.rotation_euler = euler.copy()
        pb.rotation_axis_angle = axis_angle
        pb.location = loc.copy()
        pb.scale = scale.copy()


def _bonus_bind_action(rig, act):
    """Gán đúng Action Slot cho Blender 4.4/5.x nếu có, legacy giữ nguyên."""
    ad = rig.animation_data
    ad.action = act
    if hasattr(ad, 'action_slot') and getattr(ad, 'action_slot', None) is None:
        for slot in getattr(act, 'slots', []):
            if getattr(slot, 'target_id_type', None) in ('OBJECT', 'UNSPECIFIED'):
                try:
                    ad.action_slot = slot
                    break
                except (AttributeError, TypeError, ValueError):
                    pass


def _bonus_victory_pose(power, beat=0.0, salute=0.0):
    """Vui mừng: đứng vững, nâng kích bằng tay phải, tay trái giơ nắm đấm."""
    rig = bpy.data.objects[RIG_NAME]
    hip = rig.pose.bones['Hips']
    hip.location += Vector((0.0, -0.006 * power, 0.012 * beat))
    lean('Spine', -9 * power)
    lean('Chest', -11 * power)
    twist('Chest', (7 + 5 * salute) * power)
    rot('Head', (1, 0, 0), -13 * power)
    rot('Head', (0, 0, 1), 8 * salute)
    arm_fwd('Right', 34 * power)
    arm_out('Right', 13 * power)
    elbow('Right', 8 * power)
    # Không xoay riêng cổ tay/ngón phải: ngón + Socket_Hand_R giữ nguyên
    # so với Idle; cây kích gắn socket sẽ luôn theo bàn tay phải.
    arm_fwd('Left', 65 * power)
    arm_out('Left', 35 * power)
    elbow('Left', 25 * power)
    curl('Left', 22 * power)
    leg_fwd('Left', 8 * beat)
    knee('Left', 7 * beat)
    knee('Right', 5 * beat)
    feathers(9 * (power + beat))
    cape(-9 * power + 4 * beat)


def _bonus_defeat_pose(fall, recoil=0.0, settle=0.0):
    """Trúng đòn -> loạng choạng -> quỵ gối -> đổ người nằm nghiêng bất động."""
    rig = bpy.data.objects[RIG_NAME]
    hips = rig.pose.bones['Hips']
    # Xoay Hips (xương gốc chuyển động), KHÔNG đổi transform của Armature.
    rot('Hips', (1, 0, 0), 88 * fall - 17 * recoil)
    rot('Hips', (0, 1, 0), 19 * fall)
    rot('Hips', (0, 0, 1), 8 * fall)
    hips.location += Vector((0.022 * fall, -0.10 * fall + 0.015 * recoil,
                             -0.145 * fall - 0.007 * settle))
    lean('Spine', 23 * fall - 14 * recoil)
    lean('Chest', 16 * fall - 9 * recoil)
    side_bend('Chest', 13 * fall)
    rot('Head', (1, 0, 0), 14 * fall - 6 * recoil)
    rot('Head', (0, 1, 0), 10 * fall)
    knee('Left', 65 * fall + 14 * recoil)
    knee('Right', 75 * fall + 10 * recoil)
    leg_fwd('Left', 23 * fall - 9 * recoil)
    leg_fwd('Right', -17 * fall + 6 * recoil)
    ankle('Left', 10 * fall)
    ankle('Right', -13 * fall)
    arm_out('Left', 22 * fall)
    arm_fwd('Left', -25 * fall)
    elbow('Left', -15 * fall)
    arm_fwd('Right', -18 * fall)
    arm_out('Right', 11 * fall)
    # Giữ tư thế cầm kích: không sửa Socket_Hand_R hoặc ngón tay phải.
    feathers(23 * fall + 12 * recoil)
    cape(18 * fall + 10 * recoil)


def _bonus_build_action(rig, action_name, poses, base_pose):
    """Chỉ tạo Action mới; mọi channel key đều thuộc đúng Action này."""
    name = HERO_NAME + '_' + action_name
    if bpy.data.actions.get(name):
        log('ĐÃ CÓ', name, '- bỏ qua, không ghi đè keyframe')
        return None
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    prev_q = {}
    ad = rig.animation_data
    try:
        for frame, pose_fn, params in poses:
            ad.action = None  # tránh nội suy Action vừa tạo đè lên tư thế đầu vào
            _bonus_pose_apply(rig, base_pose, force_quaternion=True)
            pose_fn(*params)
            for pb in rig.pose.bones:
                q = pb.rotation_quaternion.copy()
                # mathutils.Quaternion KHÔNG có thuộc tính .length (Blender 4.x/5.x).
                # Dùng bốn thành phần w/x/y/z, kiểm tra NaN/Inf và norm gần 0.
                norm_sq = sum(float(component) ** 2 for component in q)
                if not math.isfinite(norm_sq) or norm_sq < 1e-16:
                    raise ValueError(f'Quaternion không hợp lệ: {pb.name} frame {frame}')
                q.normalize()
                old = prev_q.get(pb.name)
                if old is not None and old.dot(q) < 0:
                    q.negate()  # chống đảo dấu quaternion giữa 2 keyframe
                pb.rotation_quaternion = q
                prev_q[pb.name] = q.copy()
            _bonus_bind_action(rig, act)
            for pb in rig.pose.bones:
                pb.keyframe_insert(data_path='rotation_quaternion', frame=frame)
                if pb.name == 'Hips':
                    pb.keyframe_insert(data_path='location', frame=frame)
        if tuple(round(float(x)) for x in act.frame_range) != (1, 151):
            raise RuntimeError(f'Sai frame range {name}: {tuple(act.frame_range)}')
        if not _bonus_action_curves(act):
            raise RuntimeError(f'Action {name} không có F-Curve được kiểm chứng')
        log('ĐÃ THÊM', name, '- 1..151 frame, 5s @ 30fps, giữ nguyên model/rig')
        return act
    except Exception:
        ad.action = None
        bpy.data.actions.remove(act)
        raise


def add_bonus_anims():
    """Thêm Victory+Defeat an toàn. Không tái tạo bất cứ Action nào có sẵn."""
    rig = bpy.data.objects.get(RIG_NAME)
    body = bpy.data.objects.get(BODY_NAME)
    if not rig or rig.type != 'ARMATURE' or not body or body.type != 'MESH':
        raise RuntimeError('Cần file .blend đã rig: thiếu ' + RIG_NAME + ' hoặc ' + BODY_NAME)
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        raise RuntimeError('Chuyển về Object Mode trước khi chạy STEP="addanims"')
    required = ('Hips', 'Spine', 'Chest', 'Head', 'LeftHand', 'RightHand',
                'RightUpperArm', 'RightLowerArm', 'Socket_Hand_R')
    if any(n not in rig.pose.bones for n in required):
        raise RuntimeError('Rig đang thiếu xương cần thiết để tạo animation an toàn')
    source = bpy.data.actions.get(HERO_NAME + '_Idle')
    if source is None:
        raise RuntimeError('Cần Action ' + HERO_NAME + '_Idle để lấy tư thế cầm kích chuẩn. Không dựng rig lại.')
    if all(bpy.data.actions.get(HERO_NAME+'_'+name) is not None for name in BONUS_NAMES):
        log('Đã có cả Victory và Defeat; KHÔNG tạo lại hoặc ghi đè. Chọn STEP="playall" để xem.')
        return

    before = _bonus_model_signature()
    rig.animation_data_create()
    ad = rig.animation_data
    original_action = ad.action
    original_slot = getattr(ad, 'action_slot', None)
    original_frame = bpy.context.scene.frame_current
    original_subframe = bpy.context.scene.frame_subframe
    original_pose = _bonus_pose_capture(rig)
    nla_state = [(t, t.mute, t.is_solo) for t in ad.nla_tracks]
    newly_created = []
    try:
        for t, _mute, _solo in nla_state:
            t.is_solo = False
            t.mute = True
        _bonus_bind_action(rig, source)
        bpy.context.scene.frame_set(int(source.frame_range[0]))
        bpy.context.view_layer.update()
        base_pose = _bonus_pose_capture(rig)
        ad.action = None

        victory = [
            (1,   _bonus_victory_pose, (0.0, 0.0, 0.0)),
            (12,  _bonus_victory_pose, (0.22, 0.0, 0.0)),
            (25,  _bonus_victory_pose, (0.92, 0.8, 0.4)),
            (40,  _bonus_victory_pose, (1.0, 0.1, 0.9)),
            (57,  _bonus_victory_pose, (0.88, -0.35, -0.6)),
            (76,  _bonus_victory_pose, (1.0, 0.85, 0.9)),
            (98,  _bonus_victory_pose, (0.93, 0.0, -0.2)),
            (121, _bonus_victory_pose, (1.0, 0.20, 0.7)),
            (151, _bonus_victory_pose, (0.95, 0.0, 0.2)),
        ]
        defeat = [
            (1,   _bonus_defeat_pose, (0.0, 0.0, 0.0)),
            (9,   _bonus_defeat_pose, (0.0, 0.9, 0.0)),
            (19,  _bonus_defeat_pose, (0.18, 0.9, 0.0)),
            (34,  _bonus_defeat_pose, (0.43, 0.25, 0.0)),
            (52,  _bonus_defeat_pose, (0.72, 0.0, 0.0)),
            (70,  _bonus_defeat_pose, (1.0, 0.0, 0.0)),
            (83,  _bonus_defeat_pose, (0.96, 0.0, 0.0)),
            (101, _bonus_defeat_pose, (1.0, 0.0, 1.0)),
            (151, _bonus_defeat_pose, (1.0, 0.0, 1.0)),
        ]
        for name, poses in (('Victory', victory), ('Defeat', defeat)):
            act = _bonus_build_action(rig, name, poses, base_pose)
            if act is not None:
                newly_created.append(act)
        after = _bonus_model_signature()
        if before != after:
            raise RuntimeError('Kiểm tra bảo toàn dữ liệu THẤT BẠI: mesh, rig, vũ khí hoặc Action cũ đã đổi')
        log('SOÁT XONG: mesh, bones/rest pose, modifier, weight group, liên kết vũ khí và 5 Action cũ không đổi')
        log('Các animation mới giữ ngón tay phải nắm kích và không sửa Socket_Hand_R')
        log('Để xem: STEP="play" với PLAY="Victory"/"Defeat", hoặc STEP="playall" (35 giây)')
    except Exception:
        ad.action = None
        for act in newly_created:
            if bpy.data.actions.get(act.name) is not None:
                bpy.data.actions.remove(act)
        log('Đã hoàn tác những Action mới tạo trong lượt bị lỗi; dữ liệu gốc không bị sửa')
        raise
    finally:
        ad.action = None
        _bonus_pose_apply(rig, original_pose)
        if original_action:
            _bonus_bind_action(rig, original_action)
            if original_slot is not None and hasattr(ad, 'action_slot'):
                try:
                    ad.action_slot = original_slot
                except (AttributeError, TypeError, ValueError):
                    pass
        for t, mute, solo in nla_state:
            t.mute = mute
            t.is_solo = solo
        bpy.context.scene.frame_set(original_frame, subframe=original_subframe)
        bpy.context.view_layer.update()



# ------------------------------------------------------------ DEFEAT v2: DROP -> KNEEL -> FACE-DOWN
# Hoàn toàn không sửa Mesh, rest pose, weight, parenting hay 5 Action gốc.
# WeaponDrop là Action riêng trên OBJECT vũ khí, bake theo WORLD để khi buông kích
# không còn dính theo Socket_Hand_R dù parent vẫn giữ nguyên.
DEFEAT_WEAPON_ACTION = HERO_NAME + "_WeaponDrop_Defeat"
DEFEAT_WEAPON_NLA_TRACK = "Arena3Q_PlayAll_WeaponDrop"
DEFEAT_WEAPON_REST_KEY = "Arena3Q_Defeat_WeaponGripBasis"
DEFEAT_RELEASE_FRAME = 19
DEFEAT_LAND_FRAME = 62


def _defeat_v2_pose(hit=0.0, kneel=0.0, fall=0.0, open_hand=0.0):
    """Đòn đánh -> GẬP GỐI RÕ Ở KHUNG 58-76 -> úp người về phía trước.

    Xương gốc Hips thay đổi tư thế; không thay object matrix/parent/rest pose.
    """
    rig = bpy.data.objects[RIG_NAME]
    h = rig.pose.bones['Hips']
    # Sau khi khuỵu: trục vai/thân còn dựng; chỉ tới >=88 mới ngã sấp.
    rot('Hips', (1, 0, 0), 87.0 * fall - 8.0 * hit * (1.0 - fall))
    rot('Hips', (0, 0, 1), 3.0 * hit * (1.0 - fall))
    h.location += Vector((0.0,
                          0.018 * hit - 0.048 * kneel - 0.185 * fall,
                          -0.148 * kneel * (1.0 - fall) - 0.216 * fall))
    # Khi quỳ gần như không gập thân: người quỵ gối trước, sau đó đổ sấp.
    lean('Spine', -13 * hit * (1-fall) + 11 * fall)
    lean('Chest', -7 * hit * (1-fall) + 5 * fall)
    rot('Head', (1, 0, 0), -6 * hit * (1-fall) - 8 * fall)
    for side, diff in (('Left', 0), ('Right', -8)):
        # Hai gối thật sự gập sâu 100-112 độ ở khoảng f58-f76.
        knee(side, (112 + diff) * kneel * (1 - 0.84 * fall) + 16 * fall + 8 * hit * (1-fall))
        leg_fwd(side, (25 if side == 'Left' else 16) * kneel * (1 - fall) - 10 * fall)
        ankle(side, -11 * kneel * (1-fall) + 3 * fall)
    # Sau khi thả, tay không còn cố giữ cây kích; 2 tay đưa ra đỡ khi đổ sấp.
    arm_fwd('Right', -9 * hit * (1-fall) + 60 * fall)
    arm_out('Right', 10 * kneel + 17 * fall)
    elbow('Right', 10 * kneel + 20 * fall)
    arm_fwd('Left', 20 * hit + 65 * fall)
    arm_out('Left', 8 * kneel + 15 * fall)
    elbow('Left', 25 * kneel + 18 * fall)
    # Mở ngón tay phải để thể hiện việc thả cán (key chỉ nằm trong Defeat).
    ident = Quaternion((1.0, 0.0, 0.0, 0.0))
    for pb in rig.pose.bones:
        if pb.name.startswith('Right') and any(f in pb.name for f in
                ('Thumb', 'Index', 'Middle', 'Ring', 'Little')):
            pb.rotation_quaternion = pb.rotation_quaternion.slerp(ident, min(1.0, max(0.0, open_hand)))
    feathers(7 * hit + 13 * fall)
    cape(10 * kneel + 20 * fall)


def _defeat_v2_poses():
    # f58 đến f76 giữ dáng QUỲ; f88 mới đổ người và f116 chạm sàn.
    return [
        (1,   _defeat_v2_pose, (0.0, 0.0, 0.0, 0.0)),
        (10,  _defeat_v2_pose, (0.70, 0.0, 0.0, 0.0)),
        (19,  _defeat_v2_pose, (1.0, 0.05, 0.0, 1.0)),  # BUÔNG KÍCH
        (32,  _defeat_v2_pose, (0.72, 0.30, 0.0, 1.0)),
        (45,  _defeat_v2_pose, (0.42, 0.70, 0.0, 1.0)),
        (58,  _defeat_v2_pose, (0.12, 1.0, 0.0, 1.0)), # HẠ GỐI
        (76,  _defeat_v2_pose, (0.12, 1.0, 0.0, 1.0)), # GIỮ TƯ THẾ QUỲ
        (88,  _defeat_v2_pose, (0.08, 1.0, 0.25, 1.0)),
        (102, _defeat_v2_pose, (0.03, 0.87, 0.70, 1.0)),
        (116, _defeat_v2_pose, (0.0, 0.65, 1.0, 1.0)), # NGÃ SẤP
        (134, _defeat_v2_pose, (0.0, 0.65, 1.0, 1.0)),
        (151, _defeat_v2_pose, (0.0, 0.65, 1.0, 1.0)),
    ]


def _defeat_unique_backup(name):
    """Không bao giờ xóa Action cũ; đặt tên backup duy nhất."""
    base = name + '_Backup_BeforeKneel'
    if bpy.data.actions.get(base) is None:
        return base
    i = 2
    while bpy.data.actions.get(f'{base}_{i:02d}') is not None:
        i += 1
    return f'{base}_{i:02d}'


def _defeat_rest_basis(w):
    """Lưu ma trận grip ở custom property để các lượt chạy script sau vẫn phục hồi được."""
    data = w.get(DEFEAT_WEAPON_REST_KEY)
    if data is not None and len(data) == 16:
        return Matrix([list(map(float, data[i*4:(i+1)*4])) for i in range(4)])
    return w.matrix_basis.copy()


def _defeat_record_grip_basis(w, basis):
    w[DEFEAT_WEAPON_REST_KEY] = [float(v) for row in basis for v in row]


def _defeat_weapon_action_bind(w, name):
    ad = w.animation_data_create()
    ad.action = bpy.data.actions.get(name)
    if ad.action and getattr(ad, 'action_slot', None) is None:
        for slot in getattr(ad.action, 'slots', []):
            if getattr(slot, 'target_id_type', None) in ('OBJECT', 'UNSPECIFIED'):
                try:
                    ad.action_slot = slot
                except (TypeError, ValueError, AttributeError):
                    pass
                break


def _defeat_weapon_stop():
    """Tắt Action drop ở chế độ xem khác; lấy lại grip gốc, KHÔNG đổi parent."""
    w = _weapon_obj()
    if w is None:
        return
    ad = w.animation_data
    if ad:
        track = ad.nla_tracks.get(DEFEAT_WEAPON_NLA_TRACK)
        if track:
            track.is_solo = False
            track.mute = True
        if ad.action and ad.action.name == DEFEAT_WEAPON_ACTION:
            ad.action = None
    if DEFEAT_WEAPON_REST_KEY in w:
        w.matrix_basis = _defeat_rest_basis(w)
    bpy.context.view_layer.update()


def _defeat_build_weapon_action(rig, w, rig_action, basis):
    """Tạo action tự rơi của kích, bù chuyển động rig từng frame 20..151.

    Parent BONE vẫn nguyên. Mỗi khung ghi local transform ngược chuyển động
    Socket_Hand_R để kết quả WORLD không dính tay sau khi thả.
    """
    sc = bpy.context.scene
    ad = w.animation_data_create()
    was_w_action = ad.action
    old_frame = sc.frame_current
    old_sub = sc.frame_subframe
    if w.rotation_mode not in ('XYZ', 'XZY', 'YXZ', 'YZX', 'ZXY', 'ZYX', 'QUATERNION'):
        raise RuntimeError('Chưa hỗ trợ rotation_mode của vũ khí: ' + w.rotation_mode)
    act = bpy.data.actions.new(DEFEAT_WEAPON_ACTION)
    act.use_fake_user = True
    prev_euler = None
    try:
        ad.action = None
        rig.animation_data.action = rig_action
        # Trước khi rơi, vẫn nằm đúng vị trí cầm gốc của cây kích.
        w.matrix_basis = basis.copy()
        sc.frame_set(DEFEAT_RELEASE_FRAME)
        bpy.context.view_layer.update()
        released_world = w.matrix_world.copy()
        grip_pos = released_world.to_translation()
        grip_rot = released_world.to_quaternion()
        start_dir = (grip_rot @ Vector((0, 1, 0))).normalized()
        target_dir = Vector((-0.16, -0.98, 0.02)).normalized()
        end_rot = start_dir.rotation_difference(target_dir) @ grip_rot
        # Tính đáy mesh cho cả cây kích (lưỡi + cán), để đáp xuống gần mặt đất.
        verts = w.data.vertices if w.data and w.type == 'MESH' else []
        sample_step = max(1, len(verts) // 1200)
        sampled = [verts[i].co.copy() for i in range(0, len(verts), sample_step)] if verts else [Vector((0,0,0))]
        # điểm rơi bên phải tướng, không bay xuyên qua bụng khi gục xuống
        lateral = Vector((-0.27, -0.13, 0.0))
        for f in range(1, 152):
            ad.action = None
            sc.frame_set(f)
            bpy.context.view_layer.update()
            if f <= DEFEAT_RELEASE_FRAME:
                w.matrix_basis = basis.copy()
            else:
                t = min(1.0, max(0.0, (f - DEFEAT_RELEASE_FRAME) /
                                    (DEFEAT_LAND_FRAME - DEFEAT_RELEASE_FRAME)))
                smooth = t * t * (3.0 - 2.0 * t)
                qq = grip_rot.slerp(end_rot, smooth)
                min_lz = min((qq @ p).z for p in sampled)
                final_grip_z = -min_lz + 0.022
                pos = grip_pos + lateral * smooth
                # Giai đoạn rơi tăng tốc, giảm tốc nhẹ khi chạm sàn.
                pos.z = grip_pos.z * (1 - t*t) + final_grip_z * (t*t)
                pos.z = max(pos.z, -min_lz + 0.012)
                desired = Matrix.Translation(pos) @ qq.to_matrix().to_4x4()
                # Copy scale nguyên bản của weapon (mesh không đổi)
                scale = released_world.to_scale()
                desired = desired @ Matrix.Diagonal((scale.x, scale.y, scale.z, 1))
                w.matrix_world = desired
                bpy.context.view_layer.update()
            # Chèn key trên OBJECT vũ khí, không key xương socket và không detach parent.
            _defeat_weapon_action_bind(w, DEFEAT_WEAPON_ACTION)
            if w.rotation_mode == 'QUATERNION':
                w.keyframe_insert(data_path='rotation_quaternion', frame=f)
            else:
                if prev_euler is not None:
                    e = w.rotation_euler.copy()
                    e.make_compatible(prev_euler)
                    w.rotation_euler = e
                prev_euler = w.rotation_euler.copy()
                w.keyframe_insert(data_path='rotation_euler', frame=f)
            w.keyframe_insert(data_path='location', frame=f)
            w.keyframe_insert(data_path='scale', frame=f)
        for fc in _bonus_action_curves(act):
            for point in fc.keyframe_points:
                point.interpolation = 'LINEAR'
        if not _bonus_action_curves(act):
            raise RuntimeError('WeaponDrop không có keyframe; hủy Action mới')
        log(f'Đã tạo {act.name}: rơi ở frame {DEFEAT_RELEASE_FRAME}, '
            f'chạm sàn ở frame {DEFEAT_LAND_FRAME}, không thay parent của vũ khí')
        return act
    except Exception:
        ad.action = None
        if bpy.data.actions.get(act.name):
            bpy.data.actions.remove(act)
        raise
    finally:
        ad.action = was_w_action
        w.matrix_basis = basis.copy()
        sc.frame_set(old_frame, subframe=old_sub)
        bpy.context.view_layer.update()


def _defeat_weapon_play(name):
    w = _weapon_obj()
    if w is None: return
    _defeat_weapon_stop()
    if name == 'Defeat' and bpy.data.actions.get(DEFEAT_WEAPON_ACTION):
        _defeat_weapon_action_bind(w, DEFEAT_WEAPON_ACTION)
        bpy.context.view_layer.update()


def _defeat_weapon_playall(span):
    """Track vũ khí chỉ có strip ở 5 giây Defeat (cuối PlayAll)."""
    w = _weapon_obj()
    act = bpy.data.actions.get(DEFEAT_WEAPON_ACTION)
    if w is None or act is None: return
    _defeat_weapon_stop()
    ad = w.animation_data_create()
    old = ad.nla_tracks.get(DEFEAT_WEAPON_NLA_TRACK)
    if old:
        old.is_solo = False
        ad.nla_tracks.remove(old)
    track = ad.nla_tracks.new()
    track.name = DEFEAT_WEAPON_NLA_TRACK
    track.mute = False
    track.is_solo = True
    # Đúng đoạn Defeat trong PLAYALL_ORDER, không dùng repeat cho action rơi.
    idx = PLAYALL_ORDER.index('Defeat')
    start = 1 + idx * span
    strip = track.strips.new('WeaponDrop', start, act)
    if hasattr(strip, 'action_slot'):
        for slot in getattr(act, 'slots', []):
            if getattr(slot, 'target_id_type', None) in ('OBJECT', 'UNSPECIFIED'):
                try: strip.action_slot = slot
                except (TypeError, ValueError, AttributeError): pass
                break
    strip.action_frame_start = float(act.frame_range[0])
    strip.action_frame_end = float(act.frame_range[1])
    strip.scale = span / max(1e-6, strip.action_frame_end - strip.action_frame_start)
    strip.repeat = 1
    strip.blend_type = 'REPLACE'
    strip.extrapolation = 'NOTHING'
    strip.blend_in = 0
    strip.blend_out = 0
    bpy.context.view_layer.update()
    log(f'PlayAll: vũ khí tự rơi trong Defeat (frame {start}-{start+span-1}); '
        '6 đoạn còn lại giữ grip gốc')


def fix_defeat_kneel_drop():
    """Thay duy nhất LuBu_Defeat; backup Action cũ; thêm weapon object Action.

    Đã có Defeat thì không gọi STEP=addanims: thao tác này CỐ Ý thay Defeat.
    Bất cứ lỗi nào sẽ khôi phục tên Action cũ, Action/NLA/pose/timeline.
    """
    rig = bpy.data.objects.get(RIG_NAME)
    body = bpy.data.objects.get(BODY_NAME)
    w = _weapon_obj()
    if not rig or rig.type != 'ARMATURE' or not body or body.type != 'MESH':
        raise RuntimeError('Thiếu rig/mesh Lữ Bố. Không được dựng lại rig trong fixdefeat.')
    if w is None or w.type != 'MESH':
        raise RuntimeError('Không có LuBu_Weapon hoặc LuBu_WeaponPreview. Hãy mở file có cây kích để tạo hiệu ứng rơi.')
    if w.parent != rig or w.parent_type != 'BONE' or w.parent_bone != 'Socket_Hand_R':
        raise RuntimeError('Cây kích chưa parent vào Socket_Hand_R; không thay parent tự động để tránh hỏng model.')
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        raise RuntimeError('Chuyển Object Mode rồi chạy STEP="fixdefeat"')
    idle = bpy.data.actions.get(HERO_NAME + '_Idle')
    if idle is None:
        raise RuntimeError('Không có LuBu_Idle để lấy tư thế grip gốc')
    for n in ('Hips', 'Spine', 'Chest', 'Head', 'RightHand', 'LeftHand',
              'RightUpperLeg', 'LeftUpperLeg', 'RightLowerLeg', 'LeftLowerLeg'):
        if n not in rig.pose.bones: raise RuntimeError('Thiếu xương ' + n)
    before = _bonus_model_signature()
    rig.animation_data_create()
    ad = rig.animation_data
    original_action = ad.action
    original_slot = getattr(ad, 'action_slot', None)
    w.animation_data_create()
    wad = w.animation_data
    original_w_action = wad.action
    original_w_slot = getattr(wad, 'action_slot', None)
    w_basis = _defeat_rest_basis(w)
    frame = bpy.context.scene.frame_current
    subframe = bpy.context.scene.frame_subframe
    pose = _bonus_pose_capture(rig)
    rigs_nla = [(t, t.mute, t.is_solo) for t in ad.nla_tracks]
    weapon_nla = [(t, t.mute, t.is_solo) for t in wad.nla_tracks]
    original_defeat = bpy.data.actions.get(HERO_NAME + '_Defeat')
    previous_drop = bpy.data.actions.get(DEFEAT_WEAPON_ACTION)
    original_defeat_name = original_defeat.name if original_defeat else None
    previous_drop_name = previous_drop.name if previous_drop else None
    created = []
    saved_rest_prop = list(w[DEFEAT_WEAPON_REST_KEY]) if DEFEAT_WEAPON_REST_KEY in w else None
    successful = False
    try:
        for tr, _, _ in rigs_nla: tr.mute = True; tr.is_solo = False
        for tr, _, _ in weapon_nla: tr.mute = True; tr.is_solo = False
        wad.action = None
        w.matrix_basis = w_basis.copy()
        _bonus_bind_action(rig, idle)
        bpy.context.scene.frame_set(1)
        bpy.context.view_layer.update()
        base_pose = _bonus_pose_capture(rig)
        ad.action = None
        # Rename only after all prechecks; Blender auto-retains old data under backup.
        if original_defeat:
            original_defeat.use_fake_user = True
            original_defeat.name = _defeat_unique_backup(original_defeat_name)
            log('Đã giữ Defeat cũ:', original_defeat.name)
        if previous_drop:
            previous_drop.use_fake_user = True
            previous_drop.name = _defeat_unique_backup(previous_drop_name)
        new_defeat = _bonus_build_action(rig, 'Defeat', _defeat_v2_poses(), base_pose)
        if new_defeat is None: raise RuntimeError('Không thể tạo Defeat mới')
        created.append(new_defeat)
        new_wact = _defeat_build_weapon_action(rig, w, new_defeat, w_basis)
        created.append(new_wact)
        ad.action = None
        wad.action = None
        w.matrix_basis = w_basis.copy()
        bpy.context.view_layer.update()
        after = _bonus_model_signature()
        # w.matrix_basis có thể đang được Action/NLA cũ điều khiển lúc bắt đầu,
        # nên so sánh cấu trúc parent và rest basis đã lưu, không so pose đánh giá lúc phát.
        if (before[0] != after[0] or before[1] != after[1]
                or before[2][:5] != after[2][:5] or before[3] != after[3]):
            raise RuntimeError('Kiểm tra mesh/rig/weight/weapon-parent/5 Action cũ KHÔNG ĐẠT; hoàn tác')
        for i in range(4):
            for j in range(4):
                if abs(w.matrix_basis[i][j] - w_basis[i][j]) > 1e-5:
                    raise RuntimeError('Transform grip của cây kích chưa phục hồi')
        _defeat_record_grip_basis(w, w_basis)
        successful = True
        log('ĐÃ SOÁT: mesh, UV, weight, rest bones, parent kích và 5 Action cũ giữ nguyên')
        log('Defeat mới: f19 rơi kích, f58-76 quỳ gối rõ, f116 úp mặt xuống sàn; 5 giây @ 30 FPS')
        log('STEP="play", PLAY="Defeat" để xem riêng; STEP="playall" để xem 35 giây')
    except Exception:
        ad.action = None; wad.action = None
        for new in created:
            if bpy.data.actions.get(new.name): bpy.data.actions.remove(new)
        if original_defeat: original_defeat.name = original_defeat_name
        if previous_drop: previous_drop.name = previous_drop_name
        if saved_rest_prop is None:
            if DEFEAT_WEAPON_REST_KEY in w: del w[DEFEAT_WEAPON_REST_KEY]
        else:
            w[DEFEAT_WEAPON_REST_KEY] = saved_rest_prop
        log('LỖI: Đã hoàn tác Defeat/WeaponDrop mới, giữ Action gốc')
        raise
    finally:
        ad.action = None
        _bonus_pose_apply(rig, pose)
        if original_action is not None:
            _bonus_bind_action(rig, original_action)
            if original_slot is not None and hasattr(ad, 'action_slot'):
                try: ad.action_slot = original_slot
                except (AttributeError, TypeError, ValueError): pass
        wad.action = None if successful and original_w_action == previous_drop else original_w_action
        if not successful and original_w_action and original_w_slot is not None and hasattr(wad, 'action_slot'):
            try: wad.action_slot = original_w_slot
            except (AttributeError, TypeError, ValueError): pass
        w.matrix_basis = w_basis.copy()
        for tr, mute, solo in rigs_nla: tr.mute = mute; tr.is_solo = solo
        for tr, mute, solo in weapon_nla: tr.mute = mute; tr.is_solo = solo
        bpy.context.scene.frame_set(frame, subframe=subframe)
        bpy.context.view_layer.update()
    if successful:
        bpy.context.scene.render.fps = 30
        bpy.context.scene.render.fps_base = 1.0
        # NLA PlayAll cũ giữ tham chiếu Action Defeat backup: bỏ track TEST để phải tạo lại.
        # Không xóa/sửa bất kỳ Action gốc nào.
        remove_playall()
        _defeat_weapon_stop()
        log('Đã gỡ PlayAll thử cũ để lần sau xây dựng lại với Defeat mới')

# ------------------------------------------------------------ BUILDALL từ gói Lữ Bố gốc mới
# Không viết đè lubu.blend, giữ nguyên texture/UV island gốc.
BUILD_OUTPUT = "LuBu_Arena3Q_7Anims.blend"
SAFE_TEXTURE = "LuBu_texture_UV_Safe.png"
EXPECTED_ACTIONS = ("Idle", "Walk", "Run", "Attack01", "Attack02", "Victory", "Defeat")


def _source_folder():
    import os
    path = bpy.data.filepath
    if not path:
        raise RuntimeError("Hãy mở file lubu.blend đã giải nén trong Blender trước khi chạy buildall")
    return os.path.dirname(os.path.abspath(path))


def _resolve_weapon_path():
    import os
    global WEAPON_FILE
    if WEAPON_FILE and os.path.isfile(WEAPON_FILE):
        return WEAPON_FILE
    f = os.path.join(_source_folder(), 'phuong-tien-hoa-kich.glb')
    if not os.path.isfile(f):
        raise FileNotFoundError("Thiếu phuong-tien-hoa-kich.glb cùng thư mục với .blend")
    WEAPON_FILE = f
    return f


def _uv_fingerprint(mesh):
    """Kiểm tra bảo toàn UV kể cả thứ tự loop và giá trị float32."""
    import array, hashlib
    layers = mesh.uv_layers
    if not layers or not layers.active:
        raise RuntimeError("Mesh gốc thiếu UVMap: dừng để tránh texture sai")
    uv = layers.active.data
    vals = array.array('f', [0.0]) * (len(uv)*2)
    uv.foreach_get('uv', vals)
    return hashlib.sha256(vals.tobytes()).hexdigest()


def _geom_fingerprint(mesh):
    return (len(mesh.vertices), len(mesh.edges), len(mesh.polygons), len(mesh.loops), _uv_fingerprint(mesh))


def _apply_safe_texture():
    """Chỉ thay image trong Image Texture node, không sửa UV, shader links hay màu bên trong UV islands."""
    import os
    image_path = os.path.join(_source_folder(), SAFE_TEXTURE)
    if not os.path.isfile(image_path):
        raise FileNotFoundError("Không thấy texture đã đệm mép UV: " + image_path)
    body = bpy.data.objects.get(BODY_NAME)
    if body is None:
        meshes = [x for x in bpy.context.scene.objects if x.type == 'MESH']
        if len(meshes) != 1:
            raise RuntimeError("Chỉ thay texture tự động khi model có duy nhất một mesh; nếu nhiều phần hãy kiểm tra thủ công")
        body = meshes[0]
    mats = [m for m in body.data.materials if m and m.use_nodes]
    nodes = [n for m in mats for n in m.node_tree.nodes if n.type == 'TEX_IMAGE']
    if not nodes:
        raise RuntimeError("Model không có Image Texture node; dừng thay texture")
    existing = [n.image for n in nodes]
    new = bpy.data.images.load(image_path, check_existing=True)
    if tuple(new.size) != (4096, 4096):
        raise RuntimeError("Texture mới không đúng 4096x4096: không thay UV atlas")
    new.colorspace_settings.name = 'sRGB'
    for node in nodes:
        node.image = new
        # Tránh hình ảnh lặp bên ngoài 0..1; bảo vệ mipmap ở sát đường mép UV.
        node.extension = 'EXTEND'
        node.interpolation = 'Linear'
    new.pack()
    log("Đã nạp UV-padded texture: 4096x4096; giữ vật liệu/nodes/UV nguyên")
    return (new, existing)


def _report_build():
    body=bpy.data.objects.get(BODY_NAME)
    rig=bpy.data.objects.get(RIG_NAME)
    w=bpy.data.objects.get(HERO_NAME+'_Weapon')
    if not body or not rig or not w:
        raise RuntimeError("Thiếu Body/Rig/Weapon: chưa đạt điều kiện lưu cuối")
    assert w.parent == rig and w.parent_type == 'BONE' and w.parent_bone == 'Socket_Hand_R', 'Weapon sai socket'
    names=[f'{HERO_NAME}_{n}' for n in EXPECTED_ACTIONS]
    missing=[n for n in names if bpy.data.actions.get(n) is None]
    if missing: raise RuntimeError('Thiếu các animation: '+', '.join(missing))
    if bpy.data.actions.get(DEFEAT_WEAPON_ACTION) is None:
        raise RuntimeError('Chưa tạo Action thả kích của Defeat')
    if not body.data.uv_layers.active:
        raise RuntimeError('Thiếu UV: lỗi nghiêm trọng, không lưu')
    armmods=[m for m in body.modifiers if m.type=='ARMATURE' and m.object==rig]
    if not armmods:raise RuntimeError('Body thiếu Armature modifier đến rig')
    if not body.vertex_groups.get('Hips'):raise RuntimeError('Body thiếu group Hips')
    if bpy.context.scene.frame_end != 1050:
        log('CẢNH BÁO: PlayAll frame_end=',bpy.context.scene.frame_end,'(dự kiến 1050)')
    log(f"KẾT QUẢ: {len(body.data.vertices)} vertices, {len(body.data.polygons)} mặt, {len(rig.data.bones)} xương, 7 Action + WeaponDrop")
    log("PlayAll: 35 giây (1050 frame, 30 FPS)")


def buildall():
    """Một lần chạy: copy nguyên bản -> UV safe -> rig -> weight -> kích -> 7 action -> PlayAll -> save.

    Không sửa file gốc; nếu lỗi giữa chừng, file gốc vẫn an toàn.
    Không tự xuất FBX: để người dùng kiểm tra chuyển động trước.
    """
    import os
    to_object_mode()
    original_path=bpy.path.abspath('//')
    folder=_source_folder()
    _resolve_weapon_path()
    if not os.path.isfile(os.path.join(folder, SAFE_TEXTURE)):
        raise FileNotFoundError('Chưa đặt LuBu_texture_UV_Safe.png cạnh file .blend')
    output=os.path.join(folder,BUILD_OUTPUT)
    current=os.path.abspath(bpy.data.filepath)
    if os.path.normcase(current)==os.path.normcase(output):
        raise RuntimeError('Đang mở file kết quả. Dùng STEP="finishall" để tiếp tục, không chạy lại STEP="buildall"')
    if os.path.exists(output):
        raise RuntimeError('File kết quả đã tồn tại; không ghi đè: '+output)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if len(meshes)!=1 or bpy.data.objects.get(RIG_NAME):
        raise RuntimeError('buildall yêu cầu file gốc có MỘT mesh chưa rig; để bảo vệ dữ liệu, không gộp/ghi đè scene có rig')
    reference=_geom_fingerprint(meshes[0].data)
    log('Model nguồn:',len(meshes[0].data.vertices),'verts,',len(meshes[0].data.polygons),'mặt; UV checksum',reference[-1][:12])
    # Trước khi sửa BẤT CỨ THỨ GÌ, Save As bản khác; original .blend không bị đụng đến.
    bpy.ops.wm.save_as_mainfile(filepath=output, check_existing=False)
    log('Đã lưu bản làm việc RIÊNG:',output)
    # Giảm lưới = 0, merge by distance = bỏ qua nếu có UV.
    _apply_safe_texture()
    step1()
    if _geom_fingerprint(bpy.data.objects[BODY_NAME].data)!=reference:
        raise RuntimeError('Sau STEP1: số đỉnh/mặt/UV thay đổi; dừng, không tiếp tục')
    log('SOÁT STEP1: giữ nguyên 100% vertices/polygons/UV')
    step2()
    if _geom_fingerprint(bpy.data.objects[BODY_NAME].data)!=reference:
        raise RuntimeError('Sau STEP2: UV/mesh thay đổi ngoài dự kiến')
    log('SOÁT STEP2: UV/mesh giữ nguyên, đã thêm vertex groups')
    # Không gọi STEP=anim riêng; weapon_attach đã tạo 5 Action theo kích thật.
    finishall()


def finishall():
    """Tiếp tục một file đã có rig nếu bị ngắt giữa chừng; không dựng lại xương/weights."""
    _resolve_weapon_path()
    to_object_mode()
    rig = bpy.data.objects.get(RIG_NAME)
    body = bpy.data.objects.get(BODY_NAME)
    if not rig or not body:
        raise RuntimeError('Chưa rig/weight xong. Mở file gốc và dùng buildall')
    if not body.vertex_groups.get('Hips'):
        raise RuntimeError('Body chưa gắn weight; chạy STEP=2 trước')
    if not bpy.data.objects.get(HERO_NAME+'_Weapon'):
        weapon_attach()
    else:
        if any(bpy.data.actions.get(f'{HERO_NAME}_{n}') is None for n in EXPECTED_ACTIONS[:5]):
            make_all_anims()
    if not bpy.data.actions.get(HERO_NAME+'_Victory') or not bpy.data.actions.get(HERO_NAME+'_Defeat'):
        add_bonus_anims()
    if not bpy.data.actions.get(DEFEAT_WEAPON_ACTION):
        fix_defeat_kneel_drop()
    playall()
    _report_build()
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath, check_existing=False)
    log('ĐÃ LƯU model/rig/7 animation vào file kết quả:',bpy.data.filepath)


def coloronly():
    """Chỉ áp dụng texture UV-safe vào file SAO CHÉP; không sửa model gốc."""
    import os
    folder=_source_folder()
    output=os.path.join(folder,'LuBu_UV_Safe_Only.blend')
    if os.path.exists(output):
        raise RuntimeError('Không ghi đè file đã có: '+output)
    bpy.ops.wm.save_as_mainfile(filepath=output,check_existing=False)
    _apply_safe_texture()
    bpy.ops.wm.save_as_mainfile(filepath=output,check_existing=False)
    log('Đã tạo bản sửa UV riêng:', output)


def run():
    if STEP == "buildall": buildall()
    elif STEP == "finishall": finishall()
    elif STEP == "coloronly": coloronly()
    elif STEP == 1: step1()
    elif STEP == 2: step2()
    elif STEP == "test": test_pose()
    elif STEP == "walk": walk()
    elif STEP == "anim": make_all_anims()
    elif STEP == "addanims": add_bonus_anims()
    elif STEP == "fixdefeat": fix_defeat_kneel_drop()
    elif STEP == "play": play()
    elif STEP == "playall": playall()
    elif STEP == "slim": slim()
    elif STEP == "weapon": weapon_attach()
    elif STEP == "reset": reset_all(); log("Đã về tư thế gốc (đã bỏ animation thử)")
    elif STEP == 3: step3()
    else: log("STEP không hợp lệ:", STEP, '- dùng 1, 2, 3, "test", "walk", "reset", "anim", "addanims", "fixdefeat", "play", "playall", "weapon", "slim"')


LOG.clear()
log(f"AutoRig {VERSION} - STEP = {STEP!r} - {HERO_NAME}")
try:
    ov = view3d_override()
    if ov:
        with bpy.context.temp_override(**ov): run()
    else:
        run()
    popup(f"AutoRig {VERSION}: XONG", 'CHECKMARK')
except Exception as e:
    import traceback; traceback.print_exc()
    if isinstance(e, KeyError):
        log("LỖI: không tìm thấy", e, "- kiểm tra HERO_NAME giống lúc chạy STEP 1, hoặc chạy STEP 1 trước")
    else:
        log("LỖI:", e)
    import traceback as _tb
    last = [f for f in _tb.extract_tb(e.__traceback__) if f.filename == "AutoRig" or f.filename.endswith(".py")]
    if last: log(f"(dòng {last[-1].lineno}: {last[-1].line})")
    popup(f"AutoRig {VERSION}: LỖI", 'ERROR')
