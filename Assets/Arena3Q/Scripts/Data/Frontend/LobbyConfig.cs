using System;
using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Lobby")]
    public sealed class LobbyConfig : ScriptableObject
    {
        [Serializable] public sealed class ArtEntry { public string id; public Sprite sprite; }
        [Serializable] public sealed class MenuEntry { public string id, label, icon; public bool notification; }
        [Serializable] public sealed class ResourceEntry { public string id, label, icon; public bool isStamina, showAdd = true; }
        [Serializable] public sealed class VipFrame { public int tier; public Sprite sprite; public float bodyAnchorY, bodyHeightY; }
        [Serializable] public sealed class BannerEntry { public string id, title, subtitle, tag, art; }
        [Serializable] public sealed class Daylight
        {
            public int startHour;
            public string label, sky;
            public Color tint = Color.white;
            public Color modelTint = Color.white;
            public float lightingAlpha;
        }
        [Serializable] public sealed class LordModel
        {
            public HeroDefinition hero;
            public float yaw = 180, cameraHeight = .9f, cameraSize = 1.05f;
        }
        public ArtEntry[] art = Array.Empty<ArtEntry>();
        public LobbyLayoutConfig layout;
        public MenuEntry[] left = Array.Empty<MenuEntry>(), bottom = Array.Empty<MenuEntry>();
        public ResourceEntry[] resources = Array.Empty<ResourceEntry>();
        public BannerEntry[] banners = Array.Empty<BannerEntry>();
        public Daylight[] daylight = Array.Empty<Daylight>();
        public LordModel[] models = Array.Empty<LordModel>();
        public Sprite plus, chevron, doubleChevron, friends, mail, settings, star;
        public VipFrame[] vipFrames = Array.Empty<VipFrame>();
        public Sprite defaultPlayerFrame;
        public bool uiRefinementsInitialized;
        public long resourceCompactThreshold = 100000;
        public int resourceCompactDecimals = 1;
        public string resourceCompactUnit = "Vạn";
        public float resourceTooltipSeconds = 2;
        public bool mailNotification = true;
        public string unavailableTitle = "Chưa mở", unavailableBody = "Tính năng đang được chuẩn bị.",
            missingModel = "Hình ảnh chủ tướng đang được cập nhật", battleLabel = "Xuất Chiến";
        public string lordHeading = "Chủ Tướng";
        public Material lordHeadingMaterial;
        public float daylightFadeSeconds = 2;
        public int renderResolution = 1024, avatarRenderResolution = 512, rendererIndex = 1, modelLayer = 30;
        public bool adaptiveRenderResolution = true;
        public int renderResolutionMaximum = 2048, renderResolutionStep = 128;
        public float directionalIntensity = 1.4f, fillIntensity = 5;
        public Color ambientColor = new Color(.55f, .55f, .6f);
        public Vector3 lightRotation = new Vector3(40, -30, 0);
        public Color lightColor = Color.white;
        public bool lightingConfigured;
        public int captureMsaa = 4;
        public Cubemap reflectionEnvironment;
        public Material captureComposite;
        public Color reflectionSky = new Color(.52f, .43f, .3f), reflectionGround = new Color(.12f, .09f, .055f);
        public Color portraitWarmth = new Color(1, .95f, .86f);
        public Vector3 stagePosition = new Vector3(-128, -128, -64);
        public Vector2 heroFeet = new Vector2(.39f, .20f);
        public float heroFrameHeight = .84f;
        public Sprite Sprite(string id) => Array.Find(art, x => x.id == id)?.sprite;
        public LordModel Model(HeroDefinition hero) => hero == null ? null :
            Array.Find(models, x => x.hero == hero) ?? new LordModel { hero = hero };
        public int TimeIndex(int hour)
        {
            var hours = new int[daylight.Length];
            for (var i = 0; i < hours.Length; i++) hours[i] = daylight[i].startHour;
            return Arena3Q.Core.LobbyTimeRules.IndexAtHour(hour, hours);
        }
    }
}
