using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>Tracks Defense-stage waves: which waves are due and when all of them are cleared (spec §8).</summary>
    public sealed class WaveTracker
    {
        private readonly float[] _startTimes;
        private int _spawned;

        public WaveTracker(IReadOnlyList<float> startTimes)
        {
            _startTimes = startTimes == null ? Array.Empty<float>() : new List<float>(startTimes).ToArray();
            for (var i = 1; i < _startTimes.Length; i++)
            {
                if (_startTimes[i] < _startTimes[i - 1])
                    throw new ArgumentException("Wave start times must be in ascending order.", nameof(startTimes));
            }
        }

        public int WaveCount => _startTimes.Length;
        public int SpawnedCount => _spawned;
        public bool AllSpawned => _spawned >= _startTimes.Length;

        /// <summary>Index of the latest spawned wave, or -1 before the first one.</summary>
        public int CurrentWave => _spawned - 1;

        /// <summary>Returns the next wave due at <paramref name="elapsed"/> and marks it spawned, or -1 if none is due.</summary>
        public int TakeDueWave(float elapsed)
        {
            if (AllSpawned || elapsed < _startTimes[_spawned])
                return -1;
            return _spawned++;
        }

        /// <summary>True when every wave has spawned and none of its enemies is alive.</summary>
        public bool IsCleared(int aliveWaveEnemies) => WaveCount > 0 && AllSpawned && aliveWaveEnemies == 0;
    }
}
