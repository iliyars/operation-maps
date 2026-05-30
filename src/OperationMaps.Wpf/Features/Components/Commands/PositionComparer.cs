using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Features.Components.Commands
{
    public sealed partial class PositionComparer : IComparer<string>
    {
        public static readonly PositionComparer Instance = new();

        private PositionComparer() { }

        public int Compare(string? x, string? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var xParts = Split(x);
            var yParts = Split(y);

            for (var i = 0; i < Math.Min(xParts.Count, yParts.Count); i++)
            {
                var xPart = xParts[i];
                var yPart = yParts[i];

                int result;
                if (int.TryParse(xPart, out var xNum) && int.TryParse(yPart, out var yNum))
                    result = xNum.CompareTo(yNum);
                else
                    result = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);

                if (result != 0) return result;
            }

            return xParts.Count.CompareTo(yParts.Count);
        }

        private static List<string> Split(string input)
        => DigitOrNonDigit().Matches(input)
                            .Select(m => m.Value)
                            .ToList();

        [GeneratedRegex(@"\d+|\D+")]
        private static partial Regex DigitOrNonDigit();
    }
}
