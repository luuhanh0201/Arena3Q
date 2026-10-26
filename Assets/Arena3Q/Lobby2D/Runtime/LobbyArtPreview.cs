using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Lobby2D
{
    // Isolated data-driven art review prefab, without player persistence or battle launch.
    public sealed class LobbyArtPreview : MonoBehaviour
    {
        public LobbyArtCatalog catalog;
        public Camera previewCamera;
        public event Action<string> NavigationRequested;
        public RectTransform SafeRoot { get; private set; }
        public int TimeIndex { get; private set; }
        public Rect? SafeAreaOverride { get; set; }
        private Canvas _canvas;
        private Image _sky;
        private Image _lighting;
        private readonly List<Image> _environments = new List<Image>();
        private readonly List<Image> _notifications = new List<Image>();
        private readonly Dictionary<string, RectTransform> _rects = new Dictionary<string, RectTransform>();
        private readonly List<RectTransform> _backdrops = new List<RectTransform>();
        private RectTransform _hero;
        private Vector2 _heroPoint;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;

        private void Start() { if (_canvas == null) Build();Application.targetFrameRate=Mathf.Max(60,catalog.targetFrameRate); }
        public void Build()
        {
            if (catalog == null) throw new InvalidOperationException("Assign LobbyArtCatalog before building preview.");
            if (_canvas != null) return;
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = previewCamera ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = previewCamera;
            if (previewCamera) _canvas.planeDistance = 1;
            gameObject.AddComponent<GraphicRaycaster>();
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = catalog.referenceResolution; scaler.matchWidthOrHeight = 1;
            foreach (var node in catalog.nodes)
            {
                var parent = string.IsNullOrEmpty(node.parent) ? transform : _rects[node.parent];
                var rect = new GameObject(node.id, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = node.anchor;
                rect.pivot = node.pivot; rect.anchoredPosition = node.position; rect.sizeDelta = node.size;
                if (node.stretch) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
                if (node.safeArea) SafeRoot = rect;
                _rects.Add(node.id, rect);
                if (node.cover) _backdrops.Add(rect);
                if (node.effect == "heroAnchor") { _hero = rect; _heroPoint = node.referencePoint; }
                if(node.effect=="heroAnchor"&&catalog.characterRig){
                    rect.sizeDelta=catalog.characterRig.frameSize;rect.pivot=catalog.characterRig.framePivot;
                    rect.localScale=Vector3.one*catalog.characterScale;
                    var rig=rect.gameObject.AddComponent<LuBuCutoutView>();rig.definition=catalog.characterRig;rig.Build();
                    continue;
                }
                if(node.effect=="ambientMotes"){rect.gameObject.AddComponent<LobbyAmbientMotes>().catalog=catalog;continue;}
                if (node.kind == LobbyArtCatalog.NodeKind.Image)
                {
                    var image = rect.gameObject.AddComponent<Image>();
                    image.sprite = catalog.Sprite(node.spriteId.Replace("{vip}", catalog.vip.ToString("D2")));
                    image.color = image.sprite ? node.tint : Color.clear;
                    image.preserveAspect = node.preserveAspect;
                    image.type = node.sliced ? Image.Type.Sliced : Image.Type.Simple;
                    if(node.effect=="environment")image.useSpriteMesh=true;
                    image.pixelsPerUnitMultiplier = Mathf.Max(.01f, node.pixelMultiplier);
                    image.raycastTarget = node.action != LobbyArtCatalog.NodeAction.None;
                    if (node.effect == "sky") _sky = image;
                    if (node.effect == "environment") _environments.Add(image);
                    if (node.effect == "lighting") _lighting=image;
                    if (node.effect == "notification") _notifications.Add(image);
                    if(node.effect=="bannerCycle")rect.gameObject.AddComponent<LobbyBannerCycle>().catalog=catalog;
                    if(node.effect=="recruitMotes"){var motes=rect.gameObject.AddComponent<LobbyAmbientMotes>();motes.catalog=catalog;motes.recruitment=true;}
                    if (node.mask) rect.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    if (node.effect == "vipSheen" || node.effect == "ctaGlow" || node.effect=="recruitSheen")
                    {
                        var light = rect.gameObject.AddComponent<LobbyArtLight>(); light.catalog = catalog; light.settings = node;
                    }
                    if (node.action != LobbyArtCatalog.NodeAction.None)
                    {
                        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                        if(!string.IsNullOrEmpty(node.pressedSpriteId)) {
                            button.transition=Selectable.Transition.SpriteSwap;
                            button.spriteState=new SpriteState { highlightedSprite=image.sprite,selectedSprite=image.sprite,
                                pressedSprite=catalog.Sprite(node.pressedSpriteId),disabledSprite=catalog.Sprite(node.disabledSpriteId) };
                        }
                        if (node.action == LobbyArtCatalog.NodeAction.TimeOfDay)
                            button.onClick.AddListener(() => SetTime(node.timeIndex));
                        else button.onClick.AddListener(() => NavigationRequested?.Invoke(node.destination));
                        rect.gameObject.AddComponent<LobbyButtonResponse>().settings = catalog;
                    }
                }
                else if (node.kind == LobbyArtCatalog.NodeKind.Text)
                {
                    var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = catalog.font;
                    text.text = ResolveText(node.text); text.fontSize = node.fontSize; text.color = node.tint;
                    text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
            SetTime(0); ApplySafeArea(); AlignBackgrounds();
        }
        public void RefreshProfile()
        {
            foreach (var node in catalog.nodes)
            {
                if (node.kind == LobbyArtCatalog.NodeKind.Text && _rects.TryGetValue(node.id, out var rect))
                    rect.GetComponent<TextMeshProUGUI>().text = ResolveText(node.text);
                if (node.kind == LobbyArtCatalog.NodeKind.Image && node.spriteId.Contains("{vip}") && _rects.TryGetValue(node.id, out rect))
                    rect.GetComponent<Image>().sprite = catalog.Sprite(node.spriteId.Replace("{vip}", catalog.vip.ToString("D2")));
            }
        }
        private string ResolveText(string value) => (value ?? "")
            .Replace("{playerName}", catalog.playerName).Replace("{level}", catalog.playerLevel.ToString())
            .Replace("{power}", catalog.power).Replace("{vip}", catalog.vip.ToString())
            .Replace("{banknotes}", catalog.banknotes).Replace("{silver}", catalog.silver).Replace("{stamina}", catalog.stamina);
        public void SetTime(int index)
        {
            if (catalog.times.Length == 0 || _sky == null) return;
            TimeIndex = Mathf.Clamp(index, 0, catalog.times.Length - 1);
            var time = catalog.times[TimeIndex]; _sky.sprite = catalog.Sprite(time.skyId);
            foreach (var environment in _environments) environment.color = time.environmentTint;
            if(_lighting){var tint=Color.white;tint.a=time.lightingAlpha;_lighting.color=tint;}
        }
        private void Update()
        {
            if (SafeRoot == null) return;
            if (_lastSafe != Screen.safeArea || _lastScreen != new Vector2Int(Screen.width, Screen.height)) ApplySafeArea();
            var alpha = catalog.notificationAnimation ? Mathf.Lerp(catalog.notificationMinimumAlpha, 1,
                .5f + .5f * Mathf.Sin(Time.unscaledTime * catalog.notificationSpeed * Mathf.PI * 2)) : 1;
            foreach (var dot in _notifications) { var color = dot.color; color.a = alpha; dot.color = color; }
        }
        private void LateUpdate() { AlignBackgrounds(); }
        public void AlignBackgrounds()
        {
            var canvasRect = ((RectTransform)transform).rect;
            var scale = Mathf.Max(canvasRect.width / catalog.referenceResolution.x, canvasRect.height / catalog.referenceResolution.y);
            foreach (var rect in _backdrops)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0);
                rect.sizeDelta = catalog.referenceResolution * scale; rect.anchoredPosition = Vector2.zero;
            }
            if (_hero && _environmentRect != null)
                _hero.position = _environmentRect.TransformPoint(Vector2.Scale(_heroPoint - _environmentRect.pivot, _environmentRect.rect.size));
        }
        private RectTransform _environmentRect => _environments.Count > 0 ? _environments[0].rectTransform : null;
        public void ApplySafeArea()
        {
            if (SafeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            _lastSafe = SafeAreaOverride ?? Screen.safeArea; _lastScreen = new Vector2Int(Screen.width, Screen.height);
            var width = previewCamera && previewCamera.targetTexture ? previewCamera.targetTexture.width : Screen.width;
            var height = previewCamera && previewCamera.targetTexture ? previewCamera.targetTexture.height : Screen.height;
            SafeRoot.anchorMin = new Vector2(_lastSafe.xMin / width, _lastSafe.yMin / height);
            SafeRoot.anchorMax = new Vector2(_lastSafe.xMax / width, _lastSafe.yMax / height);
            SafeRoot.offsetMin = SafeRoot.offsetMax = Vector2.zero;
        }
    }
}
