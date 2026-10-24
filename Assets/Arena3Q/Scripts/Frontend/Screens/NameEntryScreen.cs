using Arena3Q.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// Name the lord right after choosing it, one name per server (spec §2): Vietnamese letters with diacritics,
    /// no special characters, banned words filtered; the dice suggests a period-style name.
    /// </summary>
    public sealed class NameEntryScreen : FrontendScreen
    {
        private InputField _input;
        private Text _error;
        private LordNameRules _rules;
        private RandomNameGenerator _generator;
        private readonly System.Random _random = new System.Random(System.Environment.TickCount);

        public string Name
        {
            get => _input.text;
            set => _input.text = value;
        }

        public string Error => _error.text;

        protected override void Build()
        {
            var names = Flow.Config.Names;
            _rules = names != null ? names.CreateRules() : new LordNameRules();
            _generator = names != null ? names.CreateGenerator() : null;

            Ui.Fill(Root, "Background", Ui.Theme.background);
            var lord = Flow.ChosenLord;
            if (lord != null)
                Ui.Portrait(Root, "Lord", lord.portrait, lord.lord.PlaceholderColor, new Vector2(0.25f, 0.5f), Vector2.zero, new Vector2(420f, 420f));

            var panel = Ui.Panel(Root, "Panel", new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(900f, 560f));
            Ui.Label(panel, "Title", "Đặt tên chủ tướng", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f),
                new Vector2(820f, 70f), Ui.Theme.accent);
            Ui.Label(panel, "Hint", $"Tên của bạn trên máy chủ {Flow.Server?.Name}. {_rules.MinLength}-{_rules.MaxLength} ký tự, có dấu, không ký tự đặc biệt.",
                24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(820f, 70f));
            _input = Ui.Input(panel, "Input", "Nhập tên…", FrontendUi.Center, new Vector2(-90f, 20f), new Vector2(560f, 90f),
                characterLimit: _rules.MaxLength + 4);
            Ui.Button(panel, "Dice", "Ngẫu nhiên", FrontendUi.Center, new Vector2(330f, 20f), new Vector2(200f, 90f))
                .onClick.AddListener(Randomize);
            _error = Ui.Label(panel, "Error", "", 26, TextAnchor.MiddleCenter, FrontendUi.Center, new Vector2(0f, -80f),
                new Vector2(820f, 60f), Ui.Theme.error);
            Ui.Button(panel, "Submit", "BẮT ĐẦU", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(420f, 100f), primary: true)
                .onClick.AddListener(() => Submit());
            Randomize();
        }

        /// <summary>Fills in a random valid name (the dice).</summary>
        public void Randomize()
        {
            if (_generator == null)
                return;
            var name = _generator.Next(_random, _rules, IsTaken);
            if (!string.IsNullOrEmpty(name))
                Name = name;
            _error.text = "";
        }

        /// <summary>Validates and saves the name; returns false and shows why when it is not allowed.</summary>
        public bool Submit()
        {
            var problem = _rules.Validate(Name, IsTaken);
            if (problem != NameProblem.None)
            {
                _error.text = Describe(problem);
                return false;
            }
            Flow.SetLordName(LordNameRules.Normalize(Name));
            Flow.Complete();
            return true;
        }

        // One character per server and account: the server checks other players' names once there is a backend (⏳).
        private static bool IsTaken(string name) => false;

        private string Describe(NameProblem problem)
        {
            switch (problem)
            {
                case NameProblem.TooShort: return $"Tên cần ít nhất {_rules.MinLength} ký tự.";
                case NameProblem.TooLong: return $"Tên tối đa {_rules.MaxLength} ký tự.";
                case NameProblem.InvalidCharacters: return "Tên chỉ gồm chữ (có dấu), số và khoảng trắng.";
                case NameProblem.Banned: return "Tên có từ không được phép.";
                case NameProblem.Taken: return "Tên đã có người dùng trên máy chủ này.";
                default: return "";
            }
        }
    }
}
