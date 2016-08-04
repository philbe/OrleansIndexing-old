using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// Each workflowRecord contains the following information:
    ///    - workflowID: grainID + a sequence number
    ///    - memberUpdates: the updated values of indexed fields
    /// </summary>
    internal class IndexWorkflowRecord
    {
        /// <summary>
        /// The grain being indexes,
        /// which its ID is the first part of the workflowID
        /// </summary>
        internal IIndexableGrain Grain { get; private set; }

        /// <summary>
        /// The sequence number of update on the Grain,
        /// which is the second part of the workflowID
        /// </summary>
        internal int SeqNum { get; private set; }

        /// <summary>
        /// The list of updates to all indexes of the Grain
        /// </summary>
        internal IDictionary<string, IMemberUpdate> MemberUpdates { get; private set; }

        internal IndexWorkflowRecord(IIndexableGrain grain, int seqNum, IDictionary<string, IMemberUpdate> memberUpdates)
        {
            Grain = grain;
            SeqNum = seqNum;
            MemberUpdates = memberUpdates;
        }
    }
}