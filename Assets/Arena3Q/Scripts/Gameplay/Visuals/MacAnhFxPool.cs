using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>A pool scoped to one battle, cleared on restart/result and destroyed with the scene.</summary>
    public sealed class MacAnhFxPool : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Stack<MacAnhFx>> _available = new Dictionary<GameObject, Stack<MacAnhFx>>();
        private readonly Dictionary<MacAnhFx, GameObject> _prefabs = new Dictionary<MacAnhFx, GameObject>();
        private readonly HashSet<MacAnhFx> _active = new HashSet<MacAnhFx>();
        private readonly Dictionary<MacAnhFx,float> _lastEffectTime = new Dictionary<MacAnhFx,float>();
        private readonly List<MacAnhFx> _tickScratch = new List<MacAnhFx>();
        private BattleController _battle;
        public int ActiveCount => _active.Count;
        public static MacAnhFxPool For(BattleController battle)
        {
            var pool = battle.GetComponentInChildren<MacAnhFxPool>();
            if (pool != null) return pool;
            var go = new GameObject("Mac Anh effect pool"); go.transform.SetParent(battle.transform, false);
            pool = go.AddComponent<MacAnhFxPool>(); pool._battle = battle;
            battle.BattleStarted += pool.ClearActive; battle.Finished += pool.OnFinished;
            return pool;
        }
        public MacAnhFx Spawn(GameObject prefab, Vector3 position, Vector3 scale, Quaternion rotation,
            MacAnhFxQuality quality, float seconds, int sortingOrder, Transform follow = null)
        {
            if (prefab == null || prefab.GetComponent<MacAnhFx>() == null)
            {
                Debug.LogError("[MacAnh] Missing VFX prefab/component; effect was not spawned."); return null;
            }
            if (!_available.TryGetValue(prefab, out var stack)) _available[prefab] = stack = new Stack<MacAnhFx>();
            MacAnhFx fx = null;
            while (stack.Count > 0 && fx == null) fx = stack.Pop();
            if (fx == null)
            {
                fx = Instantiate(prefab, transform).GetComponent<MacAnhFx>(); _prefabs.Add(fx, prefab);
            }
            fx.transform.SetPositionAndRotation(position, rotation); fx.transform.localScale = scale;
            fx.gameObject.SetActive(true); _active.Add(fx);
            _lastEffectTime[fx]=_battle.BattleTime;
            fx.SetUpright(_battle.UprightRotation);
            fx.Begin(quality, seconds, sortingOrder, Release, follow);
            fx.ManualClock = true;
            fx.SetGroundLayers();
            return fx;
        }
        private void Release(MacAnhFx fx)
        {
            if (fx == null || !_active.Remove(fx)) return;
            _lastEffectTime.Remove(fx);
            fx.gameObject.SetActive(false); _available[_prefabs[fx]].Push(fx);
        }
        private void ClearActive()
        {
            foreach (var fx in new List<MacAnhFx>(_active)) if (fx != null) fx.Stop();
            _active.Clear();
            _lastEffectTime.Clear();
        }
        private void Update()
        {
            if (_battle == null) return;
            var now=_battle.BattleTime;
            _tickScratch.Clear(); _tickScratch.AddRange(_active);
            foreach (var fx in _tickScratch) if (fx != null && _lastEffectTime.TryGetValue(fx,out var previous))
            {
                _lastEffectTime[fx]=now;
                fx.Advance(Mathf.Max(0,now-previous));
            }
        }
        private void OnFinished(OutcomeDecision _) => ClearActive();
        private void OnDestroy()
        {
            if (_battle != null) { _battle.BattleStarted -= ClearActive; _battle.Finished -= OnFinished; }
        }
    }
}
