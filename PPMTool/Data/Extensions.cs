using System;
using System.Collections.Generic;
using System.Linq;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    public static class Extensions
    {
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
            comparer ??= Comparer<TKey>.Default;

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
            comparer ??= Comparer<TKey>.Default;

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

        /// <summary>
        /// Find the sum of a value in a collection, rounded to a specified number of decimal places.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static double RoundedSum<TSource>(this IEnumerable<TSource> source,
            Func<TSource, double> selector, int decimalPlaces = 3)
        {
            return Math.Round(source.Sum(selector), decimalPlaces);
        }

        /// <summary>
        /// Method to extract a suitable financial reference from a list of references given a financial year
        /// </summary>
        /// <param name="list"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If no suitable references can be found</exception>
        public static FinancialReference GetSuitableFinancialReference(this IEnumerable<FinancialReference> list, int year)
        {
            // Try find matching reference
            var match = list.FirstOrDefault(x => x.FinancialYear == year);

            // If not then look for the earliest
            if (match == null)
            {
                match = list.Where(x => x.FinancialYear < year).OrderByDescending(x => x.FinancialYear).FirstOrDefault();
            }

            // If not then use the next latest
            if (match == null)
            {
                match = list.Where(x => x.FinancialYear > year).OrderBy(x => x.FinancialYear).FirstOrDefault();
            }

            // If there are no matches then there are no references so throw exception
            if (match == null)
            {
                throw new Exception("No suitable financial references can be found!");
            }

            return match;
        }

        /// <summary>
        /// Method to extract a suitable financial reference from a list of references given a date
        /// </summary>
        /// <param name="list"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static FinancialReference GetSuitableFinancialReference(this IEnumerable<FinancialReference> list, DateTime date)
        {
            // Get financial year from date
            int year = FinancialReference.GetFinancialYear(date);

            // Call the other method
            return GetSuitableFinancialReference(list, year);
        }
    }
}
