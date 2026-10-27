using System;
using System.Globalization;

namespace Arena3Q.Core
{
    /// <summary>Presentation rules for the lobby wallet, without floating-point rounding of balances.</summary>
    public static class LobbyResourceRules
    {
        private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
        private const decimal AmountPerVan = 10000m;

        public static string Exact(long amount) => Math.Max(0, amount).ToString("N0", Vietnamese);

        public static string Compact(long amount, long threshold, int decimalPlaces, string unit)
        {
            amount = Math.Max(0, amount);
            if (threshold <= 0 || amount < threshold) return Exact(amount);
            decimalPlaces = Math.Max(0, Math.Min(4, decimalPlaces));
            var factor = 1m;
            for (var i = 0; i < decimalPlaces; i++) factor *= 10m;
            var compact = Math.Floor(amount / AmountPerVan * factor) / factor;
            var format = decimalPlaces == 0 ? "0" : "0." + new string('#', decimalPlaces);
            var label = unit ?? "";
            return compact.ToString(format, Vietnamese) + (label.Length > 0 ? " " + label : "");
        }

        public static float ExperienceFraction(long experience, long required, bool maxLevel)
        {
            if (maxLevel) return 1;
            if (required <= 0 || experience <= 0) return 0;
            return (float)Math.Min(1m, (decimal)experience / required);
        }
    }
}
