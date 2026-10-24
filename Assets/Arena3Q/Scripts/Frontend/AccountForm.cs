using System;
using Arena3Q.Core;
using Arena3Q.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>The same account form serves Login, Register and linking a guest. Services own identity changes.</summary>
    public sealed class AccountForm
    {
        private const string RememberKey = "Arena3Q.UI.Remember", AddressKey = "Arena3Q.UI.Address";
        private readonly FrontendController _flow; private readonly StartupUi _ui;
        private readonly Action _signedIn; private readonly bool _register, _link;
        private readonly RectTransform _root;
        private StartupField _address, _password, _confirmation, _code;
        private Toggle _terms, _remember;
        private Button _submit, _send;
        private TMP_Text _error;
        private bool _busy;
        public string Error => _error != null ? _error.text : "";
        public TMP_InputField AddressInput => _address.Input;
        public TMP_InputField PasswordInput => _password.Input;
        public TMP_InputField ConfirmationInput => _confirmation?.Input;
        public TMP_InputField CodeInput => _code?.Input;
        public Toggle TermsToggle => _terms;
        public Button SubmitButton => _submit;

        public AccountForm(FrontendController flow, RectTransform panel, bool register, bool link, Action<bool> switchTab, Action guest, Action signedIn)
        {
            _flow = flow; _ui = flow.StartupUi; _root = panel; _register = register; _link = link; _signedIn = signedIn;
            var art = _ui.Config;
            _ui.Button(panel, "LoginTab", _ui.C("loginTab"), new Rect(44, 30, 216, 42), () => switchTab(false), GameButtonStyle.Plain, UiSound.Tab, 16);
            _ui.Button(panel, "RegisterTab", _ui.C("registerTab"), new Rect(260, 30, 216, 42), () => switchTab(true), GameButtonStyle.Plain, UiSound.Tab, 16);
            panel.Find(register ? "LoginTab/Label" : "RegisterTab/Label").GetComponent<TMP_Text>().color = art.muted;
            _ui.Image(panel, "ActiveTabLine", null, new Rect(register ? 260 : 44, 70, 216, 3), art.goldLine);
            _address = _ui.Field(panel, "EmailOrPhone", _ui.C("address"), new Rect(44, register ? 86 : 89, 432, 44));
            if (!register)
            {
                _password = _ui.Field(panel, "Password", _ui.C("password"), new Rect(44, 143, 432, 44), true, 128);
                _remember = _ui.Toggle(panel, "RememberLogin", _ui.C("remember"), new Rect(44, 197, 244, 24), PlayerPrefs.GetInt(RememberKey, 1) == 1);
                if (_remember.isOn) _address.Value = PlayerPrefs.GetString(AddressKey, "");
                _ui.Button(panel, "ForgotPassword", _ui.C("forgot"), new Rect(300, 197, 176, 24), ResetPassword, GameButtonStyle.Plain, fontSize: 13);
                _submit = _ui.Button(panel, "Submit", _ui.C("login"), new Rect(44, 227, 432, 50), Submit, GameButtonStyle.Red, UiSound.Confirm);
                _ui.Image(panel, "DividerLeft", null, new Rect(44, 293, 104, 1), art.goldLine * new Color(1, 1, 1, .3f));
                _ui.Text(panel, "QuickLogin", _ui.C("quickLogin"), new Rect(150, 281, 220, 28), 12, art.muted, TextAlignmentOptions.Center);
                _ui.Image(panel, "DividerRight", null, new Rect(372, 293, 104, 1), art.goldLine * new Color(1, 1, 1, .3f));
                var providers = new[] { AccountKind.Google, AccountKind.Apple, AccountKind.Facebook };
                for (var i = 0; i < providers.Length; i++)
                {
                    var provider = providers[i];
                    _ui.Button(panel, provider.ToString(), provider.ToString(), new Rect(44 + i * 147, 311, 138, 44), () => Provider(provider));
                }
                _ui.Image(panel, "GoldDivider", null, new Rect(44, 370, 432, 1), art.goldLine);
                _ui.Button(panel, "PlayNow", _ui.C("playNow"), new Rect(44, 382, 432, 64), guest, GameButtonStyle.Gold, UiSound.Enter);
                _ui.Text(panel, "GuestCaption", _ui.C("guestCaption"), new Rect(44, 450, 432, 20), 12, art.gold, TextAlignmentOptions.Center);
                _ui.LegalText(panel, "LegalSentence", new Rect(44, 474, 432, 30), _flow);
                _error = _ui.Text(panel, "FormError", "", new Rect(44, 518, 432, 32), 12, art.bad, TextAlignmentOptions.Center);
                _error.enabled = false; // The shared toast handles service errors; field errors remain inline.
            }
            else
            {
                _code = _ui.Field(panel, "VerificationCode", _ui.C("verification"), new Rect(44, 140, 304, 44), limit: 6);
                _code.Input.contentType = TMP_InputField.ContentType.IntegerNumber;
                _send = _ui.Button(panel, "SendCode", _ui.C("sendCode"), new Rect(356, 140, 120, 44), RequestVerification);
                _password = _ui.Field(panel, "Password", _ui.C("newPassword"), new Rect(44, 194, 212, 44), true, 128);
                _confirmation = _ui.Field(panel, "ConfirmPassword", _ui.C("confirmPassword"), new Rect(264, 194, 212, 44), true, 128);
                _terms = _ui.Toggle(panel, "AcceptTerms", "", new Rect(44, 248, 432, 38));
                _ui.LegalText(_terms.transform, "LegalSentence", new Rect(26, 0, 406, 38), _flow, _terms);
                _submit = _ui.Button(panel, "Submit", _ui.C(link ? "linkAccount" : "createAccount"), new Rect(44, 296, 432, 64), Submit, GameButtonStyle.Gold, UiSound.Confirm, 20);
                _ui.Button(panel, "ExistingAccount", _ui.C("haveAccount"), new Rect(44, 370, 290, 24), () => switchTab(false), GameButtonStyle.Plain, fontSize: 12);
                _ui.Button(panel, "PlayNow", _ui.C(link ? "later" : "playNowSmall"), new Rect(342, 370, 134, 24), guest, GameButtonStyle.Plain, UiSound.Back, 12);
                _error = _ui.Text(panel, "FormError", "", new Rect(44, 420, 432, 36), 12, art.bad, TextAlignmentOptions.Center);
                _error.enabled = false;
                _password.Input.onValueChanged.AddListener(_ => Refresh());
                _confirmation.Input.onValueChanged.AddListener(_ => Refresh());
                _terms.onValueChanged.AddListener(_ => Refresh());
            }
            Refresh();
        }

        private bool Alive => _root != null && _root.gameObject.activeInHierarchy;
        public void Tick()
        {
            if (_send == null || !Alive) return;
            var seconds = Mathf.CeilToInt(Mathf.Max(0, _flow.VerificationReadyAt - Time.unscaledTime));
            _send.interactable = !_busy && seconds == 0;
            _ui.SetLabel(_send, seconds > 0 ? string.Format(_ui.C("resendCountdown"), seconds) : _ui.C("sendCode"));
        }
        private void Refresh()
        {
            _submit.interactable = !_busy && (!_register || (_terms.isOn && _password.Value.Length >= _flow.AccountRules.MinPasswordLength
                && _password.Value == _confirmation.Value));
        }
        private bool AddressValid()
        {
            if (AccountRules.IsEmailOrPhone(_address.Value)) return true;
            var message = _ui.C("invalidAddress"); _address.Error(message); ShowError(message); return false;
        }
        public void RequestVerification()
        {
            if (_busy || Time.unscaledTime < _flow.VerificationReadyAt || !AddressValid()) return;
            SetBusy(true);
            FrontendServiceLocator.AccountActions.RequestVerification(_address.Value.Trim(), error =>
            {
                if (!Alive) return; SetBusy(false);
                if (!string.IsNullOrEmpty(error)) { ShowError(error); return; }
                _flow.VerificationReadyAt = Time.unscaledTime + _ui.Config.verificationSeconds;
                _flow.ShowMessage(_ui.C("verificationSent")); Tick();
            });
        }
        private void ResetPassword()
        {
            if (_busy || !AddressValid()) return;
            SetBusy(true);
            FrontendServiceLocator.AccountActions.ResetPassword(_address.Value.Trim(), message =>
            { if (Alive) { SetBusy(false); _flow.ShowMessage(message ?? _ui.C("resetSent")); } });
        }
        public void Submit()
        {
            if (_busy || !AddressValid()) return;
            var problem = _flow.AccountRules.ValidatePassword(_password.Value, _register ? _confirmation.Value : null);
            if (problem != CredentialProblem.None)
            {
                var error = problem == CredentialProblem.PasswordMismatch ? _ui.C("passwordMismatch")
                    : string.Format(_ui.C("passwordShort"), _flow.AccountRules.MinPasswordLength);
                (problem == CredentialProblem.PasswordMismatch ? _confirmation : _password).Error(error); ShowError(error); return;
            }
            if (_register && !_terms.isOn) { ShowError(_ui.C("termsRequired")); return; }
            if (_register && !AccountRules.IsVerificationCode(_code.Value)) { _code.Error(_ui.C("invalidCode")); ShowError(_ui.C("invalidCode")); return; }
            SetBusy(true);
            if (_link)
                FrontendServiceLocator.AccountActions.Link(_flow.Profile.accountId, AccountKind.Password, _address.Value.Trim(), _password.Value, _code.Value, OnResult);
            else if (_register)
                FrontendServiceLocator.AccountActions.RegisterVerified(_address.Value.Trim(), _password.Value, _code.Value, OnResult);
            else
                _flow.Accounts.SignIn(AccountKind.Password, _address.Value.Trim(), _password.Value, OnResult);
        }
        public void Provider(AccountKind provider)
        {
            if (_busy) return; SetBusy(true);
            if (_link) FrontendServiceLocator.AccountActions.Link(_flow.Profile.accountId, provider, null, null, null, OnResult);
            else _flow.Accounts.SignIn(provider, null, null, OnResult);
        }
        private void OnResult(SignInResult result)
        {
            if (!Alive) return; SetBusy(false);
            if (result == null || !result.Success) { ShowError(result?.Message ?? _ui.C("loginFailed")); return; }
            if (_link && result.AccountId != _flow.Profile.accountId)
            {
                // Linking must preserve identity/progress. A backend may perform a merge explicitly, never the UI.
                ShowError(_ui.C("linkIdentityMismatch")); return;
            }
            if (_remember != null)
            {
                PlayerPrefs.SetInt(RememberKey, _remember.isOn ? 1 : 0);
                if (_remember.isOn) PlayerPrefs.SetString(AddressKey, _address.Value.Trim()); else PlayerPrefs.DeleteKey(AddressKey);
                PlayerPrefs.Save();
            }
            _flow.SignIn(result); _signedIn();
        }
        private void SetBusy(bool value) { _busy = value; Refresh(); if (_send != null) Tick(); }
        public void ShowError(string message)
        {
            if (_error != null) _error.text = message;
            _ui.Audio.Play(UiSound.Error); _flow.ShowMessage(message);
        }
    }
}
