using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// Defines the interface for range indexes
    /// </summary>
    /// <typeparam name="K">the type of indexed attribute for
    /// the range index</typeparam>
    /// <typeparam name="V">the type of grain interface that is
    /// being indexed</typeparam>
    public interface IRangeIndex<K,V> : IIndex where V : IGrain
    {
        /// <summary>
        /// Given the bounds, this method retrieves the result of
        /// a lookup into the range index.
        /// </summary>
        /// <param name="from">the lower bound of the range</param>
        /// <param name="to">the upper bound of the range</param>
        /// <returns>the result of lookup</returns>
        Task<IEnumerable<V>> LookupRange(K from, K to);

        /// <summary>
        /// Given the lower bound, this method retrieves the result of
        /// a lookup into the range index.
        /// </summary>
        /// <param name="from">the lower bound of the range</param>
        /// <returns>the result of lookup</returns>
        Task<IEnumerable<V>> LookupFromRange(K from);

        /// <summary>
        /// Given the upper bound, this method retrieves the result of
        /// a lookup into the range index.
        /// </summary>
        /// <param name="to">the upper bound of the range</param>
        /// <returns>the result of lookup</returns>
        Task<IEnumerable<V>> LookupToRange(K to);

        /// <summary>
        /// Each range index requires a comparison function and user
        /// can provide this comparison function via a call to this method.
        /// 
        /// This method should be used internally by the index grain and
        /// should not be invoked from other grains.
        /// </summary>
        /// <param name="compLambda">the comparison function</param>
        //void SetComparisonLambda(Func<K, long> compLambda);
    }
}
