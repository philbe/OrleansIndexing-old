using Orleans;
using Orleans.Concurrency;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// This interface defines the functionality
    /// that is required for an index implementation.
    /// </summary>
    public interface IIndex : IGrain
    {
        /// <summary>
        /// This method applies a given update to the current index.
        /// </summary>
        /// <param name="iUpdate">contains the data for the update</param>
        /// <returns>true, if the index update was successful, otherwise false</returns>
        Task<bool> ApplyIndexUpdate(Immutable<IMemberUpdate> iUpdate);

        /// <summary>
        /// Each index has an IIndexOps specific to it, which this method returns.
        /// </summary>
        /// <returns>the IIndexOps instance of the current index.</returns>
        Task<IIndexOps> GetIndexOps();
    }
}
