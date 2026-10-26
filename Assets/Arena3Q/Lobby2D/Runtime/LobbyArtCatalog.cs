using System;
using UnityEngine;
using TMPro;

namespace Arena3Q.Lobby2D
{
    [CreateAssetMenu(menuName = "Arena3Q/Lobby 2D/Art Catalog")]
    public sealed class LobbyArtCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry { public string id, role; public Sprite sprite; }
        [Serializable] public sealed class TimeOfDay
        {
            public string label, skyId;
            public Color environmentTint = Color.white;
            public float lightingAlpha;
        }
        [Serializable] public sealed class NavigationItem { public string label, iconId; }
        public enum NodeKind { Image, Text, Container }
        public enum NodeAction { None, Navigate, TimeOfDay }
        [Serializable] public sealed class Node
        {
            public string id, parent, spriteId, text, destination, effect;
            public string pressedSpriteId, disabledSpriteId;
            public NodeKind kind;
            public NodeAction action;
            public int timeIndex;
            public Vector2 anchor, position, size, pivot = Vector2.one * .5f;
            public bool stretch, sliced, preserveAspect, safeArea, mask, cover;
            public float fontSize, pixelMultiplier = 1, period, activeSeconds, amplitude, travel, fadeSeconds;
            public int minVip;
            public Vector2 referencePoint;
            public Color tint = Color.white;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public TimeOfDay[] times = Array.Empty<TimeOfDay>();
        public NavigationItem[] bottom = Array.Empty<NavigationItem>(), left = Array.Empty<NavigationItem>();
        public Node[] nodes = Array.Empty<Node>();
        public TMP_FontAsset font;
        public LuBuCutoutDefinition characterRig;
        public float characterScale = .8f;
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public int targetFrameRate=60;
        public string playerName = "Arena3Q";
        public int playerLevel = 50, vip = 12;
        public string power = "1.245.680", banknotes = "12.680", silver = "5.320.000", stamina = "180/180";
        public bool notificationAnimation = true, buttonResponse = true, vipAnimation = true, ctaAnimation = true;
        public bool bannerAnimation=true;
        public float bannerSeconds=7,bannerFadeSeconds=1.2f;
        public string[] eventBannerSprites={"Banner_Event","Banner_Recruit"};
        public bool ambientAnimation=true,recruitAnimation=true;
        public int ambientMoteCount=14,recruitMoteCount=7;
        public float moteMinSize=3,moteMaxSize=7,moteLifetime=18,moteDrift=7,moteAlpha=.32f,moteFadeSeconds=.2f;
        public Color moteColor=new Color(1,.82f,.4f);
        public float notificationSpeed = 1.2f, notificationMinimumAlpha = .65f;
        public float pressedScale = .95f, responseSeconds = .1f;

        public Sprite Sprite(string id)
        {
            foreach (var item in entries) if (item.id == id) return item.sprite;
            return null;
        }
    }
}
