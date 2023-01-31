using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;

namespace TimeTreeShared
{
    static class Functions
    {
        public static double RoundToSignificantDigits(this double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }

        public static double RoundByMagnitude(this double d, int digits = 1)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

            int magnitude = (int)Math.Log10(d);
            int final = Math.Max(digits - magnitude, 1);

            return scale * Math.Round(d / scale, final, MidpointRounding.ToEven);


        }

        public static double RoundByLevel(this double d, int level)
        {
            int magnitude = 0;
            if (level > 100)
                magnitude = 1;
            else if (level > 250)
                magnitude = 2;
            else if (level > 500)
                magnitude = 3;

            int digits = 3;

            return Math.Round(d, digits - magnitude);
        }

        public static double Mean(List<double> values)
        {
            return values.Average();
        }

        public static double StdDev(List<double> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }

        public static Tuple<double, double> TConfidenceInterval(List<double> samples, double interval)
        {
            double theta = (interval + 1.0) / 2;
            double mean = samples.Mean();
            double sd = samples.StandardDeviation();
            double T = StudentT.InvCDF(0, 1, samples.Count - 1, theta);
            double t = T * (sd / Math.Sqrt(samples.Count));
            return Tuple.Create(mean - t, mean + t);
        }

        public static Tuple<double, double> MedianConfidenceInterval(List<double> samples)
        {
            IEnumerable<double> sortedSamples = samples.OrderBy(x => x);

            if (samples.Count() <= 3)
                return Tuple.Create(sortedSamples.First(), sortedSamples.Last());

            double z = 1.96;
            double q = 0.5;
            double n = samples.Count;

            double interval = z * Math.Sqrt(n * q * (1 - q));
            int j = (int)(Math.Round((n * q) - interval + 0.5) - 1);
            int k = (int)(Math.Round((n * q) + interval + 0.5) - 1);

            return Tuple.Create(sortedSamples.ElementAt(j), sortedSamples.ElementAt(k));
        }

        public static string ShortName(string TaxonName)
        {
            if (TaxonName.Length > 0 && TaxonName.Contains(" "))
                return TaxonName[0] + ". " + TaxonName.Substring(TaxonName.IndexOf(" "));
            return TaxonName;
        }

        public static string TruncateSubspecies(string TaxonName)
        {
            int SecondSpaceIndex = GetNthIndex(TaxonName, ' ', 2);

            if (SecondSpaceIndex > 0)
                return TaxonName.Substring(0, SecondSpaceIndex);
            else
            {
                int BracketIndex = GetNthIndex(TaxonName, '[', 1);
                if (BracketIndex > 0)
                    return TaxonName.Substring(0, BracketIndex - 1);
            }

            return TaxonName;
        }

        public static int GetNthIndex(string s, char t, int n)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == t)
                {
                    count++;
                    if (count == n)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

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

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector)
        {
            return source.MaxBy(selector, null);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
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
                var max = sourceIterator.Current;
                var maxKey = selector(max);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, maxKey) > 0)
                    {
                        max = candidate;
                        maxKey = candidateProjected;
                    }
                }
                return max;
            }
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
