# Quy tắc làm việc người dùng đã giao (mọi agent)

Gom các quy tắc người dùng đã nói rải rác qua các phiên, để **mọi agent (Claude, Codex) cùng đọc một chỗ**. Quy tắc gameplay vẫn ở `docs/GameplaySpecification.md`. Luật kỹ thuật 3D ở `Model3D_Rules.md`. Quy tắc mới thì thêm vào đây (ngày + lời người dùng nếu có).

## 1. Giao tiếp và tiến độ
- Người dùng nói tiếng Việt: trả lời và viết tài liệu tiếng Việt.
- Làm theo **từng bước**, có kế hoạch trước khi tạo file. Mỗi bước một commit, **commit xong push ngay** lên `origin` (không cần hỏi). Không force-push. Không commit file không thuộc việc của mình.
- **Không hỏi lại điều đã chốt** (2026-10-17): tra `docs/` trước. Ý mới mâu thuẫn với điều đã ghi (nhất là mục ✅) thì nói rõ mâu thuẫn và đề xuất cách giải quyết, không lặng lẽ chọn.
- Báo cáo trung thực: test nào thật sự đã chạy, cái gì chưa kiểm, cái gì còn hỏng.
- Máy 16 GB RAM: build nặng (Android IL2CPP) có thể bị kill. Báo người dùng tự chạy hoặc đóng bớt ứng dụng.

## 2. Code và dữ liệu
- **Code phải dễ sửa về sau** (2026-10-17):
  - số / danh sách / nội dung nằm trong ScriptableObject, không hard-code;
  - luật thuần trong `Arena3Q.Core`;
  - phần sẽ thay (tài khoản, máy chủ, lưu hồ sơ, video) nằm sau interface;
  - luồng chạy theo dữ liệu.
- **Cân bằng độ khó để tới giai đoạn sắp phát hành** (2026-10-16): khi số đổi thì báo lại, không tự chỉnh lại.
- **Chỉ commit code và asset nhỏ** (< ~1 MB; 2026-10-20). Atlas sprite, FBX lớn, .blend, texture lớn để ở máy. **Không dùng Git LFS** (2026-10-21); báo cáo ghi rõ file nào không có trong git.
- File Unity tự sinh khi build/test (`Assets/Settings/UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/*`) không commit cùng việc.
- **Không xoá cứng file 2D cũ**: chuyển sang `D:\UnityProjects\Arena3Q_Archive_2D` (giữ cấu trúc + `.meta`, ghi vào README ở đó).
- File 3D (blend, script Blender) của tướng nào được người dùng **chốt** thì chuyển ra khỏi `docs/` (đề xuất `ArtSource/Heroes/<Tướng>/`) và sửa mọi đường dẫn.

## 3. Hình ảnh, 3D, VFX
- **Mọi chỉnh sửa nhỏ về hình** đều có ảnh **trước – sau** cùng góc, cùng khung. Lưu ảnh vào `AgentComms/claude/NghiemThu/…` hoặc `NghiemThu/<Tướng>/XemTruoc_<Việc>_<NgàyThángNăm>/`, và **mở cho người dùng xem** trước khi làm phần tiếp theo (2026-10-20).
- **Tối thiểu 60 FPS** mọi nền tảng (spec §1, 2026-10-21). Đo trên máy thật mới tính; FPS Editor hoặc con số target không phải bằng chứng. Hiện người dùng **chỉ test trong Unity**, chưa build điện thoại.
- Flipbook hiệu ứng hoà khung theo đồng hồ hiệu ứng; sprite nhân vật không hoà khung.
- **VFX phép của Mưu Sĩ là "khí"**: đậm nhạt, xuất hiện / trúng / tan tự nhiên, không phải nét vẽ. Gió / khói **không bao giờ** là xoắn tuần hoàn hay các đường đều nhau; chỉ hình học trận pháp được đều (`Model3D_Rules.md`).
- **Luồng khí đi lên không làm thành tia / đường line** (2026-10-21): không có lõi sáng sắc, không có mép cứng, không thẳng tắp. Làm thành dòng khí mờ: bề rộng thay đổi theo chiều cao, mép tan bằng noise, đậm ở chân rồi loãng và tan dần khi lên cao, cuộn nhẹ theo gió. Nhìn ở zoom trận phải đọc ra "khí bốc lên", không phải "tia sáng".
- Mọi việc model / rig / animation / VFX theo `Model3D_Rules.md`. Bắt buộc đo bề mặt sau skinning và xem ảnh thật. Test transform pass không đủ để bàn giao.
- **Gặp hướng sai, lỗi, bug khi làm 3D / animation / VFX: ghi ngay vào `NhatKyVanHanh_3D.md`** (2026-10-21, cả hai agent).
- Hiển thị 3D (2026-10-21): camera orthographic nghiêng 41,4°, mặt đất mesh + texture. Model các tướng khác do người dùng tự làm (agent chỉ làm khi được giao trực tiếp).

## 4. Hai agent (AgentComms)
- Giao việc qua `AgentComms/` (giao thức: `AgentComms/README.md`, trạng thái và khoá Unity: `AgentComms/STATUS.md`). Vai làm / review đổi thường xuyên; **luôn đọc ghi chú `DOI-VAI` / chia việc mới nhất** trước khi động vào file.
- Không sửa file agent kia đang giữ. Unity chỉ một phiên một lúc: xin / nhả khoá trong STATUS.
- **Làm xong phần nhỏ thì báo ngay** cho agent kia kiểm (đường dẫn, SHA nguồn, log, ảnh trước–sau). Giữa các bước luôn xem inbox của mình (2026-10-20).
- Sau khi xong việc: chờ **1 giờ** theo dõi inbox. Có việc mới thì làm tiếp; hết giờ không có gì thì ghi "dừng – hết giờ chờ" vào STATUS. Áp dụng khi người dùng không giao việc khác.
- **Review**:
  - tự chạy lại kiểm tra, không nhận test pass của bên làm là đã nghiệm thu;
  - mỗi vòng báo **% đạt so với tài liệu tướng**, chia theo mục (Mặc Ảnh: §17 `MacAnh_Production_Spec_v1_1.md`), giữ cùng thang điểm giữa các vòng;
  - yêu cầu sửa tới mức người dùng đặt (2026-10-21: mỗi mục §17 ≥ 9/10).
- **Thư mục nghiệm thu** (2026-10-20): chỉ khi review đạt mới mở một đợt trong `NghiemThu/` (ảnh trong trận theo pha, preview VFX, model nhiều góc, số đo JSON, README kết luận + %). Không ghi đè đợt cũ.
