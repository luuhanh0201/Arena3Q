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
            if (flow.Config.StartupUi != null && !(this is StartupScreen))
            {
                Root.anchorMin = Root.anchorMax = Root.pivot = FrontendUi.Center;
                Root.sizeDelta = new Vector2(1920, 1080);
                Root.gameObject.AddComponent<DesignFrame>().Configure(Root.sizeDelta);
            }
            Build();
            if (Root != null && flow.Config.StartupUi != null)
                Root.gameObject.AddComponent<UiReveal>().Configure(flow.Config.StartupUi.screenSeconds);
        }

        internal void Hide(bool transition = false)
        {
            OnHide();
            if (Root != null)
            {
                if (transition && Flow.Config.StartupUi != null)
                    UiReveal.Exit(Root.gameObject, Flow.Config.StartupUi.screenSeconds, fade: false);
                else
                {
                    Root.gameObject.SetActive(false);
                    Object.Destroy(Root.gameObject);
                }
            }
            Root = null;
        }

        protected abstract void Build();
        protected virtual void OnHide() { }

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
