#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arena3Q.Tests.PlayMode
{
    public sealed class LobbyUiRefinementTests
    {
        private static readonly string Output = "NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027/ui_refinements_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        private FrontendController _flow;
        private PlayerProfile _profile;
        private Mouse _mouse;
        private LobbyScreen Lobby => (LobbyScreen)_flow.Screen;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _profile = new PlayerProfile { accountId = "ui-refinement-test", accountKind = AccountKind.Guest, lastServerId = "s3", introSeen = true };
            var character = _profile.GetOrCreateCharacter("s3"); character.name = "Lâm Tử Yên";
            character.lordHeroId = "lord_strategist"; character.lordLevel = 1; character.lordExperience = 50;
            character.lordStars = 5; character.vip = 12; character.combatPower = 1245680;
            character.wallet.Add(new ResourceBalance { id = "Silver", amount = 5320000 });
            character.wallet.Add(new ResourceBalance { id = "Banknotes", amount = 12680 });
            character.wallet.Add(new ResourceBalance { id = "Stamina", amount = 180, capacity = 180 });
            character.storySeen = character.tutorialCleared = true;
            FrontendServiceLocator.Reset(); FrontendServiceLocator.Override(profiles: new InMemoryProfileStore(_profile));
            FrontendController.ResumeAt = FrontendStep.Lobby;
            yield return SceneManager.LoadSceneAsync("Frontend"); yield return null;
            _flow = Object.FindAnyObjectByType<FrontendController>();
            Assert.That(_flow.Step, Is.EqualTo(FrontendStep.Lobby));
            yield return new WaitForSecondsRealtime(.5f);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            FrontendServiceLocator.Reset(); FrontendController.ResumeAt = null; SafeAreaFitter.OverrideArea = null;
            var transition = Object.FindAnyObjectByType<Arena3Q.UI.SceneTransition>();
            if (transition != null) Object.Destroy(transition.gameObject);
        }

        [UnityTest]
        public IEnumerator ResourceHintsSupportHoverTouchExpiryAndOutsideTap()
        {
            var resource = _flow.Config.Lobby.resources.First(entry => entry.id == "Silver");
            var hint = Lobby.Root.GetComponentsInChildren<LobbyResourceHint>().Single(x => x.transform.parent.name == "Resource_" + resource.id);
            var amount = hint.transform.parent.Find("Amount").GetComponent<TMP_Text>();
            var balance = FrontendServiceLocator.Lobby.Read(_profile.FindCharacter("s3"), _flow.Config.Lobby, _flow.Config.Progression).Balance(resource.id);
            Assert.That(amount.text, Is.EqualTo(LobbyResourceRules.Compact(balance.amount, _flow.Config.Lobby.resourceCompactThreshold,
                _flow.Config.Lobby.resourceCompactDecimals, _flow.Config.Lobby.resourceCompactUnit)));
            Assert.That(hint.transform.parent.Find("Name"), Is.Null, "the normal row shows no resource-name label");
            yield return Capture("Resources_Normal");
            ExecuteEvents.Execute(hint.gameObject, new PointerEventData(EventSystem.current) { pointerId = -1 }, ExecuteEvents.pointerEnterHandler);
            Assert.That(Lobby.ResourceTooltipVisible, Is.True);
            Assert.That(Lobby.ResourceTooltipText, Is.EqualTo(resource.label + " " + LobbyResourceRules.Exact(balance.amount)));
            yield return Capture("Resources_Hover");
            foreach (var width in new[] { 2160, 2340, 2400 }) yield return Capture("Resources_Hover_" + width, width);
            Lobby.Tick(_flow.Config.Lobby.resourceTooltipSeconds + .1f);
            Assert.That(Lobby.ResourceTooltipVisible, Is.False);
            ExecuteEvents.Execute(hint.gameObject, new PointerEventData(EventSystem.current) { pointerId = 1, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(Lobby.ResourceTooltipVisible, Is.True, "touch click shares the resource hint path");
            yield return Capture("Resources_Touch");
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(_mouse, new MouseState());
            InputSystem.Update(); _mouse.MakeCurrent(); Lobby.Tick(0);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width * .60f, Screen.height * .35f) }.WithButton(MouseButton.Left));
            InputSystem.Update();
            Assert.That(_mouse.leftButton.isPressed, Is.True, "virtual outside click is held");
            _mouse.MakeCurrent(); Lobby.Tick(0);
            Assert.That(Lobby.ResourceTooltipVisible, Is.False, "a press elsewhere dismisses the hint");
            ExecuteEvents.Execute(hint.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            hint.transform.parent.Find("Add").GetComponent<Button>().onClick.Invoke();
            Assert.That(Lobby.ResourceTooltipVisible, Is.False, "the plus opens its modal without leaving a hint over it");
            Assert.That(Lobby.Root.Find("SafeArea/Design1280/UnavailableModal"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ExperienceUsesProfileProgressionAndRectangularOrangeGoldBar()
        {
            var progression = _flow.Config.Progression;
            var required = progression.ExperienceRequiredForLevel(1);
            Assert.That(required, Is.GreaterThan(0));
            var character = _profile.FindCharacter("s3");
            var cases = new[] { (level: 1, experience: 0, expected: 0f), (level: 1, experience: (int)(required / 2), expected: .5f),
                (level: 1, experience: (int)required, expected: 1f), (level: progression.MaxLevel, experience: 0, expected: 1f) };
            foreach (var state in cases)
            {
                character.lordLevel = state.level; character.lordExperience = state.experience;
                FrontendController.ResumeAt = FrontendStep.Lobby; _flow.Begin();
                yield return new WaitForSecondsRealtime(.4f);
                var bar = Lobby.Root.GetComponentsInChildren<Image>().Single(x => x.name == "Experience");
                var viewport = (RectTransform)bar.transform.parent;
                Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null, "EXP stays clipped inside its frame");
                Assert.That(bar.rectTransform.sizeDelta.x / viewport.rect.width, Is.EqualTo(state.expected).Within(.001f));
                AssertInsideSafe(viewport, bar.rectTransform);
                Assert.That(bar.sprite, Is.Null, "EXP has square ends");
                Assert.That(bar.GetComponent<LobbyHorizontalGradient>(), Is.Not.Null);
                yield return Capture("Experience_" + state.level + "_" + state.experience);
                foreach (var width in new[] { 2160, 2340, 2400 })
                    yield return Capture("Experience_" + state.level + "_" + state.experience + "_" + width, width);
            }
        }

        [UnityTest]
        public IEnumerator VipFramesContainPlayerAndCornerIconsAreSharedSolidSources()
        {
            Assert.That(Lobby.Root.GetComponentsInChildren<Button>().Any(x => x.name == "Friends"), Is.False);
            foreach (var name in new[] { "Mail", "Settings" })
            {
                var button = Lobby.Root.GetComponentsInChildren<Button>().Single(x => x.name == name);
                var icon = button.transform.Find("Icon").GetComponent<Image>();
                Assert.That(icon.GetComponent<UiGradient>(), Is.Not.Null);
                Assert.That(icon.GetComponent<Outline>(), Is.Not.Null);
                Assert.That(icon.GetComponent<Shadow>(), Is.Not.Null);
            }
            _profile.FindCharacter("s3").lordExperience = (int)_flow.Config.Progression.ExperienceRequiredForLevel(1);
            foreach (var tier in _flow.Config.Lobby.vipFrames.Select(frame => frame.tier))
            {
                FrontendServiceLocator.Override(lobby: new VipSource(tier));
                FrontendController.ResumeAt = FrontendStep.Lobby; _flow.Begin();
                yield return new WaitForSecondsRealtime(.4f);
                var player = Lobby.Root.GetComponentsInChildren<RectTransform>().Single(x => x.name == "Player");
                var background = player.Find("VipBackground").GetComponent<Image>();
                Assert.That(background.sprite, Is.SameAs(_flow.Config.Lobby.vipFrames.Single(x => x.tier == tier).sprite));
                Assert.That(background.rectTransform.pivot.y, Is.EqualTo(1 - _flow.Config.Lobby.vipFrames.Single(x => x.tier == tier).bodyAnchorY).Within(.001f));
                var nameText = player.GetComponentsInChildren<TMP_Text>().Single(text => text.name == "PlayerName");
                nameText.ForceMeshUpdate();
                Assert.That(nameText.isTextOverflowing, Is.False, "player name fits its protected slot for VIP " + tier);
                var centre = player.InverseTransformPoint(background.transform.position).y;
                var halfBand = background.rectTransform.rect.height * _flow.Config.Lobby.vipFrames.Single(frame => frame.tier == tier).bodyHeightY * .5f;
                foreach (var glyph in nameText.textInfo.characterInfo.Where(glyph => glyph.isVisible))
                {
                    var top = player.InverseTransformPoint(nameText.transform.TransformPoint(glyph.topLeft)).y;
                    var bottom = player.InverseTransformPoint(nameText.transform.TransformPoint(glyph.bottomRight)).y;
                    Assert.That(top, Is.LessThanOrEqualTo(centre + halfBand + 1), "name stays below VIP crest " + tier);
                    Assert.That(bottom, Is.GreaterThanOrEqualTo(centre - halfBand - 1), "name stays above lower VIP frame " + tier);
                }
                foreach (var name in new[] { "AvatarBorder", "PlayerName", "Level", "ExperienceBorder", "PowerRibbon", "VipLabel" })
                {
                    var element = player.GetComponentsInChildren<RectTransform>().Single(x => x.name == name);
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(player, element);
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-.01f), name);
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(player.rect.width + .01f), name);
                }
                if (tier == _flow.Config.Lobby.vipFrames.Min(frame => frame.tier) || tier == _flow.Config.Lobby.vipFrames.Max(frame => frame.tier))
                {
                    yield return Capture("Vip_" + tier);
                    foreach (var width in new[] { 2160, 2340, 2400 }) yield return Capture("Vip_" + tier + "_" + width, width);
                }
            }
            Lobby.PreviewHour = 11; Lobby.Tick(_flow.Config.Lobby.daylightFadeSeconds + 1); yield return Capture("CornerIcons_Noon");
            Lobby.PreviewHour = 21; Lobby.Tick(_flow.Config.Lobby.daylightFadeSeconds + 1); yield return Capture("CornerIcons_Night");
        }

        private sealed class VipSource : ILobbyProfileSource
        {
            private readonly int _tier;
            public VipSource(int tier) => _tier = tier;
            public LobbyProfile Read(CharacterRecord character, LobbyConfig config, ProgressionConfig progression = null)
            {
                var profile = new LocalLobbyProfileSource().Read(character, config, progression); profile.vip = _tier; return profile;
            }
        }

        [Test]
        public void WalletJsonPreservesLargeBalancesAndLegacyMissingFieldsAreEmpty()
        {
            var character = new CharacterRecord { vip = 12, combatPower = long.MaxValue };
            character.wallet.Add(new ResourceBalance { id = "Silver", amount = long.MaxValue });
            var restored = JsonUtility.FromJson<CharacterRecord>(JsonUtility.ToJson(character));
            var source = new LocalLobbyProfileSource();
            Assert.That(source.Read(restored, _flow.Config.Lobby).Balance("Silver").amount, Is.EqualTo(long.MaxValue));
            Assert.That(source.Read(restored, _flow.Config.Lobby).power, Is.EqualTo(long.MaxValue));
            var legacy = JsonUtility.FromJson<CharacterRecord>("{\"lordLevel\":1,\"lordStars\":3}");
            var profile = source.Read(legacy, _flow.Config.Lobby);
            Assert.That(profile.Balance("Silver").amount, Is.Zero);
            Assert.That(profile.stars, Is.EqualTo(3));
            Assert.That(profile.vip, Is.Zero);
            Assert.That(source.Read(restored, null).Balance("Silver").amount, Is.EqualTo(long.MaxValue), "presentation config cannot change a balance");
        }

        private IEnumerator Capture(string name, int width = 1920)
        {
            Directory.CreateDirectory(Output);
            var camera = Camera.main; var canvas = _flow.GetComponentInChildren<Canvas>();
            var target = new RenderTexture(width, 1080, 24); var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var safe = Lobby.Root.Find("SafeArea").GetComponent<RectTransform>();
            var fitter = safe.GetComponent<SafeAreaFitter>(); fitter.enabled = false;
            safe.anchorMin = new Vector2(width == 1920 ? 0 : 60f / width, 24f / 1080);
            safe.anchorMax = new Vector2(width == 1920 ? 1 : 1 - 60f / width, 1);
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            foreach (var frame in _flow.GetComponentsInChildren<DesignFrame>()) frame.SendMessage("LateUpdate");
            foreach (var frame in _flow.GetComponentsInChildren<LobbyEdgeGroup>()) frame.SendMessage("LateUpdate");
            Lobby.Tick(0);
            if (Lobby.ResourceTooltipVisible)
            {
                var tooltip = Lobby.Root.GetComponentsInChildren<RectTransform>().Single(x => x.name == "ResourceTooltip");
                AssertInsideSafe(safe, tooltip);
            }
            foreach (var item in Lobby.Root.GetComponentsInChildren<RectTransform>().Where(x => new[] { "PlayerName", "Level", "Power", "VipLabel", "ExperienceBorder" }.Contains(x.name)))
                AssertInsideSafe(safe, item);
            Lobby.LordView.CaptureCamera.Render(); camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(width, 1080, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, 1080), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG()); Object.Destroy(image);
            RenderTexture.active = previous; camera.targetTexture = oldTarget; canvas.renderMode = oldMode; target.Release(); Object.Destroy(target);
            fitter.enabled = true; fitter.Apply();
        }

        private static void AssertInsideSafe(RectTransform safe, RectTransform child)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(safe, child);
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(safe.rect.xMin - .1f), child.name);
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(safe.rect.xMax + .1f), child.name);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(safe.rect.yMin - .1f), child.name);
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(safe.rect.yMax + .1f), child.name);
        }
    }
}
#endif
