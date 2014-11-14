using System;
using System.Collections.Generic;

namespace LaunchPad2
{
    public static class LinqExtensions
    {
        public static double StdDev(this IEnumerable<double> source)
        {
            double average;
            return source.StdDev(out average);
        }

        public static double StdDev(this IEnumerable<double> source, out double average)
        {
            average = 0.0;
            double sum = 0.0;
            double stdDev = 0.0;
            int n = 0;

            foreach (double val in source)
            {
                n++;
                double delta = val - average;
                average += delta / n;
                sum += delta * (val - average);
            }

            if (1 < n)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;
        }

        public static IEnumerable<IEnumerable<TSource>> BatchSimilar<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if(source == null)
                throw new ArgumentNullException("source");

            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                var bucket = new List<TSource>();

                if (!enumerator.MoveNext())
                    yield return bucket;

                TSource prev = enumerator.Current;
                bucket.Add(prev);
                while (enumerator.MoveNext())
                {
                    var prevKey = keySelector(prev);
                    var currentKey = keySelector(enumerator.Current);
                    if (!currentKey.Equals(prevKey))
                    {
                        yield return bucket;
                        bucket = new List<TSource>();
                    }

                    bucket.Add(enumerator.Current);
                    prev = enumerator.Current;
                }

                yield return bucket;
            }
        }
    }
}
