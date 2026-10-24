using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// Server choice (docs/KeHoach/VaoGame.md §8): the recommended (or last) server is preselected, so a new player only
    /// taps "Vào game". Servers under maintenance cannot be entered; "Của tôi" lists servers with a character.
    /// </summary>
    public sealed class ServerSelectScreen : FrontendScreen
    {
        public enum Tab
        {
            Recommended,
            Mine,
            All
        }

        private RectTransform _list;
        private Text _selectedLabel;

        public ServerInfo Selected { get; private set; }
        public Tab CurrentTab { get; private set; } = Tab.All;

        protected override void Build()
        {
            AddBackdrop(0.55f, showTitle: false);
            Ui.Label(Root, "Title", "Chọn máy chủ", 48, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(330f, -70f),
                new Vector2(600f, 80f), Ui.Theme.accent);

            var tabs = new[] { (Tab.Recommended, "Đề xuất"), (Tab.Mine, "Của tôi"), (Tab.All, "Tất cả") };
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i].Item1;
                Ui.Button(Root, $"Tab {tab}", tabs[i].Item2, new Vector2(0f, 0.5f), new Vector2(200f, 220f - i * 110f), new Vector2(300f, 90f))
                    .onClick.AddListener(() => ShowTab(tab));
            }

            _list = Ui.Panel(Root, "List", FrontendUi.Center, new Vector2(150f, 30f), new Vector2(1150f, 640f));
            _selectedLabel = Ui.Label(Root, "Selected", "", 32, TextAnchor.MiddleLeft, new Vector2(0.5f, 0f), new Vector2(-250f, 90f),
                new Vector2(800f, 60f));
            Ui.Button(Root, "Enter", "VÀO GAME", new Vector2(0.5f, 0f), new Vector2(450f, 90f), new Vector2(380f, 100f), primary: true)
                .onClick.AddListener(Enter);

            Selected = StaticServerDirectory.PickDefault(Flow.Servers, Flow.Profile.lastServerId);
            ShowTab(Tab.All);
        }

        public void ShowTab(Tab tab)
        {
            CurrentTab = tab;
            foreach (Transform child in _list)
                Object.Destroy(child.gameObject);
            var servers = Visible(tab);
            for (var i = 0; i < servers.Count; i++)
            {
                var server = servers[i];
                var button = Ui.Button(_list, $"Server {server.Id}", Describe(server), new Vector2(0.5f, 1f),
                    new Vector2(0f, -60f - i * 100f), new Vector2(1080f, 86f), primary: server == Selected);
                button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                button.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(80f, 0f);
                if (server.Recommended)
                {
                    var icons = UiIconSet.Load();
                    UiFactory.AddIcon("Recommended", button.transform, icons != null ? icons.Star : null, new Vector2(0f, 0.5f),
                        new Vector2(30f, 0f), 34f, new Color(1f, 0.85f, 0.3f));
                }
                button.interactable = server.CanEnter;
                button.onClick.AddListener(() => Select(server));
            }
            if (servers.Count == 0)
                Ui.Label(_list, "Empty", tab == Tab.Mine ? "Bạn chưa có nhân vật ở máy chủ nào." : "Không có máy chủ.", 30,
                    TextAnchor.MiddleCenter, FrontendUi.Center, Vector2.zero, new Vector2(1000f, 80f));
            _selectedLabel.text = Selected != null ? $"Đang chọn: {Selected.Name}" : "Chưa chọn máy chủ";
        }

        public void Select(ServerInfo server)
        {
            if (server == null)
                return;
            if (!server.CanEnter)
            {
                Flow.ShowMessage($"{server.Name} đang bảo trì.");
                return;
            }
            Selected = server;
            ShowTab(CurrentTab);
        }

        public void Enter()
        {
            if (Selected == null || !Selected.CanEnter)
            {
                Flow.ShowMessage("Hãy chọn một máy chủ đang mở.");
                return;
            }
            Flow.SelectServer(Selected);
            Flow.Complete();
        }

        private List<ServerInfo> Visible(Tab tab)
        {
            var list = new List<ServerInfo>();
            foreach (var server in Flow.Servers)
            {
                var show = tab == Tab.All ||
                           (tab == Tab.Recommended && server.Recommended) ||
                           (tab == Tab.Mine && Flow.Profile.FindCharacter(server.Id) != null);
                if (show)
                    list.Add(server);
            }
            return list;
        }

        private string Describe(ServerInfo server)
        {
            var status = server.Status == ServerStatus.New ? "[Mới]" : server.Status == ServerStatus.Busy ? "[Đông]" : "[Bảo trì]";
            var text = $"{server.Name}   {status}";
            var character = Flow.Profile.FindCharacter(server.Id);
            if (character != null && character.HasLord)
                text += $"   · {character.name} Lv{character.lordLevel}";
            return text;
        }
    }
}
