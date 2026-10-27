using System.Collections.Generic;

namespace Arena3Q.Core
{
    public static class LobbyTimeRules
    {
        public static int IndexAtHour(int hour, IReadOnlyList<int> startHours)
        {
            if (startHours == null || startHours.Count == 0) return -1;
            hour = ((hour % 24) + 24) % 24;
            var result = startHours.Count - 1;
            for (var i = 0; i < startHours.Count; i++) if (hour >= startHours[i]) result = i;
            return result;
        }
    }
}
