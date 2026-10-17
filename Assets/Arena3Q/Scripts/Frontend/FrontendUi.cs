using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// Menu widgets in the theme of <see cref="FrontendConfig"/>: placeholder colours until the UI kit art is assigned,
    /// then its 9-sliced sprites and font, without touching the screens.
    /// </summary>
    public sealed class FrontendUi
    {
        public FrontendUi(UiTheme theme)
        {
            Theme = theme ?? new UiTheme();
        }

        public UiTheme Theme { get; }
        public Font Font => Theme.font != null ? Theme.font : UiFactory.Font;

        public static Vector2 Center => new Vector2(0.5f, 0.5f);

        public Image Fill(RectTransform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var image = UiFactory.AddImage(UiFactory.Stretch(name, parent), color, sprite, raycast);
            if (sprite != null)
                image.preserveAspect = false;
            return image;
        }

        public RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = UiFactory.CreateRect(name, parent, anchor, anchor, Center, position, size);
            var image = UiFactory.AddImage(rect, Theme.panelSprite != null ? Color.white : Theme.panel, Theme.panelSprite);
            if (Theme.panelSprite != null)
                image.type = Image.Type.Sliced;
            return rect;
        }

        /// <summary>
        /// A dialog: a full-screen dim layer that blocks taps behind it, with an opaque panel on top. Returns the panel;
        /// destroy <c>panel.parent</c> to close the dialog.
        /// </summary>
        public RectTransform Modal(RectTransform parent, string name, Vector2 size)
        {
            var dim = UiFactory.Stretch(name, parent);
            UiFactory.AddImage(dim, new Color(0f, 0f, 0f, 0.75f));
            var panel = Panel(dim, "Panel", Center, Vector2.zero, size);
            var image = panel.GetComponent<Image>();
            if (Theme.panelSprite == null)
                image.color = new Color(Theme.panel.r, Theme.panel.g, Theme.panel.b, 1f);
            return panel;
        }

        public Text Label(RectTransform parent, string name, string content, int size, TextAnchor alignment, Vector2 anchor,
            Vector2 position, Vector2 box, Color? color = null)
        {
            var rect = UiFactory.CreateRect(name, parent, anchor, anchor, Center, position, box);
            var text = UiFactory.AddText(rect, content, size, alignment, color ?? Theme.text);
            text.font = Font;
            return text;
        }

        public Button Button(RectTransform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size,
            bool primary = false)
        {
            var rect = UiFactory.CreateRect(name, parent, anchor, anchor, Center, position, size);
            var color = primary ? Theme.primaryButton : Theme.secondaryButton;
            var image = UiFactory.AddImage(rect, Theme.buttonSprite != null ? color * 1.6f : color, Theme.buttonSprite);
            if (Theme.buttonSprite != null)
                image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = UiFactory.AddText(UiFactory.Stretch("Label", rect), label, primary ? 36 : 30, TextAnchor.MiddleCenter, Theme.text);
            text.font = Font;
            return button;
        }

        public static void SetLabel(Button button, string label) => button.GetComponentInChildren<Text>().text = label;

        public InputField Input(RectTransform parent, string name, string placeholder, Vector2 anchor, Vector2 position,
            Vector2 size, bool password = false, int characterLimit = 0)
        {
            var rect = UiFactory.CreateRect(name, parent, anchor, anchor, Center, position, size);
            var background = UiFactory.AddImage(rect, new Color(0.06f, 0.05f, 0.05f, 0.95f));

            var textRect = UiFactory.CreateRect("Text", rect, Vector2.zero, Vector2.one, Center, Vector2.zero, new Vector2(-30f, -10f));
            var text = UiFactory.AddText(textRect, "", 32, TextAnchor.MiddleLeft, Theme.text);
            text.font = Font;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            var hintRect = UiFactory.CreateRect("Placeholder", rect, Vector2.zero, Vector2.one, Center, Vector2.zero, new Vector2(-30f, -10f));
            var hint = UiFactory.AddText(hintRect, placeholder, 30, TextAnchor.MiddleLeft, new Color(Theme.text.r, Theme.text.g, Theme.text.b, 0.4f));
            hint.font = Font;
            hint.fontStyle = FontStyle.Italic;

            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = characterLimit;
            if (password)
                input.contentType = InputField.ContentType.Password;
            return input;
        }

        /// <summary>A round placeholder "portrait" in a colour, or the sprite when there is one.</summary>
        public Image Portrait(RectTransform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            var rect = UiFactory.CreateRect(name, parent, anchor, anchor, Center, position, size);
            var image = UiFactory.AddImage(rect, sprite != null ? Color.white : color, sprite != null ? sprite : PlaceholderArt.Circle, false);
            image.preserveAspect = true;
            return image;
        }
    }
}
