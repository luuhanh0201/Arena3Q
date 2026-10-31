using System;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Arena3Q.Exploration
{
    public sealed class ChapterMapController : MonoBehaviour
    {
        [SerializeField] private ChapterMapConfig config;
        public ChapterMapConfig Config => config;
        public ChapterMapSession Session { get; private set; }
        public ChapterMapCamera View { get; private set; }
        public ChapterMapMotor Motor { get; private set; }
        public ChapterMapHud Hud { get; private set; }
        public ChapterMapWorld World { get; private set; }
        public ChapterMapAudio Audio { get; private set; }
        public Transform[][] Anchors { get; private set; }
        public Transform[] Areas { get; private set; }
        public bool Ready { get; private set; }
        public bool Busy { get; set; }
        public string LastMessage { get; private set; } = "";
        private NavMeshDataInstance _navigation;
        private GameObject _model;
        private HeroDefinition _hero;
        private float _animationTime;
        public void Configure(ChapterMapConfig value) => config = value;
        [UnityEngine.Scripting.Preserve]
        public void InspectUi()
        {
            if (!Ready || Hud == null) return;
            // WebGL truncates long log messages. Keep each transport chunk comfortably below its limit.
            string payload = Hud.InspectUi();
            const int chunkSize = 1000;
            int count = (payload.Length + chunkSize - 1) / chunkSize;
            for (int part = 0; part < count; part++)
                Debug.Log($"[ChapterMapQA:{part}:{count}]" + payload.Substring(part * chunkSize, Math.Min(chunkSize, payload.Length - part * chunkSize)));
        }
        private void Start()
        {
            if (config == null) { Debug.LogError("[ChapterMap] Missing config."); return; }
            var errors = new List<string>(); config.Validate(errors);
            if (errors.Count > 0) { Debug.LogError(string.Join("\n", errors)); return; }
            try { Session = new ChapterMapSession(config, FrontendServiceLocator.Profiles); }
            catch (InvalidOperationException) { FrontendController.ResumeAt = FrontendStep.ServerSelect; SceneTransition.Load(config.frontend.FrontendSceneName); return; }
            var camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null) { Debug.LogError("[ChapterMap] Missing camera."); return; }
            View = gameObject.AddComponent<ChapterMapCamera>();
            var p = Session.Progress;
            View.Configure(camera, config, p.hasReturnPose ? new Vector3(p.cameraX, p.cameraY, p.cameraZ) : config.initialPosition, p.hasReturnPose ? p.cameraSize : config.cameraSize);
            BindScene();
            gameObject.AddComponent<ChapterMapRoute>().Configure(this);
            Hud = gameObject.AddComponent<ChapterMapHud>(); Hud.Configure(this);
            if (!LoadNavigation()) { Message("Đường đi đang được chuẩn bị."); return; }
            var desired = p.hasReturnPose ? new Vector3(p.playerX, p.playerY, p.playerZ) : config.initialPosition;
            if (!NavMesh.SamplePosition(desired, out var hit, config.pathSampleDistance, NavMesh.AllAreas)
                && !NavMesh.SamplePosition(config.initialPosition, out hit, config.pathSampleDistance, NavMesh.AllAreas))
            { Message("Chưa tìm được vị trí bắt đầu hợp lệ."); return; }
            var actor = new GameObject("Chapter player"); actor.transform.SetParent(transform, false); actor.transform.position = hit.position;
            actor.transform.rotation = Quaternion.Euler(0, p.hasReturnPose ? p.playerYaw : 0, 0);
            Motor = actor.AddComponent<ChapterMapMotor>(); Motor.Configure(config, Session);
            Motor.Arrived += OnArrived; Motor.Message += Message;
            BuildPlayer(actor.transform);
            gameObject.AddComponent<ChapterMapInput>().Configure(this);
            Ready = true; Hud.Refresh();
            gameObject.AddComponent<ChapterMapEnvironment>().Configure(this);
            Audio = gameObject.AddComponent<ChapterMapAudio>(); Audio.Configure(this, config.audio);
            World = gameObject.AddComponent<ChapterMapWorld>(); World.Configure(this);
            gameObject.AddComponent<ChapterMapDetails>().Configure(this);
        }
        private void BindScene()
        {
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Areas = new Transform[config.stages.Length]; Anchors = new Transform[config.stages.Length][];
            for (int i = 0; i < config.stages.Length; i++)
            {
                var entry = config.stages[i]; Areas[i] = all.FirstOrDefault(t => Path(t) == entry.areaPath);
                Anchors[i] = new Transform[entry.targets.Length];
                for (int j = 0; j < entry.targets.Length; j++)
                {
                    var target = entry.targets[j];
                    Anchors[i][j] = all.Where(t => Path(t) == target.anchorPath).OrderBy(t => Vector3.SqrMagnitude(t.position - target.position)).FirstOrDefault();
                    var anchor = Anchors[i][j];
                    if (anchor == null) { Debug.LogError("[ChapterMap] Missing anchor: " + target.anchorPath); continue; }
                    // Marker UI remains clickable when buildings obscure this physical target.
                    var collider = anchor.parent.GetComponent<CapsuleCollider>();
                    if (collider == null) collider = anchor.parent.gameObject.AddComponent<CapsuleCollider>();
                    collider.isTrigger = true; collider.radius = 1; collider.height = 2;
                }
            }
        }
        public static string Path(Transform item) => item.parent != null ? Path(item.parent) + "/" + item.name : item.name;
        private bool LoadNavigation()
        {
            if (_navigation.valid) _navigation.Remove();
            int index = Session.Rules.UnlockedCount(Session.Progress) - 1;
            if (index < 0 || index >= config.navigationByUnlockedStage.Length || config.navigationByUnlockedStage[index] == null) return false;
            _navigation = NavMesh.AddNavMeshData(config.navigationByUnlockedStage[index]); return _navigation.valid;
        }
        private void BuildPlayer(Transform parent)
        {
            _hero = config.frontend.LordRoster.Find(Session.Character.lordHeroId)?.lord;
            var source = _hero?.Model3D != null ? _hero.Model3D.Prefab : _hero?.PortraitModelPrefab;
            if (source == null)
            {
                _model = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(_model.GetComponent<Collider>());
                _model.transform.SetParent(parent, false); _model.transform.localPosition = Vector3.up * config.agentHeight * .5f;
                _model.transform.localScale = new Vector3(config.agentRadius * 2, config.agentHeight * .5f, config.agentRadius * 2);
                Message("Hình chủ tướng đang được cập nhật."); return;
            }
            _model = Instantiate(source, parent); _model.transform.localPosition = Vector3.zero;
            if (_hero.Model3D != null) { _model.transform.localScale *= _hero.Model3D.Scale; _model.transform.localRotation = Quaternion.Euler(0, _hero.Model3D.YawOffset, 0); }
            foreach (var animator in _model.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (var animation in _model.GetComponentsInChildren<Animation>()) animation.enabled = false;
        }
        private void LateUpdate()
        {
            if (_model == null || _hero == null || Motor == null) return;
            var clip = _hero.Model3D?.Find(Motor.Moving ? HeroAnimationState.Walk : HeroAnimationState.Idle) ?? _hero.PortraitIdle;
            if (clip != null) { _animationTime += Time.deltaTime; clip.SampleAnimation(_model, Mathf.Repeat(_animationTime, Mathf.Max(.01f, clip.length))); }
        }
        public void SelectTarget(int stage, int target)
        {
            if (!Ready || Busy || stage < 0 || stage >= config.stages.Length
                || target < 0 || target >= config.stages[stage].targets.Length) return;
            Hud.CloseModal();
            if (Motor.RequestTarget(stage, target, Anchors[stage][target]))
            { View.Follow(Motor.transform); Audio?.Play(MapSound.SelectNpc, Anchors[stage][target].position); }
        }
        public void SelectStage(int stage)
        {
            if (!Ready || Busy || stage < 0 || stage >= config.stages.Length) return;
            var state = Session.Rules.State(Session.Progress, stage);
            if (state == ChapterStageState.Locked) { Message(config.ui.lockedMessage); return; }
            if (state == ChapterStageState.Completed) { Motor.Cancel(); Hud.ShowStage(stage); return; }
            for (int t = 0; t < config.stages[stage].targets.Length; t++)
                if (Session.Rules.MarkerVisible(Session.Progress, stage, config.stages[stage].targets[t].kind)) { SelectTarget(stage, t); return; }
        }
        private void OnArrived(int stage, int target)
        {
            if (!Session.Rules.MarkerVisible(Session.Progress, stage, config.stages[stage].targets[target].kind)) return;
            if (config.stages[stage].targets[target].kind == MapInteractionKind.Quest) Hud.ShowQuest(stage, target); else Hud.ShowStage(stage);
        }
        public void AcceptQuest(int stage) { if (Session.AcceptQuest(stage)) { Audio?.Play(MapSound.AcceptQuest, config.stages[stage].center); Hud.CloseModal(); Hud.Refresh(); } }
        public bool ClaimReward(int index)
        {
            if (!Session.Claim(index)) return false;
            Audio?.Play(MapSound.OpenChest, View.Focus); return true;
        }
        public void GroundTap(Vector2 screen)
        {
            if (!Ready || Busy) return;
            var ray = View.Camera.ScreenPointToRay(screen);
            foreach (var hit in Physics.RaycastAll(ray, View.Camera.farClipPlane, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance))
                for (int s = 0; s < Anchors.Length; s++) for (int t = 0; t < Anchors[s].Length; t++)
                    if (Anchors[s][t] != null && hit.transform == Anchors[s][t].parent && Session.Rules.MarkerVisible(Session.Progress, s, config.stages[s].targets[t].kind)) { SelectTarget(s, t); return; }
            Hud.CloseModal(); if (Motor.MoveGround(View.Ground(screen))) View.Follow(Motor.transform);
        }
        public void LaunchBattle(int stage)
        {
            if (!Ready || Busy) return;
            SavePose(); var request = Session.CreateBattle(stage);
            if (request == null) { Message(config.ui.unavailable); return; }
            Busy = true; Motor.Cancel(); BattleLaunch.Request(request); SceneTransition.Load(config.frontend.BattleSceneName);
        }
        public void ReturnToLobby()
        {
            if (Busy) return; SavePose(); Motor?.Cancel(); Busy = true;
            FrontendController.ResumeAt = FrontendStep.Lobby; SceneTransition.Load(config.frontend.FrontendSceneName);
        }
        public void SavePose()
        {
            if (Session == null || Motor == null) return;
            var p = Session.Progress; var actor = Motor.transform.position; var focus = View.Focus;
            p.hasReturnPose = true; p.playerX = actor.x; p.playerY = actor.y; p.playerZ = actor.z; p.playerYaw = Motor.transform.eulerAngles.y;
            p.cameraX = focus.x; p.cameraY = focus.y; p.cameraZ = focus.z; p.cameraSize = View.Camera.orthographicSize; Session.Save();
        }
        public void Message(string value) { LastMessage = value; if (Hud != null) Hud.ShowMessage(value); }
        private void OnApplicationPause(bool paused) { if (paused) SavePose(); }
        private void OnDestroy()
        {
            if (Motor != null) { Motor.Arrived -= OnArrived; Motor.Message -= Message; Motor.Cancel(); }
            if (_navigation.valid) _navigation.Remove();
        }
    }
}
