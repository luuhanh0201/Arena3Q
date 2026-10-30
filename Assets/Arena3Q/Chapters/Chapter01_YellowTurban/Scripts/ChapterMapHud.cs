using System;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using Arena3Q.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Exploration
{
    public sealed class ChapterMapHud : MonoBehaviour
    {
        private sealed class Marker
        {
            public int stage, target;
            public RectTransform rect;
            public CanvasGroup group;
            public Image icon;
            public Image backing;
            public TMP_Text text;
            public RectTransform name;
            public RectTransform stars;
            public bool locked;
            public RectTransform hit;
            public float alpha;
        }
        private ChapterMapController _map;
        private ChapterMapConfig Config => _map.Config;
        private MapUiConfig Ui => Config.ui;
        private StartupUi _kit;
        private Canvas _canvas;
        private RectTransform _root, _safe, _modal;
        private TMP_Text _message, _quest;
        private Image _questArrow;
        private float _messageSeconds;
        private readonly List<Marker> _markers = new List<Marker>();
        private readonly List<RectTransform> _markerOccluders = new List<RectTransform>();
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<TMP_Text> _rewardLabels = new List<TMP_Text>();
        private readonly List<(string id, TMP_Text label)> _resourceLabels = new List<(string, TMP_Text)>();
        public bool ModalVisible => _modal != null;
        [Serializable] private sealed class UiReading
        {
            public int width, height, frameRate, previewFrameRate, unlocked, revealed;
            public float dpi;
            public Rect safeArea;
            public bool modal, revealing;
            public float[] fogVisibility;
            public string message;
            public ElementReading[] buttons, markers, labels, occluders, directions;
        }
        [Serializable] private sealed class ElementReading
        {
            public string name;
            public Rect rect;
            public int stage = -1, target = -1;
            public bool active;
            public string text;
            public bool overflow;
            public Rect visualRect;
            public float alpha;
        }
        // Read-only telemetry for browser acceptance: all interaction still goes through real pointer input.
        public string InspectUi()
        {
            Rect Bounds(RectTransform rect)
            {
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
                var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                return new Rect(min, max - min);
            }
            return JsonUtility.ToJson(new UiReading
            {
                width = Screen.width, height = Screen.height, frameRate = Application.targetFrameRate,
                previewFrameRate = DisplayMetrics.PreviewFrameRate, dpi = DisplayMetrics.Dpi,
                safeArea = DisplayMetrics.SafeArea, modal = ModalVisible, message = _map.LastMessage,
                unlocked = _map.Session.Rules.UnlockedCount(_map.Session.Progress),
                revealed = _map.Session.Progress.revealedStages, revealing = _map.World != null && _map.World.Revealing,
                fogVisibility = _map.World == null ? Array.Empty<float>() : Enumerable.Range(0, Config.stages.Length).Select(_map.World.Visibility).ToArray(),
                buttons = _safe.GetComponentsInChildren<Button>().Select(button => new ElementReading
                { name = button.name, rect = Bounds((RectTransform)button.transform), active = button.interactable }).ToArray(),
                markers = _markers.Select(marker => new ElementReading { name = marker.rect.name,
                    rect = Bounds(marker.hit), stage = marker.stage, target = marker.target,
                    visualRect = MarkerBounds(marker), alpha = marker.group.alpha, active = marker.group.blocksRaycasts }).ToArray(),
                occluders = _markerOccluders.Select(rect => new ElementReading { name = rect.name, rect = GraphicBounds(rect), active = rect.gameObject.activeInHierarchy }).ToArray(),
                directions = new[]{new ElementReading {name="QuestDirection",rect=ScreenBounds(_questArrow.rectTransform),active=_questArrow.enabled}},
                labels = _safe.GetComponentsInChildren<TMP_Text>().Select(label => new ElementReading
                { name = label.name, rect = Bounds(label.rectTransform), text = label.text, overflow = label.isTextOverflowing, active = label.enabled }).ToArray()
            });
        }
        public void Configure(ChapterMapController map)
        {
            _map = map; UiFactory.EnsureEventSystem();
            _canvas = UiFactory.CreateCanvas(transform, "ChapterMapCanvas", 10);
            _canvas.GetComponent<CanvasScaler>().referenceResolution = Ui.referenceSize;
            _root = (RectTransform)_canvas.transform; _safe = UiFactory.Stretch("SafeArea", _root);
            _safe.gameObject.AddComponent<SafeAreaFitter>().Apply();
            var audio = gameObject.AddComponent<UIAudio>(); audio.Configure(Config.frontend.StartupUi);
            _kit = new StartupUi(Config.frontend.StartupUi, audio); SceneTransition.Configure(Config.frontend.StartupUi.screenSeconds);
            BuildChrome(); BuildMarkers(); _safe.SetAsLastSibling(); Refresh();
            foreach (string name in Ui.markerOccluders ?? Array.Empty<string>())
                if (_safe.Find(name) is RectTransform rect) _markerOccluders.Add(rect);
        }
        private RectTransform Frame(Transform parent, string name, Rect rect, Color? fill = null)
        {
            var panel = _kit.Rect(parent, name, rect); var frame = panel.gameObject.AddComponent<LobbyFrameGraphic>();
            frame.color = Ui.gold; frame.fill = fill ?? new Color(.12f,.08f,.06f,.85f); frame.raycastTarget = false; return panel;
        }
        private void BuildChrome()
        {
            var back = _kit.Button(_safe, "Back", "", Ui.back, _map.ReturnToLobby, GameButtonStyle.Red);
            _kit.Image(back.transform, "Icon", Ui.backIcon, new Rect(24, 8, 34, 34), Ui.gold).preserveAspect = true;
            Frame(_safe, "Title", Ui.title);
            _kit.Text(_safe, "ChapterTitle", Config.title + "  |  " + Config.subtitle, Ui.title, Ui.titleFont, Ui.gold, TextAlignmentOptions.Center, Ui.textWeight);
            _kit.IconButton(_safe, "Information", Ui.infoIcon, Ui.info, () => ShowInfo()).image.color = Ui.gold;
            _kit.Text(_safe, "Introduction", Config.introduction, Ui.introduction, Ui.bodyFont, Color.white, weight: Ui.textWeight);
            var rewards = Frame(_safe, "ChapterRewards", Ui.rewards);
            var badge = _kit.Image(rewards, "RewardBadge", Ui.medallion, Ui.rewardBadge).rectTransform;
            _kit.Text(badge, "Heading", "Thưởng\nChương", Ui.rewardHeading, Ui.rewardHeadingFont, Ui.gold, TextAlignmentOptions.Center, Ui.textWeight);
            for (int i = 0; i < Config.rewards.Length; i++)
            {
                int index = i; var r = Config.rewards[i]; var rect = Ui.rewardButton; rect.x += i * Ui.rewardStep;
                var button = _kit.IconButton(rewards, "Reward_" + r.id, null, rect, () => Claim(index));
                button.image.color = Color.clear;
                _kit.Image(button.transform, "Chest", i < Ui.chests.Length ? Ui.chests[i] : null, Ui.rewardArt).preserveAspect = true;
                _kit.Image(button.transform, "Star", UiIconSet.Load().Star, Ui.rewardStar, Ui.gold).preserveAspect = true;
                _rewardLabels.Add(_kit.Text(button.transform, "Stars", "", Ui.rewardCount, Ui.bodyFont, Ui.gold, TextAlignmentOptions.Center));
            }
            var right = UiFactory.CreateRect("Resources", _safe, Vector2.one, Vector2.one, Vector2.one, Vector2.zero, Vector2.zero);
            var lobby = Config.frontend.Lobby; var profile = FrontendServiceLocator.Lobby.Read(_map.Session.Character, lobby, Config.frontend.Progression);
            var resources=Ui.resourceIds.Select(id=>lobby.resources.FirstOrDefault(r=>r.id==id)).Where(r=>r!=null).ToArray();
            int count = resources.Length;
            for (int i = 0; i < count; i++)
            {
                var entry = resources[i]; var bounds = Ui.resource; bounds.position += Ui.resourceStep * i;
                var bar = _kit.Image(right, "Resource_" + entry.id, lobby.Sprite("Resource_Bar"), bounds, sliced:true).rectTransform;
                _kit.Image(bar, "Icon", lobby.Sprite(entry.icon), new Rect(-8, -4, 56, 56)).preserveAspect = true;
                var balance = profile.Balance(entry.id);
                var amount = _kit.Text(bar, "Amount", LobbyResourceRules.Compact(balance.amount, lobby.resourceCompactThreshold, lobby.resourceCompactDecimals, lobby.resourceCompactUnit), new Rect(50, 4, 132, 42), 22, Ui.gold, TextAlignmentOptions.Center, Ui.textWeight);
                _resourceLabels.Add((entry.id, amount));
                var plus = _kit.Button(bar, "Add", "", new Rect(190, 5, 44, 42), () => ShowMessage(Ui.unavailable), GameButtonStyle.Gold);
                _kit.Image(plus.transform, "Plus", lobby.plus, new Rect(12, 11, 20, 20), Ui.ink);
            }
            var quest = Frame(_safe, "CurrentQuest", Ui.quest);
            var questText = Ui.questText;
            float arrowRadius = Ui.questArrow.size.magnitude * .5f;
            questText.width = Mathf.Min(questText.width, Ui.questArrow.center.x - arrowRadius - Ui.questArrowGap - questText.x);
            _quest = _kit.Text(quest, "Text", "", questText, Ui.bodyFont, Ui.gold, weight: Ui.textWeight);
            _quest.enableAutoSizing = true; _quest.fontSizeMin = Ui.questMinimumFont; _quest.fontSizeMax = Ui.bodyFont;
            _quest.overflowMode = TextOverflowModes.Ellipsis;
            _questArrow = _kit.Image(quest, "Direction", Ui.backIcon, Ui.questArrow, Ui.gold);
            _questArrow.rectTransform.pivot = Vector2.one * .5f;
            _questArrow.rectTransform.anchoredPosition = new Vector2(Ui.questArrow.center.x,-Ui.questArrow.center.y);
            _questArrow.preserveAspect = true;
            var questHit = quest.GetComponent<LobbyFrameGraphic>(); questHit.raycastTarget = true;
            var questButton = quest.gameObject.AddComponent<Button>(); questButton.targetGraphic = questHit;
            questButton.onClick.AddListener(() => _map.SelectStage(Mathf.Max(0, _map.Session.Rules.UnlockedCount(_map.Session.Progress)-1)));
            _message = _kit.Text(_safe, "Message", "", new Rect(400, Ui.referenceSize.y-100, 872, 55), 23, Ui.gold, TextAlignmentOptions.Center);
            _message.rectTransform.anchorMin = _message.rectTransform.anchorMax = new Vector2(.5f,0);
            _message.rectTransform.pivot = new Vector2(.5f,0); _message.rectTransform.anchoredPosition = new Vector2(0, 34);
        }
        private void BuildMarkers()
        {
            for (int stage = 0; stage < Config.stages.Length; stage++)
            {
                int s = stage; var data = Config.stages[s];
                AddMarker(s, -1);
                for (int target = 0; target < data.targets.Length; target++) AddMarker(s, target);
            }
        }
        private void AddMarker(int stage, int target)
        {
            float size = target < 0 ? Ui.stageSize : Ui.markerSize;
            if (Config.stages[stage].boss) size *= Ui.bossScale;
            var rect = _kit.Rect(_root, "Marker_"+stage+"_"+target, new Rect(0,0,size,size));
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one*.5f;
            bool battle=target>=0&&Config.stages[stage].targets[target].kind==MapInteractionKind.Battle;
            var art=(battle||Config.stages[stage].boss) ? Ui.bossMedallion : Ui.medallion;
            var button = _kit.IconButton(rect, "Hit", art!=null?art:Config.frontend.StartupUi.rounded, new Rect(0,0,size,size), null);
            button.image.preserveAspect=true;
            // The transparent hit area grows with device dpi without stretching the medallion art.
            var hitArea = _kit.Image(rect, "TouchArea", null, new Rect(0,0,size,size), Color.clear);
            hitArea.raycastTarget = true;
            hitArea.rectTransform.anchorMin = hitArea.rectTransform.anchorMax = hitArea.rectTransform.pivot = Vector2.one * .5f;
            hitArea.rectTransform.anchoredPosition = Vector2.zero;
            hitArea.gameObject.AddComponent<ChapterMapMarkerHit>().Click = ActivateNearestMarker;
            var icon = _kit.Image(rect, "Symbol", null, new Rect(size*.22f,size*.22f,size*.56f,size*.56f), Ui.gold); icon.preserveAspect = true;
            var label = _kit.Text(rect, "Number", "", new Rect(0,0,size,size), size*.48f, Ui.gold, TextAlignmentOptions.Center,800);
            var marker = new Marker { stage=stage,target=target,rect=rect,hit=hitArea.rectTransform,icon=icon,backing=button.image,text=label,group=rect.gameObject.AddComponent<CanvasGroup>() };
            if(target<0)
            {
                marker.stars = UiFactory.AddStars("Stars",rect,new Vector2(.5f,0),new Vector2(-25,-18),3,16,Ui.gold);
                marker.name=_kit.Image(rect,"RegionName",Ui.nameplate,new Rect((size-Ui.nameWidth)/2,-Ui.nameLift,Ui.nameWidth,Ui.nameHeight)).rectTransform;
                _kit.Text(marker.name,"Label",Config.stages[stage].areaName,new Rect(16,0,Ui.nameWidth-32,Ui.nameHeight),Ui.bodyFont,Ui.gold,TextAlignmentOptions.Center,Ui.textWeight);
                if(Config.stages[stage].boss)_kit.Text(marker.name,"Boss",Config.stages[stage].bossName,new Rect(0,Ui.nameLift+size+25,Ui.nameWidth,30),Ui.bodyFont,Ui.gold,TextAlignmentOptions.Center,800);
            }
            _markers.Add(marker);
        }
        public void ActivateNearestMarker(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var point);
            Marker nearest = null; float best = float.MaxValue;
            foreach (var marker in _markers)
            {
                if (!marker.group.blocksRaycasts) continue;
                float radius = marker.hit.sizeDelta.x * marker.rect.localScale.x * .5f;
                float distance = (point - marker.rect.anchoredPosition).sqrMagnitude;
                if (distance <= radius * radius && distance < best) { best = distance; nearest = marker; }
            }
            if (nearest == null) return;
            _kit.Audio.Play(UiSound.Tap);
            if (nearest.target < 0) _map.SelectStage(nearest.stage);
            else _map.SelectTarget(nearest.stage, nearest.target);
        }
        public void Refresh()
        {
            if (_map.Session == null) return;
            int total = _map.Session.Rules.TotalStars(_map.Session.Progress);
            for(int i=0;i<_rewardLabels.Count;i++) _rewardLabels[i].text = total+"/"+Config.rewards[i].stars;
            int current = Mathf.Max(0,_map.Session.Rules.UnlockedCount(_map.Session.Progress)-1);
            _quest.text = Config.stages[current].areaName+"\n"+Config.stages[current].objective;
            var lobby = Config.frontend.Lobby;
            var profile = FrontendServiceLocator.Lobby.Read(_map.Session.Character, lobby, Config.frontend.Progression);
            foreach (var resource in _resourceLabels)
                resource.label.text = LobbyResourceRules.Compact(profile.Balance(resource.id).amount, lobby.resourceCompactThreshold, lobby.resourceCompactDecimals, lobby.resourceCompactUnit);
        }
        private void LateUpdate()
        {
            if (_map?.Session == null || _map.View == null) return;
            var rules=_map.Session.Rules; var progress=_map.Session.Progress;
            foreach(var marker in _markers)
            {
                var data=Config.stages[marker.stage]; var state=rules.State(progress,marker.stage); bool locked=state==ChapterStageState.Locked;
                bool visible=marker.target<0 || rules.MarkerVisible(progress,marker.stage,data.targets[marker.target].kind);
                bool presented = _map.World == null || _map.World.MarkersVisible(marker.stage);
                if (!locked && !presented) visible = false;
                var anchor=marker.target>=0?_map.Anchors[marker.stage][marker.target]:null;
                if(marker.target>=0&&anchor==null) visible=false;
                var world=anchor!=null?anchor.position:data.center;
                var viewport=_map.View.Camera.WorldToViewportPoint(world);
                visible &= viewport.z>0 && viewport.x>=0 && viewport.x<=1 && viewport.y>=0 && viewport.y<=1;
                var pixels = _canvas.pixelRect;
                var screen = pixels.min + Vector2.Scale(new Vector2(viewport.x,viewport.y), pixels.size);
                var uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root,screen,uiCamera,out var local);
                float bob=marker.target>=0 ? Mathf.Sin((Time.unscaledTime+marker.stage*.71f+marker.target)*Mathf.PI*2/Config.motion.markerBobSeconds)*Config.motion.markerBobPixels : 0;
                marker.rect.anchoredPosition=local+new Vector2(0,(marker.target>=0?Ui.markerLift:0)+bob);
                if(marker.name!=null){marker.name.gameObject.SetActive(!locked);marker.name.localScale=Vector3.one*Mathf.Clamp(Config.cameraSize/_map.View.Camera.orthographicSize,Ui.minimumNameScale,Ui.maximumNameScale);}
                if(marker.stars!=null)marker.stars.gameObject.SetActive(!locked);
                var markerBounds = MarkerBounds(marker);
                bool overlapsHud = false, nearHud = false;
                foreach (var chrome in _markerOccluders)
                {
                    if (!chrome.gameObject.activeInHierarchy) continue;
                    var bounds = GraphicBounds(chrome); overlapsHud |= bounds.Overlaps(markerBounds);
                    float padding = Ui.markerHudPadding * _canvas.scaleFactor;
                    bounds.xMin -= padding; bounds.xMax += padding; bounds.yMin -= padding; bounds.yMax += padding;
                    nearHud |= bounds.Overlaps(markerBounds);
                }
                visible &= !nearHud;
                if (marker.locked && !locked) marker.alpha = 0;
                marker.locked = locked;
                marker.alpha=Mathf.MoveTowards(marker.alpha,visible?1:0,Time.unscaledDeltaTime/(visible?Config.motion.markerFadeIn:Config.motion.markerFadeOut));
                float pulse = !locked && state != ChapterStageState.Completed ? 1 + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / Config.motion.markerPulseSeconds) * Ui.stagePulseScale : 1;
                marker.rect.localScale = Vector3.one * Mathf.Lerp(Ui.markerStartScale, pulse, marker.alpha);
                float dpi = DisplayMetrics.Dpi;
                float tapSize = Mathf.Max(marker.rect.sizeDelta.x, Ui.minimumTapDp * dpi / 160f / _canvas.scaleFactor / Mathf.Max(.01f, marker.rect.localScale.x));
                marker.hit.sizeDelta = Vector2.one * tapSize;
                // Scale/pulse and the dpi hit target have just changed; use the final footprint for clipping.
                markerBounds = MarkerBounds(marker);
                overlapsHud = _markerOccluders.Any(chrome => chrome.gameObject.activeInHierarchy && GraphicBounds(chrome).Overlaps(markerBounds));
                float obscured=1;
                if(anchor!=null){var ray=_map.View.Camera.ViewportPointToRay(viewport); if(Physics.Raycast(ray,out var hit,Vector3.Distance(ray.origin,world),~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(anchor.parent))obscured=Ui.occludedAlpha;}
                // Fade starts in the padded margin. A fast camera jump must never flash through HUD text.
                marker.group.alpha=overlapsHud?0:marker.alpha*obscured; marker.group.blocksRaycasts=visible&&!overlapsHud&&marker.alpha>.5f;
                marker.icon.sprite=locked?Ui.lockIcon:marker.target>=0?(data.targets[marker.target].kind==MapInteractionKind.Quest?Ui.question:Ui.swords):null;
                marker.icon.enabled=marker.icon.sprite!=null;
                marker.backing.color=locked?new Color(.6f,.6f,.6f):Color.white;
                marker.icon.color=marker.target>=0&&data.targets[marker.target].kind==MapInteractionKind.Battle?Color.white:Ui.gold;
                marker.text.text=marker.target<0&&!locked?(marker.stage+1).ToString():"";
                if(marker.name!=null){marker.name.gameObject.SetActive(!locked);marker.name.localScale=Vector3.one*Mathf.Clamp(Config.cameraSize/_map.View.Camera.orthographicSize,Ui.minimumNameScale,Ui.maximumNameScale);}
                if(marker.stars!=null){marker.stars.gameObject.SetActive(!locked);int stars=progress.Find(data.id)?.stars??0;for(int i=0;i<marker.stars.childCount;i++)marker.stars.GetChild(i).GetComponent<Image>().color=i<stars?Ui.gold:Ui.locked;}
            }
            if (_questArrow != null)
            {
                int current = Mathf.Max(0, rules.UnlockedCount(progress)-1);
                var target = _map.View.Camera.WorldToViewportPoint(Config.stages[current].center);
                _questArrow.enabled = target.x < 0 || target.x > 1 || target.y < 0 || target.y > 1;
                var direction = new Vector2(target.x-.5f,target.y-.5f);
                _questArrow.rectTransform.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-180);
            }
            if(_messageSeconds>0){_messageSeconds-=Time.unscaledDeltaTime;if(_messageSeconds<=0)_message.text="";}
        }
        private Rect ScreenBounds(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            var min = new Vector2(float.MaxValue,float.MaxValue); var max = new Vector2(float.MinValue,float.MinValue);
            foreach(var corner in _corners){var p=RectTransformUtility.WorldToScreenPoint(camera,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return new Rect(min,max-min);
        }
        private Rect GraphicBounds(RectTransform root)
        {
            var rect = ScreenBounds(root);
            foreach(var graphic in root.GetComponentsInChildren<Graphic>())
            {
                if(!graphic.enabled)continue;
                var child = ScreenBounds(graphic.rectTransform);
                rect = Rect.MinMaxRect(Mathf.Min(rect.xMin,child.xMin),Mathf.Min(rect.yMin,child.yMin),Mathf.Max(rect.xMax,child.xMax),Mathf.Max(rect.yMax,child.yMax));
            }
            return rect;
        }
        private Rect MarkerBounds(Marker marker) => GraphicBounds(marker.rect);
        private RectTransform OpenModal(string title)
        {
            CloseModal(); _modal=_kit.Dim(_safe,"ChapterModal",.65f);
            var size=Ui.modalSize;var panel=Frame(_modal,"Panel",new Rect(0,0,size.x,size.y));
            panel.GetComponent<LobbyFrameGraphic>().raycastTarget=true;
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;panel.anchoredPosition=Vector2.zero;
            _kit.Text(panel,"Title",title,new Rect(25,20,size.x-50,44),28,Ui.gold,TextAlignmentOptions.Center,800);
            _kit.Button(panel,"Close","Đóng",new Rect(size.x-150,size.y-68,125,44),CloseModal,GameButtonStyle.Plain);
            _kit.DismissOutside(_modal,CloseModal); return panel;
        }
        public void ShowQuest(int stage,int target)
        {
            var npc=Config.stages[stage].targets[target]; var panel=OpenModal(npc.name);
            _kit.Text(panel,"Dialogue",npc.dialogue,new Rect(30,80,Ui.modalSize.x-60,145),21,Color.white);
            _kit.Button(panel,"AcceptQuest","Nhận nhiệm vụ",new Rect(30,Ui.modalSize.y-74,270,52),()=>_map.AcceptQuest(stage),GameButtonStyle.Gold);
        }
        public void ShowStage(int stage)
        {
            var data=Config.stages[stage];var panel=OpenModal(data.areaName);
            _kit.Text(panel,"Objective",data.objective,new Rect(30,80,Ui.modalSize.x-60,110),22,Color.white);
            _kit.Text(panel,"Stars","Sao tốt nhất: "+(_map.Session.Progress.Find(data.id)?.stars??0)+"/3",new Rect(30,190,Ui.modalSize.x-60,35),20,Ui.gold);
            _kit.Button(panel,"Deploy","Xuất Chiến",new Rect(30,Ui.modalSize.y-74,270,52),()=>_map.LaunchBattle(stage),GameButtonStyle.Red);
        }
        private void ShowInfo(){var panel=OpenModal(Config.title+" "+Config.subtitle);_kit.Text(panel,"Introduction",Config.introduction,new Rect(30,90,Ui.modalSize.x-60,150),22,Color.white);}
        private void Claim(int index){ShowMessage(_map.Session.Claim(index)?"Đã nhận thưởng chương.":"Chưa thể nhận phần thưởng này.");Refresh();}
        public void CloseModal(){if(_modal!=null)_kit.Close(_modal);_modal=null;}
        public void ShowMessage(string text){if(_message!=null){_message.text=text;_messageSeconds=Ui.messageSeconds;}}
    }
}
