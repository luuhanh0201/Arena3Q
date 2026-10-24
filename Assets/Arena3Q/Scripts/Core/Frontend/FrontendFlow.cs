using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>The screens from launching the game to the first battle (spec §2 "Luồng vào game").</summary>
    public enum FrontendStep
    {
        Intro,
        Login,
        ServerSelect,
        LordSelect,
        NameEntry,
        Story,
        Battle,
        /// <summary>Nothing left to show.</summary>
        Done,
        // Appended to preserve existing serialized enum values.
        Loading
    }

    /// <summary>What the flow knows about the player at the moment it picks the next screen.</summary>
    public readonly struct FrontendState
    {
        public FrontendState(bool signedIn, bool serverChosen, bool hasLord, bool hasName, bool storySeen)
        {
            SignedIn = signedIn;
            ServerChosen = serverChosen;
            HasLord = hasLord;
            HasName = hasName;
            StorySeen = storySeen;
        }

        public bool SignedIn { get; }
        public bool ServerChosen { get; }
        public bool HasLord { get; }
        public bool HasName { get; }
        public bool StorySeen { get; }
    }

    /// <summary>
    /// Walks the screen order from data and skips screens the player does not need: a returning player does not pick
    /// a lord, name it or watch the opening story again. Reordering or removing screens is a data change.
    /// </summary>
    public sealed class FrontendFlowPlanner
    {
        public static readonly IReadOnlyList<FrontendStep> DefaultOrder = new[]
        {
            FrontendStep.Intro, FrontendStep.Login, FrontendStep.ServerSelect, FrontendStep.LordSelect,
            FrontendStep.NameEntry, FrontendStep.Story, FrontendStep.Battle
        };

        private readonly List<FrontendStep> _order = new List<FrontendStep>();

        public FrontendFlowPlanner(IReadOnlyList<FrontendStep> order)
        {
            if (order != null)
            {
                foreach (var step in order)
                {
                    if (step != FrontendStep.Done && !_order.Contains(step))
                        _order.Add(step);
                }
            }
            if (_order.Count == 0)
                _order.AddRange(DefaultOrder);
        }

        public IReadOnlyList<FrontendStep> Order => _order;

        /// <summary>The first screen to show.</summary>
        public FrontendStep First(FrontendState state) => NextFrom(-1, state);

        /// <summary>The screen after <paramref name="current"/>.</summary>
        public FrontendStep Next(FrontendStep current, FrontendState state) => NextFrom(_order.IndexOf(current), state);

        public static bool IsNeeded(FrontendStep step, FrontendState state)
        {
            switch (step)
            {
                case FrontendStep.LordSelect: return !state.HasLord;
                case FrontendStep.NameEntry: return !state.HasName;
                case FrontendStep.Story: return !state.StorySeen;
                case FrontendStep.Done: return false;
                // Intro, login ("tap to enter" for a returning player), server and battle are always shown.
                default: return true;
            }
        }

        private FrontendStep NextFrom(int index, FrontendState state)
        {
            for (var i = index + 1; i < _order.Count; i++)
            {
                if (IsNeeded(_order[i], state))
                    return _order[i];
            }
            return FrontendStep.Done;
        }
    }

    /// <summary>Position in a story: panel by panel, line by line; a panel without lines still takes one tap; skipping jumps to the end.</summary>
    public sealed class StoryCursor
    {
        private readonly IReadOnlyList<int> _linesPerPanel;

        /// <param name="linesPerPanel">Number of dialogue lines of each panel (a panel may have 0 lines).</param>
        public StoryCursor(IReadOnlyList<int> linesPerPanel)
        {
            _linesPerPanel = linesPerPanel ?? Array.Empty<int>();
            Panel = 0;
            Line = 0;
        }

        public int Panel { get; private set; }
        public int Line { get; private set; }
        public bool IsFinished => Panel >= _linesPerPanel.Count;

        /// <summary>Next line; after a panel's last line, the next panel. Returns false when the story is over.</summary>
        public bool Advance()
        {
            if (IsFinished)
                return false;
            Line++;
            if (Line >= _linesPerPanel[Panel])
            {
                Panel++;
                Line = 0;
            }
            return !IsFinished;
        }

        public void Skip()
        {
            Panel = _linesPerPanel.Count;
            Line = 0;
        }

        /// <summary>Replaces the player-name tokens ({Tên} or {name}) in a story line.</summary>
        public static string Format(string line, string playerName) =>
            (line ?? "").Replace("{Tên}", playerName ?? "").Replace("{name}", playerName ?? "");
    }
}
