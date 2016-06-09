using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// Defines the interface for hash-indexes
    /// </summary>
    /// <typeparam name="K">the type of key for the hash index</typeparam>
    /// <typeparam name="V">the type of grain interface that is
    /// being indexed</typeparam>
    public interface IHashIndex<K,V> : IIndex where V : IGrain
    {
        /// <summary>
        /// hash-indexes can be either unique or non-unique.
        /// If the user defines a hash-index as a unique hash-index,
        /// then we want to guarantee that uniqueness. This method
        /// determines whether this hash-index is a unique hash-index.
        /// </summary>
        /// <returns>true, if there should be a single grain
        /// associated with each key, otherwise false</returns>
        Task<bool> IsUnique();

        /// <summary>
        /// This method retrieves the result of a lookup into the hash-index
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        Task<IEnumerable<V>> Lookup(Immutable<K> key);
    }
}
