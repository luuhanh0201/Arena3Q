using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    public sealed class LobbyScreen : StartupScreen
    {
        private LobbyConfig _config;
        private LobbyLayoutConfig Layout => _config.layout;
        private Rect B(string id, Vector2 offset = default) => Layout.Bounds(id, offset);
        private float F(string id) => Layout.Font(id);
        private LobbyProfile _profile;
        private Image _sky, _nextSky, _lighting;
        private Color _fromTint;
        private float _fromLighting, _fade = 1;
        public int? PreviewHour { get; set; }
        private readonly List<Image> _environment = new List<Image>();
        private RectTransform _background, _heroRect, _lordPlate, _modal;
        private RectTransform _leftChrome, _rightChrome, _bottomChrome;
        private RectTransform _tooltip, _tooltipSource;
        private TextMeshProUGUI _tooltipText;
        private float _tooltipRemaining;
        private bool _mouseHeld, _touchHeld;
        private LobbyLordView _lord;
        private int _time = -1;
        public LobbyLordView LordView => _lord;
        public bool ResourceTooltipVisible => _tooltip != null && _tooltip.gameObject.activeSelf;
        public string ResourceTooltipText => ResourceTooltipVisible ? _tooltipText.text : "";
        protected override void BuildScreen()
        {
            _config = Flow.Config.Lobby;
            if (_config == null) throw new InvalidOperationException("Apply the integrated lobby content first.");
            if (Layout == null) throw new InvalidOperationException("Apply the lobby layout asset first.");
            _profile = FrontendServiceLocator.Lobby.Read(Flow.Character, _config, Flow.Config.Progression);
            _leftChrome = EdgeGroup("LeftChrome", new Vector2(0, 1));
            _rightChrome = EdgeGroup("RightChrome", Vector2.one);
            _bottomChrome = EdgeGroup("BottomChrome", Vector2.zero);
            BuildEnvironment(); BuildHero(); BuildPlayer(); BuildResources(); BuildLeft(); BuildBanners(); BuildBottom();
            SetTime(_config.TimeIndex(DateTime.Now.Hour));
        }
        private RectTransform EdgeGroup(string name, Vector2 anchor)
        {
            var rect = Kit.Rect(Edge, name, B("EdgeGroup"));
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = Vector2.zero;
            rect.gameObject.AddComponent<LobbyEdgeGroup>(); return rect;
        }
        private Image ArtImage(Transform parent, string name, string id, Rect bounds, bool sliced = false) =>
            Kit.Image(parent, name, _config.Sprite(id), bounds, sliced: sliced);
        private Button ArtButton(Transform parent, string name, string art, Rect bounds, Action action, UiSound sound = UiSound.Tap)
        {
            var image = ArtImage(parent, name, art, bounds);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            image.gameObject.AddComponent<GameButton>().Configure(button, Kit.Audio, sound, Art);
            button.onClick.AddListener(() => action?.Invoke()); return button;
        }
        private void BuildEnvironment()
        {
            _background = UiFactory.Stretch("LobbyBackground", Root); _background.SetAsFirstSibling();
            _background.pivot = Layout.backdropPivot;
            var fit = _background.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = Layout.backdropAspect;
            Image Layer(string id)
            {
                var rect = UiFactory.Stretch(id, _background);
                return UiFactory.AddImage(rect, Color.white, _config.Sprite(id), false);
            }
            _sky = Layer("Sky_Morning");
            _nextSky = Layer("SkyTransition"); _nextSky.color = Color.clear;
            foreach (var id in new[] { "Environment_Far_Background", "Environment_Buildings", "Environment_Foreground" })
                _environment.Add(Layer(id));
            _lighting = Layer("Environment_Lighting");
        }
        private void BuildHero()
        {
            _heroRect = Kit.Rect(Root, "Lord3D", B("Hero"));
            _heroRect.pivot = new Vector2(.5f, 0);
            _heroRect.SetSiblingIndex(1);
            var image = _heroRect.gameObject.AddComponent<RawImage>();
            _lord = _heroRect.gameObject.AddComponent<LobbyLordView>();
            if (!_lord.Initialize(image, _config, _config.Model(Flow.ChosenLord?.lord)))
            {
                image.enabled = false;
                Kit.Text(_heroRect, "MissingLordModel", _config.missingModel, B("MissingModel"), F("MissingModel"), Art.gold, TextAlignmentOptions.Center);
            }
            var plate = Frame(Design, "LordNameplate", B("LordNameplate"), Art.input, Layout.lordPlateCorner, Layout.lordPlateStroke);
            _lordPlate = plate; plate.pivot = new Vector2(1, .5f);
            var heading = Kit.Text(plate, "LordHeading", _config.lordHeading, B("LordHeading"), F("LordHeading"), Art.gold, TextAlignmentOptions.Center, 800);
            heading.enableVertexGradient = true;
            heading.colorGradient = new VertexGradient(Art.gold, Art.gold, Art.goldLine, Art.goldLine);
            if (_config.lordHeadingMaterial != null) heading.fontSharedMaterial = _config.lordHeadingMaterial;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
        }
        private RectTransform Frame(Transform parent, string name, Rect bounds, Color fill, float corner, float stroke)
        {
            var rect = Kit.Rect(parent, name, bounds);
            var frame = rect.gameObject.AddComponent<LobbyFrameGraphic>(); frame.color = Art.goldLine;
            frame.fill = fill; frame.corner = corner; frame.stroke = stroke; frame.raycastTarget = false;
            return rect;
        }
        private void BuildPlayer()
        {
            var player = Kit.Rect(_leftChrome, "Player", B("Player"));
            // The tier art is the frame of this entire player group, rather than a separate VIP badge.
            var frameSprite = _config.defaultPlayerFrame;
            var bodyAnchor = .5f;
            var bestTier = -1;
            foreach (var tier in _config.vipFrames)
            {
                if (tier != null && tier.sprite != null && tier.tier <= _profile.vip && tier.tier > bestTier)
                { frameSprite = tier.sprite; bestTier = tier.tier; bodyAnchor = tier.bodyAnchorY; }
            }
            var vipArt = Kit.Image(player, "VipBackground", frameSprite, B("VipBackground"));
            vipArt.rectTransform.pivot = new Vector2(0, 1 - bodyAnchor);
            var nameBounds = B("PlayerName");
            var nameClip = Kit.Rect(player, "PlayerNameClip", nameBounds); nameClip.gameObject.AddComponent<RectMask2D>();
            var playerName = Kit.Text(nameClip, "PlayerName", _profile.name, new Rect(Vector2.zero, nameBounds.size), F("PlayerName"), Art.text, TextAlignmentOptions.Center, 800);
            playerName.textWrappingMode = TextWrappingModes.NoWrap; playerName.enableAutoSizing = true;
            playerName.fontSizeMin = Layout.playerNameMinimumFont; playerName.fontSizeMax = F("PlayerName");
            if (_config.lordHeadingMaterial != null) playerName.fontSharedMaterial = _config.lordHeadingMaterial;
            var stats = Frame(player, "PlayerStats", B("PlayerStats"), Art.lobbyPlayerStats, Layout.playerFrameCorner, Layout.playerFrameStroke);
            Kit.Text(stats, "Level", "Lv." + _profile.level, B("Level"), F("Level"), Art.text);
            if (_profile.vip > 0)
            {
                var vip = Frame(stats, "VipBadge", B("VipBadge"), Art.lobbyVipBadge, Layout.playerFrameCorner, Layout.playerFrameStroke);
                Kit.Text(vip, "VipLabel", "VIP " + _profile.vip, B("VipLabel"), F("VipLabel"), Art.gold, TextAlignmentOptions.Center, 800);
            }
            var circle = Kit.Image(player, "AvatarBorder", Art.circle, B("AvatarBorder"), Art.goldLine);
            var mask = Kit.Image(circle.transform, "AvatarMask", Art.circle, B("AvatarMask"));
            Kit.Image(circle.transform, "AvatarBackground", Art.circle, B("AvatarMask"), Art.input).transform.SetAsFirstSibling();
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portrait = Flow.ChosenLord?.portrait;
            Kit.Image(mask.transform, "LordAvatar", portrait != null ? portrait : Art.user, B("LordAvatar"));
            var experienceFrame = B("ExperienceBorder");
            var border = Kit.Image(stats, "ExperienceBorder", null, experienceFrame, Art.goldLine);
            var inset = Mathf.Clamp(Layout.experienceInset, 0, Mathf.Min(experienceFrame.width, experienceFrame.height) * .5f);
            var innerSize = experienceFrame.size - Vector2.one * (inset * 2);
            var viewport = Kit.Rect(border.transform, "ExperienceViewport", new Rect(Vector2.one * inset, innerSize));
            viewport.gameObject.AddComponent<RectMask2D>();
            Kit.Image(viewport, "ExperienceTrack", null, new Rect(Vector2.zero, innerSize), Art.lobbyExpTrack);
            var experience = Kit.Image(viewport, "Experience", null,
                new Rect(0, 0, innerSize.x * _profile.ExperienceFraction, innerSize.y), Color.white);
            experience.gameObject.AddComponent<LobbyHorizontalGradient>().Configure(Art.lobbyExpStart, Art.lobbyExpEnd);
            Kit.Image(player, "PowerRibbon", Art.redButton, B("PowerRibbon"), sliced: true);
            Kit.Text(player, "Power", "Chiến Lực " + LobbyResourceRules.Exact(_profile.power),
                B("Power"), F("Power"), Art.gold, TextAlignmentOptions.Center, 800);
        }
        private void BuildResources()
        {
            for (var i = 0; i < _config.resources.Length; i++)
            {
                var entry = _config.resources[i];
                var box = ArtImage(_rightChrome, "Resource_" + entry.id, "Resource_Bar", B("Resource", Layout.resourceStep * i), true).rectTransform;
                ArtImage(box, "Icon", entry.icon, B("ResourceIcon"));
                var balance = _profile.Balance(entry.id);
                var value = entry.isStamina ? LobbyResourceRules.Exact(balance.amount) + "/" + LobbyResourceRules.Exact(balance.capacity)
                    : LobbyResourceRules.Compact(balance.amount, _config.resourceCompactThreshold, _config.resourceCompactDecimals, _config.resourceCompactUnit);
                Kit.Text(box, "Amount", value, B("ResourceAmount"), F("ResourceAmount"), Art.text, TextAlignmentOptions.Center, 800);
                var hintHit = Kit.Image(box, "Hint", null, B("ResourceHint"), Color.clear);
                hintHit.raycastTarget = true;
                hintHit.gameObject.AddComponent<LobbyResourceHint>().Configure(() => ShowResourceTooltip(entry, hintHit.rectTransform));
                if (entry.showAdd)
                {
                    var add = Kit.Button(box, "Add", "", B("ResourceAdd"), ShowUnavailable, GameButtonStyle.Gold);
                    Kit.Image(add.transform, "Plus", _config.plus, B("ResourcePlus"), Art.ink);
                }
            }
            var icons = new[] { _config.mail, _config.settings };
            var names = new[] { "Mail", "Settings" };
            for (var i = 0; i < icons.Length; i++)
            {
                var button = Kit.IconButton(_rightChrome, names[i], null, B("CornerButton", Layout.cornerStep * i), ShowUnavailable);
                button.targetGraphic.color = Color.clear;
                var icon = Kit.Image(button.transform, "Icon", icons[i], B("CornerIcon"), Color.white);
                icon.preserveAspect = true;
                icon.gameObject.AddComponent<UiGradient>().Configure(Art.goldLine, Art.lobbyIconHighlight);
                var shadow = icon.gameObject.AddComponent<Shadow>(); shadow.effectColor = Art.lobbyIconShadow;
                shadow.effectDistance = Layout.iconShadowOffset;
                var outline = icon.gameObject.AddComponent<Outline>(); outline.effectColor = Art.ink; outline.effectDistance = Layout.iconOutlineOffset;
                if (i == 0 && _config.mailNotification) Dot(button.transform, Layout.cornerNotification);
            }
        }
        private void BuildLeft()
        {
            var panel = ArtImage(_leftChrome, "LeftNavigation", "Navigation_Vertical", B("LeftNavigation"), true).rectTransform;
            for (var i = 0; i < _config.left.Length; i++)
            {
                var item = _config.left[i];
                var button = Kit.Button(panel, item.id, "", B("LeftButton", Layout.leftStep * i), ShowUnavailable, GameButtonStyle.Plain);
                ArtImage(button.transform, "Icon", item.icon, B("LeftIcon"));
                Kit.Text(button.transform, "Caption", item.label, B("LeftCaption"), F("LeftCaption"), Art.text, TextAlignmentOptions.Center, 700);
                if (item.notification) Dot(button.transform, Layout.leftNotification);
                if (i < _config.left.Length - 1) Kit.Image(panel, "Divider" + i, null, B("LeftDivider", Layout.leftStep * i), Art.goldLine * Art.lobbyDividerTint);
            }
        }
        private void BuildBanners()
        {
            var bounds = Layout.banners;
            for (var i = 0; i < _config.banners.Length && i < bounds.Length; i++)
            {
                var entry = _config.banners[i]; var box = bounds[i];
                var button = ArtButton(_rightChrome, entry.id, entry.art, box, ShowUnavailable);
                Kit.Image(button.transform, "Readability", Art.gradient, new Rect(Layout.bannerOverlayInset, box.height * Layout.bannerOverlayStart,
                    box.width - Layout.bannerOverlayInset * 2, box.height * Layout.bannerOverlayHeight), Art.lobbyBannerOverlay);
                var border = Kit.Rect(button.transform, "Border", new Rect(0, 0, box.width, box.height)).gameObject.AddComponent<LobbyFrameGraphic>();
                border.color = Art.goldLine; border.raycastTarget = false;
                if (!string.IsNullOrEmpty(entry.tag))
                {
                    Kit.Image(button.transform, "TagBacking", Art.goldButton, Layout.bannerTagBacking, sliced: true);
                    Kit.Text(button.transform, "Tag", entry.tag, Layout.bannerTag, Layout.bannerTagFont, Art.ink, TextAlignmentOptions.Center, 800);
                }
                Kit.Text(button.transform, "Title", entry.title, new Rect(Layout.bannerTextInset, box.height - (i == 0 ? Layout.eventTitleBottom : Layout.bannerTitleBottom),
                    i == 0 ? Layout.eventTitleWidth : box.width - Layout.bannerTextRightInset, i == 0 ? Layout.eventTitleHeight : Layout.bannerTitleHeight),
                    i < 2 ? Layout.bannerTitleFont : Layout.smallBannerTitleFont, Art.gold, weight: 800);
                Kit.Text(button.transform, "Subtitle", entry.subtitle, new Rect(Layout.bannerTextInset, box.height - Layout.bannerSubtitleBottom,
                    box.width - Layout.bannerSubtitleRightInset, Layout.bannerSubtitleHeight), i < 2 ? Layout.bannerSubtitleFont : Layout.smallBannerSubtitleFont, Art.text);
                Kit.Image(button.transform, "Chevron", _config.chevron, new Rect(new Vector2(box.width, box.height) - Layout.bannerChevronInset, Layout.bannerChevronSize), Art.gold);
            }
        }
        private void BuildBottom()
        {
            var panel = Kit.Rect(_bottomChrome, "BottomNavigation", B("BottomNavigation"));
            var frame = panel.gameObject.AddComponent<LobbyFrameGraphic>(); frame.color = Art.goldLine;
            frame.corner = Layout.navCorner; frame.stroke = Layout.navStroke; frame.fill = Art.lobbyNavigationFill; frame.raycastTarget = false;
            for (var i = 0; i < _config.bottom.Length; i++)
            {
                var item = _config.bottom[i];
                var button = Kit.Button(panel, item.id, "", B("BottomButton", Layout.bottomStep * i),
                    i == 0 ? (Action)(() => { }) : ShowUnavailable, GameButtonStyle.Plain, UiSound.Tab);
                if (i == 0)
                {
                    var selected = Kit.Rect(button.transform, "Selected", B("BottomSelected")).gameObject.AddComponent<LobbyFrameGraphic>();
                    selected.color = Art.gold; selected.corner = Layout.selectedCorner; selected.stroke = Layout.selectedStroke;
                    selected.fill = Art.lobbySelectedFill; selected.raycastTarget = false; selected.transform.SetAsFirstSibling();
                    button.GetComponent<GameButton>().SetSelected(true);
                }
                ArtImage(button.transform, "Icon", item.icon, B(i == 0 ? "BottomSelectedIcon" : "BottomIcon"));
                Kit.Text(button.transform, "Caption", item.label, B("BottomCaption"), F("BottomCaption"), Art.text, TextAlignmentOptions.Center, 800);
                if (item.notification) Dot(button.transform, Layout.bottomNotification);
                if (i > 0)
                {
                    var divider = Kit.Image(panel, "Divider" + i, null, B("BottomDivider", Layout.bottomStep * i), Art.goldLine);
                    divider.rectTransform.localRotation = Quaternion.Euler(0, 0, Layout.dividerRotation);
                }
            }
            var battle = ArtButton(_rightChrome, "EnterBattle", "Battle_Normal", B("EnterBattle"), Flow.EnterBattleFromLobby, UiSound.Enter);
            Kit.Text(battle.transform, "Caption", _config.battleLabel, B("BattleCaption"), F("BattleCaption"), Art.gold, TextAlignmentOptions.Center, 800);
            Kit.Image(battle.transform, "DoubleChevron", _config.doubleChevron, B("BattleChevron"), Art.gold);
        }
        private void Dot(Transform parent, Vector2 point) =>
            Kit.Image(parent, "Notification", Art.circle, new Rect(point, Vector2.one * Layout.notificationSize), Art.bad);
        private void ShowResourceTooltip(LobbyConfig.ResourceEntry entry, RectTransform source)
        {
            if (_tooltip == null)
            {
                _tooltip = Kit.Rect(Edge, "ResourceTooltip", B("ResourceTooltip"));
                var frame = Kit.Image(_tooltip, "Frame", Art.panel, B("TooltipFrame"), sliced: true);
                frame.pixelsPerUnitMultiplier = Layout.tooltipPixelsPerUnit;
                _tooltipText = Kit.Text(_tooltip, "ExactBalance", "", B("TooltipText"), F("TooltipText"),
                    Art.text, TextAlignmentOptions.Center, 700);
                _tooltipText.enableAutoSizing = true; _tooltipText.fontSizeMin = Layout.tooltipFontMin; _tooltipText.fontSizeMax = F("TooltipText");
            }
            var balance = _profile.Balance(entry.id);
            var exact = LobbyResourceRules.Exact(balance.amount);
            if (entry.isStamina) exact += "/" + LobbyResourceRules.Exact(balance.capacity);
            _tooltipText.text = entry.label + " " + exact;
            _tooltipSource = source; _tooltipRemaining = Mathf.Max(.1f, _config.resourceTooltipSeconds);
            _tooltip.gameObject.SetActive(true); _tooltip.SetAsLastSibling(); PositionTooltip();
        }
        private void PositionTooltip()
        {
            if (!ResourceTooltipVisible || _tooltipSource == null) return;
            var sourceBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(Edge, _tooltipSource);
            var size = B("ResourceTooltip").size;
            var padding = Layout.tooltipPadding;
            var width = Mathf.Min(size.x, Edge.rect.width - padding * 2); _tooltip.sizeDelta = new Vector2(width, size.y);
            var x = Mathf.Clamp(sourceBounds.center.x - width * .5f, Edge.rect.xMin + padding, Edge.rect.xMax - width - padding);
            var top = Mathf.Clamp(sourceBounds.min.y - Layout.tooltipGap, Edge.rect.yMin + size.y + padding, Edge.rect.yMax - padding);
            _tooltip.anchoredPosition = new Vector2(x - Edge.rect.xMin, top - Edge.rect.yMax);
        }
        private void HideResourceTooltip()
        {
            if (_tooltip != null) _tooltip.gameObject.SetActive(false);
            _tooltipRemaining = 0; _tooltipSource = null;
        }
        private void TickResourceTooltip(float deltaTime)
        {
            var mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
            var touchHeld = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            var mousePressed = mouseHeld && !_mouseHeld;
            var touchPressed = touchHeld && !_touchHeld;
            _mouseHeld = mouseHeld; _touchHeld = touchHeld;
            if (!ResourceTooltipVisible) return;
            _tooltipRemaining -= deltaTime;
            if (_tooltipRemaining <= 0) { HideResourceTooltip(); return; }
            PositionTooltip();
            Vector2 point;
            if (touchPressed) point = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (mousePressed) point = Mouse.current.position.ReadValue();
            else return;
            var canvas = Root.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint(_tooltipSource, point, camera) &&
                !RectTransformUtility.RectangleContainsScreenPoint(_tooltip, point, camera)) HideResourceTooltip();
        }
        public void ShowUnavailable()
        {
            HideResourceTooltip();
            if (_modal != null) return;
            _modal = Kit.Dim(Design, "UnavailableModal", Layout.unavailableDimAlpha);
            Kit.DismissOutside(_modal, CloseModal);
            var panel = Kit.Panel(_modal, "Panel", B("UnavailablePanel"), animate: true);
            Kit.Text(panel, "Title", _config.unavailableTitle, B("UnavailableTitle"), F("UnavailableTitle"), Art.gold, TextAlignmentOptions.Center, 800);
            Kit.Text(panel, "Body", _config.unavailableBody, B("UnavailableBody"), F("UnavailableBody"), Art.text, TextAlignmentOptions.Center);
            Kit.Button(panel, "Close", "Đóng", B("UnavailableClose"), CloseModal, GameButtonStyle.Gold, UiSound.Back);
        }
        private void CloseModal() { Kit.Close(_modal); _modal = null; }
        public void SetTime(int index)
        {
            if (index < 0 || index >= _config.daylight.Length) return;
            var initial = _time < 0;
            _fromTint = _environment.Count > 0 ? _environment[0].color : Color.white;
            _fromLighting = _lighting.color.a;
            if (_fade >= 1 && _nextSky.sprite != null) _sky.sprite = _nextSky.sprite;
            _time = index; var time = _config.daylight[index];
            _nextSky.sprite = _config.Sprite(time.sky); _fade = initial ? 1 : 0;
            ApplyDaylight();
        }
        private void ApplyDaylight()
        {
            var time = _config.daylight[_time]; var t = Mathf.SmoothStep(0, 1, _fade);
            var tint = Color.Lerp(_fromTint, time.tint, t);
            _nextSky.color = new Color(1, 1, 1, t);
            foreach (var layer in _environment) layer.color = tint;
            _lighting.color = new Color(1, 1, 1, Mathf.Lerp(_fromLighting, time.lightingAlpha, t));
            _lord.SetTint(tint);
        }
        public override void Tick(float deltaTime)
        {
            TickResourceTooltip(deltaTime);
            var current = _config.TimeIndex(PreviewHour ?? DateTime.Now.Hour); if (current != _time) SetTime(current);
            if (_fade < 1) { _fade = Mathf.Min(1, _fade + deltaTime / Mathf.Max(.01f, _config.daylightFadeSeconds)); ApplyDaylight(); }
            if (_background != null && _heroRect != null)
            {
                _heroRect.sizeDelta = Vector2.one * (Edge.rect.height * _config.heroFrameHeight);
                var point = new Vector2(_background.rect.xMin + _background.rect.width * _config.heroFeet.x,
                    _background.rect.yMin + _background.rect.height * _config.heroFeet.y);
                var model = _config.Model(Flow.ChosenLord?.lord);
                var groundInTexture = model == null ? 0 : (model.cameraSize - model.cameraHeight) / (2 * model.cameraSize);
                _heroRect.position = _background.TransformPoint(point) - Root.TransformVector(
                    Vector3.up * (_heroRect.sizeDelta.y * groundInTexture));
                if (_lordPlate != null) _lordPlate.position = _heroRect.TransformPoint(
                    _heroRect.rect.min + Vector2.Scale(_heroRect.rect.size, Layout.lordPlateAnchor));
            }
        }
    }
}
