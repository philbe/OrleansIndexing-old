using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// IMemberUpdate is an encapsulation of the information
    /// related to an update to a single grain in relation with
    /// a specific index
    /// </summary>
    public interface IMemberUpdate
    {
        /// <summary>
        /// Produces the after image of the grain, after applying this update
        /// </summary>
        /// <returns>the after image of the grain, after applying this update</returns>
        object GetAftImg();

        /// <summary>
        /// If there was no changes happened in this update,
        /// this method should return false
        /// </summary>
        /// <returns>false, if no update is happened, otherwise true</returns>
        bool IsUpdated();
    }
}
