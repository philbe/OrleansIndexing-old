using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The grain interface for the IndexableGrain grain.
    /// </summary>
    public interface IIndexableGrain : IGrain
    {
        /// <summary>
        /// Extracts the corresponding image of grain for a particular index
        /// identified by iUpdateGen.
        /// 
        /// IIndexUpdateGenerator should always be applied inside the grain
        /// implementation, as it might contain blocking code, and it is not
        /// intended to be called externally.
        /// </summary>
        /// <param name="iUpdateGen">IIndexUpdateGenerator for a particular index</param>
        /// <returns>the corresponding image of grain for a particular index</returns>
        Task<object> ExtractIndexImage(IIndexUpdateGenerator iUpdateGen);
    }
}
