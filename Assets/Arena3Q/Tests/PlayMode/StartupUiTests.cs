#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class StartupUiTests
    {
        private FrontendController _flow;
        private readonly Dictionary<string, int?> _ints = new Dictionary<string, int?>();
        private float? _musicVolume; private string _rememberAddress;
        private bool _hadAddress;
        private static string Output => Path.GetFullPath("NghiemThu/UI/XemTruoc_Startup_Review2_20261024/after");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var key in new[] { "Arena3Q.Music.Muted", "Arena3Q.UI.Muted", "Arena3Q.UI.Remember" })
            { _ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null; PlayerPrefs.DeleteKey(key); }
            _musicVolume = PlayerPrefs.HasKey("Arena3Q.Music.Volume") ? PlayerPrefs.GetFloat("Arena3Q.Music.Volume") : (float?)null;
            PlayerPrefs.DeleteKey("Arena3Q.Music.Volume");
            _hadAddress = PlayerPrefs.HasKey("Arena3Q.UI.Address"); _rememberAddress = PlayerPrefs.GetString("Arena3Q.UI.Address"); PlayerPrefs.DeleteKey("Arena3Q.UI.Address");
            FrontendServiceLocator.Reset(); FrontendController.ResumeAt = null;
            FrontendServiceLocator.Override(profiles: new InMemoryProfileStore());
            yield return SceneManager.LoadSceneAsync("Frontend"); yield return null;
            _flow = Object.FindAnyObjectByType<FrontendController>();
            Assert.That(_flow.Config.StartupUi, Is.Not.Null);
        }
        [TearDown]
        public void TearDown()
        {
            FrontendServiceLocator.Reset(); FrontendController.ResumeAt = null; SafeAreaFitter.OverrideArea = null;
            foreach (var pair in _ints) { if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key); }
            if (_musicVolume.HasValue) PlayerPrefs.SetFloat("Arena3Q.Music.Volume", _musicVolume.Value); else PlayerPrefs.DeleteKey("Arena3Q.Music.Volume");
            if (_hadAddress) PlayerPrefs.SetString("Arena3Q.UI.Address", _rememberAddress); else PlayerPrefs.DeleteKey("Arena3Q.UI.Address");
            PlayerPrefs.Save();
        }
        private LoginScreen Login()
        {
            ((IntroScreen)_flow.Screen).Finish();
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Loading));
            _flow.Screen.Tick(_flow.Config.StartupUi.localPreparationSeconds + 1);
            return (LoginScreen)_flow.Screen;
        }
        private ServerSelectScreen StartScreen() { Login().PlayAsGuest(); return (ServerSelectScreen)_flow.Screen; }

        [UnityTest]
        public IEnumerator ButtonPress_UsesSameSprite_AndReturnsToItsOriginalLayout()
        {
            var login = Login(); yield return new WaitForSecondsRealtime(.3f);
            var button = login.Root.GetComponentsInChildren<Button>().Single(b => b.name == "PlayNow");
            var rect = (RectTransform)button.transform; var sprite = ((Image)button.targetGraphic).sprite;
            var position = rect.anchoredPosition;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.07f);
            Assert.That(rect.localScale.x, Is.LessThan(.97f));
            Assert.That(((Image)button.targetGraphic).sprite, Is.SameAs(sprite));
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(rect.localScale.x, Is.EqualTo(1).Within(.001));
            Assert.That(Vector2.Distance(rect.anchoredPosition, position), Is.LessThan(.01));
        }
        [UnityTest]
        public IEnumerator GuestButton_ReceivesActualRaycast_AndCannotCreateDuplicateIdentity()
        {
            var login = Login(); yield return new WaitForSecondsRealtime(.3f);
            var button = login.Root.GetComponentsInChildren<Button>().Single(b => b.name == "PlayNow");
            Click(button);
            Assert.That(_flow.Screen, Is.InstanceOf<ServerSelectScreen>());
            var id = _flow.Profile.accountId;
            Assert.That(id, Does.StartWith("guest-"));
            Assert.That(_flow.Profile.characters, Is.Empty, "entering Start does not manufacture a character");
            ((ServerSelectScreen)_flow.Screen).OpenBind(); yield return new WaitForSecondsRealtime(.3f);
            var provider = _flow.Screen.Root.GetComponentsInChildren<Button>().Single(b => b.name == "LinkGoogle"); Click(provider);
            Assert.That(_flow.Profile.accountId, Is.EqualTo(id));
            Assert.That(_flow.Profile.accountKind, Is.EqualTo(AccountKind.Guest));
            Assert.That(_flow.LastMessage, Does.Contain("Chưa hỗ trợ"));
        }
        [UnityTest]
        public IEnumerator ServerPopup_CancelKeepsSelection_ConfirmPersists_ClosedServersExplain()
        {
            var start = StartScreen(); var original = start.Selected;
            start.OpenPicker(); yield return new WaitForSecondsRealtime(.3f);
            var other = _flow.Servers.First(s => s.CanEnter && s.Id != original.Id);
            start.Select(other); start.CancelPicker();
            Assert.That(start.Selected, Is.SameAs(original));
            Assert.That(_flow.Profile.lastServerId, Is.Empty);
            start.OpenPicker(); yield return new WaitForSecondsRealtime(.3f);
            var full = _flow.Servers.First(s => s.Status == ServerStatus.Full);
            start.Select(full); Assert.That(start.Selected, Is.SameAs(original));
            Assert.That(_flow.LastMessage, Does.Contain("Đầy"));
            start.Select(other); start.ConfirmServer(); Assert.That(_flow.Profile.lastServerId, Is.EqualTo(other.Id));
            Assert.That(start.PickerOpen, Is.False);
            yield return null;
            Assert.That(start.Root.GetComponentsInChildren<Button>().Count(b => b.name == "Sound"), Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator Register_ValidatesFields_TermsAndVerification_AndDoesNotPretendToSendOffline()
        {
            var login = Login(); login.ShowAccountForm(true); yield return null;
            var form = login.Form;
            Assert.That(form.SubmitButton.interactable, Is.False);
            form.AddressInput.text = "bad"; form.RequestVerification(); Assert.That(login.Error, Does.Contain("email"));
            form.AddressInput.text = "player@example.com";
            form.PasswordInput.text = "password123"; form.ConfirmationInput.text = "different"; form.TermsToggle.isOn = true;
            Assert.That(form.SubmitButton.interactable, Is.False);
            form.ConfirmationInput.text = "password123";
            Assert.That(form.SubmitButton.interactable, Is.True);
            form.Submit(); Assert.That(login.Error, Does.Contain("6 chữ số"));
            form.RequestVerification(); Assert.That(_flow.VerificationReadyAt, Is.Zero, "failed offline send must not start a fake countdown");
            Assert.That(_flow.LastMessage, Does.Contain("Chưa hỗ trợ"));
            form.CodeInput.text = "012345"; form.Submit(); Assert.That(_flow.Profile.IsSignedIn, Is.False);
        }
        [UnityTest]
        public IEnumerator VerificationCooldown_SurvivesTabChanges_WhenServiceAcceptsRequest()
        {
            FrontendServiceLocator.Override(accountActions: new AcceptCodeActions());
            var login = Login(); login.ShowAccountForm(true); var form = login.Form;
            form.AddressInput.text = "player@example.com"; form.RequestVerification();
            var deadline = _flow.VerificationReadyAt; Assert.That(deadline, Is.GreaterThan(Time.unscaledTime));
            login.ShowAccountForm(false); login.ShowAccountForm(true); login.Tick(0); yield return null;
            var send = login.Root.GetComponentsInChildren<Button>().Single(b => b.name == "SendCode");
            Assert.That(send.interactable, Is.False); Assert.That(_flow.VerificationReadyAt, Is.EqualTo(deadline));
        }
        [UnityTest]
        public IEnumerator Music_StartsAtLoading_LoopsAndSettingsPersistVolumeAndMute()
        {
            Assert.That(_flow.Music.Started, Is.False, "no BGM over the supplied splash sound");
            ((IntroScreen)_flow.Screen).Finish();
            Assert.That(_flow.Music.Started, Is.True); Assert.That(_flow.Music.Source.loop, Is.True);
            Assert.That(_flow.Music.Source.clip, Is.SameAs(_flow.Config.StartupUi.music));
            Assert.That(_flow.Music.Volume, Is.EqualTo(.12f).Within(.001));
            _flow.Screen.Tick(10); ((LoginScreen)_flow.Screen).PlayAsGuest();
            var start = (ServerSelectScreen)_flow.Screen; start.OpenSettings(); yield return new WaitForSecondsRealtime(.3f);
            var slider = start.Root.GetComponentsInChildren<Slider>().Single(s => s.name == "MusicVolume"); slider.value = .28f;
            var mute = start.Root.GetComponentsInChildren<Toggle>().Single(t => t.name == "MuteMusic"); mute.isOn = true;
            Assert.That(_flow.Music.Source.volume, Is.EqualTo(.28f).Within(.001)); Assert.That(_flow.Music.Source.mute, Is.True);
            var probe = new GameObject("MusicSettingsProbe").AddComponent<FrontendMusic>(); probe.Configure(_flow.Config.StartupUi);
            Assert.That(probe.Volume, Is.EqualTo(.28f).Within(.001)); Assert.That(probe.Muted, Is.True); Object.Destroy(probe.gameObject);
        }
        [UnityTest]
        public IEnumerator Loading_RetryPreservesProgress_AndLocalPreparationNeedsNoInternet()
        {
            ((IntroScreen)_flow.Screen).Finish(); var loading = (LoadingScreen)_flow.Screen;
            loading.Preview(new StartupProgress(.63f, StartupNetState.Lost)); yield return new WaitForSecondsRealtime(.3f);
            var retry = loading.Root.GetComponentsInChildren<Button>().Single(b => b.name == "Retry"); Click(retry);
            Assert.That(loading.Progress.Progress, Is.EqualTo(.63f).Within(.001));
            Assert.That(loading.Progress.State, Is.EqualTo(StartupNetState.Retrying));
        }
        [UnityTest]
        public IEnumerator SafeArea_ProtectsStartAndServerControls_WhileBackdropCoversCanvas()
        {
            var start = StartScreen();
            var area = new Rect(Screen.width * .06f, Screen.height * .02f, Screen.width * .88f, Screen.height * .96f);
            SafeAreaFitter.OverrideArea = area;
            foreach (var fitter in _flow.GetComponentsInChildren<SafeAreaFitter>()) fitter.Apply();
            yield return new WaitForSecondsRealtime(.3f);
            AssertButtonsInside(area);
            start.OpenPicker(); yield return new WaitForSecondsRealtime(.3f); AssertButtonsInside(area);
            var shade = start.Root.GetComponentsInChildren<CanvasBackdrop>().First(x => !x.coverSprite);
            var corners = new Vector3[4]; ((RectTransform)shade.transform).GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.LessThanOrEqualTo(1));
            Assert.That(corners[2].x, Is.GreaterThanOrEqualTo(Screen.width - 1));
        }
        private void AssertButtonsInside(Rect area)
        {
            var corners = new Vector3[4];
            foreach (var button in _flow.Screen.Root.GetComponentsInChildren<Button>())
            {
                if (!button.IsInteractable()) continue;
                ((RectTransform)button.transform).GetWorldCorners(corners);
                Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(area.xMin - 1), button.name);
                Assert.That(corners[2].x, Is.LessThanOrEqualTo(area.xMax + 1), button.name);
                Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(area.yMin - 1), button.name);
                Assert.That(corners[2].y, Is.LessThanOrEqualTo(area.yMax + 1), button.name);
            }
        }
        [UnityTest]
        public IEnumerator EnteringModal_DoesNotLeaveEnabledButtonsWithDisabledRendererTint()
        {
            var start = StartScreen(); start.OpenBind(); yield return new WaitForSecondsRealtime(.4f);
            foreach (var button in start.Root.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Link")))
            {
                Assert.That(button.IsInteractable(), Is.True, button.name);
                var tint = button.targetGraphic.canvasRenderer.GetColor();
                Assert.That(tint.r, Is.EqualTo(1).Within(.01), button.name + " renderer tint");
                Assert.That(tint.a, Is.EqualTo(1).Within(.01), button.name + " renderer alpha");
            }
            start.OpenPicker(); yield return new WaitForSecondsRealtime(.4f);
            var confirm = start.Root.GetComponentsInChildren<Button>().Single(b => b.name == "ConfirmServer");
            Assert.That(confirm.IsInteractable(), Is.True);
            Assert.That(confirm.targetGraphic.canvasRenderer.GetColor(), Is.EqualTo(Color.white));
        }
        [UnityTest]
        public IEnumerator LegalSentence_HasTwoInlineLinks_AndCheckboxTextStillToggles()
        {
            var login = Login(); yield return new WaitForSecondsRealtime(.3f);
            var legal = login.Root.GetComponentsInChildren<UiLegalLinks>().Single();
            var text = legal.GetComponent<TMP_Text>(); text.ForceMeshUpdate();
            Assert.That(text.textInfo.linkCount, Is.EqualTo(2));
            Assert.That(text.GetParsedText(), Does.Contain("Khi tiếp tục, bạn đồng ý với Điều khoản dịch vụ và Chính sách bảo mật."));
            legal.Open("terms"); Assert.That(_flow.LastMessage, Does.Contain("cập nhật"));
            login.ShowAccountForm(true); yield return null;
            legal = login.Root.GetComponentsInChildren<UiLegalLinks>().Single(); text = legal.GetComponent<TMP_Text>(); text.ForceMeshUpdate();
            Assert.That(text.textInfo.linkCount, Is.EqualTo(2));
            Assert.That(login.Form.TermsToggle.isOn, Is.False);
            var point = text.rectTransform.TransformPoint(new Vector3(text.rectTransform.rect.xMin + 2, text.rectTransform.rect.center.y, 0));
            legal.OnPointerClick(new PointerEventData(EventSystem.current) { position = point });
            Assert.That(login.Form.TermsToggle.isOn, Is.True, "normal words in the sentence toggle the checkbox");
            var panel = login.Root.GetComponentsInChildren<RectTransform>().Single(r => r.name == "RegisterPanel");
            Assert.That(panel.rect.height, Is.EqualTo(414));
        }
        [UnityTest]
        public IEnumerator BottomShade_CoversCanvasWidthOutsideTheSafeDesignFrame()
        {
            var start = StartScreen();
            SafeAreaFitter.OverrideArea = new Rect(Screen.width * .06f, Screen.height * .02f, Screen.width * .88f, Screen.height * .96f);
            foreach (var fitter in _flow.GetComponentsInChildren<SafeAreaFitter>()) fitter.Apply();
            yield return new WaitForSecondsRealtime(.3f);
            var strip = start.Root.GetComponentsInChildren<CanvasBackdrop>().Single(x => x.bottomHeight > 0);
            var corners = new Vector3[4]; ((RectTransform)strip.transform).GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.LessThanOrEqualTo(1)); Assert.That(corners[2].x, Is.GreaterThanOrEqualTo(Screen.width - 1));
            Assert.That(corners[0].y, Is.EqualTo(0).Within(1));
        }
        [UnityTest]
        public IEnumerator Spinner_RendersGoldPixelsInBothRings_OnAnIsolatedNavyBackground()
        {
            Login();
            var canvas = _flow.GetComponentInChildren<Canvas>();
            var root = Arena3Q.UI.UiFactory.Stretch("IsolatedSpinner", canvas.transform);
            Arena3Q.UI.UiFactory.AddImage(root, new Color(.02f, .04f, .08f), raycastTarget: false);
            LoadingSpinner.Create(_flow.StartupUi, root, new Vector2(640, 360));
            yield return Capture("qa_spinner_isolated");
            var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Output, "qa_spinner_isolated.png")));
            var outer = 0; var inner = 0;
            for (var x = -66; x <= 66; x++) for (var y = -66; y <= 66; y++)
            {
                var radius = x * x + y * y; var pixel = texture.GetPixel(640 + x, 360 + y);
                if (pixel.r < .6f || pixel.g < .4f || pixel.b > .75f) continue;
                if (radius >= 59 * 59 && radius <= 65 * 65) outer++;
                if (radius >= 40 * 40 && radius <= 45 * 45) inner++;
            }
            Object.Destroy(texture); Object.Destroy(root.gameObject);
            Assert.That(outer, Is.GreaterThan(100), "outer arc must be visible, not merely have geometry");
            Assert.That(inner, Is.GreaterThan(60), "inner dashed ring must be visible");
        }
        [UnityTest]
        public IEnumerator Screenshot_AllStartupStates_AndSafeAreas()
        {
            Directory.CreateDirectory(Output);
            yield return Capture("01_splash");
            ((IntroScreen)_flow.Screen).Finish(); var loading = (LoadingScreen)_flow.Screen;
            foreach (StartupNetState state in Enum.GetValues(typeof(StartupNetState)))
            {
                loading.Preview(new StartupProgress(.63f, state, state == StartupNetState.Good ? 2400000 : state == StartupNetState.Weak ? 86000 : 0, 2));
                yield return new WaitForSecondsRealtime(.3f); yield return Capture("02_loading_" + state);
            }
            loading.Preview(new StartupProgress(1, local: true, complete: true)); loading.Tick(1);
            var login = (LoginScreen)_flow.Screen; yield return Capture("03_login");
            login.ShowAccountForm(true); yield return Capture("04_register");
            login.PlayAsGuest(); var start = (ServerSelectScreen)_flow.Screen; yield return Capture("05_start");
            start.OpenPicker(); yield return new WaitForSecondsRealtime(.3f); yield return Capture("06_servers");
            start.ShowTab(ServerSelectScreen.Tab.SecondRange); yield return Capture("06_servers_range2");
            start.ShowTab(ServerSelectScreen.Tab.Mine); yield return Capture("06_servers_mine_empty");
            start.CancelPicker(); start.OpenBind(); yield return new WaitForSecondsRealtime(.3f); yield return Capture("07_bind");
            start.OpenLinkForm(); yield return new WaitForSecondsRealtime(.3f); yield return Capture("08_link_form");
            start.OpenSettings(); yield return new WaitForSecondsRealtime(.3f); yield return Capture("09_settings");
            start.CancelPicker(); yield return Capture("10_start_ultrawide", 2340, 1080);
            SafeAreaFitter.OverrideArea = new Rect(Screen.width * .06f, Screen.height * .02f, Screen.width * .88f, Screen.height * .96f);
            foreach (var safe in _flow.GetComponentsInChildren<SafeAreaFitter>()) safe.Apply();
            yield return Capture("11_start_cutout", 2340, 1080);
            start.OpenPicker(); yield return new WaitForSecondsRealtime(.3f); yield return Capture("12_servers_cutout", 2340, 1080);
            SafeAreaFitter.OverrideArea = null;
        }

        private void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " must receive a raycast");
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(target, Is.EqualTo(button.gameObject), "topmost hit must be the requested button");
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        private IEnumerator Capture(string name, int width = 1280, int height = 720)
        {
            yield return new WaitForSecondsRealtime(.3f);
            var camera = Camera.main; var canvas = _flow.GetComponentInChildren<Canvas>();
            var target = new RenderTexture(width, height, 24);
            var oldMode = canvas.renderMode; var oldTarget = camera.targetTexture;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            foreach (var frame in _flow.GetComponentsInChildren<DesignFrame>()) frame.SendMessage("LateUpdate");
            camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG()); Object.Destroy(image);
            var widgetState = _flow.Screen.Root.GetComponentsInChildren<Button>().Select(button =>
                button.name + " interactable=" + button.IsInteractable() + " image=" + button.targetGraphic.color
                + " renderer=" + button.targetGraphic.canvasRenderer.GetColor()
                + " alpha=" + (button.GetComponent<CanvasGroup>() != null ? button.GetComponent<CanvasGroup>().alpha.ToString() : "none"));
            File.WriteAllLines(Path.Combine(Output, name + "_widgets.txt"), widgetState);
            RenderTexture.active = previous; camera.targetTexture = oldTarget; canvas.renderMode = oldMode; target.Release(); Object.Destroy(target);
        }
        private sealed class AcceptCodeActions : IAccountActions
        {
            public void RequestVerification(string address, Action<string> done) => done(null);
            public void ResetPassword(string address, Action<string> done) => done(null);
            public void Link(string id, AccountKind provider, string address, string password, string code, Action<SignInResult> done) => done(SignInResult.Fail("Unavailable"));
            public void RegisterVerified(string address, string password, string code, Action<SignInResult> done) => done(SignInResult.Fail("Unavailable"));
        }
    }
}
#endif
