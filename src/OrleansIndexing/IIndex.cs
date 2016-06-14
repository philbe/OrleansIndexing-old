using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This interface defines the functionality
    /// that is required for an index implementation.
    /// </summary>
    public interface IIndex : IGrainWithStringKey
    {
        /// <summary>
        /// This method applies a given update to the current index.
        /// </summary>
        /// <param name="updatedGrain">the grain that issued the update</param>
        /// <param name="iUpdate">contains the data for the update</param>
        /// <returns>true, if the index update was successful, otherwise false</returns>
        Task<bool> ApplyIndexUpdate(IGrain updatedGrain, Immutable<IMemberUpdate> iUpdate);

        /// <summary>
        /// Each index has an IIndexUpdateGenerator specific to it, which this method returns.
        /// </summary>
        /// <returns>the IIndexUpdateGenerator instance of the current index.</returns>
        Task<IIndexUpdateGenerator> GetIndexUpdateGenerator();
    }

    /// <summary>
    /// This is the typed variant of IIndex, which is assumed to be 
    /// the root interface for the index implementations.
    /// </summary>
    public interface IIndex<K,V> : IIndex where V : IGrain
    {
        /// <summary>
        /// This method retrieves the result of a lookup into the hash-index
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        Task<IEnumerable<V>> Lookup(Immutable<K> key);
    }
}
