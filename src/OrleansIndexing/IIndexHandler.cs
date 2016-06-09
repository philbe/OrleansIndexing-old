using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// The grain interface for the index handler grain.
    /// </summary>
    public interface IIndexHandler : IGrainWithStringKey
    {
        /// <summary>
        /// This method is responsible for applying the updates received by an
        /// index handlerto all the indexes that are controlled by it
        /// </summary>
        /// <param name="updates">the immutable map of index updates for each index,
        /// which maps indexID to the actual update</param>
        /// <returns>false, if there was any change in the list of indexes of the
        /// index handler compared to the list of updates, otherwise true</returns>
        Task<bool> ApplyIndexUpdates(Immutable<IDictionary<string, IMemberUpdate>> updates);

        /// <summary>
        /// Exposes the index operations for the indexes handled by this index handler
        /// </summary>
        /// <returns>the dictionary from indexID to index operations for all
        /// the existing indexes handled by this index handler</returns>
        Task<Immutable<IDictionary<string, IIndexOps>>> GetIndexOps();

        /// <summary>
        /// Exposes the indexes handled by this index handler
        /// </summary>
        /// <returns>the dictionary from indexID to index grain for all
        /// the existing indexes handled by this index handler</returns>
        Task<Immutable<IDictionary<string, IIndex>>> GetIndexes();
    }
}
