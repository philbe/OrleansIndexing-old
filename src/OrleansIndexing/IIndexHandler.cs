using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The grain interface for the index handler grain.
    /// </summary>
    public interface IIndexHandler : IGrainWithStringKey
    {
        /// <summary>
        /// This method is responsible for applying the updates received by an
        /// index handler to all of the indexes that it controls
        /// </summary>
        /// <param name="updatedGrain">the grain that issued the update</param>
        /// <param name="updates">the immutable map of index updates for each index,
        /// which maps each indexID to an update</param>
        /// <returns>false, if the index handler controls an index for which there 
        /// was no update in 'updates' or if 'updates' contains an update for a
        /// nonexistent index. Otherwise true</returns>
        Task<bool> ApplyIndexUpdates(IIndexableGrain updatedGrain, Immutable<IDictionary<string, IMemberUpdate>> updates);

        /// <summary>
        /// Exposes the index operations for the indexes handled by this index handler
        /// </summary>
        /// <returns>the dictionary from indexID to index operation for all
        /// the  indexes controlled by this index handler</returns>
        Task<Immutable<IDictionary<string, IIndexUpdateGenerator>>> GetIndexUpdateGenerators();

        /// <summary>
        /// Exposes the indexes handled by this index handler
        /// </summary>
        /// <returns>the dictionary from indexID to index grain for all
        /// the indexes controlled by this index handler</returns>
        Task<Immutable<IDictionary<string, Tuple<IIndex, IndexMetaData>>>> GetIndexes();

        /// <summary>
        /// Reloads the list of indexes by looking into the index registry
        /// </summary>
        Task ReloadIndexes();
    }

    /// <summary>
    /// The grain interface for the index handler grain,
    /// which indexes a single grain.
    /// </summary>
    public interface IIndexHandler<T> : IIndexHandler where T : IGrain
    {
    }
}
