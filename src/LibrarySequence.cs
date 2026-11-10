using System;
using System.Collections.Generic;
using System.Linq;

namespace HexMapEditor
{
    internal static class LibrarySequence
    {
        public static long Next(IEnumerable<long> current)
        {
            return Math.Max(DateTime.UtcNow.Ticks, current.Where(value => value > 0 && value < Int64.MaxValue)
                .DefaultIfEmpty(0).Max() + 1);
        }

        public static List<T> Sort<T>(IEnumerable<T> items, Func<T, long> order, Func<T, string> id)
        {
            return items.OrderBy(item => Math.Max(0, order(item)))
                .ThenBy(id, StringComparer.Ordinal).ToList();
        }

        public static long Read(Dictionary<string, object> raw)
        {
            object value;
            long result;
            return raw.TryGetValue("LibraryOrder", out value) && (value is int || value is long) &&
                Int64.TryParse(value.ToString(), out result) && result >= 0 ? result : 0;
        }
    }
}
