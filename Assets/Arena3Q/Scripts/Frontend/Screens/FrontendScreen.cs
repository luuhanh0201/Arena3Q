using Arena3Q.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// One screen of the game flow. Builds its own placeholder UI from the theme; the methods behind its buttons are
    /// public so tests (and later tutorials or automation) can drive the flow without clicking.
    /// </summary>
    public abstract class FrontendScreen
    {
        protected FrontendController Flow { get; private set; }
        protected FrontendUi Ui => Flow.Ui;
        public RectTransform Root { get; private set; }

        internal void Show(FrontendController flow, RectTransform parent)
        {
            Flow = flow;
            Root = UiFactory.Stretch(GetType().Name, parent);
            Build();
        }

        internal void Hide()
        {
            if (Root != null)
                Object.Destroy(Root.gameObject);
            Root = null;
        }

        protected abstract void Build();

        public virtual void Tick(float deltaTime)
        {
        }

        /// <summary>The banner (or the game title on the theme colour) behind the menu screens.</summary>
        protected void AddBackdrop(float dim = 0f, bool showTitle = true)
        {
            var banner = Flow.Config.Banner;
            if (banner != null)
            {
                var image = Ui.Fill(Root, "Banner", Color.white, banner);
                image.preserveAspect = false;
            }
            else if (showTitle)
            {
                Ui.Label(Root, "Title", Flow.Config.GameTitle, 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f),
                    new Vector2(0f, -150f), new Vector2(1200f, 180f), Ui.Theme.accent);
            }
            if (dim > 0f)
                Ui.Fill(Root, "Dim", new Color(0f, 0f, 0f, dim));
        }

        /// <summary>A full-screen invisible button (tap anywhere), placed under the other widgets.</summary>
        protected void AddTapCatcher(System.Action onTap)
        {
            var image = Ui.Fill(Root, "TapCatcher", new Color(0f, 0f, 0f, 0f), null, raycast: true);
            image.transform.SetAsFirstSibling();
            var trigger = image.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => onTap());
            trigger.triggers.Add(entry);
        }
    }
}
