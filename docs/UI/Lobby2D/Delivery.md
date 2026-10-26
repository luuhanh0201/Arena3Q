# Arena3Q: bàn giao bộ sảnh2D và Lữ Bố

Ngày27/10/2026. Phạm vi kiểm theo yêu cầu mới nhất: xem và kiểm trên web, không build game. Đây là bộ tài nguyên độc lập để xây sảnh trong Unity, chưa nối vào luồng phát hành/battle3D hiện tại.

## Xem và nhập

- Trang xem: <http://127.0.0.1:4330>. Nếu chưa chạy: `node ArtSource/Lobby2D/serve.cjs` từ gốc project.
- Gói đầy đủ: `Builds/Art/Arena3Q_Lobby2D.unitypackage`, khoảng193MB. Import vào Unity6 có uGUI, TMP, InputSystem. Gói gồm ảnh, metadata, lớp nguồn, prefab, Animator và dependency; không cần cắt ảnh concept.
- Scene riêng: `Assets/Arena3Q/Lobby2D/Unity/LobbyArtReview.unity`.
- Prefab sảnh: `Unity/Prefabs/LobbyArtReview.prefab`, dữ liệu `Unity/LobbyArtCatalog.asset`.
- Rig liên tục: `Unity/Prefabs/LuBu_Skeletal_UI.prefab`, đặt dưới Canvas. Cấu hình `LuBuCutoutRig.asset`; gọi `Play(0/1/2)`, đăng ký `Impact`.
- Sprite fallback: `Unity/Prefabs/LuBu_Sprite_Animation.prefab`; controller có `IsRunning` và trigger `Attack`. Nhận `LuBuAnimationEventRelay.onImpact` khi tích hợp.

## Nội dung đã xuất

| Nhóm | Đầu ra |
| --- | --- |
| Tài nguyên cơ sở |96 PNG độc lập, đầy đủ theo `ArtSource/Lobby2D/delivery.json` |
| Nhân vật |5 portrait: FullBody, Head, Avatar, Bust, BattlePose;26 layer chung canvas/pivot;6 key pose lấy từ rig thật |
| Chuyển động |Idle, Run, Attack: mỗi clip24 frame khác nhau, tổng72 PNG RGBA2048×2048 |
| Dữ liệu clip |`frames.json`, `timing.csv`, `events.json`, atlas6×4, GIF;3 `.anim` và1 controller |
| VFX |24 PNG riêng đồng bộ Attack, preview/timing riêng; slash/mote từ Kenney CC0 |
| UI |17 icon, CTA4 lớp/trạng thái, khung/panel9-slice,12 thiết kế VIP khác nhau,4 banner, notification và light sweep |
| Sảnh |5 sky; Far Background, Buildings, Foreground đăng ký cùng geometry; Lighting và Ambient VFX riêng |
| Unity |Catalog/motion ScriptableObjects, UI material/atlas, prefab sảnh, rig và sprite, scene xem riêng |

PNG nhân vật/animation không chứa chữ hoặc background. Avatar là chân dung cận mặt có chủ ý crop phần lông vũ ngoài khung; FullBody và72 frame giữ đủ silhouette. Head/Hair/Helmet vẫn tách riêng trong rig. Các vùng khớp dùng underlay và nắp khớp để che phần khuất. Giáp và kích đi theo phép biến đổi cứng, chỉ tóc/vải/lông vũ có độ mềm.

## Timing và tích hợp

| Clip | Thời lượng | Loop | Lưu ý |
| --- | --- | --- | --- |
| LuBu_Idle |2s | Có | Lấy mẫu i/24, không chép frame đầu vào cuối |
| LuBu_Run |0,9s | Có | In-place, hai chân luân phiên; vận tốc tiến ảo502,6455px/s trong hệ nguồn |
| LuBu_Attack_01 |1,1s | Không | Impact tại index13,0,595833s; có recovery về Idle |

Pivot tất cả source frame `(0.5,0.2)`, canvas2048 vuông để chứa kích và lông vũ khi xoay. Frame index bắt đầu000. Atlas xem có cell512 (3072×2048); ảnh PNG rời vẫn2048. GIF có delay phân bố theo centisecond để tổng thời lượng đúng, không đổi frame cho đủ số. Fallback sprite có tần số12/26,67/21,82fps theo clip; rig liên tục nội suy theo render clock thích hợp cho hiển thị60Hz.

Run giữ vị trí gốc để game điều khiển di chuyển; muốn khóa tiếp đất trong thế giới phải khớp vận tốc locomotion với vận tốc tiến ảo nhân scale. Không tự áp con số này vào chỉ số chiến đấu hiện có. Váy/áo choàng và kích che một phần chân ở góc3/4 này. Event chỉ là hook để gameplay gọi sát thương đúng một lần; VFX không gây sát thương.

Text/profile/tài nguyên/label do TMP dựng từ catalog. `NavigationRequested` cho host nối chức năng. Thư chỉ ở top-right; Chiêu Mộ chỉ ở thẻ phải; không thêm Chinh Chiến. CTA có normal/pressed/disabled và overlay sáng riêng, press scale cấu hình. VIP8–12 có sweep riêng, banner crossfade, mote nhẹ, notification pulse và hiệu ứng tuyển mộ có cờ bật/tắt độc lập. Hỗ trợ CanvasScaler/safe area4tỷ lệ;5thời điểm thay sky/tint/light, giữ geometry/camera. Atlas UI max2048, rotation/mipmaps tắt; loại layer nguồn khỏi catalog runtime. Atlas chưa include vào bản phát hành hiện tại để tránh tăng dung lượng ngoài phạm vi.

## Bằng chứng và giới hạn

- 96/96 base asset qua kích thước, alpha và mép canvas;0file thiếu,0lỗi kỹ thuật.
- Mỗi clip24/24 decoded-pixel hash khác nhau, đúng kích thước/pivot/order, không cắt alpha ở biên.
- Audit291pose:38mảng,1840đỉnh; reach error0px; rigid scale error≤1,79e-7; độ giãn cạnh vải0,9025–1,0813; lệch đỉnh tại biên loop≤0,000063px.
- Run contact slip số học0,0705px/s sau bù vận tốc tiến ảo.150mẫu chuyển clip, reach error0.
- Ghép3layer architecture trùng nguồn:1.621.875pixel được phân vùng,0pixel mismatch.
- Edge/Playwright26/26 kiểm web:5thời điểm,3khung ngang mở rộng,3clip phát thường/chậm, Attack dừng,5portrait/26layer/12VIP/17icon, toggle micro, landscape740×400,0pageerror/0file404.
- Đã xem hình atlas/pose, trước–sau, sảnh sáng/đêm, safe area và VFX ở impact. Đây là tự kiểm của Codex; nghiệm thu thẩm mỹ cuối cùng thuộc người dùng.
- Chỉ dùng Unity để nhập, render tài nguyên và xuất asset archive. Không build APK/player/WebGL trong phiên này. Mục tiêu60FPS đã cấu hình; FPS máy thật, tiêu thụ bộ nhớ/overdraw và playback native Android/iOS/PC chưa được đo. `fullPlatformAcceptance` còn false.

Bằng chứng ở `NghiemThu/UI/XemTruoc_Lobby2D_20261026/` và `NghiemThu/LuBu/XemTruoc_LobbyRig_20261026/`. Báo cáo máy đọc: `technical-report.json`, `acceptance-report.json`, `background-registration.json`, `visual-review.json`, `package-report.json` trong ArtSource; `surface-audit.json` và Web/report.json trong thư mục xem. Ảnh xem ở web là ảnh thật từ tài nguyên, không phải một ảnh concept được coi là UI hoàn chỉnh.

## File trong Git và file chỉ ở máy

Theo quy tắc project20/10, chỉ commit code và asset nhỏ dưới khoảng1MB; không GitLFS. Source sinh ảnh `Generated/`, geometry partition `Registered/`, thumbnails `WebFrames/`, atlas và ảnh/font lớn ở máy. Danh sách chính xác kèm SHA256 nằm trong `ArtSource/Lobby2D/local-only-assets.json`. Vì vậy clone Git mới cần import gói `Builds/Art/Arena3Q_Lobby2D.unitypackage` để có đầy đủ hình nguồn và font riêng. Gói nặng cũng chỉ ở máy, không nằm trên GitHub. Không xóa chúng sau commit.

ArtSource giữ script, prompt sinh ảnh, nguồn portable theo đường dẫn project, rig/layout/spec, SVG khung native và manifest. Không tái sinh hình AI giống từng pixel chỉ từ prompt; bản nguồn đã sinh mới là nguồn sản xuất cố định. 72frame được render từ rig này, không nội suy các key AI không nhất quán.

## Công cụ

Node dependencies được ghi trong `ArtSource/Lobby2D/package.json`; công cụ ưu tiên local node_modules, có fallback toolchain đang có trong project. Browser check dùng Edge đã cài. Không cần gọi dịch vụ AI để xem hoặc render lại rig hiện tại.

1. Nguồn: `stage-rig.cjs`, `create-rig.cjs`; `native-ui.cjs`, `partition-background.cjs`, `create-layout.cjs` khi sửa tương ứng.
2. `prepare.cjs` xuất base PNG từ nguồn đã lưu. `create-spec.cjs` tái lập hợp đồng và cố ý xóa trạng thái review cũ.
3. Editor: `LuBuCutoutBuilder.Export`, rồi `package-animation.cjs`; `LuBuCutoutBuilder.BuildAndReview`, rồi `package-effects.cjs`.
4. Editor: `LobbyArtBuilder.BuildAndCapture` nhập, dựng prefab/capture và xuất asset package. Đây không phải player build.
5. `review.cjs`, `review-differences.cjs`, `verify-web.cjs`; xem hình và chuyển động thật trước ghi review. `finalize-review.cjs` ghi bằng chứng tự kiểm có hash; `validate.cjs` kiểm72frame và chặn review cũ khi bằng chứng thay đổi. `inspect-package.cjs` kiểm archive, `finalize-files.cjs` ghi danh sách local. Hai cache font dùng chung bị capture sửa trong phiên này đã được trả về trạng thái sạch ban đầu.

Font BeVietnamPro: SIL OFL, license đi kèm gói. Mail/settings: Lucide ISC, phần kế thừa Feather MIT. Slash/mote: Kenney Particle Pack1.1 CC0, license đi kèm. Khung SVG mở rộng hệ UI native đã có; các tranh/portrait/icon fantasy được sinh theo bộ reference người dùng.
