using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// A battle requested by another scene (the game flow): which stage, which lineup, and what to do with the result.
    /// The Battle scene picks it up when it starts; without one it plays its own stages with the test loadout.
    /// </summary>
    public sealed class BattleLaunchRequest
    {
        public StageConfig Stage;
        public IReadOnlyList<LineupEntry> Lineup;

        /// <summary>Scene the result screen can go back to; empty = no "back" button.</summary>
        public string ReturnScene = "";

        /// <summary>Called when a battle ends (also after replays); returns a note for the result screen (rewards), or null.</summary>
        public Func<StageConfig, OutcomeDecision, string> OnFinished;

        /// <summary>Called right before going back to <see cref="ReturnScene"/>.</summary>
        public Action OnReturn;
    }

    /// <summary>Hands a <see cref="BattleLaunchRequest"/> from one scene to the next.</summary>
    public static class BattleLaunch
    {
        private static BattleLaunchRequest _pending;

        public static bool HasPending => _pending != null;

        public static void Request(BattleLaunchRequest request) => _pending = request;

        /// <summary>The pending request, removed so it is used once.</summary>
        public static BattleLaunchRequest Take()
        {
            var request = _pending;
            _pending = null;
            return request;
        }

        public static void Clear() => _pending = null;
    }
}
