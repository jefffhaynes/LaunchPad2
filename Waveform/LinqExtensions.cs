using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Waveform
{
    public static class LinqExtensions
    {
        internal static Tuple<T, T> MinMax<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            Comparer<T> comparer = Comparer<T>.Default;

            using (IEnumerator<T> sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }

                T max = sourceIterator.Current;
                T min = sourceIterator.Current;

                while (sourceIterator.MoveNext())
                {
                    T candidate = sourceIterator.Current;

                    if (comparer.Compare(candidate, min) < 0)
                        min = candidate;
                    else if (comparer.Compare(candidate, max) > 0)
                        max = candidate;
                }

                return new Tuple<T, T>(min, max);
            }
        }

        //public static IEnumerable<TSource> TakeEvery<TSource>(this IEnumerable<TSource> source, int step)
        //{
        //    if (source == null) throw new ArgumentNullException("source");
        //    if (step <= 0) throw new ArgumentOutOfRangeException("step");
        //    return source.Where((e, i) => i % step == 0);
        //}

        /// <summary>
        ///     Processes a sequence into a series of subsequences representing a windowed subset of the original
        /// </summary>
        /// <remarks>
        ///     This operator is guaranteed to return at least one result, even if the source sequence is smaller
        ///     than the window size.<br />
        ///     The number of sequences returned is: <c>Max(0, sequence.Count() - windowSize) + 1</c><br />
        ///     Returned subsequences are buffered, but the overall operation is streamed.<br />
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements of the source sequence</typeparam>
        /// <param name="source">The sequence to evaluate a sliding window over</param>
        /// <param name="size">The size (number of elements) in each window</param>
        /// <returns>A series of sequences representing each sliding window subsequence</returns>
        /// <summary>
        ///     Batches the source sequence into sized buckets.
        /// </summary>
        /// <typeparam name="TSource">Type of elements in <paramref name="source" /> sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="size">Size of buckets.</param>
        /// <returns>A sequence of equally sized buckets containing elements of the source collection.</returns>
        /// <remarks> This operator uses deferred execution and streams its results (buckets and bucket content).</remarks>
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            return Batch(source, size, x => x);
        }

        /// <summary>
        ///     Batches the source sequence into sized buckets and applies a projection to each bucket.
        /// </summary>
        /// <typeparam name="TSource">Type of elements in <paramref name="source" /> sequence.</typeparam>
        /// <typeparam name="TResult">Type of result returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="size">Size of buckets.</param>
        /// <param name="resultSelector">The projection to apply to each bucket.</param>
        /// <returns>A sequence of projections on equally sized buckets containing elements of the source collection.</returns>
        /// <remarks> This operator uses deferred execution and streams its results (buckets and bucket content).</remarks>
        public static IEnumerable<TResult> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size,
            Func<IEnumerable<TSource>, TResult> resultSelector)
        {
            source.ThrowIfNull("source");
            size.ThrowIfNonPositive("size");
            resultSelector.ThrowIfNull("resultSelector");
            return BatchImpl(source, size, resultSelector);
        }

        private static IEnumerable<TResult> BatchImpl<TSource, TResult>(this IEnumerable<TSource> source, int size,
            Func<IEnumerable<TSource>, TResult> resultSelector)
        {
            Debug.Assert(source != null);
            Debug.Assert(size > 0);
            Debug.Assert(resultSelector != null);

            TSource[] bucket = null;
            int count = 0;

            foreach (TSource item in source)
            {
                if (bucket == null)
                {
                    bucket = new TSource[size];
                }

                bucket[count++] = item;

                // The bucket is fully buffered before it's yielded
                if (count != size)
                {
                    continue;
                }

                // Select is necessary so bucket contents are streamed too
                yield return resultSelector(bucket.Select(x => x));

                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                yield return resultSelector(bucket.Take(count));
            }
        }

        internal static IEnumerable<T> Interleave<T>(
            this IEnumerable<T> first,
            IEnumerable<T> second)
        {
            using (IEnumerator<T>
                enumerator1 = first.GetEnumerator(),
                enumerator2 = second.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    yield return enumerator1.Current;
                    if (enumerator2.MoveNext())
                        yield return enumerator2.Current;
                }
            }
        }
    }
}