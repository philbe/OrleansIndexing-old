using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for HashIndexPartitionedPerKeyBucket<K, V> grain,
    /// which is created in order to guide Orleans to find the grain instances
    /// more efficiently.
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    public interface HashIndexPartitionedPerKeyBucketInterface<K, V> : HashIndexInterface<K, V> where V : IIndexableGrain
    {

        /// <summary>
        /// This method applies a given update to the current index.
        /// </summary>
        /// <param name="updatedGrain">the grain that issued the update</param>
        /// <param name="iUpdate">contains the data for the update</param>
        /// <param name="isUnique">whether this is a unique index that we are updating</param>
        /// <param name="op">the actual type of the operation, which override the operation-type in iUpdate</param>
        /// <returns>true, if the index update was successful, otherwise false</returns>
        Task<bool> ApplyIndexUpdate(IIndexableGrain updatedGrain, IMemberUpdate iUpdate, bool isUnique, IndexOperationType op);
    }
}
