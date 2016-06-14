using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The grain interface for the index registry grain.
    /// </summary>
    public interface IIndexRegistry : IGrainWithStringKey
    {
        /// <summary>
        /// Exposes the indexes defined under this index registry
        /// </summary>
        /// <returns>the dictionary from indexID to index grain for all
        /// the indexes defined under this index registry</returns>
        Task<IDictionary<string, IIndex>> GetIndexes();

        /// <summary>
        /// Registers a new index in this index registry
        /// </summary>
        /// <param name="indexName">name of the index</param>
        /// <param name="index">a reference to the index grain</param>
        Task<bool> RegisterIndex(string indexName, IIndex index);
    }

    /// <summary>
    /// The grain interface for the index registry grain,
    /// which contains the indexes for a single grain.
    /// </summary>
    public interface IIndexRegistry<T> : IIndexRegistry where T : IGrain
    {
    }
}
