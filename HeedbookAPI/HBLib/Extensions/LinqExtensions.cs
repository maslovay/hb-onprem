using System;
using System.Collections.Generic;
using System.Linq;

namespace HBLib.Extensions
{
    public static class LinqExtensions
    {
        public static T MinBy<T>(this IEnumerable<T> source, Func<T, IComparable> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return source.Aggregate((min, cur) =>
            {
                if (min == null) return cur;
                var minComparer = selector(min);
                if (minComparer == null) return cur;
                var curComparer = selector(cur);
                if (curComparer == null) return min;
                return minComparer.CompareTo(curComparer) > 0 ? cur : min;
            });
        }


        public static T MaxBy<T>(this IEnumerable<T> source, Func<T, IComparable> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return source.Aggregate((max, cur) =>
            {
                if (max == null) return cur;
                var maxComparer = selector(max);
                if (maxComparer == null) return cur;
                var curComparer = selector(cur);
                if (curComparer == null) return max;
                return maxComparer.CompareTo(curComparer) < 0 ? cur : max;
            });
        }
    }
}