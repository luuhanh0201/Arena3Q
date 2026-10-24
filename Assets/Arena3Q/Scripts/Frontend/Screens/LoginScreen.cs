using Arena3Q.Core;
using Arena3Q.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// Sign in (docs/KeHoach/VaoGame.md §6): Google, Apple (iOS only), game account, register, or "Chơi ngay" as a guest.
    /// A returning player only sees "tap to enter" and "change account".
    /// </summary>
    public sealed class LoginScreen : FrontendScreen
    {
        private RectTransform _content;
        private Text _error;
        private InputField _username;
        private InputField _password;
        private InputField _confirmation;
        private bool _registering;

        public bool IsReturning => Flow.Profile.IsSignedIn;
        public string Error => _error != null ? _error.text : "";

        protected override void Build()
        {
            AddBackdrop(0.35f);
            Ui.Label(Root, "Version", "v" + Application.version, 24, TextAnchor.LowerLeft, Vector2.zero, new Vector2(140f, 30f),
                new Vector2(260f, 40f));
            Ui.Label(Root, "Terms", "Tiếp tục nghĩa là bạn đồng ý Điều khoản dịch vụ và Chính sách bảo mật.", 22,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1400f, 40f));
            Rebuild();
        }

        private void Rebuild()
        {
            if (_content != null)
                Object.Destroy(_content.gameObject);
            _content = Ui.Panel(Root, "Panel", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(760f, 500f));
            _error = Ui.Label(_content, "Error", "", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 40f),
                new Vector2(700f, 60f), Ui.Theme.error);
            if (IsReturning)
                BuildReturning();
            else
                BuildChoices();
        }

        private void BuildChoices()
        {
            var y = 175f;
            Ui.Button(_content, "Google", "Đăng nhập bằng Google", FrontendUi.Center, new Vector2(0f, y), new Vector2(620f, 80f))
                .onClick.AddListener(() => SignIn(AccountKind.Google));
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                y -= 95f;
                Ui.Button(_content, "Apple", "Đăng nhập với Apple", FrontendUi.Center, new Vector2(0f, y), new Vector2(620f, 80f))
                    .onClick.AddListener(() => SignIn(AccountKind.Apple));
            }
            y -= 95f;
            Ui.Button(_content, "Account", "Tài khoản", FrontendUi.Center, new Vector2(-160f, y), new Vector2(300f, 80f))
                .onClick.AddListener(() => ShowAccountForm(false));
            Ui.Button(_content, "Register", "Đăng ký", FrontendUi.Center, new Vector2(160f, y), new Vector2(300f, 80f))
                .onClick.AddListener(() => ShowAccountForm(true));
            y -= 120f;
            Ui.Button(_content, "PlayNow", "CHƠI NGAY", FrontendUi.Center, new Vector2(0f, y), new Vector2(620f, 100f), primary: true)
                .onClick.AddListener(PlayAsGuest);
        }

        private void BuildReturning()
        {
            Ui.Label(_content, "Account", $"Tài khoản: {Flow.Profile.accountLabel}", 30, TextAnchor.MiddleCenter, FrontendUi.Center,
                new Vector2(0f, 170f), new Vector2(700f, 50f));
            Ui.Button(_content, "Enter", "CHẠM ĐỂ VÀO GAME", FrontendUi.Center, new Vector2(0f, 40f), new Vector2(620f, 110f), primary: true)
                .onClick.AddListener(Enter);
            Ui.Button(_content, "SwitchAccount", "Đổi tài khoản", FrontendUi.Center, new Vector2(0f, -100f), new Vector2(420f, 80f))
                .onClick.AddListener(AskSwitchAccount);
        }

        // ---- Actions ----

        /// <summary>"Chơi ngay": a guest account bound to this device.</summary>
        public void PlayAsGuest() => Flow.Accounts.SignIn(AccountKind.Guest, null, null, OnSignedIn);

        public void SignIn(AccountKind kind) => Flow.Accounts.SignIn(kind, null, null, OnSignedIn);

        public void Enter() => Flow.Complete();

        public void ShowAccountForm(bool register)
        {
            _registering = register;
            if (_content != null)
                Object.Destroy(_content.gameObject);
            _content = Ui.Panel(Root, "AccountForm", new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(760f, register ? 640f : 540f));
            Ui.Label(_content, "Title", register ? "Đăng ký tài khoản" : "Đăng nhập tài khoản", 38, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(700f, 60f), Ui.Theme.accent);
            var y = register ? 140f : 100f;
            _username = Ui.Input(_content, "Username", "Tên đăng nhập (6-20 ký tự)", FrontendUi.Center, new Vector2(0f, y), new Vector2(620f, 80f));
            _password = Ui.Input(_content, "Password", "Mật khẩu (từ 8 ký tự)", FrontendUi.Center, new Vector2(0f, y - 95f), new Vector2(620f, 80f), password: true);
            _confirmation = register
                ? Ui.Input(_content, "Confirm", "Nhập lại mật khẩu", FrontendUi.Center, new Vector2(0f, y - 190f), new Vector2(620f, 80f), password: true)
                : null;
            var buttonsY = register ? -170f : -110f;
            Ui.Button(_content, "Back", "Quay lại", FrontendUi.Center, new Vector2(-160f, buttonsY), new Vector2(300f, 80f))
                .onClick.AddListener(Rebuild);
            Ui.Button(_content, "Submit", register ? "Đăng ký" : "Đăng nhập", FrontendUi.Center, new Vector2(160f, buttonsY),
                new Vector2(300f, 80f), primary: true).onClick.AddListener(() =>
                SubmitAccount(_username.text, _password.text, _confirmation != null ? _confirmation.text : null));
            _error = Ui.Label(_content, "Error", "", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 40f),
                new Vector2(700f, 60f), Ui.Theme.error);
        }

        /// <summary>Checks the fields, then signs in or registers through the account service.</summary>
        public void SubmitAccount(string username, string password, string confirmation)
        {
            var rules = Flow.AccountRules;
            var problem = rules.ValidateUsername(username);
            if (problem == CredentialProblem.None)
                problem = rules.ValidatePassword(password, _registering ? confirmation ?? "" : null);
            if (problem != CredentialProblem.None)
            {
                ShowError(Describe(problem, rules));
                return;
            }
            if (_registering)
                Flow.Accounts.Register(username, password, OnSignedIn);
            else
                Flow.Accounts.SignIn(AccountKind.Password, username, password, OnSignedIn);
        }

        private void AskSwitchAccount()
        {
            if (_content != null)
                Object.Destroy(_content.gameObject);
            _content = Ui.Modal(Root, "Confirm", new Vector2(860f, 420f)).parent as RectTransform;
            var dialog = (RectTransform)_content.Find("Panel");
            var warning = Flow.Profile.accountKind == AccountKind.Guest
                ? "Tài khoản khách chưa liên kết: đổi tài khoản sẽ XOÁ toàn bộ dữ liệu trên máy này."
                : "Đăng xuất khỏi tài khoản này?";
            Ui.Label(dialog, "Warning", warning, 32, TextAnchor.MiddleCenter, FrontendUi.Center, new Vector2(0f, 70f), new Vector2(780f, 200f));
            Ui.Button(dialog, "Cancel", "Huỷ", FrontendUi.Center, new Vector2(-180f, -120f), new Vector2(320f, 80f)).onClick.AddListener(Rebuild);
            Ui.Button(dialog, "Confirm", "Đồng ý", FrontendUi.Center, new Vector2(180f, -120f), new Vector2(320f, 80f), primary: true)
                .onClick.AddListener(SwitchAccount);
        }

        /// <summary>Signs out (and for a guest forgets everything), then shows the sign-in choices.</summary>
        public void SwitchAccount()
        {
            Flow.SignOut();
            Rebuild();
        }

        private void OnSignedIn(SignInResult result)
        {
            if (result == null || !result.Success)
            {
                ShowError(result?.Message ?? "Đăng nhập thất bại.");
                return;
            }
            Flow.SignIn(result);
            Flow.Complete();
        }

        private void ShowError(string message)
        {
            if (_error != null)
                _error.text = message;
            Flow.ShowMessage(message);
        }

        private static string Describe(CredentialProblem problem, AccountRules rules)
        {
            switch (problem)
            {
                case CredentialProblem.UsernameLength: return $"Tên đăng nhập cần {rules.MinUsernameLength}-{rules.MaxUsernameLength} ký tự.";
                case CredentialProblem.UsernameCharacters: return "Tên đăng nhập chỉ gồm chữ không dấu, số và dấu _.";
                case CredentialProblem.PasswordLength: return $"Mật khẩu cần ít nhất {rules.MinPasswordLength} ký tự.";
                case CredentialProblem.PasswordMismatch: return "Hai lần nhập mật khẩu không khớp.";
                default: return "";
            }
        }
    }
}
