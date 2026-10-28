using System;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Arena3Q.Exploration
{
    public sealed class ChapterMapMotor : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }
        public event Action<int, int> Arrived;
        public event Action<string> Message;
        private ChapterMapConfig _config;
        private ChapterMapSession _session;
        private Transform _target;
        private int _stage = -1, _targetIndex = -1;
        private Vector3 _lastPosition;
        private float _stuck;
        private bool _hasDestination;
        public string PathFailure { get; private set; }
        public bool Moving => Agent != null && Agent.isOnNavMesh && (Agent.pathPending || Agent.hasPath);
        public void Configure(ChapterMapConfig config, ChapterMapSession session)
        {
            _config = config; _session = session;
            Agent = gameObject.AddComponent<NavMeshAgent>(); Agent.radius = config.agentRadius; Agent.height = config.agentHeight;
            Agent.speed = config.walkSpeed; Agent.stoppingDistance = config.agentRadius; Agent.autoBraking = true;
            _lastPosition = transform.position;
        }
        public bool RequestTarget(int stage, int targetIndex, Transform anchor)
        {
            Cancel();
            if (anchor == null || !Valid(stage, targetIndex)) { Message?.Invoke(_config.ui.lockedMessage); return false; }
            // Aim at the floor the NPC stands on (Trương Giác is on the 2.6 m altar plateau), not at the walker's height.
            var targetPoint = anchor.position;
            if (Physics.Raycast(anchor.position, Vector3.down, out var floor, 30f, ~0, QueryTriggerInteraction.Ignore)) targetPoint = floor.point;
            else targetPoint.y = transform.position.y;
            if (!FindDestination(targetPoint, _config.interactionRadius, out var destination)) { Message?.Invoke(_config.ui.stuckMessage); return false; }
            Agent.stoppingDistance = _config.agentRadius;
            if (!Agent.SetDestination(destination)) { Message?.Invoke(_config.ui.stuckMessage); return false; }
            _target = anchor; _stage = stage; _targetIndex = targetIndex;
            _hasDestination = true; _lastPosition = transform.position; return true;
        }
        public bool MoveGround(Vector3 point)
        {
            Cancel();
            if (!PointUnlocked(point)) { Message?.Invoke(_config.ui.lockedMessage); return false; }
            if (!FindDestination(point, _config.pathSampleDistance, out var destination)) { Message?.Invoke(_config.ui.stuckMessage); return false; }
            Agent.stoppingDistance = _config.agentRadius;
            _hasDestination = Agent.SetDestination(destination); _lastPosition = transform.position;
            if (!_hasDestination) Message?.Invoke(_config.ui.stuckMessage);
            return _hasDestination;
        }
        public bool PointUnlocked(Vector3 point)
        {
            if (!_config.groundBounds.Contains(new Vector2(point.x, point.z))) return false;
            int nearest = -1; float best = float.MaxValue;
            for (int i = 0; i < _config.stages.Length; i++)
            {
                var delta = point - _config.stages[i].center; delta.y = 0;
                if (delta.sqrMagnitude < best) { best = delta.sqrMagnitude; nearest = i; }
            }
            return _session.Rules.State(_session.Progress, nearest) != ChapterStageState.Locked;
        }
        private bool FindDestination(Vector3 point, float range, out Vector3 destination)
        {
            destination = default;
            PathFailure="No allowed navigation point near "+point;
            if (!Agent.isOnNavMesh || !NavMesh.SamplePosition(point, out var hit, range, Agent.areaMask) || !PointUnlocked(hit.position)) return false;
            var path = new NavMeshPath();
            bool found=Agent.CalculatePath(hit.position, path);PathFailure=$"Path {transform.position} to {hit.position}: {path.status}, corners={path.corners.Length}";
            if (!found || path.status != NavMeshPathStatus.PathComplete) return false;
            // Check segments too; corner-only checks miss a shortcut crossing a locked region.
            for (int i = 1; i < path.corners.Length; i++)
            {
                float distance = Vector3.Distance(path.corners[i - 1], path.corners[i]);
                int samples = Mathf.Max(1, Mathf.CeilToInt(distance / _config.agentRadius));
                for (int sample = 0; sample <= samples; sample++) if (!PointUnlocked(Vector3.Lerp(path.corners[i - 1], path.corners[i], (float)sample / samples))) {PathFailure="Path crosses locked region near "+path.corners[i];return false;}
            }
            PathFailure="";destination = hit.position; return true;
        }
        private bool Valid(int stage, int targetIndex)
        {
            return stage >= 0 && stage < _config.stages.Length && targetIndex >= 0 && targetIndex < _config.stages[stage].targets.Length
                && _session.Rules.MarkerVisible(_session.Progress, stage, _config.stages[stage].targets[targetIndex].kind);
        }
        public void Cancel()
        {
            _target = null; _stage = _targetIndex = -1; _stuck = 0; _hasDestination = false;
            if (Agent != null && Agent.isOnNavMesh) Agent.ResetPath();
        }
        private void Update()
        {
            if (Agent == null || !Agent.isOnNavMesh) return;
            // Keep the interaction identity even if Unity has destroyed the target object.
            if (_stage >= 0 && (_target == null || !Valid(_stage, _targetIndex) || !_target.gameObject.activeInHierarchy)) { Cancel(); return; }
            if (Agent.pathPending) return;
            if (_target != null)
            {
                var delta = _target.position - transform.position; delta.y = 0;
                if (delta.magnitude <= _config.interactionRadius)
                {
                    int stage = _stage, target = _targetIndex;
                    if (delta.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(delta);
                    Cancel(); Arrived?.Invoke(stage, target); return;
                }
            }
            // A cleared/failed path has hasPath=false, but the outstanding interaction still
            // needs a timeout. Otherwise it waits forever and can open unexpectedly later.
            if (_hasDestination)
            {
                if (Vector3.Distance(transform.position, _lastPosition) >= _config.minimumProgress) { _stuck = 0; _lastPosition = transform.position; }
                else _stuck += Time.deltaTime;
                if (_stuck >= _config.stuckSeconds || Agent.pathStatus == NavMeshPathStatus.PathInvalid) { Cancel(); Message?.Invoke(_config.ui.stuckMessage); }
                else if (_target == null && (!Agent.hasPath || Agent.remainingDistance <= Agent.stoppingDistance)) Cancel();
            }
        }
        private void OnDisable() => Cancel();
    }
}
