using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The grain interface for the index finder grain.
    /// </summary>
    public interface IIndexFinder : IGrainWithStringKey
    {
        /// <summary>
        /// finds the index with the given name in the cached
        /// indexes from index handler
        /// </summary>
        /// <param name="indexName">name of the index</param>
        /// <returns>the requested index</returns>
        Task<IIndex> GetIndex(Type iGrainType, string indexName);
    }
}
