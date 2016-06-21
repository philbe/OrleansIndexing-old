using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public enum IndexStatus { /*Created,*/ UnderConstruction, Available }

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
        Task<bool> ApplyIndexUpdate(IIndexableGrain updatedGrain, Immutable<IMemberUpdate> iUpdate);
        
        /// <summary>
        /// Disposes of the index and removes all the data stored
        /// for the index. This method is called before removing
        /// the index from index registry
        /// </summary>
        Task Dispose();

        /// <summary>
        /// Determines whether the index is available for lookup
        /// </summary>
        Task<bool> IsAvailable();
    }

    /// <summary>
    /// This is the typed variant of IIndex, which is assumed to be 
    /// the root interface for the index implementations.
    /// </summary>
    public interface IIndex<K,V> : IIndex where V : IIndexableGrain
    {
        /// <summary>
        /// This method retrieves the result of a lookup into the hash-index
        /// </summary>
        /// <param name="key">the lookup key</param>
        /// <returns>the result of lookup into the hash-index</returns>
        Task<IEnumerable<V>> Lookup(K key);
    }
}
