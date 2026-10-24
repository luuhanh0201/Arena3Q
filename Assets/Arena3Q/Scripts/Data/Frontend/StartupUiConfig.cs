using System;
using Arena3Q.Core;
using TMPro;
using UnityEngine;

namespace Arena3Q.Data
{
    public enum UiSound { Tap, Tab, Confirm, Enter, Back, Error }
    public enum GameButtonStyle { Gold, Blue, Red, Plain }

    /// <summary>The handoff's editable presentation data. Nothing in this asset changes combat rules.</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Startup UI")]
    public sealed class StartupUiConfig : ScriptableObject
    {
        [Header("Art")]
        public Sprite background, blurredBackground, logo, splashStill;
        public Sprite goldButton, blueButton, redButton, panel, largePanel;
        public Sprite crest, tassel, cloud, backIcon, soundIcon, settingsIcon, language;
        public Sprite cart, wheel, flag, spear, gradient, vignette, rounded;
        public Sprite ringOuter, ringInner, spinnerDisc, circle;
        [Header("Lucide icons (ISC)")]
        public Sprite wifi, wifiLow, wifiOff, serverOff, warning, download, check, user, gift, slash;
        [Header("Typography: Be Vietnam Pro, OFL")]
        public TMP_FontAsset regular, semiBold, bold, extraBold;
        [Header("Audio: enum order Tap, Tab, Confirm, Enter, Back, Error")]
        public AudioClip[] sounds = new AudioClip[6];
        public AudioClip music;
        [Range(0, 1)] public float musicVolume = .65f;
        [Range(0, 1)] public float introVolume = 1f;
        [Range(0, 1)] public float soundVolume = .65f;
        [Min(.01f)] public float sameClipInterval = .08f;
        [Header("Design tokens")]
        public Color gold = Hex("FFD877"), goldLine = Hex("E2B85C"), ink = Hex("3A1804");
        public Color text = Hex("E6ECF5"), dim = Hex("A9B8CF"), muted = Hex("8FA2BF");
        public Color good = Hex("4ADE80"), weak = Hex("F59E0B"), bad = Hex("DC2626"), off = Hex("6B7280");
        public Color input = Hex("0A1424"), inputBorder = Hex("2F5584"), placeholder = Hex("7F93B0");
        [Header("Layout: 1280 x 720, top-left origin")]
        public Vector2 referenceSize = new Vector2(1280, 720);
        public Rect loginPanel = new Rect(380, 134, 520, 512);
        public Rect registerPanel = new Rect(380, 134, 520, 414);
        public Rect bindPanel = new Rect(380, 116, 520, 510);
        public Rect serverPanel = new Rect(76, 46, 1128, 650);
        public Rect networkPanel = new Rect(360, 168, 560, 354);
        public float modalSeconds = .24f, screenSeconds = .35f, tipSeconds = 3.5f;
        public float statusSeconds = 15f, verificationSeconds = 60f;
        [Header("Offline resource preparation (seconds, no download is simulated)")]
        [Min(.05f)] public float localPreparationSeconds = 2f;
        public StartupNetworkSettings network = new StartupNetworkSettings();
        [Header("Copy and localization")]
        public string[] tips =
        {
            "Thu thập và bồi dưỡng danh tướng, sắp xếp đội hình hợp lý để làm chủ chiến trường!",
            "Chọn một tướng rồi chạm vào bản đồ để ra lệnh di chuyển!",
            "Mỗi trận triệu hồi tối đa 5 tướng, mỗi tướng dẫn theo một đội quân!",
            "Phá hủy nhà chính của đối phương để giành chiến thắng trong ải Công!",
            "Giữ thẻ của tướng đã ra sân 1,5 giây để chọn tướng đó!",
            "Liên kết tài khoản để giữ tiến trình khi đổi máy hoặc cài lại game!",
            "Chọn máy chủ có trạng thái Tốt để bắt đầu hành trình!"
        };
        public string healthNotice = "Chơi quá 180 phút một ngày sẽ ảnh hưởng xấu đến sức khỏe.";
        public string ageLabel = "16+";
        [Serializable] public sealed class CopyEntry { public string key; [TextArea] public string value; }
        public CopyEntry[] copy = Array.Empty<CopyEntry>();
        public string Copy(string key)
        {
            foreach (var entry in copy)
                if (entry != null && entry.key == key) return entry.value;
            Debug.LogError($"[Arena3Q UI] Missing copy '{key}' in {name}.");
            return key;
        }
        public static Color Hex(string value) => ColorUtility.TryParseHtmlString("#" + value, out var color) ? color : Color.white;
    }
}
