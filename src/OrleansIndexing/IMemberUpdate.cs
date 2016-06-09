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
        /// Produces the after-image of the grain, after applying this update
        /// </summary>
        /// <returns>the after-image of the grain, after applying this update</returns>
        object GetAftImg();

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
