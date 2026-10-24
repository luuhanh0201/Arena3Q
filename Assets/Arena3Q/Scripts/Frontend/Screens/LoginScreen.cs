using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Frontend
{
    /// <summary>Login and registration use the same reusable account form as guest linking.</summary>
    public sealed class LoginScreen : StartupScreen
    {
        private RectTransform _panel, _formLayer, _reopen;
        private AccountForm _form;
        private bool _register;
        public bool IsReturning => Flow.Profile.IsSignedIn;
        public string Error => _form?.Error ?? "";
        public AccountForm Form => _form;

        protected override void BuildScreen()
        {
            Backdrop(form: true); Kit.Logo(Design, 495, 8, 290);
            CornerButtons(language: true); Kit.Footer(Design, false);
            _reopen = Kit.Rect(Design, "LoginActions", new Rect(400, 510, 480, 150));
            Kit.Button(_reopen, "OpenLogin", Kit.C("login"), new Rect(0, 0, 480, 64), () => ShowAccountForm(false), Arena3Q.Data.GameButtonStyle.Red);
            Kit.Button(_reopen, "PlayNow", Kit.C("playNow"), new Rect(0, 80, 480, 64), PlayAsGuest, Arena3Q.Data.GameButtonStyle.Gold);
            Rebuild();
        }
        private void Rebuild()
        {
            if (_formLayer != null) { _formLayer.gameObject.SetActive(false); Object.Destroy(_formLayer.gameObject); }
            _reopen.gameObject.SetActive(false);
            _formLayer = Kit.Dim(Design, "AccountFormOverlay", 0);
            Kit.DismissOutside(_formLayer, CloseForm);
            var bounds = _register ? Art.registerPanel : Art.loginPanel;
            _panel = Kit.Panel(_formLayer, _register ? "RegisterPanel" : "LoginPanel", bounds, animate: true);
            _form = new AccountForm(Flow, _panel, _register, false, ShowAccountForm, PlayAsGuest, Enter);
            if (_register) Back(() => ShowAccountForm(false));
        }
        public void CloseForm()
        {
            Kit.Close(_formLayer); _formLayer = null; _panel = null; _form = null;
            var back = Design.Find("Back"); if (back != null) { back.gameObject.SetActive(false); Object.Destroy(back.gameObject); }
            _reopen.gameObject.SetActive(true);
            _reopen.gameObject.AddComponent<UiReveal>().Configure(Art.modalSeconds);
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
                if (result == null || !result.Success) { Flow.ShowMessage(result?.Message ?? Kit.C("loginFailed")); return; }
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
