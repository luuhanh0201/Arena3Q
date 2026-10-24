using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Frontend
{
    /// <summary>Login and registration use the same reusable account form as guest linking.</summary>
    public sealed class LoginScreen : StartupScreen
    {
        private RectTransform _panel;
        private AccountForm _form;
        private bool _register;
        public bool IsReturning => Flow.Profile.IsSignedIn;
        public string Error => _form?.Error ?? "";
        public AccountForm Form => _form;

        protected override void BuildScreen()
        {
            Backdrop(form: true); Kit.Logo(Design, 495, 8, 290);
            CornerButtons(language: true); Kit.Footer(Design, false);
            Rebuild();
        }
        private void Rebuild()
        {
            if (_panel != null) { _panel.gameObject.SetActive(false); Object.Destroy(_panel.gameObject); }
            var bounds = _register ? Art.registerPanel : Art.loginPanel;
            // AccountForm reserves space for inline validation and has a single layout for all consumers.
            bounds.height = _register ? Mathf.Max(480, bounds.height) : bounds.height;
            _panel = Kit.Panel(Design, _register ? "RegisterPanel" : "LoginPanel", bounds);
            _form = new AccountForm(Flow, _panel, _register, false, ShowAccountForm, PlayAsGuest, Enter);
            if (_register) Back(() => ShowAccountForm(false));
        }
        public override void Tick(float deltaTime) => _form?.Tick();
        public void ShowAccountForm(bool register)
        {
            _register = register;
            var back = Design.Find("Back"); if (back != null) { back.gameObject.SetActive(false); Object.Destroy(back.gameObject); }
            Rebuild();
        }
        public void PlayAsGuest()
        {
            if (Flow.Profile.IsSignedIn) { Enter(); return; }
            Flow.Accounts.SignIn(AccountKind.Guest, null, null, result =>
            {
                if (Root == null) return;
                if (result == null || !result.Success) { _form.ShowError(result?.Message ?? Kit.C("loginFailed")); return; }
                Flow.SignIn(result); Enter();
            });
        }
        public void SignIn(AccountKind kind) => _form.Provider(kind);
        public void Enter()
        {
            if (!Flow.Profile.IsSignedIn) { _form.ShowError(Kit.C("loginRequired")); return; }
            Flow.Complete();
        }
        public void SwitchAccount() { Flow.SignOut(); _register = false; Rebuild(); }
        public void SubmitAccount(string username, string password, string confirmation)
        {
            _form.AddressInput.text = username;
            _form.PasswordInput.text = password;
            if (_form.ConfirmationInput != null) _form.ConfirmationInput.text = confirmation ?? "";
            _form.Submit();
        }
    }
}
