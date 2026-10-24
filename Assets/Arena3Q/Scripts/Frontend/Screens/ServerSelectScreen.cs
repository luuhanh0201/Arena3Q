using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Arena3Q.Frontend
{
    /// <summary>Start screen and its server/account dialogs; all cards use saved character data.</summary>
    public sealed class ServerSelectScreen : StartupScreen
    {
        public enum Tab { Recommended, Mine, All, FirstRange, SecondRange }
        private RectTransform _start, _overlay, _list, _panel;
        private TMP_Text _selectedLabel, _count, _tabTitle;
        private Button _confirm;
        private readonly Dictionary<Tab, Button> _tabs = new Dictionary<Tab, Button>();
        private readonly Dictionary<string, Button> _cards = new Dictionary<string, Button>();
        private ServerInfo _confirmed;
        private AccountForm _linkForm;
        public ServerInfo Selected { get; private set; }
        public Tab CurrentTab { get; private set; } = Tab.FirstRange;
        public bool PickerOpen { get; private set; }
        public AccountForm LinkForm => _linkForm;

        protected override void BuildScreen()
        {
            // Full-screen dialogs cover the start chrome, including the player chip near Back.
            Edge.SetAsFirstSibling();
            Backdrop();
            Selected = StaticServerDirectory.PickDefault(Flow.Servers, Flow.Profile.lastServerId);
            _confirmed = Selected;
            Flow.ServersChanged += RefreshServers;
            BuildStart();
        }
        private void BuildStart()
        {
            if (_start != null) { _start.gameObject.SetActive(false); Object.Destroy(_start.gameObject); }
            _start = UiFactory.Stretch("StartScreen", Design);
            Kit.BottomShade(_start, 280);
            Kit.Logo(_start, 390, 16, 500); Kit.Footer(_start);
            var previousChip = Edge.Find("PlayerChip");
            if (previousChip != null) { previousChip.gameObject.SetActive(false); Object.Destroy(previousChip.gameObject); }
            var chip = Kit.Image(Edge, "PlayerChip", Art.panel, new Rect(16, 14, 288, 64), sliced: true);
            chip.pixelsPerUnitMultiplier = 2;
            Kit.CircleBadge(chip.transform, "AvatarBorder", Art.user, new Rect(12, 12, 40, 40), StartupUiConfig.Hex("13284A"), Art.goldLine, Art.gold, 22);
            var guest = Flow.Profile.accountKind == AccountKind.Guest;
            var suffix = Flow.Profile.accountId.Length > 6 ? Flow.Profile.accountId.Substring(Flow.Profile.accountId.Length - 5) : "";
            Kit.Text(chip.transform, "AccountName", guest ? Kit.C("guest") + " #" + suffix : Flow.Profile.accountLabel,
                new Rect(62, 7, 208, 26), 14, Art.text, weight: 700);
            var tag = Kit.Image(chip.transform, "AccountTag", Art.rounded, new Rect(62, 34, 208, 20), StartupUiConfig.Hex("F0A63A"), true);
            tag.pixelsPerUnitMultiplier = 3.5f;
            var link = Kit.Button(chip.transform, "BindAccount", Kit.C(guest ? "unlinked" : "linked"), new Rect(62, 34, 208, 20),
                guest ? OpenBind : (Action)(() => Flow.ShowMessage(Kit.C("linked"))), GameButtonStyle.Plain, fontSize: 11.5f);
            link.GetComponentInChildren<TMP_Text>().color = StartupUiConfig.Hex("1D0F05");
            CornerButtons(settings: OpenSettings);
            var serverBox = Kit.Image(_start, "ServerPill", Art.panel, new Rect(355, 524, 570, 62), sliced: true);
            serverBox.pixelsPerUnitMultiplier = 2;
            if (Selected != null)
            {
                Kit.ServerState(serverBox.transform, Selected, 22, 20);
                Kit.Image(serverBox.transform, "Divider", null, new Rect(102, 20, 1, 24), Art.goldLine);
                Kit.Text(serverBox.transform, "ServerName", Selected.Name, new Rect(120, 0, 255, 62), 17, weight: 700);
            }
            else Kit.Text(serverBox.transform, "NoServer", Kit.C("noOpenServers"), new Rect(22, 0, 350, 62), 15, Art.dim);
            Kit.Button(serverBox.transform, "ChangeServer", Kit.C("changeServer"), new Rect(390, 11, 150, 40), OpenPicker, fontSize: 13);
            var enter = Kit.Button(_start, "Enter", Kit.C("enterGame"), new Rect(460, 600, 360, 76), Enter, GameButtonStyle.Gold, UiSound.Enter);
            enter.interactable = Selected != null && Selected.CanEnter;
        }
        public void OpenPicker()
        {
            CloseOverlay(); PickerOpen = true; _confirmed = Selected;
            _overlay = Kit.ModalBackdrop(Design, "ServerOverlay", .45f, true);
            Kit.DismissOutside(_overlay, CancelPicker);
            Kit.IconButton(_overlay, "Back", Art.backIcon, new Rect(14, 14, 52, 52), CancelPicker, UiSound.Back);
            _panel = Kit.Panel(_overlay, "ServerSelectPopup", Art.serverPanel, big: true, tassels: false, animate: true);
            Kit.Text(_panel, "Title", Kit.C("chooseServer"), new Rect(40, 30, 1048, 36), 22, Art.gold, TextAlignmentOptions.Center, 800).characterSpacing = 3;
            var tabs = new[] { (Tab.Mine, "myServers"), (Tab.Recommended, "recommended"), (Tab.FirstRange, "range1"), (Tab.SecondRange, "range2") };
            _tabs.Clear();
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i].Item1;
                _tabs[tab] = Kit.Button(_panel, "Tab" + tab, Kit.C(tabs[i].Item2), new Rect(40, 84 + i * 60, 200, 50), () => ShowTab(tab), sound: UiSound.Tab);
            }
            var recent = FindRecent();
            var recentFrame = Kit.Image(_panel, "RecentServer", Art.rounded, new Rect(258, 84, 830, 80), new Color(.16f, .21f, .3f, .95f), true);
            Kit.Image(recentFrame.transform, "Avatar", Art.user, new Rect(14, 12, 56, 56), Art.gold);
            Kit.Text(recentFrame.transform, "RecentHeading", Kit.C("recentServer"), new Rect(84, 8, 560, 24), 12, Art.gold);
            var character = recent != null ? Flow.Profile.FindCharacter(recent.Id) : null;
            Kit.Text(recentFrame.transform, "RecentName", recent == null ? Kit.C("noRecentServer") : recent.Name + (character != null ? " · Lv." + character.lordLevel : ""),
                new Rect(84, 34, 580, 34), 18, weight: 700);
            var recentButton = Kit.Button(recentFrame.transform, "PickRecent", Kit.C("select"), new Rect(706, 18, 110, 44), () => Select(recent));
            recentButton.interactable = recent != null && recent.CanEnter;
            _tabTitle = Kit.Text(_panel, "TabTitle", "", new Rect(258, 176, 560, 28), 15, Art.gold);
            _count = Kit.Text(_panel, "Count", "", new Rect(900, 176, 188, 28), 12, Art.muted, TextAlignmentOptions.MidlineRight);
            var viewport = Kit.Rect(_panel, "ServerViewport", new Rect(258, 212, 830, 326));
            var hit = UiFactory.AddImage(viewport, new Color(0, 0, 0, .01f)); hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _list = Kit.Rect(viewport, "ServerGrid", new Rect(0, 0, 830, 326));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = _list; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            Kit.Image(_panel, "FooterSeparator", null, new Rect(40, 558, 1048, 1), new Color(Art.goldLine.r, Art.goldLine.g, Art.goldLine.b, .35f));
            var legend = Kit.Rect(_panel, "Legend", new Rect(40, 578, 340, 50));
            var states = new[] { ServerStatus.Good, ServerStatus.Busy, ServerStatus.Full, ServerStatus.Maintenance };
            for (var i = 0; i < states.Length; i++)
            {
                Kit.Image(legend, "Dot" + states[i], Art.circle, new Rect(i * 80, 20, 10, 10), Kit.StatusColor(states[i]));
                Kit.Text(legend, "Label" + states[i], Kit.StatusText(states[i]), new Rect(i * 80 + 16, 0, 62, 50), 13, Art.dim);
            }
            _selectedLabel = Kit.Text(_panel, "Selected", "", new Rect(472, 578, 386, 50), 14, Art.gold, TextAlignmentOptions.MidlineRight);
            _confirm = Kit.Button(_panel, "ConfirmServer", Kit.C("confirm"), new Rect(878, 578, 210, 50), ConfirmServer, GameButtonStyle.Gold, UiSound.Confirm);
            ShowTab(CurrentTab);
        }
        public void ShowTab(Tab tab)
        {
            CurrentTab = tab;
            if (!PickerOpen || _list == null) return;
            foreach (Transform child in _list) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
            _cards.Clear();
            foreach (var pair in _tabs)
            {
                var image = (Image)pair.Value.targetGraphic;
                image.sprite = pair.Key == tab ? Art.redButton : Art.blueButton;
                image.pixelsPerUnitMultiplier = image.sprite.rect.height / 50;
                pair.Value.GetComponent<GameButton>().SetSelected(pair.Key == tab);
            }
            var servers = Visible(tab);
            _list.sizeDelta = new Vector2(830, Mathf.Max(326, Mathf.Ceil(servers.Count / 2f) * 64));
            _list.anchoredPosition = Vector2.zero;
            for (var i = 0; i < servers.Count; i++) BuildCard(servers[i], i);
            if (servers.Count == 0) Kit.Text(_list, "Empty", Kit.C(tab == Tab.Mine ? "noCharacters" : "noServers"), new Rect(20, 80, 790, 70), 18, Art.dim, TextAlignmentOptions.Center);
            _tabTitle.text = Kit.C(tab == Tab.Mine ? "myServers" : tab == Tab.Recommended ? "recommended" : tab == Tab.SecondRange ? "range2" : tab == Tab.FirstRange ? "range1" : "allServers");
            _count.text = string.Format(Kit.C("serverCount"), servers.Count);
            UpdateSelection();
        }
        private void BuildCard(ServerInfo server, int index)
        {
            var bounds = new Rect(2 + index % 2 * 419, 2 + index / 2 * 64, 409, 56);
            var image = Kit.Image(_list, "Server" + server.Id, Art.rounded, bounds, new Color(.11f, .2f, .32f, 1), true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var tint = button.colors; tint.highlightedColor = new Color(1.2f, 1.2f, 1.2f); tint.pressedColor = Color.gray; button.colors = tint;
            // Unavailable rows still receive a tap so they can explain the status and play the error cue.
            button.onClick.AddListener(() => { if (server.CanEnter) Kit.Audio.Play(UiSound.Tap); Select(server); });
            if (!server.CanEnter) image.gameObject.AddComponent<CanvasGroup>().alpha = .55f;
            Kit.ServerState(image.transform, server, 12, 17);
            var character = Flow.Profile.FindCharacter(server.Id);
            Kit.Text(image.transform, "Name", server.Name, character != null && character.HasName ? new Rect(90, 4, 230, 28) : new Rect(90, 0, 230, 56), 15, weight: 700);
            if (character != null && character.HasLord)
            {
                Kit.Image(image.transform, "Avatar", Art.user, new Rect(322, 10, 30, 30), Art.gold);
                Kit.Text(image.transform, "Level", "Lv." + character.lordLevel, new Rect(354, 10, 52, 30), 11, Art.gold);
            }
            else if (server.IsNew || server.IsHot)
            {
                var color = server.IsNew ? Art.good : Art.bad;
                Kit.Image(image.transform, "Badge", Art.rounded, new Rect(340, 18, 54, 20), color, true);
                Kit.Text(image.transform, "BadgeLabel", Kit.C(server.IsNew ? "newBadge" : "hotBadge"), new Rect(340, 18, 54, 20), 10,
                    server.IsNew ? Art.ink : Art.text, TextAlignmentOptions.Center, 800);
            }
            if (character != null && character.HasName)
                Kit.Text(image.transform, "Character", character.name, new Rect(90, 30, 230, 22), 11, Art.dim);
            _cards[server.Id] = button;
        }
        private void UpdateSelection()
        {
            if (!PickerOpen) return;
            _selectedLabel.text = Selected != null ? Kit.C("selectedServer") + " " + Selected.Name : Kit.C("noSelection");
            _confirm.interactable = Selected != null && Selected.CanEnter;
            foreach (var pair in _cards)
            {
                var image = (Image)pair.Value.targetGraphic;
                var outline = image.GetComponent<Outline>();
                if (outline == null) { outline = image.gameObject.AddComponent<Outline>(); outline.effectDistance = new Vector2(2, -2); outline.effectColor = Art.goldLine; }
                outline.enabled = Selected != null && Selected.Id == pair.Key;
            }
        }
        public void Select(ServerInfo server)
        {
            if (server == null) return;
            if (!server.CanEnter)
            {
                Kit.Audio.Play(UiSound.Error);
                Flow.ShowMessage(server.Name + ": " + Kit.StatusText(server.Status) + ". " + Kit.C("chooseAnotherServer")); return;
            }
            Selected = server; UpdateSelection();
        }
        public void ConfirmServer()
        {
            if (Selected == null || !Selected.CanEnter) return;
            Flow.SelectServer(Selected); _confirmed = Selected; CloseOverlay(); BuildStart();
        }
        public void CancelPicker() { Selected = _confirmed; CloseOverlay(); BuildStart(); }
        public void Enter()
        {
            if (Selected == null || !Selected.CanEnter) { Flow.ShowMessage(Kit.C("noOpenServers")); return; }
            Flow.SelectServer(Selected); Flow.Complete();
        }
        private List<ServerInfo> Visible(Tab tab)
        {
            var list = new List<ServerInfo>();
            for (var i = 0; i < Flow.Servers.Count; i++)
            {
                var server = Flow.Servers[i];
                if (server == null) continue;
                var number = int.TryParse(server.Id.TrimStart('s', 'S'), out var n) ? n : i + 1;
                if (tab == Tab.All || tab == Tab.Recommended && server.Recommended || tab == Tab.Mine && Flow.Profile.FindCharacter(server.Id) != null
                    || tab == Tab.FirstRange && number >= 1 && number <= 10 || tab == Tab.SecondRange && number >= 11 && number <= 20) list.Add(server);
            }
            return list;
        }
        private ServerInfo FindRecent()
        {
            foreach (var server in Flow.Servers) if (server != null && server.Id == Flow.Profile.lastServerId) return server;
            return null;
        }
        private void RefreshServers()
        {
            var selectedId = Selected?.Id;
            Selected = StaticServerDirectory.PickDefault(Flow.Servers, selectedId ?? Flow.Profile.lastServerId);
            if (PickerOpen) { _confirmed = StaticServerDirectory.PickDefault(Flow.Servers, _confirmed?.Id); ShowTab(CurrentTab); }
            else { _confirmed = Selected; BuildStart(); }
        }
        private void CloseOverlay()
        {
            if (_overlay != null && _overlay.name == "SettingsOverlay") PlayerPrefs.Save();
            Kit.Close(_overlay);
            _overlay = null; _linkForm = null; PickerOpen = false;
        }
        public void OpenBind()
        {
            CloseOverlay(); _overlay = Kit.ModalBackdrop(Design, "BindOverlay", .66f);
            Kit.DismissOutside(_overlay, CloseOverlay);
            Kit.IconButton(_overlay, "Back", Art.backIcon, new Rect(14, 14, 52, 52), CloseOverlay, UiSound.Back);
            var panel = Kit.Panel(_overlay, "BindAccountPopup", Art.bindPanel, animate: true);
            Kit.Text(panel, "Title", Kit.C("linkTitle"), new Rect(48, 36, 424, 36), 22, Art.gold, TextAlignmentOptions.Center, 800);
            Kit.Text(panel, "Explanation", Kit.C("linkExplanation"), new Rect(48, 76, 424, 64), 14, Art.dim, TextAlignmentOptions.Center);
            var reward = Kit.Image(panel, "RewardStrip", Art.rounded, new Rect(48, 148, 424, 56), new Color(.18f, .2f, .23f, 1), true);
            Kit.Text(reward.transform, "RewardCaption", Kit.C("linkReward"), new Rect(12, 0, 264, 56), 12, Art.gold);
            for (var i = 0; i < 3; i++) Kit.Image(reward.transform, "RewardPlaceholder" + i, Art.gift, new Rect(286 + i * 42, 12, 30, 30), Art.muted);
            var providers = new[] { AccountKind.Google, AccountKind.Apple, AccountKind.Facebook };
            for (var i = 0; i < providers.Length; i++)
            {
                var provider = providers[i];
                Kit.Button(panel, "Link" + provider, Kit.C("linkProvider") + " " + provider, new Rect(48, 214 + i * 60, 424, 50), () => LinkProvider(provider), sound: UiSound.Confirm);
            }
            Kit.Button(panel, "LinkEmail", Kit.C("emailOrPhone"), new Rect(48, 394, 424, 50), OpenLinkForm, GameButtonStyle.Red);
            Kit.Button(panel, "Later", Kit.C("later"), new Rect(170, 454, 180, 32), CloseOverlay, GameButtonStyle.Plain, UiSound.Back);
        }
        private void LinkProvider(AccountKind provider)
        {
            var overlay = _overlay;
            FrontendServiceLocator.AccountActions.Link(Flow.Profile.accountId, provider, null, null, null, result =>
            {
                if (Root == null || overlay == null || !overlay.gameObject.activeInHierarchy) return;
                var group = overlay.GetComponent<CanvasGroup>();
                if (group != null && !group.blocksRaycasts) return;
                if (result == null || !result.Success) { Kit.Audio.Play(UiSound.Error); Flow.ShowMessage(result?.Message ?? Kit.C("loginFailed")); return; }
                if (result.AccountId != Flow.Profile.accountId) { Flow.ShowMessage(Kit.C("linkIdentityMismatch")); return; }
                Flow.SignIn(result); CloseOverlay(); BuildStart();
            });
        }
        public void OpenLinkForm()
        {
            CloseOverlay(); _overlay = Kit.ModalBackdrop(Design, "LinkFormOverlay", .66f);
            Kit.DismissOutside(_overlay, CloseOverlay);
            var bounds = Art.registerPanel;
            var panel = Kit.Panel(_overlay, "LinkFormPanel", bounds, animate: true);
            _linkForm = new AccountForm(Flow, panel, true, true, register => { if (register) OpenLinkForm(); else OpenBind(); },
                OpenBind, () => { CloseOverlay(); BuildStart(); });
            Kit.IconButton(_overlay, "Back", Art.backIcon, new Rect(14, 14, 52, 52), OpenBind, UiSound.Back);
        }
        public void OpenSettings()
        {
            CloseOverlay(); _overlay = Kit.Dim(Design, "SettingsOverlay", .6f);
            Kit.DismissOutside(_overlay, () => { CloseOverlay(); BuildStart(); });
            var panel = Kit.Panel(_overlay, "SettingsPanel", new Rect(400, 140, 480, 440), animate: true);
            Kit.Text(panel, "Title", Kit.C("settings"), new Rect(40, 32, 400, 40), 22, Art.gold, TextAlignmentOptions.Center, 800);
            var volumeLabel = Kit.Text(panel, "MusicVolumeLabel", string.Format(Kit.C("musicVolume"), Mathf.RoundToInt(Flow.Music.Volume * 100)), new Rect(40, 82, 400, 30), 16, Art.gold);
            Kit.Slider(panel, "MusicVolume", new Rect(40, 112, 400, 40), Flow.Music.Volume, volume =>
            {
                Flow.Music.SetVolume(volume); volumeLabel.text = string.Format(Kit.C("musicVolume"), Mathf.RoundToInt(volume * 100));
            });
            Kit.Toggle(panel, "MuteMusic", Kit.C("muteMusic"), new Rect(40, 162, 400, 36), Flow.Music.Muted)
                .onValueChanged.AddListener(Flow.Music.SetMuted);
            Kit.Toggle(panel, "MuteUi", Kit.C("muteUi"), new Rect(40, 204, 400, 36), Kit.Audio.Muted)
                .onValueChanged.AddListener(Kit.Audio.SetMuted);
            Kit.Button(panel, "Account", Kit.C("account"), new Rect(40, 252, 400, 50), OpenBind);
            Kit.Button(panel, "SwitchAccount", Kit.C("switchAccount"), new Rect(40, 312, 400, 50), ConfirmSignOut);
            Kit.Button(panel, "Close", Kit.C("close"), new Rect(130, 376, 220, 44), () => { CloseOverlay(); BuildStart(); PlayerPrefs.Save(); }, GameButtonStyle.Plain, UiSound.Back);
        }
        private void ConfirmSignOut()
        {
            CloseOverlay(); _overlay = Kit.Dim(Design, "SignOutOverlay", .65f);
            Kit.DismissOutside(_overlay, CloseOverlay);
            var panel = Kit.Panel(_overlay, "ConfirmSignOut", new Rect(360, 220, 560, 260), animate: true);
            Kit.Text(panel, "Warning", Kit.C(Flow.Profile.accountKind == AccountKind.Guest ? "guestSignOutWarning" : "signOutWarning"),
                new Rect(50, 44, 460, 110), 16, Art.text, TextAlignmentOptions.Center);
            Kit.Button(panel, "Cancel", Kit.C("cancel"), new Rect(50, 172, 220, 50), CloseOverlay, sound: UiSound.Back);
            Kit.Button(panel, "Confirm", Kit.C("confirm"), new Rect(290, 172, 220, 50), () => { Flow.SignOut(); Flow.ReturnToLogin(); }, GameButtonStyle.Gold, UiSound.Confirm);
        }
        public override void Tick(float deltaTime) => _linkForm?.Tick();
        protected override void OnHide() { Flow.ServersChanged -= RefreshServers; }
    }
}
