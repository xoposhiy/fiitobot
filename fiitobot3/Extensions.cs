using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

#nullable disable
namespace fiitobot
{
    public static class Extensions
    {
        public static string Canonize(this string text)
        {
            return text.ToLower().Replace("ë", "е").Replace("ё", "е").Replace("́", "");
        }

        public static bool ContainsSameText(this string text, string substr)
        {
            return text.Canonize().Contains(substr.Canonize(), StringComparison.OrdinalIgnoreCase);
        }

        public static int SetBit(this int x, int bitIndex)
        {
            return x | (1 << bitIndex);
        }

        public static int GetBit(this int x, int bitIndex)
        {
            return (x >> bitIndex) & 1;
        }
        public static double Distance(this double a, double b)
        {
            return Math.Abs(a - b);
        }

        public static double Squared(this double x) => x * x;

        public static int IndexOf<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            var i = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return i;
                i++;
            }

            return -1;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> readOnlyList, T value)
        {
            var count = readOnlyList.Count;
            var equalityComparer = EqualityComparer<T>.Default;
            for (var i = 0; i < count; i++)
            {
                var current = readOnlyList[i];
                if (equalityComparer.Equals(current, value)) return i;
            }

            return -1;
        }

        public static T MinBy<T>(this IEnumerable<T> items, Func<T, IComparable> getKey)
        {
            var best = default(T);
            IComparable bestKey = null;
            var found = false;
            foreach (var item in items)
                if (!found || getKey(item).CompareTo(bestKey) < 0)
                {
                    best = item;
                    bestKey = getKey(best);
                    found = true;
                }

            return best;
        }

        public static double MaxOrDefault<T>(this IEnumerable<T> items, Func<T, double> getCost, double defaultValue)
        {
            var bestCost = double.NegativeInfinity;
            foreach (var item in items)
            {
                var cost = getCost(item);
                if (cost > bestCost)
                    bestCost = cost;
            }

            return double.IsNegativeInfinity(bestCost) ? defaultValue : bestCost;
        }

        public static T MaxBy<T>(this IEnumerable<T> items, Func<T, IComparable> getKey)
        {
            var best = default(T);
            IComparable bestKey = null;
            var found = false;
            foreach (var item in items)
                if (!found || getKey(item).CompareTo(bestKey) > 0)
                {
                    best = item;
                    bestKey = getKey(best);
                    found = true;
                }

            return best;
        }

        public static IList<T> AllMaxBy<T>(this IEnumerable<T> items, Func<T, double> getKey)
        {
            IList<T> result = null;
            double bestKey = double.MinValue;
            foreach (var item in items)
            {
                var itemKey = getKey(item);
                if (result == null || bestKey < itemKey)
                {
                    result = new List<T> { item };
                    bestKey = itemKey;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (bestKey == itemKey)
                {
                    result.Add(item);
                }
            }
            return result ?? Array.Empty<T>();
        }

        public static int BoundTo(this int v, int left, int right)
        {
            if (v < left) return left;
            if (v > right) return right;
            return v;
        }


        public static double ToDegrees(this double radians)
        {
            return 180 * radians / Math.PI;
        }

        public static double ToRadians(this double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToRadians(this int degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double BoundTo(this double v, double left, double right)
        {
            if (v < left) return left;
            if (v > right) return right;
            return v;
        }

        public static double TruncateAbs(this double v, double maxAbs)
        {
            if (v < -maxAbs) return -maxAbs;
            if (v > maxAbs) return maxAbs;
            return v;
        }

        public static int TruncateAbs(this int v, int maxAbs)
        {
            if (v < -maxAbs) return -maxAbs;
            if (v > maxAbs) return maxAbs;
            return v;
        }

        public static IEnumerable<T> Times<T>(this int count, Func<int, T> create)
        {
            return Enumerable.Range(0, count).Select(create);
        }

        public static IEnumerable<T> Times<T>(this int count, T item)
        {
            return Enumerable.Repeat(item, count);
        }

        public static T GetOrDefault<T>(this T[,] grid, int x, int y, T defaultValue = default)
        {
            if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1)) return defaultValue;
            return grid[x, y];
        }

        public static bool InRange(this int v, int min, int max)
        {
            return v >= min && v <= max;
        }

        public static bool InRange(this double v, double min, double max)
        {
            return v >= min && v <= max;
        }

        public static TV GetOrCreate<TK, TV>(this IDictionary<TK, TV> d, TK key, Func<TK, TV> create)
        {
            TV v;
            if (d.TryGetValue(key, out v)) return v;
            return d[key] = create(key);
        }

        public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> d, TK key, TV def = default)
        {
            TV v;
            if (d.TryGetValue(key, out v)) return v;
            return def;
        }

        public static void Increment<TK>(this IDictionary<TK, int> d, TK key)
        {
            if (d.TryGetValue(key, out var v))
                d[key] = v + 1;
            else
                d[key] = 1;
        }

        public static int ElementwiseHashcode<T>(this IEnumerable<T> items)
        {
            unchecked
            {
                return items.Select(t => t.GetHashCode()).Aggregate((res, next) => (res * 379) ^ next);
            }
        }

        public static List<T> Shuffle<T>(this IEnumerable<T> items, Random random)
        {
            var copy = items.ToList();
            for (var i = 0; i < copy.Count; i++)
            {
                var nextIndex = random.Next(i, copy.Count);
                (copy[nextIndex], copy[i]) = (copy[i], copy[nextIndex]);
            }

            return copy;
        }

        public static double NormAngleInRadians(this double angle)
        {
            angle %= 2*Math.PI;
            if (angle < 0) angle += 2*Math.PI;
            if (angle > Math.PI) angle -= 2*Math.PI;
            return angle;
        }

        public static int ToInt(this string s)
        {
            return int.Parse(s);
        }

        public static long ToLong(this string s)
        {
            return long.Parse(s);
        }

        public static string StrJoin<T>(this IEnumerable<T> items, string delimiter)
        {
            return string.Join(delimiter, items);
        }
        
        public static string StrJoin<T>(this IEnumerable<T> items, char delimiter)
        {
            return string.Join(delimiter, items);
        }

        public static string StrJoin<T>(this IEnumerable<T> items, string delimiter, Func<T, string> toString)
        {
            return items.Select(toString).StrJoin(delimiter);
        }
        
        public static bool IsOneOf<T>(this T item, params T[] set)
        {
            return set.IndexOf(item) >= 0;
        }

        public static string ToCompactString(this double x)
        {
            if (Math.Abs(x) > 100) return x.ToString("0", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 10) return x.ToString("0.#", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 1) return x.ToString("0.##", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 0.1) return x.ToString("0.###", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 0.01) return x.ToString("0.####", CultureInfo.InvariantCulture);
            return x.ToString(CultureInfo.InvariantCulture);
        }
        
    }

    public static class RandomExtensions
    {
        public static T GetRandomBest<T>(this IEnumerable<T> items, Func<T, double> getKey, Random random)
        {
            return items.AllMaxBy(getKey).SelectOne(random);
        }

        public static T SelectOne<T>(this IEnumerable<T> items, Random random)
        {
            if (!(items is ICollection<T> col))
                col = items.ToList();
            if (col.Count == 0) return default;
            var index = random.Next(col.Count);
            if (col is IList<T> list) return list[index];
            if (col is IReadOnlyList<T> roList) return roList[index];
            return col.ElementAt(index);
        }

        public static T[] Sample<T>(this Random r, IList<T> list, int sampleSize)
        {
            var sample = new T[sampleSize];
            for (var i = 0; i < sampleSize; i++)
                sample[i] = list[r.Next(list.Count)];
            return sample;
        }
        
        public static bool Chance(this Random r, double probability)
        {
            return r.NextDouble() < probability;
        }

        public static ulong NextUlong(this Random r)
        {
            var a = unchecked((ulong) r.Next());
            var b = unchecked((ulong) r.Next());
            return (a << 32) | b;
        }

        public static double NextDouble(this Random r, double min, double max)
        {
            return r.NextDouble() * (max - min) + min;
        }
    }
}
