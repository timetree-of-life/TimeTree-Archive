using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;

namespace TopoTime
{

    public static class LinqExtension
    {
        public static double Median(this IEnumerable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.OrderBy(n => n).ToArray();
            if (data.Length == 0)
                throw new InvalidOperationException();
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;
            return data[data.Length / 2];
        }

        public static double? Median(this IEnumerable<int?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.Where(n => n.HasValue).Select(n => n.Value).OrderBy(n => n).ToArray();
            if (data.Length == 0)
                return null;
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;
            return data[data.Length / 2];
        }

        public static double Median(this IEnumerable<long> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.OrderBy(n => n).ToArray();
            if (data.Length == 0)
                throw new InvalidOperationException();
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;
            return data[data.Length / 2];
        }

        public static double? Median(this IEnumerable<long?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.Where(n => n.HasValue).Select(n => n.Value).OrderBy(n => n).ToArray();
            if (data.Length == 0)
                return null;
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;
            return data[data.Length / 2];
        }

        public static float Median(this IEnumerable<float> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.OrderBy(n => n).ToArray();
            if (data.Length == 0)
                throw new InvalidOperationException();
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0f;
            return data[data.Length / 2];
        }

        public static float? Median(this IEnumerable<float?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.Where(n => n.HasValue).Select(n => n.Value).OrderBy(n => n).ToArray();
            if (data.Length == 0)
                return null;
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0f;
            return data[data.Length / 2];
        }

        public static double Median(this IEnumerable<double> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.OrderBy(n => n).ToArray();
            if (data.Length == 0)
                throw new InvalidOperationException();
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;
            return data[data.Length / 2];
        }

        public static double? Median(this IEnumerable<double?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.Where(n => n.HasValue).Select(n => n.Value).OrderBy(n => n).ToArray();
            if (data.Length == 0)
                return null;
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0;
            return data[data.Length / 2];
        }

        public static decimal Median(this IEnumerable<decimal> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.OrderBy(n => n).ToArray();
            if (data.Length == 0)
                throw new InvalidOperationException();
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0m;
            return data[data.Length / 2];
        }

        public static decimal? Median(this IEnumerable<decimal?> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var data = source.Where(n => n.HasValue).Select(n => n.Value).OrderBy(n => n).ToArray();
            if (data.Length == 0)
                return null;
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2.0m;
            return data[data.Length / 2];
        }

        public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Median();
        }

        public static double? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Median();
        }

        public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Median();
        }

        public static double? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Median();
        }

        public static float Median<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Median();
        }

        public static float? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Median();
        }

        public static double Median<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Median();
        }

        public static double? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Median();
        }

        public static decimal Median<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Median();
        }

        public static decimal? Median<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Median();
        }

        public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }


        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, null);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer = comparer ?? Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var min = sourceIterator.Current;
                var minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }

    public static IEnumerable<T> Traverse<T>(this T item, Func<T, T> childSelector)
        {
            var stack = new Stack<T>(new T[] { item });

            while (stack.Any())
            {
                var next = stack.Pop();
                if (next != null)
                {
                    yield return next;
                    stack.Push(childSelector(next));
                }
            }
        }

        public static IEnumerable<T> Traverse<T>(this T item, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(new T[] { item });

            while (stack.Any())
            {
                var next = stack.Pop();
                //if(next != null)
                //{
                yield return next;
                foreach (var child in childSelector(next))
                {
                    stack.Push(child);
                }
                //}
            }
        }

        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items,
          Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }
    }
}
