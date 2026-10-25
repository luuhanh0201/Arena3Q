# Icon theo nền tảng và bản mobile riêng

Yêu cầu25/10: tối ưu icon, build mobile riêng, để bản web riêng cho giai đoạn sau. Ảnh nguồn và số liệu kỹ năng giữ nguyên; chỉ importer42icon thay cấu hình nền tảng. Không đổi cấu hình nén map, build Release hoặc gameplay trong bước này.

| Nền tảng | Cấu hình icon | Tình trạng |
|---|---|---|
| Android mobile | ASTC4×4, encoder100,1254×1254 | Đã import/kiểm42icon; APK riêng263,4MB |
| iOS mobile | ASTC4×4, encoder100,1254×1254 | Đã gán/kiểm override; chưa build iOS |
| WebGL tương lai | RGBA32 riêng,1254×1254 | Giữ chất lượng gốc ở cấu hình dự phòng; chưa build hoặc chốt tối ưu theo trình duyệt |
| Admin web / PNG nguồn | SHA/pixel nguồn nguyên | Không đổi |
| Standalone / Editor | RGBA32 riêng | Độc lập với override mobile |

Cấu hình có thể chỉnh trong Inspector: `Assets/Arena3Q/Scripts/Editor/SkillIconPlatforms.asset`. Đây là assetEditor, không nằm Resources hoặc được scene tham chiếu. Đường dẫn Android mặc định `Builds/Android/Arena3Q-Mobile-20261025.apk`; đường dẫn dự kiến WebGL `Builds/WebGL/Arena3Q`, chưa sinh bản web. Tách platform override trên cùng nguồn/GUID để không nhân đôi PNG/Sprite hoặc thêm nhánh platform vào gameplay.

Menu theo thứ tự: **Arena3Q > Skills > Import Approved Icon Textures**, **Verify Approved Icon Bindings**, **Arena3Q > Build > Mobile Android (Optimized Icons)**. CLI build: `-executeMethod Arena3Q.Editor.BattlefieldAndroidBuild.BuildMobile`. Import không ghi lại SkillDefinition và kiểm SHA phê duyệt trước khi áp dụng. Build kiểm lại platform/kích thước/binding trước khi tạo APK, giữ APKFeedback để đối chiếu/quay lại.

Bằng chứng `NghiemThu/Icons/Mobile_20261025`:42ảnh trước/sau qua UnityGPUreadback, bảng42cặp, cận cảnh, metrics và source verification. PSNR tối thiểu41,51dB ở ảnh gốc; đây là phép đo Editor của khác biệt nén, không là chứng nhận hình/FPS mọi thiết bị. Đã mở ảnh thật. 4/4PlayMode smoke đạt về thẻ/kỹ năng và trận. PNG nguồn,42SkillDefinition và42SpriteGUID/fileID giữ nguyên.

APK dùng cùng3scene và Development options của bảnFeedback. Build cuối thành công76,6giây,0lỗi/3cảnh báo;39icon được dùng có61.528.848byte texture ASTC4×4 trong BuildReport (58,7MB trướcZIP), so234MB RGBA32 trước.42ảnh phê duyệt đều có platform override,3ảnh chưa được game tham chiếu không đi vào APK.

**Builds/Android/Arena3Q-Mobile-20261025.apk:263,4MB (276.149.760byte), giảm117,1MB/30,8% so380,5MB trước.** Chữ kýv2 cùng signer, có thể cài đè; package/ARM64/minAPI26/target36 giữ. SHA256`0460f1b7416c6095efa7eb2df6c81e3d738edfc891d13b862491a9759d390221`. Native libil2cpp.so nguyên SHA,42ảnh/42SkillDefinition và42Sprite binding giữ nguyên.

Nén texture đã giảm payload nhưng Gradle incremental giữ vùng ZIP cũ làm APK đầu không nhỏ. Cờ `recreateAndroidPackage` chỉ loại APK trung gian được sinh tại Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/apk/debug/launcher-debug.apk trước build, không xóa APK bàn giao/cachecode/nguồn. Gradle tạo và ký gói mới theo pipeline chuẩn. Overhead cuối138.304byte so122.929.229byte của lần đóng gói đầu sau nén. Không coi toàn bộ header/alignment của mọi APK là phần có thể xóa.

[Tải APK mobile](http://127.0.0.1:4311/test-apk/Arena3Q-Mobile-20261025.apk), [xem42cặp trước/sau](http://127.0.0.1:4311/app/icon-mobile.html). Browser kiểm84ảnh/42cặp, nguyên1254px, không lỗi JS hoặc tràn ngang ở412px, HEADAPK200 đúng dung lượng. Nodepreview PID31420. ADB sau build rỗng, chưa chạy/FPS điện thoại; WebGL/iOS chưa build.

Tái tạo bằng chứng: Unity `SkillIconMobileQa.Before` trước khi đổi importer, `SkillIconMobileQa.ApplyAndCapture` sau đó; `node docs/UI/Tools/skill-icon-mobile-review.cjs` ghép so sánh/metrics, `node docs/UI/Tools/verify-skill-icon-mobile.cjs` kiểm trang local. ImportVerifier dùng cùng settings cho mọi lần build, không ép mobile sang WebGL. Bản web giữ cấu hình riêng để tối ưu/chốt theo trình duyệt khi người dùng triển khai sau này.
