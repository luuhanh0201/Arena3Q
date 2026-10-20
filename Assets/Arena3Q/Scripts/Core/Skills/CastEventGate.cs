using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>Rejects duplicate, cancelled and stale animation events without owning damage rules.</summary>
    public sealed class CastEventGate
    {
        private readonly HashSet<string> _seen = new HashSet<string>();
        private int _generation;
        private bool _active;
        public int Begin()
        {
            _generation++;
            _seen.Clear();
            _active = true;
            return _generation;
        }
        public void Cancel()
        {
            _active = false;
            _seen.Clear();
        }
        public bool TryAccept(int castId, string eventKey) => _active && castId == _generation &&
            !string.IsNullOrWhiteSpace(eventKey) && _seen.Add(eventKey);
    }
}
