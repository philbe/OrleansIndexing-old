using System.Threading.Tasks;
using Orleans;
using System.Collections.Generic;
using System;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// Grain interface IActiveGrainEnumeratorGrain
    /// </summary>
	public interface IActiveGrainEnumeratorGrain : IGrainWithIntegerKey
    {
        /// <summary>
        /// Enumerates grains of type grainTypeName
        /// </summary>
        /// <param name="grainTypeName">Name of the grain type to enumerate</param>
        /// <returns></returns>
        Task<List<Guid>> GetActiveGrains(string grainTypeName);

    }
}
