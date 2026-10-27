CÔNG CỤ XEM ART CŨ, không dùng trong bản phát hành từ 27/10/2026

Sảnh người chơi duy nhất: Scripts/Frontend/Screens/LobbyScreen.cs.
Dữ liệu: Content/Frontend/LobbyConfig.asset và LobbyLayout.asset, tạo bởi LobbyContentBuilder.
Sửa sảnh ở đường Frontend này, không sửa LobbyArtPreview để thay giao diện người chơi.
Phát hành: CurrentGameRelease.Apply, rồi BattlefieldAndroidBuild.BuildMobile hoặc WebGameBuild.Build.

Thư mục này vẫn cung cấp UI/nền cho sảnh mới. Giữ artwork, GUID và nguồn Lữ Bố 2D đã duyệt.
Runtime và Tests chỉ biên dịch khi UNITY_EDITOR; includePlatforms để rỗng để MonoBehaviour vẫn chạy khi Play trong Editor.
LobbyArtPreview, LobbyButtonResponse, LobbyArtCatalog và LobbyAndroidTestHost là công cụ cũ.
Scene LobbyAndroidTest/LobbyArtReview và prefab review không nằm trong release.
LobbyAndroidBuild.Build đã nghỉ; gọi API này sẽ hướng dẫn dùng đường release chung.
Menu cũ nằm ở Arena3Q/Legacy Art Review/Lobby 2D; atlas riêng LegacyLobbyReview không vào build.
LobbyCompressionQa kiểm dependency scene và BuildReport để chặn nội dung legacy.
Historical delivery notes below (not the current player flow):
Arena3Q Lobby 2D art delivery, 2026-10-27

Open Unity/LobbyArtReview.unity or instantiate Unity/Prefabs/LobbyArtReview.prefab.
Dependencies: Unity 6, uGUI, TextMeshPro, Input System. Isolated from the current game release.
Native continuous rig: Unity/Prefabs/LuBu_Skeletal_UI.prefab (Canvas child).
Sprite fallback: Unity/Prefabs/LuBu_Sprite_Animation.prefab.
72 transparent source frames, fixed 2048 square canvas, pivot (0.5,0.2).
24 frames per clip: Idle 2s loop, Run 0.9s loop, Attack 1.1s once.
Review atlas cells512 are thumbnails. Original independent frames retain2048.
Attack Impact is frame index13 (0.595833s). VFX is independent and never deals damage.
Run is in-place. Virtual forward motion502.6455 source pixels/s matches planted foot motion.
All text/profile/resources remain dynamic via TMP/catalog; artwork contains no dynamic labels.
UI icon/frame/portrait/VIP/button/background components can be used independently.
SafeArea, layered five-time backgrounds,9-slice borders and independent effect flags included.
Runtime layout and motion parameters are in LobbyArtCatalog and LuBuCutoutRig ScriptableObjects.
NavigationRequested and Impact are integration hooks, without changing existing gameplay.
UI atlas is excluded from the existing release until this lobby is integrated explicitly.
Target60 configured. Device FPS and native platform playback have not been measured in this web-only session.
Fonts use SIL OFL (UI/Fonts/OFL.txt). Mail/settings derive from Lucide (UI/Lucide_LICENSE.txt).
Slash/motes reuse Kenney Particle Pack1.1 CC0 (Effects/Kenney_CC0_LICENSE.txt).
Art generated from the approved Arena3Q/LuBu reference direction, then assembled into a fixed-source cutout rig.
Full production notes: docs/UI/Lobby2D/Delivery.md in the project/repository.
