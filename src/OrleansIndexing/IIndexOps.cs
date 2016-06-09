using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// This interface specifies the functionality that each index
    /// should define for extracting the part of the state of the
    /// grain that it has an interest in it, and also creating the
    /// update objects after an update happens on the indexed grain
    /// </summary>
    public interface IIndexOps
    {
        /// <summary>
        /// Extracts some part of the state of the grain that this
        /// index is interested in it
        /// </summary>
        /// <param name="indexedGrain">the grain that we want to
        /// extract some part of its state to be indexed</param>
        /// <returns>an encapsulation of some part of the state of
        /// the grain that this index is interested in it</returns>
        object ExtractIndexImage(Grain indexedGrain);

        /// <summary>
        /// Creates an update object after receiving the current state of the grain
        /// and an earlier image of the grain 
        /// </summary>
        /// <param name="indexedGrain">the grain that we want to
        /// extract some part of its state to be indexed</param>
        /// <param name="beforeImage">the before image of the indexedGrain,
        /// which was captured earlier via a call to ExtractIndexImage(indexedGrain)</param>
        /// <returns>an IMemberUpdate instance that contains the update information</returns>
        IMemberUpdate CreateMemberUpdate(Grain indexedGrain, object beforeImage);
    }
}
