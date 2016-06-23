using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public static class OrleansQueryableExtensions
    {

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IOrleansQueryable<T> Where<T>(this IOrleansQueryable<T> source, Expression<Func<T, bool>> predicate) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.Where(source, predicate);
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IOrleansQueryable<T> Where<T>(this IOrleansQueryable<T> source, Expression<Func<T, int, bool>> predicate) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.Where(source, predicate);
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        public static IOrleansQueryable<T> OrderBy<T, TK>(this IOrleansQueryable<T> source, Expression<Func<T, TK>> keySelector) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.OrderBy(source, keySelector);
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        public static IOrleansQueryable<T> OrderBy<T, TK>(this IOrleansQueryable<T> source, Expression<Func<T, TK>> keySelector, IComparer<TK> comparer) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.OrderBy(source, keySelector, comparer);
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        public static IOrleansQueryable<T> OrderByDescending<T, TK>(this IOrleansQueryable<T> source, Expression<Func<T, TK>> keySelector) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.OrderByDescending(source, keySelector);
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        public static IOrleansQueryable<T> OrderByDescending<T, TK>(this IOrleansQueryable<T> source, Expression<Func<T, TK>> keySelector, IComparer<TK> comparer) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.OrderByDescending(source, keySelector, comparer);
        }

        /// <summary>
        /// Sorts(secondary) the elements of a sequence in ascending order according to a key.
        /// </summary>
        public static IOrleansQueryable<T> ThenBy<T, TK>(this IOrleansQueryable<T> source, Expression<Func<T, TK>> keySelector) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.ThenBy(source, keySelector);
        }


        /// <summary>
        /// Sorts(secondary) the elements of a sequence in descending order according to a key.
        /// </summary>
        public static IOrleansQueryable<T> ThenByDescending<T, TK>(this IOrleansQueryable<T> source, Expression<Func<T, TK>> keySelector) where T : IIndexableGrain
        {
            return (IOrleansQueryable<T>)Queryable.ThenByDescending(source, keySelector);
        }


        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IOrleansQueryable<TResult> Select<TSource, TResult>(this IOrleansQueryable<TSource> source, Expression<Func<TSource, TResult>> selector) where TSource : IIndexableGrain where TResult : IIndexableGrain
        {
            return (IOrleansQueryable<TResult>)Queryable.Select(source, selector);
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IOrleansQueryable<TResult> Select<TSource, TResult>(this IOrleansQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector) where TSource : IIndexableGrain where TResult : IIndexableGrain
        {
            return (IOrleansQueryable<TResult>)Queryable.Select(source, selector);
        }

        /// <summary>
        /// Implementation of In operator
        /// </summary>
        public static bool In<T>(this T field, IEnumerable<T> values)
        {
            return values.Any(value => field.Equals(value));
        }

        /// <summary>
        /// Implementation of In operator
        /// </summary>
        public static bool In<T>(this T field, params T[] values)
        {
            return values.Any(value => field.Equals(value));
        }

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
        /// </summary>
        /// Summary:
        public static IOrleansQueryable<TSource> Skip<TSource>(this IOrleansQueryable<TSource> source, int count) where TSource : IIndexableGrain
        {
            return (IOrleansQueryable<TSource>)Queryable.Skip(source, count);
        }
        public static IOrleansQueryable<TSource> Take<TSource>(this IOrleansQueryable<TSource> source, int count) where TSource : IIndexableGrain
        {
            return (IOrleansQueryable<TSource>)Queryable.Take(source, count);
        }

        /// <summary>
        /// Implementation of the Contains ANY operator
        /// </summary>
        public static bool ContainsAny<T>(this IEnumerable<T> list, IEnumerable<T> items)
        {
            throw new InvalidOperationException(
                "This method isn't meant to be called directly, it just exists as a place holder, for the LINQ provider");
        }

        /// <summary>
        /// Implementation of the Contains ALL operatior
        /// </summary>
        public static bool ContainsAll<T>(this IEnumerable<T> list, IEnumerable<T> items)
        {
            throw new InvalidOperationException(
                "This method isn't meant to be called directly, it just exists as a place holder for the LINQ provider");
        }
    }
}
