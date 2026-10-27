using System;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>Shared handoff widgets, using one sprite per button colour and one coordinate convention.</summary>
    public sealed class StartupUi
    {
        public StartupUi(StartupUiConfig config, UIAudio audio) { Config = config; Audio = audio; }
        public StartupUiConfig Config { get; }
        public UIAudio Audio { get; }
        public string C(string key) => Config.Copy(key);
        public static readonly Vector2 TopLeft = new Vector2(0, 1);

        public RectTransform Rect(Transform parent, string name, Rect bounds) =>
            UiFactory.CreateRect(name, parent, TopLeft, TopLeft, TopLeft,
                new Vector2(bounds.x, -bounds.y), bounds.size);

        public Image Image(Transform parent, string name, Sprite sprite, Rect bounds, Color? color = null, bool sliced = false)
        {
            var image = UiFactory.AddImage(Rect(parent, name, bounds), color ?? Color.white, sprite, false);
            if (sliced && sprite != null) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public TextMeshProUGUI Text(Transform parent, string name, string content, Rect bounds, float size = 16,
            Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, int weight = 600)
        {
            var text = Rect(parent, name, bounds).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = weight >= 800 ? Config.extraBold : weight >= 700 ? Config.bold : weight >= 600 ? Config.semiBold : Config.regular;
            text.text = content ?? ""; text.fontSize = size; text.color = color ?? Config.text;
            text.alignment = align; text.raycastTarget = false; text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        public Button Button(Transform parent, string name, string label, Rect bounds, Action action,
            GameButtonStyle style = GameButtonStyle.Blue, UiSound sound = UiSound.Tap, float fontSize = 0)
        {
            var sprite = style == GameButtonStyle.Gold ? Config.goldButton : style == GameButtonStyle.Red ? Config.redButton
                : style == GameButtonStyle.Blue ? Config.blueButton : null;
            var image = Image(parent, name, sprite, bounds, style == GameButtonStyle.Plain ? Color.clear : Color.white, true);
            image.raycastTarget = true;
            if (sprite != null)
            {
                // Preserve ornament end-cap proportions at every supported button height.
                image.pixelsPerUnitMultiplier = sprite.rect.height / bounds.height;
            }
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var labelSize = fontSize > 0 ? fontSize : bounds.height >= 76 ? 28 : bounds.height >= 64 ? 22 : bounds.height >= 50 ? 16 : 14;
            var text = Text(image.transform, "Label", label, new Rect(12, 0, bounds.width - 24, bounds.height), labelSize,
                style == GameButtonStyle.Gold ? Config.ink : style == GameButtonStyle.Plain ? Config.gold : Config.text,
                TextAlignmentOptions.Center, style == GameButtonStyle.Plain ? 600 : 800);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (style == GameButtonStyle.Gold) text.characterSpacing = 2;
            var motion = image.gameObject.AddComponent<GameButton>(); motion.Configure(button, Audio, sound, Config);
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        public Button IconButton(Transform parent, string name, Sprite sprite, Rect bounds, Action action, UiSound sound = UiSound.Tap)
        {
            var image = Image(parent, name, sprite, bounds, sprite != null ? Color.white : Color.clear); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            image.gameObject.AddComponent<GameButton>().Configure(button, Audio, sound, Config, true);
            button.onClick.AddListener(() => action?.Invoke()); return button;
        }

        public void SetLabel(Button button, string content) => button.GetComponentInChildren<TMP_Text>().text = content;

        public Slider Slider(Transform parent, string name, Rect bounds, float value, Action<float> changed)
        {
            var root = Rect(parent, name, bounds);
            var track = Image(root, "Track", Config.rounded, new Rect(0, bounds.height / 2 - 4, bounds.width, 8), Config.inputBorder, true);
            var hit = UiFactory.AddImage(root, Color.clear); hit.raycastTarget = true;
            var fillArea = UiFactory.Stretch("FillArea", root);
            fillArea.offsetMin = new Vector2(8, bounds.height / 2 - 4); fillArea.offsetMax = new Vector2(-8, -bounds.height / 2 + 4);
            var fill = UiFactory.AddImage(UiFactory.Stretch("Fill", fillArea), Config.goldLine, Config.rounded, false); fill.type = UnityEngine.UI.Image.Type.Sliced;
            var handles = UiFactory.Stretch("HandleArea", root);
            handles.offsetMin = new Vector2(11, bounds.height / 2 - 11); handles.offsetMax = new Vector2(-11, -bounds.height / 2 + 11);
            var handle = Image(handles, "Handle", Config.rounded, new Rect(0, 0, 22, 22), Config.gold, true);
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, .5f);
            handle.rectTransform.pivot = new Vector2(.5f, .5f); handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.rectTransform.sizeDelta = new Vector2(22, 0);
            var slider = root.gameObject.AddComponent<Slider>(); slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform;
            slider.minValue = 0; slider.maxValue = 1; slider.value = value;
            slider.onValueChanged.AddListener(v => changed?.Invoke(v)); return slider;
        }

        public RectTransform Panel(Transform parent, string name, Rect bounds, bool big = false, bool tassels = true, bool animate = false)
        {
            var image = Image(parent, name, big ? Config.largePanel : Config.panel, bounds, sliced: true);
            image.raycastTarget = true; image.pixelsPerUnitMultiplier = big ? .875f : 1;
            var rect = image.rectTransform;
            rect.pivot = FrontendUi.Center;
            rect.anchoredPosition += new Vector2(bounds.width / 2, -bounds.height / 2);
            Image(rect, "DragonCrest", Config.crest, new Rect((bounds.width - 238) / 2, -32, 238, 59));
            if (tassels)
            {
                Image(rect, "TasselLeft", Config.tassel, new Rect(-12, 40, 24, 150));
                var right = Image(rect, "TasselRight", Config.tassel, new Rect(bounds.width + 12, 40, 24, 150));
                right.rectTransform.localScale = new Vector3(-1, 1, 1);
            }
            if (animate) rect.gameObject.AddComponent<UiReveal>().Configure(Config.modalSeconds, .9f);
            return rect;
        }

        public RectTransform Dim(Transform parent, string name, float alpha)
        {
            var rect = UiFactory.Stretch(name, parent);
            var shade = UiFactory.Stretch("DimBackdrop", rect);
            UiFactory.AddImage(shade, new Color(.02f, .01f, .01f, DimAlpha(alpha)));
            shade.gameObject.AddComponent<CanvasBackdrop>();
            rect.gameObject.AddComponent<UiReveal>().Configure(Config.modalSeconds);
            return rect;
        }

        public void DismissOutside(RectTransform overlay, Action close)
        {
            var shade = overlay.GetComponentsInChildren<CanvasBackdrop>().FirstOrDefault(x => x.name == "DimBackdrop");
            if (shade == null) return;
            var image = shade.GetComponent<Image>(); image.raycastTarget = true;
            var button = shade.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { Audio.Play(UiSound.Back); close(); });
        }

        public void Close(RectTransform overlay)
        {
            if (overlay == null) return;
            UiReveal.Exit(overlay.gameObject, Config.modalSeconds);
        }

        // The HTML handoff blends in sRGB. Compensate dark overlays in this project's linear renderer.
        public static float DimAlpha(float alpha) => QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 - Mathf.Pow(1 - alpha, 2.2f) : alpha;

        public RectTransform ModalBackdrop(Transform parent, string name, float dim, bool blur = false)
        {
            var rect = UiFactory.Stretch(name, parent);
            Image(rect, "KeyArt", blur ? Config.blurredBackground : Config.background, new Rect(0, 0, 1280, 720))
                .gameObject.AddComponent<CanvasBackdrop>().coverSprite = true;
            Dim(rect, "Dim", dim);
            rect.gameObject.AddComponent<UiReveal>().Configure(Config.modalSeconds);
            return rect;
        }

        public void BottomShade(Transform parent, float height)
        {
            Image(parent, "BottomShade", Config.gradient, new Rect(0, 720 - height, 1280, height))
                .gameObject.AddComponent<CanvasBackdrop>().bottomHeight = height;
        }

        public void Logo(Transform parent, float x, float y, float width)
        {
            var sprite = Config.logo;
            if (sprite != null) Image(parent, "Arena3QLogo", sprite, new Rect(x, y, width, width * sprite.rect.height / sprite.rect.width));
        }

        public RectTransform CircleBadge(Transform parent, string name, Sprite icon, Rect bounds, Color background, Color border, Color iconColor, float iconSize)
        {
            var badge = Image(parent, name, Config.circle, bounds, border).rectTransform;
            Image(badge, "Background", Config.circle, new Rect(2, 2, bounds.width - 4, bounds.height - 4), background);
            Image(badge, "Icon", icon, new Rect((bounds.width - iconSize) / 2, (bounds.height - iconSize) / 2, iconSize, iconSize), iconColor);
            return badge;
        }

        public StartupField Field(Transform parent, string name, string placeholder, Rect bounds, bool password = false, int limit = 254)
        {
            var frame = Image(parent, name, Config.rounded, bounds, Config.inputBorder, true);
            frame.raycastTarget = true;
            var inner = Image(frame.transform, "Background", Config.rounded, new Rect(1, 1, bounds.width - 2, bounds.height - 2), Config.input, true);
            frame.pixelsPerUnitMultiplier = inner.pixelsPerUnitMultiplier = 3.5f;
            inner.color = Color.white;
            inner.gameObject.AddComponent<UiGradient>().Configure(Config.input, StartupUiConfig.Hex("132A49"));
            var viewport = Rect(frame.transform, "Viewport", new Rect(14, 0, bounds.width - 28, bounds.height));
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Text(viewport, "Text", "", new Rect(0, 0, bounds.width - 28, bounds.height), 15, weight: 400);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var hint = Text(viewport, "Placeholder", placeholder, new Rect(0, 0, bounds.width - 28, bounds.height), 14, Config.placeholder, weight: 400);
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            var input = frame.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport; input.textComponent = text; input.placeholder = hint;
            input.targetGraphic = inner; input.characterLimit = limit;
            input.lineType = TMP_InputField.LineType.SingleLine;
            if (password) { input.contentType = TMP_InputField.ContentType.Password; input.asteriskChar = '*'; }
            var error = Text(parent, name + "Error", "", new Rect(bounds.x, bounds.y + bounds.height, bounds.width, 10), 10, Config.bad);
            var field = new StartupField(input, frame, error, Config);
            input.onSelect.AddListener(_ => field.Focus(true));
            input.onDeselect.AddListener(_ => field.Focus(false));
            input.onValueChanged.AddListener(_ => field.Error(""));
            return field;
        }

        public TMP_Text LegalText(Transform parent, string name, Rect bounds, FrontendController flow, Toggle toggle = null)
        {
            var hex = ColorUtility.ToHtmlStringRGB(Config.gold);
            var terms = "<link=\"terms\"><color=#" + hex + ">" + C("terms") + "</color></link>";
            var privacy = "<link=\"privacy\"><color=#" + hex + ">" + C("privacy") + "</color></link>";
            var content = string.Format(C(toggle == null ? "legalLogin" : "legalAgreement"), terms, privacy);
            var text = Text(parent, name, content, bounds, toggle == null ? 11.5f : 12.5f, Config.dim,
                toggle == null ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft, 400);
            text.richText = true; text.raycastTarget = true;
            text.gameObject.AddComponent<UiLegalLinks>().Configure(flow, text, toggle); return text;
        }

        public Toggle Toggle(Transform parent, string name, string label, Rect bounds, bool initial = false)
        {
            var root = Rect(parent, name, bounds);
            var hit = UiFactory.AddImage(root, Color.clear); hit.raycastTarget = true;
            Image(root, "Box", null, new Rect(0, (bounds.height - 18) / 2, 18, 18), Config.goldLine);
            Image(root, "BoxInner", null, new Rect(1, (bounds.height - 16) / 2, 16, 16), Config.input);
            var check = Image(root, "Check", Config.check, new Rect(2, (bounds.height - 14) / 2, 14, 14), Config.gold);
            var toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = hit; toggle.graphic = check;
            toggle.isOn = initial;
            Text(root, "Label", label, new Rect(26, 0, bounds.width - 26, bounds.height), 12, Config.dim);
            toggle.onValueChanged.AddListener(_ => Audio.Play(UiSound.Tap)); return toggle;
        }

        public void Footer(Transform parent, bool health = true)
        {
            Text(parent, "Version", C("version") + " " + Application.version, new Rect(24, 686, 230, 26), 12, Config.dim);
            if (!health) return;
            Text(parent, "HealthNotice", Config.healthNotice, new Rect(630, 686, 570, 26), 12, Config.dim, TextAlignmentOptions.MidlineRight);
            Image(parent, "AgeBadge", Config.rounded, new Rect(1210, 690, 44, 22), Config.gold, true);
            Text(parent, "Age", Config.ageLabel, new Rect(1210, 690, 44, 22), 12, Config.ink, TextAlignmentOptions.Center, 800);
        }

        public string StatusText(ServerStatus status) => C(status == ServerStatus.Busy ? "busy" : status == ServerStatus.Full ? "full"
            : status == ServerStatus.Maintenance ? "maintenance" : "good");
        public Color StatusColor(ServerStatus status) => status == ServerStatus.Busy ? Config.weak : status == ServerStatus.Full ? Config.bad
            : status == ServerStatus.Maintenance ? Config.off : Config.good;

        public void ServerState(Transform parent, ServerInfo server, float x, float y)
        {
            var color = StatusColor(server.Status);
            Image(parent, "StatusDot", Config.rounded, new Rect(x, y + 6, 8, 8), color, true);
            var textColor = server.Status == ServerStatus.Busy ? StartupUiConfig.Hex("FCD34D")
                : server.Status == ServerStatus.Full ? StartupUiConfig.Hex("FCA5A5")
                : server.Status == ServerStatus.Maintenance ? StartupUiConfig.Hex("C7C9CF") : StartupUiConfig.Hex("86EFAC");
            Text(parent, "Status", StatusText(server.Status), new Rect(x + 14, y, 56, 22), 13, textColor, weight: 700);
        }
    }

    public sealed class StartupField
    {
        private readonly Image _border; private readonly TMP_Text _error; private readonly StartupUiConfig _config;
        public StartupField(TMP_InputField input, Image border, TMP_Text error, StartupUiConfig config)
        { Input = input; _border = border; _error = error; _config = config; }
        public TMP_InputField Input { get; }
        public string Value { get => Input.text; set => Input.text = value ?? ""; }
        public void Focus(bool focused) { if (string.IsNullOrEmpty(_error.text)) _border.color = focused ? _config.goldLine : _config.inputBorder; }
        public void Error(string message)
        {
            _error.text = message;
            _border.color = string.IsNullOrEmpty(message) ? Input.isFocused ? _config.goldLine : _config.inputBorder : _config.bad;
        }
    }

    /// <summary>Only startup screens use the new 1280 layout; gameplay menus keep their own coordinate system.</summary>
    public abstract class StartupScreen : FrontendScreen
    {
        protected StartupUi Kit => Flow.StartupUi;
        protected StartupUiConfig Art => Kit.Config;
        public RectTransform Design { get; private set; }
        protected RectTransform Edge { get; private set; }
        protected sealed override void Build()
        {
            if (Flow.Config.StartupUi == null) throw new InvalidOperationException("Run Arena3Q > UI > Import Startup Handoff first.");
            var safe = UiFactory.Stretch("SafeArea", Root); safe.gameObject.AddComponent<SafeAreaFitter>().Apply();
            Design = UiFactory.CreateRect("Design1280", safe, FrontendUi.Center, FrontendUi.Center,
                FrontendUi.Center, Vector2.zero, Art.referenceSize);
            Design.gameObject.AddComponent<DesignFrame>().Configure(Art.referenceSize);
            Edge = UiFactory.Stretch("EdgeControls", safe);
            BuildScreen();
        }
        protected abstract void BuildScreen();
        protected void Backdrop(bool form = false, bool blur = false)
        {
            var background = UiFactory.Stretch("KeyArt", Root); background.SetAsFirstSibling();
            UiFactory.AddImage(background, Color.white, blur ? Art.blurredBackground : Art.background, false);
            var aspect = background.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = Art.background != null ? Art.background.rect.width / Art.background.rect.height : 16f / 9;
            if (form)
            {
                var overlay = UiFactory.Stretch("Vignette", Root); overlay.SetSiblingIndex(1);
                UiFactory.AddImage(overlay, Color.white, Art.vignette, false);
            }
        }
        protected void CornerButtons(bool language = false, Action settings = null)
        {
            foreach (var name in new[] { "Sound", "Language", "Settings" })
            {
                var previous = Edge.Find(name);
                if (previous != null) { previous.gameObject.SetActive(false); UnityEngine.Object.Destroy(previous.gameObject); }
            }
            Image slash = null;
            var sound = Kit.IconButton(Edge, "Sound", Art.soundIcon, new Rect(settings != null && !language ? -132 : -72, 40, 52, 52), ToggleSound);
            sound.GetComponent<RectTransform>().anchorMin = sound.GetComponent<RectTransform>().anchorMax = Vector2.one;
            slash = Kit.Image(sound.transform, "Muted", Art.slash, new Rect(9, 9, 34, 34), Art.bad);
            slash.enabled = Kit.Audio.Muted && (Flow.Music == null || Flow.Music.Muted);
            if (language) AnchorRight(Kit.IconButton(Edge, "Language", Art.language, new Rect(-262, 35, 180, 62), () => Flow.ShowMessage(Kit.C("vietnameseOnly"))));
            else if (settings != null) AnchorRight(Kit.IconButton(Edge, "Settings", Art.settingsIcon, new Rect(-70, 40, 52, 52), settings));
            void AnchorRight(Button button) { var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = Vector2.one; }
            void ToggleSound()
            {
                var mute = !(Kit.Audio.Muted && (Flow.Music == null || Flow.Music.Muted));
                Kit.Audio.SetMuted(mute); if (Flow.Music != null) Flow.Music.SetMuted(mute); slash.enabled = mute;
            }
        }
        protected void Back(Action action) => Kit.IconButton(Design, "Back", Art.backIcon, new Rect(14, 14, 52, 52), action, UiSound.Back);
    }
}
