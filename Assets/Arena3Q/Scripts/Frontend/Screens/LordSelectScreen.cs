using System;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// Pick one of the four lords for good (spec §2, docs/KeHoach/VaoGame.md §9): tabs, the lord in the middle, an
    /// info panel with role, difficulty, level-1 stats and the four skills with the level that unlocks each.
    /// </summary>
    public sealed class LordSelectScreen : FrontendScreen
    {
        private RectTransform _stage;
        private RectTransform _info;
        private RectTransform _confirm;

        public int SelectedIndex { get; private set; }
        public LordRoster.Entry Selected => Flow.Config.LordRoster.Lords[SelectedIndex];
        public bool IsConfirming => _confirm != null;

        protected override void Build()
        {
            Ui.Fill(Root, "Background", Ui.Theme.background);
            Ui.Label(Root, "Title", "Chọn chủ tướng", 48, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(330f, -70f),
                new Vector2(600f, 80f), Ui.Theme.accent);
            _stage = UiFactory.CreateRect("Stage", Root, new Vector2(0.3f, 0.55f), new Vector2(0.3f, 0.55f), FrontendUi.Center,
                Vector2.zero, new Vector2(700f, 700f));
            _info = Ui.Panel(Root, "Info", new Vector2(0.75f, 0.55f), Vector2.zero, new Vector2(780f, 760f));

            var lords = Flow.Config.LordRoster.Lords;
            var width = 1100f / Mathf.Max(1, lords.Count);
            for (var i = 0; i < lords.Count; i++)
            {
                var index = i;
                Ui.Button(Root, $"Tab {i}", lords[i].lord.DisplayName, new Vector2(0.3f, 0f),
                    new Vector2((i - (lords.Count - 1) * 0.5f) * width, 80f), new Vector2(width - 20f, 90f))
                    .onClick.AddListener(() => SelectLord(index));
            }
            Ui.Button(Root, "Choose", "CHỌN", new Vector2(0.75f, 0f), new Vector2(0f, 80f), new Vector2(360f, 100f), primary: true)
                .onClick.AddListener(Choose);
            SelectLord(0);
        }

        public void SelectLord(int index)
        {
            var lords = Flow.Config.LordRoster.Lords;
            SelectedIndex = Mathf.Clamp(index, 0, lords.Count - 1);
            ShowLord(lords[SelectedIndex]);
        }

        private void ShowLord(LordRoster.Entry entry)
        {
            foreach (Transform child in _stage)
                UnityEngine.Object.Destroy(child.gameObject);
            foreach (Transform child in _info)
                UnityEngine.Object.Destroy(child.gameObject);

            // Placeholder until the rig (clip "select") or the cut-out art exists.
            Ui.Portrait(_stage, "Lord", entry.portrait, entry.lord.PlaceholderColor, FrontendUi.Center, new Vector2(0f, 40f), new Vector2(460f, 460f));
            Ui.Label(_stage, "Name", entry.lord.DisplayName, 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 40f),
                new Vector2(600f, 80f), Ui.Theme.accent);

            Ui.Label(_info, "Role", entry.role, 28, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 90f));
            Ui.Label(_info, "Difficulty", "Độ khó: " + new string('★', entry.difficulty) + new string('☆', 3 - entry.difficulty), 28,
                TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(700f, 44f), Ui.Theme.accent);

            // Level-1 stats as bars relative to the strongest lord in each stat.
            var rows = new (string label, Func<CombatNumbers, float> value)[]
            {
                ("Máu", n => n.MaxHealth),
                ("Sát thương / giây", n => n.Damage * n.AttacksPerSecond),
                ("Tầm đánh", n => n.AttackRange),
                ("Tốc chạy", n => n.MoveSpeed)
            };
            var numbers = Numbers(entry);
            for (var r = 0; r < rows.Length; r++)
            {
                var max = 0f;
                foreach (var other in Flow.Config.LordRoster.Lords)
                    max = Mathf.Max(max, rows[r].value(Numbers(other)));
                var share = max > 0f ? rows[r].value(numbers) / max : 0f;
                var y = -200f - r * 50f;
                Ui.Label(_info, $"Stat {r}", rows[r].label, 24, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(-200f, y), new Vector2(300f, 40f));
                var bar = UiFactory.CreateRect($"Bar {r}", _info, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f),
                    new Vector2(-30f, y), new Vector2(380f * Mathf.Clamp01(share), 22f));
                UiFactory.AddImage(bar, Ui.Theme.accent, null, false);
            }

            // Skills: a lord starts without skills and learns them by level (spec §2).
            Ui.Label(_info, "SkillsTitle", "Kỹ năng (học dần khi lên cấp)", 28, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f),
                new Vector2(0f, -420f), new Vector2(700f, 44f), Ui.Theme.accent);
            foreach (SkillSlot slot in Enum.GetValues(typeof(SkillSlot)))
            {
                var y = -470f - (int)slot * 64f;
                var text = $"Lv{Flow.Rules.LordUnlockLevel(slot)} · {SkillSlotNames.Of(slot)}: {entry.SkillName(slot)}";
                var summary = entry.SkillSummary(slot);
                Ui.Label(_info, $"Skill {slot}", text, 25, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, y),
                    new Vector2(700f, 34f));
                if (!string.IsNullOrEmpty(summary))
                    Ui.Label(_info, $"SkillInfo {slot}", summary, 20, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f),
                        new Vector2(700f, 30f), new Color(Ui.Theme.text.r, Ui.Theme.text.g, Ui.Theme.text.b, 0.7f));
            }
        }

        private CombatNumbers Numbers(LordRoster.Entry entry) =>
            CombatNumbersFactory.ForHero(entry.lord, 1, 1, Flow.Rules, CombatModifiers.None);

        /// <summary>Asks for confirmation: the lord cannot be changed later.</summary>
        public void Choose()
        {
            Cancel();
            var entry = Selected;
            _confirm = Ui.Modal(Root, "Confirm", new Vector2(900f, 640f));
            Ui.Portrait(_confirm, "Splash", entry.splash != null ? entry.splash : entry.portrait, entry.lord.PlaceholderColor,
                new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(260f, 260f));
            Ui.Label(_confirm, "Question", $"Chủ tướng là nhân vật cố định, KHÔNG đổi được.\nChọn {entry.lord.DisplayName}?", 32,
                TextAnchor.MiddleCenter, FrontendUi.Center, new Vector2(0f, -70f), new Vector2(820f, 120f));
            Ui.Button(_confirm, "Cancel", "Xem lại", FrontendUi.Center, new Vector2(-200f, -220f), new Vector2(340f, 90f))
                .onClick.AddListener(Cancel);
            Ui.Button(_confirm, "Confirm", "Đồng ý", FrontendUi.Center, new Vector2(200f, -220f), new Vector2(340f, 90f), primary: true)
                .onClick.AddListener(Confirm);
        }

        public void Cancel()
        {
            // The modal is the panel's parent (dim layer + panel).
            if (_confirm != null)
                UnityEngine.Object.Destroy(_confirm.parent.gameObject);
            _confirm = null;
        }

        public void Confirm()
        {
            Flow.ChooseLord(Selected);
            Flow.Complete();
        }
    }
}
