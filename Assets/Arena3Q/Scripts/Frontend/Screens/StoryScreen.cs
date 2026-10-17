using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// The opening story (docs/KeHoach/VaoGame.md §10): one picture panel at a time with a dialogue box; tap for the next
    /// line, "Bỏ qua" to skip. {Tên} becomes the player's name; the player's lord speaks with its own portrait.
    /// </summary>
    public sealed class StoryScreen : FrontendScreen
    {
        private StoryScript _story;
        private StoryCursor _cursor;
        private Image _picture;
        private Text _caption;
        private RectTransform _box;
        private Image _portrait;
        private Text _speaker;
        private Text _line;

        public int Panel => _cursor.Panel;
        public int Line => _cursor.Line;
        public string CurrentText => _line != null ? _line.text : "";
        public string CurrentSpeaker => _speaker != null ? _speaker.text : "";

        protected override void Build()
        {
            _story = Flow.Config.OpeningStory;
            _cursor = new StoryCursor(_story != null ? _story.LinesPerPanel() : null);

            Ui.Fill(Root, "Background", Color.black);
            _picture = Ui.Fill(Root, "Picture", Color.white);
            _caption = Ui.Label(Root, "Caption", "", 40, TextAnchor.MiddleCenter, FrontendUi.Center, new Vector2(0f, 150f),
                new Vector2(1500f, 300f), new Color(1f, 1f, 1f, 0.55f));
            AddTapCatcher(Advance);

            _box = Ui.Panel(Root, "DialogueBox", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1700f, 260f));
            _portrait = Ui.Portrait(_box, "Portrait", null, Color.white, new Vector2(0f, 0.5f), new Vector2(130f, 40f), new Vector2(200f, 200f));
            _speaker = Ui.Label(_box, "Speaker", "", 32, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(600f, -35f),
                new Vector2(700f, 50f), Ui.Theme.accent);
            _line = Ui.Label(_box, "Line", "", 32, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(120f, -25f),
                new Vector2(1300f, 160f));
            Ui.Button(Root, "Skip", "Bỏ qua", new Vector2(1f, 1f), new Vector2(-150f, -60f), new Vector2(220f, 80f))
                .onClick.AddListener(Skip);

            if (_cursor.IsFinished)
                Finish();
            else
                Render();
        }

        /// <summary>Next line or panel; ends the story after the last line.</summary>
        public void Advance()
        {
            if (_cursor.IsFinished)
                return;
            if (_cursor.Advance())
                Render();
            else
                Finish();
        }

        public void Skip()
        {
            _cursor.Skip();
            Finish();
        }

        private void Render()
        {
            var panel = _story.Panels[_cursor.Panel];
            var lordId = Flow.Character != null ? Flow.Character.lordHeroId : "";
            var image = panel.ImageFor(lordId);
            _picture.sprite = image;
            _picture.color = image != null ? Color.white : panel.placeholderColor;
            _caption.text = image != null ? "" : panel.placeholderCaption;

            var hasLine = panel.lines != null && _cursor.Line < panel.lines.Length;
            _box.gameObject.SetActive(hasLine);
            if (!hasLine)
                return;

            var line = panel.lines[_cursor.Line];
            var speaker = _story.FindSpeaker(line.speakerId);
            var playerName = Flow.Character != null ? Flow.Character.name : "";
            _line.text = StoryCursor.Format(line.text, playerName);

            var narrator = speaker == null || speaker.isNarrator;
            _portrait.gameObject.SetActive(!narrator);
            _speaker.text = narrator ? "" : SpeakerName(speaker, playerName);
            if (narrator)
                return;

            var lord = Flow.ChosenLord;
            var sprite = speaker.isPlayerLord && lord != null ? lord.portrait : speaker.portrait;
            var color = speaker.isPlayerLord && lord != null ? lord.lord.PlaceholderColor : SpeakerColor(speaker);
            _portrait.sprite = sprite != null ? sprite : Arena3Q.Gameplay.PlaceholderArt.Circle;
            _portrait.color = sprite != null ? Color.white : color;
            // Allies speak from the left, enemies from the right.
            var x = speaker.rightSide ? 1570f : 130f;
            _portrait.rectTransform.anchoredPosition = new Vector2(x, 40f);
        }

        private static string SpeakerName(StoryScript.Speaker speaker, string playerName)
        {
            if (speaker.isPlayerLord)
                return playerName;
            return speaker.hero != null ? speaker.hero.DisplayName : speaker.displayName;
        }

        private static Color SpeakerColor(StoryScript.Speaker speaker) =>
            speaker.hero != null ? speaker.hero.PlaceholderColor : speaker.color;

        private void Finish()
        {
            if (Flow.Character != null)
                Flow.MarkStorySeen();
            Flow.Complete();
        }
    }
}
