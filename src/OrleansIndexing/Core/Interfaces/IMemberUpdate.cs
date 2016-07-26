using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public enum OperationType { None, Insert, Update, Delete };
    /// <summary>
    /// IMemberUpdate encapsulates the information related to a grain update 
    /// with respect to a specific index
    /// </summary>
    public interface IMemberUpdate
    {
        /// <summary>
        /// Returns the before-image of the grain, before applying this update
        /// </summary>
        /// <returns>the before-image of the grain, before applying this update</returns>
        object GetBeforeImage();

        /// <summary>
        /// Produces the after-image of the grain, after applying this update
        /// </summary>
        /// <returns>the after-image of the grain, after applying this update</returns>
        object GetAfterImage();

        /// <summary>
        /// Combines a list of updates into a single update
        /// </summary>
        /// <param name="updates">the list of updates</param>
        /// <returns>a single update containing all the updates in the list</returns>
        IMemberUpdate Combine(params IMemberUpdate[] updates);

        /// <summary>
        /// Determines the type of operation done, which can be:
        ///  - Insert
        ///  - Update
        ///  - Delete
        ///  - None, which implies there was no change
        /// </summary>
        /// <returns>the type of operation in this update</returns>
        OperationType GetOperationType();
    }
}
