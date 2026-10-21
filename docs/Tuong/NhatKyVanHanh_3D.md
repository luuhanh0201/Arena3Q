# Nhật ký vận hành – thiết kế 3D, animation, VFX

Dùng chung cho **mọi agent** (Claude, Codex) khi làm model 3D, rig / weight / LOD, animation, VFX, map 3D và đường hiển thị 3D. Yêu cầu của người dùng (21/10/2026):

> Trong lúc sửa (bắt đầu từ Mặc Ảnh), hễ đi **hướng sai**, gặp **lỗi** hay **bất kỳ bug nào** thì phải ghi vào đây, để lần sau không dính lại.

## Quy tắc ghi
- **Ghi ngay khi phát hiện**, kể cả khi đã sửa xong trong vài phút, khi lỗi do mình gây ra, hay khi đó chỉ là hướng thử rồi bỏ. Không đợi cuối việc mới ghi.
- Mỗi mục có:
  - ngày, agent, phần việc;
  - **chuyện gì xảy ra**;
  - **nguyên nhân gốc**;
  - **phát hiện bằng cách nào**;
  - **quy tắc phòng tránh** (làm gì khác đi lần sau);
  - bằng chứng (commit, ảnh, log, test).
- Mục mới thêm **trên cùng** của bảng tương ứng. Không xoá mục cũ. Nếu quy tắc thay đổi, ghi mục mới và dẫn tới mục cũ.
- Khác với các file khác:
  - `docs/Tuong/Model3D_Rules.md` là **luật bắt buộc** đã chắt lọc. Khi một bài học ở đây thành luật chung, chép thêm vào đó.
  - `docs/NhatKySuaLoi.md` là nhật ký sửa lỗi gameplay/code chung. Lỗi gameplay vẫn ghi ở đó; lỗi thuộc 3D / animation / VFX / pipeline đồ hoạ ghi ở đây.
- Trước khi bắt đầu một phần việc 3D / animation / VFX: đọc lướt file này cùng `Model3D_Rules.md`.

## 1. Model, rig, weight, LOD

| Ngày · agent · phần | Chuyện gì xảy ra | Nguyên nhân gốc | Phát hiện | Phòng tránh | Bằng chứng |
|---|---|---|---|---|---|
| 21/10 · Claude · Lữ Bố LOD | Kiểm da LOD báo hàng trăm cạnh lỗi ở mọi clip, kể cả với chính hình đã duyệt. Hướng "so số cạnh lỗi của LOD với hình Blender" là **hướng sai**. | Thước đo cạnh so với rest không dùng được trên lưới đã giảm: cạnh dài của LOD nối qua khớp gập thì đổi độ dài dây cung dù bề mặt đúng. Lần đầu còn lấy hình chuẩn bằng Corrective Smooth chạy lại trên LOD; 30 vòng chỉnh cho 98 nghìn đỉnh làm LOD bị mượt quá tay. | Hình chuẩn (thân full chuyển sang LOD) cũng có 588–1409 cạnh "lỗi" ở Idle frame 1 | Hình chuẩn của LOD = biến dạng của **thân full** chuyển sang LOD (điểm gần nhất, barycentric). Đo cạnh LOD **so với hình chuẩn**, không so với rest; đạt khi 0 cạnh giãn/ép thêm. Không chạy lại modifier làm mượt trên bản đã giảm lưới. | `ExportLuBuBattle3D.py`, `Art/Heroes/LuBu/Graphics3D/LuBu_Battle_Audit.json` |
| 21/10 · Claude · Lữ Bố LOD | Unity skin tuyến tính 4 xương lệch so với Blender (DQ + Corrective Smooth). Top-4 thường: RMS 11 mm, gấp đôi số cạnh lỗi. | Blender bật Preserve Volume (dual quaternion) và Corrective Smooth. Unity không có cả hai. | Audit `plain_*` trong JSON | Khớp lại weight tuyến tính ≤ 4 xương theo hình chuẩn trên mọi khung mẫu (bình phương tối thiểu, w ≥ 0, tổng 1, kéo nhẹ về weight cũ). Ứng viên = xương của đỉnh và đỉnh lân cận. Kết quả RMS 2–5,6 mm. Vẫn chưa đạt 0 cạnh lỗi so với hình chuẩn (163–354 / clip) – còn mở. | như trên |
| 21/10 · Claude · đạo cụ map | Phiến đá giảm lưới còn 1 500 tam giác thành khối méo, UV nhoè. | Collapse quá mạnh (≈ 0,1 % số tam giác gốc) trên vật cứng góc cạnh. | Ảnh close-up trước/sau | Vật cứng góc cạnh (phiến, cột vuông) ≥ 6 nghìn; luôn xem close-up sau giảm lưới, không chỉ ảnh toàn cảnh. Một vật mỗi lúc chỉ có vài cái hiện thì được ngân sách cao hơn. | `NghiemThu/P2_DaoCu3D/slab_zoom.png` |

## 2. Animation, model trong trận

| Ngày · agent · phần | Chuyện gì xảy ra | Nguyên nhân gốc | Phát hiện | Phòng tránh | Bằng chứng |
|---|---|---|---|---|---|
| 21/10 · Claude · Mặc Ảnh 3D (P3a) | Bóng ảnh DodgeAttack thành mảng trắng khổng lồ phủ kín màn hình, lại thiếu quạt. | `CaptureDirectAfterimage` dùng `BakeMesh(false)` rồi nhân `localToWorldMatrix`, nên áp scale 100 của FBX thêm lần nữa (bẫy đã có trong Model3D_Rules). Lại chỉ gộp `SkinnedMeshRenderer`, bỏ sót phần `MeshRenderer` cứng. | Test `CodexMacAnhDirectTests` của Codex và ảnh chụp các chiêu trong trận | Nướng mesh: `BakeMesh(true)` + chỉ vị trí/xoay, hoặc tự skin bằng `bones[i].localToWorldMatrix * bindposes[i]`. Gộp **mọi** renderer của model. Assert chiều cao/bounds so với bản đang chạy. Luôn chụp ảnh trong trận của mọi hiệu ứng mới, không chỉ test logic. | `NghiemThu/Review_MacAnh3D_dot1/MacAnh3D_12khoanh_khac.png` |
| 21/10 · Claude · Lữ Bố skill (test) | Đo sát thương / tầng ngay sau bước mô phỏng thấy lệch (thêm tầng, sai tỉ lệ). | Tướng không có animation chiêu kết thúc chiêu ngay bước phát động nên đánh thường luôn trong cùng bước. | `LuBuSkillsTests` | Đo kết quả của chiêu trong sự kiện `HeroSkills.Used`. Với model 3D, clip kỹ năng chạy tiếp phần thu đòn (`HeroModel3DView`), không khoá gameplay. | `NhatKySuaLoi.md` mục Lữ Bố |

## 3. VFX

| Ngày · agent · phần | Chuyện gì xảy ra | Nguyên nhân gốc | Phát hiện | Phòng tránh | Bằng chứng |
|---|---|---|---|---|---|
| 21/10 · Claude · VFX 3D (P1) | Tia sáng đứng của MagicArray / Ultimate cao vượt vòng trận (hệ số 1,333 thay cho 1,0). | Khi chuyển sang billboard, `stretchToGround` vẫn dùng `projection` của đường 2.5D (0,819 cố định cho hiệu ứng đứng, scale.y/radius = 1 cho hiệu ứng nằm đất). Billboard không bị camera nén nên phải dùng `GroundProjection`. | Test `GroundAnchoredBillboardsUseCameraProjection` của Codex | Khi đổi phép chiếu, rà **mọi** chỗ dùng hệ số nén cũ (`ViewYScale`, `projection`, `renderProjection`). So chân hiệu ứng với vòng logic đặt cùng chỗ bằng ảnh. | `NghiemThu/Review_MacAnh3D_dot1/Loi2_VongPhep_Footprint.png` |
| 21/10 · Claude · A3 hoà khung | Flipbook đứng yên 3–5 khung rồi nhảy ở 60 FPS. | Chọn `floor(frame)` cho `SpriteRenderer`, không hoà khung kế. | Đo khác biệt giữa các khung 60 Hz (WindSlash 34/60 bước đứng yên) | Lớp flipbook = 1 quad phủ 2 khung, UV và vùng atlas riêng cho từng khung, `_FrameBlend` theo đồng hồ hiệu ứng. Test bằng điểm ảnh, không chỉ đọc chỉ số khung. | `NghiemThu/A3_FrameBlend/` |

## 4. Map 3D, hiển thị chung

| Ngày · agent · phần | Chuyện gì xảy ra | Nguyên nhân gốc | Phát hiện | Phòng tránh | Bằng chứng |
|---|---|---|---|---|---|
| 21/10 · Claude · mặt đất mesh | Dải viền ngoài ảnh nền thành các sọc dọc xấu. | UV kẹp (clamp) về mép ảnh nên một hàng điểm ảnh bị kéo dài nhiều mét. | Ảnh toàn cảnh sau đổi | Viền dùng UV **phản chiếu lặp**. Đường lưới trùng đúng mép ảnh để chỗ gấp không méo. Luôn xem ảnh toàn cảnh, không chỉ ảnh trong trận. | `NghiemThu/P2_MatDat3D/` |
| 21/10 · Claude · nền 3D (thiết kế) | (Quyết định, tránh lỗi) Mặt đất ghi độ sâu sẽ che nửa dưới của billboard và hiệu ứng đứng ở mặt đất. | Billboard đứng xuyên mặt phẳng đất. | Phân tích trước khi làm | Mặt đất vẽ đầu tiên (`Queue = Background`), `ZWrite Off`. Đồ nằm đất xếp theo `sortingOrder`. Phiến đá "nâng lên" bằng scale chiều cao, không bằng hạ xuống dưới đất. | `Ground3DView`, `BaguaFormationView` |

## 5. Công cụ, pipeline, phối hợp

| Ngày · agent · phần | Chuyện gì xảy ra | Nguyên nhân gốc | Phát hiện | Phòng tránh | Bằng chứng |
|---|---|---|---|---|---|
| 21/10 · Claude · test hiệu năng | Batch PlayMode treo 18 phút không log. | `WaitForEndOfFrame` không bao giờ chạy trong `-batchmode`. | Log đứng yên sau "Loaded scene" | Trong test batch dùng `yield return null`. Đặt `[Timeout]` cho test chờ khung. Chạy test mới **riêng một mình** trước khi chạy cả bộ. | `RenderBudgetTests.cs` |
| 21/10 · Claude · test hiệu năng | File test mới làm hỏng biên dịch cả project, chặn lượt Unity của Codex. | `Assert.Warn` không có trong NUnit của Test Framework 1.8. | Codex báo lỗi CS0117 | Không thêm file vào `Assets/` khi agent khác đang giữ Unity mà chưa tự biên dịch được. Nếu buộc phải thêm, chỉ dùng API đã có trong project. Báo ngay khi đã sửa. | `claude/inbox/tu-codex_20261021-0629_*` |
| 21/10 · Claude · đo lượt vẽ | `ProfilerRecorder` "Draw Calls Count" ra 0 trong batch. | Batch không vẽ khung thật. | `Logs/RenderStats.json` | Số lượt vẽ đo trong Editor mở hoặc trên máy. Batch chỉ đếm tam giác. | `Logs/RenderStats.json` |
| 21/10 · Claude · Blender | Script Blender báo lỗi Python nhưng tiến trình vẫn exit 0. Danh sách object của view layer trả `None` sau khi xoá object. | Blender không trả mã lỗi; `view_layer.objects` còn chỗ của object đã xoá. | Đọc log | Luôn tìm dòng kết thúc (`*_DONE`) và `Traceback` trong log. Bỏ chọn bằng `bpy.ops.object.select_all(action="DESELECT")`. | `Logs/lubu3d-export.log` |
| 21/10 · Claude · script | Chữ Việt trong code bị vỡ thành `Máº·c áº¢nh`. | PowerShell 5.1 đọc `.ps1` không BOM theo ANSI. | `grep "á»"` | Script sửa file: UTF-8, chỉ ASCII trong chuỗi thay thế, hoặc dùng Edit. Sau khi ghi luôn kiểm `á»` / `Ã`. | — |
| 21/10 · Claude · Unity | Dừng Unity giữa lượt thì lượt sau thoát ngay (khoá `Temp/UnityLockfile`). Script cũ còn chạy tiếp bước EditMode. | Lệnh nhiều bước tiếp tục khi bước trước bị dừng. | `tasklist`, dòng lệnh của tiến trình | Trước khi dừng Unity, xem dòng lệnh (`Win32_Process`) để chắc là tiến trình của mình. Chờ cả chuỗi lệnh kết thúc. Không xoá lockfile khi Unity khác còn chạy. | — |
