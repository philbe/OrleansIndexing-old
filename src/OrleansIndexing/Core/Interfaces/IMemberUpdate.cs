using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public static class OperationTypeExtensions
    {
        public static OperationType CombineWith(this OperationType thisOp, OperationType otherOp)
        {
            switch (thisOp)
            {
                case OperationType.None:
                    return otherOp;
                case OperationType.Insert:
                    switch (otherOp)
                    {
                        case OperationType.Insert:
                            throw new Exception(string.Format("Two subsequent Insert operations are not allowed."));
                        case OperationType.Update:
                            return OperationType.Insert;
                        case OperationType.Delete:
                            return OperationType.None;
                        default: //case OperationType.None
                            return thisOp;
                    }
                case OperationType.Update:
                    switch (otherOp)
                    {
                        case OperationType.Insert:
                            throw new Exception(string.Format("An Insert operation after an Update operation is not allowed."));
                        case OperationType.Delete:
                            return otherOp; //i.e., OperationType.Delete
                        default: //case OperationType.None or OperationType.Update
                            return thisOp;
                    }
                case OperationType.Delete:
                    switch (otherOp)
                    {
                        case OperationType.Insert:
                            return OperationType.Update;
                        case OperationType.Update:
                            throw new Exception(string.Format("An Update operation after a Delete operation is not allowed."));
                        default: //case OperationType.None or OperationType.Delete
                            return thisOp;
                    }
                default:
                    throw new Exception(string.Format("Operation type {0} is not valid", thisOp));
            }
        }
    }

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
