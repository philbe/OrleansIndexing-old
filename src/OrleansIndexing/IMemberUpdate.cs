using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// IMemberUpdate encapsulates the information related to a grain update 
    /// with respect to a specific index
    /// </summary>
    public interface IMemberUpdate
    {
        /// <summary>
        /// Produces the after-image of the grain, after applying this update
        /// </summary>
        /// <returns>the after-image of the grain, after applying this update</returns>
        object GetAftImg();

        /// <summary>
        /// If this is a null update, that is, no change was made by this update,
        /// then this method should return false
        /// </summary>
        /// <returns>false, if no update is happened, otherwise true</returns>
        bool IsUpdated();
    }
}
