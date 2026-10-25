using Arena3Q.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>Small helpers to build placeholder uGUI from code.</summary>
    public static class UiFactory
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
#if UNITY_WEBGL && !UNITY_EDITOR
                var typography = Resources.Load<UiTypographyConfig>(UiTypographyConfig.ResourceName);
                if (typography != null) _font = typography.browserFont;
#endif
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>uGUI needs an EventSystem; use the Input System UI module (legacy input is disabled).</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Object.DontDestroyOnLoad(go);
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rect;
        }

        public static Image AddImage(RectTransform rect, Color color, Sprite sprite = null, bool raycastTarget = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>
        /// A white library icon tinted with <paramref name="color"/> (docs/QuyTac/ThietKeUI.md: icons, never Unicode
        /// symbols). Pivot = anchor, so <paramref name="position"/> is the icon's edge or centre on that side.
        /// </summary>
        public static Image AddIcon(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, float size, Color color)
        {
            var image = AddImage(CreateRect(name, parent, anchor, anchor, anchor, position, new Vector2(size, size)), color, sprite, false);
            image.preserveAspect = true;
            image.enabled = sprite != null;
            return image;
        }

        /// <summary>
        /// <paramref name="count"/> star icons in a row going right from <paramref name="position"/> (left middle of the
        /// row). Returns the row, whose children are the stars.
        /// </summary>
        public static RectTransform AddStars(string name, Transform parent, Vector2 anchor, Vector2 position, int count, float size, Color color)
        {
            const float spacing = 1f;
            var row = CreateRect(name, parent, anchor, anchor, new Vector2(0f, 0.5f), position,
                new Vector2(Mathf.Max(0, count) * (size + spacing), size));
            var icons = UiIconSet.Load();
            var star = icons != null ? icons.Star : null;
            for (var i = 0; i < count; i++)
                AddIcon($"Star {i}", row, star, new Vector2(0f, 0.5f), new Vector2(i * (size + spacing), 0f), size, color);
            return row;
        }

        public static Text AddText(RectTransform rect, string content, int fontSize, TextAnchor alignment, Color color)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Text CreateLabel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size, string content, int fontSize, TextAnchor alignment, Color color)
        {
            var rect = CreateRect(name, parent, anchor, anchor, pivot, position, size);
            var text = AddText(rect, content, fontSize, alignment, color);
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
            string label, Color color)
        {
            var rect = CreateRect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            var image = AddImage(rect, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var labelRect = Stretch("Label", rect);
            AddText(labelRect, label, 34, TextAnchor.MiddleCenter, Color.white);
            return button;
        }
    }
}
