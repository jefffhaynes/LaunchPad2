using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;

namespace FMOD
{
    public static class MoreEnumerable
    {
        /// <summary>
        /// Batches the source sequence into sized buckets.
        /// </summary>
        /// <typeparam name="TSource">Type of elements in <paramref name="source"/> sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="size">Size of buckets.</param>
        /// <returns>A sequence of equally sized buckets containing elements of the source collection.</returns>
        /// <remarks> This operator uses deferred execution and streams its results (buckets and bucket content).</remarks>
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            return Batch(source, size, x => x);
        }

        /// <summary>
        /// Batches the source sequence into sized buckets and applies a projection to each bucket.
        /// </summary>
        /// <typeparam name="TSource">Type of elements in <paramref name="source"/> sequence.</typeparam>
        /// <typeparam name="TResult">Type of result returned by <paramref name="resultSelector"/>.</typeparam>
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
            var count = 0;

            foreach (var item in source)
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
        public static IEnumerable<TSource> TakeEvery<TSource>(this IEnumerable<TSource> source, int step)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
            return source.Where((e, i) => i % step == 0);
        }

        public static IEnumerable<IEnumerable<TSource>> Window<TSource>(this IEnumerable<TSource> source, int size)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            return WindowedImpl(source, size);
        }

        private static IEnumerable<IEnumerable<TSource>> WindowedImpl<TSource>(this IEnumerable<TSource> source,
            int size)
        {
            using (IEnumerator<TSource> iter = source.GetEnumerator())
            {
                // generate the first window of items
                int countLeft = size;
                var window = new List<TSource>();
                // NOTE: The order of evaluation in the if() below is important
                //       because it relies on short-circuit behavior to ensure
                //       we don't move the iterator once the window is complete
                while (countLeft-- > 0 && iter.MoveNext())
                {
                    window.Add(iter.Current);
                }

                // return the first window (whatever size it may be)
                yield return window;

                // generate the next window by shifting forward by one item
                while (iter.MoveNext())
                {
                    // NOTE: If we used a circular queue rather than a list, 
                    //       we could make this quite a bit more efficient.
                    //       Sadly the BCL does not offer such a collection.
                    window = new List<TSource>(window.Skip(1)) { iter.Current };
                    yield return window;
                }
            }
        }

        public static IEnumerable<TResult> ZipMany<TSource, TResult>(this IEnumerable<IEnumerable<TSource>> source, Func<IEnumerable<TSource>, TResult> resultSelector)
        {
            var enumerators = source.Select(item => item.GetEnumerator()).ToArray();

            while (enumerators.All(x => x.MoveNext()))
            {
                yield return resultSelector(enumerators.Select(x => x.Current).ToArray());
            }
        }
    }
}
