using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Spawns the stage's enemies: defenders for Attack stages, timed waves for Defense stages (spec §8).</summary>
    internal sealed class StageRunner
    {
        private readonly BattleController _battle;
        private readonly StageConfig _stage;
        private readonly WaveTracker _waves;

        public StageRunner(BattleController battle, StageConfig stage)
        {
            _battle = battle;
            _stage = stage;

            var startTimes = new List<float>();
            if (stage.StageType == StageType.Defense && stage.Waves != null)
            {
                foreach (var wave in stage.Waves)
                    startTimes.Add(wave.StartTime);
            }
            _waves = new WaveTracker(startTimes);
        }

        public void Begin()
        {
            if (_stage.StageType != StageType.Attack || _stage.Defenders == null)
                return;
            foreach (var defender in _stage.Defenders)
            {
                if (defender?.Unit != null)
                    _battle.SpawnEnemy(defender.Unit, EnemyRole.Guard, defender.Position, defender.GuardRadius, -1);
            }
        }

        public void Tick(BattleRuntimeState state)
        {
            if (_stage.StageType != StageType.Defense)
                return;

            int wave;
            while ((wave = _waves.TakeDueWave(state.ElapsedTime)) >= 0)
                SpawnWave(_stage.Waves[wave], wave);

            state.CurrentWave = _waves.CurrentWave;
            var alive = _battle.Registry.Count(u => u is EnemyUnit enemy && enemy.WaveIndex >= 0);
            state.AllWavesCleared = _waves.IsCleared(alive);
        }

        public int WaveCount => _waves.WaveCount;

        private void SpawnWave(WaveConfig wave, int waveIndex)
        {
            if (wave?.Spawns == null)
                return;
            foreach (var spawn in wave.Spawns)
            {
                if (spawn?.Unit == null)
                    continue;
                for (var i = 0; i < spawn.Count; i++)
                {
                    var position = spawn.SpawnPoint + FormationMath.SlotOffset(i, 0.8f) * (i == 0 ? 0f : 1f);
                    _battle.SpawnEnemy(spawn.Unit, EnemyRole.Assault, position, 0f, waveIndex);
                }
            }
        }
    }
}
