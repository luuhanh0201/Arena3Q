# Quy tắc làm việc người dùng đã giao (mọi agent)

Gom các quy tắc người dùng đã nói rải rác qua các phiên, để **mọi agent (Claude, Codex) cùng đọc một chỗ**. Quy tắc gameplay vẫn ở `docs/GameplaySpecification.md`. Luật kỹ thuật 3D ở `Model3D_Rules.md`. Quy tắc mới thì thêm vào đây (ngày + lời người dùng nếu có).

## 1. Giao tiếp và tiến độ
- Khi mở test hoặc preview trong Unity, tắt nhạc nền (người dùng 27/10/2026). Dùng cấu hình `StartupUi.muteMusicInEditor`, giữ thao tác kiểm im nhạc ngay từ lúc nguồn nhạc được tạo.
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
- **Nguồn VFX: tận dụng hiệu ứng có sẵn, không tự dựng từ đầu** (người dùng 2026-10-22): lấy từ gói VFX có sẵn (Asset Store, gói miễn phí / người dùng mua), rồi **chỉnh cho hợp dự án**:
  - chuyển vật liệu sang URP (`ParticleUpgrader` của Unity), xoay từ trục lên Y sang trục lên −Z của trận, chạy theo đồng hồ trận, cỡ theo bán kính chiêu (`ParticleFxPresenter`, `ThirdPartyVfxBuilder`);
  - sau đó mới chỉnh màu, nhịp, độ lớn theo style của tướng (bảng màu, "khí" của Mưu Sĩ).
  - Người dùng chọn hiệu ứng (gửi ảnh / video từng hiệu ứng để xác nhận trước khi thay).
  - File của gói để trong `Assets/ThirdParty/` (đã `.gitignore`: giấy phép Asset Store không cho phát tán lại, repo đang **công khai**). Dữ liệu tướng trỏ tới chúng qua `MacAnhPerformance.thirdPartyEffects`; máy không có file thì tự về hiệu ứng cũ.
  - Chỉ tự dựng (script Blender → flipbook) khi không gói nào dùng được. Các flipbook tự làm hiện có giữ làm phương án dự phòng.
- **Hiệu ứng hiện và tan tự nhiên** (người dùng 2026-10-22): mọi hiệu ứng, từ lúc xuất hiện tới lúc mất đi, phải có chuyển động hiện / tan (mờ dần, tan theo noise, thu nhỏ…), **không bao giờ bật / tắt cái rụp**, kể cả khi chiêu bị huỷ, tướng chết, buff hết hay hiệu ứng được trả về pool (dùng `MacAnhFx.FadeOut`, không `Stop`). Khi kiểm, chụp cả khung đầu và khung cuối của hiệu ứng.
- **Việc chính của agent hiện nay** (người dùng 2026-10-22): **sửa chuyển động / hoạt ảnh của tướng cho hợp với chiêu** và **kiểm tra kỹ để không có lỗi**:
  - mỗi chiêu: đà, thời điểm ra đòn khớp mốc sự kiện (event) và hiệu ứng, hướng nhìn, theo đà, thu về; chuyển clip mượt, chân không trượt khi đi / chạy (test chân chạm đất);
  - kiểm bằng test, ảnh / chuỗi khung trong trận theo pha, và trên máy thật; ghi mọi lỗi vào `NhatKyVanHanh_3D.md`.
- **Mọi chỉnh sửa nhỏ về hình** đều có ảnh **trước – sau** cùng góc, cùng khung. Lưu ảnh vào `AgentComms/claude/NghiemThu/…` hoặc `NghiemThu/<Tướng>/XemTruoc_<Việc>_<NgàyThángNăm>/`, và **mở cho người dùng xem** trước khi làm phần tiếp theo (2026-10-20).
- **UI làm và duyệt trên web trước** (người dùng 2026-10-28): mọi việc giao diện (HUD trận, Frontend, sảnh, map Chương 1) sửa và cho người dùng xem trên bản web trước; APK chỉ build khi cần nghiệm thu trên máy hoặc khi người dùng yêu cầu, để tránh build APK nhiều lần. Muốn vậy, **chế độ "Điện thoại" của web phải hiển thị sát điện thoại thật**:
  - đúng độ phân giải và tỉ lệ của máy thử (Redmi Note 8 Pro 2340×1080, 19,5:9), có thêm các tỉ lệ ngang mục tiêu 16:9, 18:9, 20:9 để chọn;
  - vùng an toàn (tai thỏ, thanh hệ thống) giống máy: `UiSafeArea` nhận đúng phần lề như trên Android;
  - mật độ điểm ảnh và cỡ chữ, icon như trên máy (cùng `CanvasScaler`, cùng ngưỡng chạm theo dpi), không phóng to theo màn hình PC;
  - thao tác chạm thật (chạm, vuốt, chụm hai ngón), không chỉ chuột;
  - cùng cấu hình hiển thị (30 / 60 / 120 FPS) và cùng mức chất lượng hình như bản điện thoại.
  Chỗ web chưa giống được máy (nén texture ASTC so với DXT, hiệu năng, nhiệt) phải ghi rõ khi báo cáo; FPS và nghiệm thu cuối vẫn đo trên máy thật.
- **Ba cấu hình hiển thị: 30 FPS = thấp, 60 FPS = mặc định, 120 FPS = cao** (người dùng 2026-10-28, thay quy tắc "tối thiểu 60 FPS, không có 30" ngày 21/10; spec §1). Áp cho **mọi agent, mọi thiết kế và gameplay**:
  - Mọi thiết kế (trận, map, HUD, VFX, model, animation, Frontend, sảnh) phải chạy và nhìn đúng ở **cả ba** cấu hình, và ghi rõ chất lượng hình ảnh ở mỗi cấu hình (mật độ cây cỏ, bóng, hậu kỳ, số hạt VFX, LOD, độ phân giải render…). Cấu hình thấp được giảm chi tiết trang trí, không được làm hỏng hình (không vỡ, không nhấp nháy, không mất chuyển động hiện / tan).
  - **Hạ cấu hình không được ảnh hưởng gameplay:** luật, sát thương, tốc độ, tầm, thời gian hồi, mốc phát chiêu, đường đi, va chạm và kết quả trận phải giống hệt ở 30 / 60 / 120 (logic chạy theo đồng hồ trận, không theo số khung). Mọi thứ người chơi cần để chơi (vùng cảnh báo đỏ, vùng chiêu, cổng Bát Môn, thanh máu, tướng / lính / địch, nút HUD, chạm và kéo) phải hiện đủ và rõ ở cấu hình thấp; chỉ phần trang trí được bớt.
  - 120 FPS chỉ bật khi màn hình hỗ trợ tần số đó (Redmi Note 8 Pro chỉ 60 Hz); các con số và mức chất lượng nằm trong dữ liệu cấu hình, không viết cứng.
  - Đo trên máy thật mới tính (FPS trung bình / p95, cùng workload trận); FPS Editor hoặc con số target không phải bằng chứng. Mỗi thay đổi đồ hoạ / VFX phải kiểm ở cả ba cấu hình, kèm ảnh so sánh, và test chứng minh kết quả trận không đổi khi đổi cấu hình.
  - Code hiện còn chặn target dưới 60 (`RuntimePerformanceConfig.MinimumFrameRate`, `BattlefieldQuality`): phải đổi theo quy tắc này khi làm phần cấu hình, không coi là lỗi của quy tắc.
- Flipbook hiệu ứng hoà khung theo đồng hồ hiệu ứng; sprite nhân vật không hoà khung.
- **VFX phép của Mưu Sĩ là "khí"**: đậm nhạt, xuất hiện / trúng / tan tự nhiên, không phải nét vẽ. Gió / khói **không bao giờ** là xoắn tuần hoàn hay các đường đều nhau; chỉ hình học trận pháp được đều (`Model3D_Rules.md`).
- **Luồng khí đi lên không làm thành tia / đường line** (2026-10-21): không có lõi sáng sắc, không có mép cứng, không thẳng tắp. Làm thành dòng khí mờ: bề rộng thay đổi theo chiều cao, mép tan bằng noise, đậm ở chân rồi loãng và tan dần khi lên cao, cuộn nhẹ theo gió. Nhìn ở zoom trận phải đọc ra "khí bốc lên", không phải "tia sáng".
- Mọi việc model / rig / animation / VFX theo `Model3D_Rules.md`. Bắt buộc đo bề mặt sau skinning và xem ảnh thật. Test transform pass không đủ để bàn giao.
- **Gặp hướng sai, lỗi, bug khi làm 3D / animation / VFX: ghi ngay vào `NhatKyVanHanh_3D.md`** (2026-10-21, cả hai agent).
- Hiển thị 3D (2026-10-21, góc cập nhật 28/10): camera orthographic chéo cố định, nghiêng 40° (cao 50°) và xoay 45°, nền và rừng trải ra ngoài biên; mặt đất mesh + texture. Model các tướng khác do người dùng tự làm (agent chỉ làm khi được giao trực tiếp).

## 4. Hai agent (AgentComms)
- **Chỉ đạo trực tiếp ngày 24/10/2026:** Codex tự kiểm UI và báo cáo người dùng, không dùng Claude trong ngày 24/10; hủy luồng chờ review UI cũ. Người dùng yêu cầu build dự án sau khi tự kiểm, cho phép build Android Dev APK trong việc này (thay mốc chỉ test Unity trước đó).
- Giao việc qua `AgentComms/` (giao thức: `AgentComms/README.md`, trạng thái và khoá Unity: `AgentComms/STATUS.md`). Vai làm / review đổi thường xuyên; **luôn đọc ghi chú `DOI-VAI` / chia việc mới nhất** trước khi động vào file.
- Không sửa file agent kia đang giữ. Unity chỉ một phiên một lúc: xin / nhả khoá trong STATUS.
- **Làm xong phần nhỏ thì báo ngay** cho agent kia kiểm (đường dẫn, SHA nguồn, log, ảnh trước–sau). Giữa các bước luôn xem inbox của mình (2026-10-20).
- **Chỉ theo dõi khi người dùng yêu cầu rõ ràng** (25/10/2026: "bỏ theo dõi đi, chỉ thoe dõi khi nào tôi yêu cầu"). Hoàn tất việc thì bàn giao, không tự chờ/theo dõi1giờ. Có yêu cầu theo dõi thì làm đúng phạm vi/thời hạn được giao; khi người dùng bảo dừng, dừng ngay cả watcher nền. Quy tắc này thay thế mặc định chờ1giờ trước đây.
- **Review**:
  - tự chạy lại kiểm tra, không nhận test pass của bên làm là đã nghiệm thu;
  - mỗi vòng báo **% đạt so với tài liệu tướng**, chia theo mục (Mặc Ảnh: §17 `MacAnh_Production_Spec_v1_1.md`), giữ cùng thang điểm giữa các vòng;
  - yêu cầu sửa tới mức người dùng đặt (2026-10-21: mỗi mục §17 ≥ 9/10).
- **Thư mục nghiệm thu** (2026-10-20): chỉ khi review đạt mới mở một đợt trong `NghiemThu/` (ảnh trong trận theo pha, preview VFX, model nhiều góc, số đo JSON, README kết luận + %). Không ghi đè đợt cũ.
