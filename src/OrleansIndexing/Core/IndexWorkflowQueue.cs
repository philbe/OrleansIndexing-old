using Orleans.Runtime;
using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// To minimize the number of RPCs, we process index updates for each grain
    /// on the silo where the grain is active. To do this processing, each silo
    /// has one or more IndexWorkflowQueue system-targets for each grain class,
    /// up to the number of hardware threads. A system-target is a grain that
    /// belongs to a specific silo.
    /// + Each of these system-targets has a queue of workflowRecords, which describe
    ///   updates that must be propagated to indexes.Each workflowRecord contains
    ///   the following information:
    ///    - workflowID: grainID + a sequence number
    ///    - memberUpdates: the updated values of indexed fields
    ///  
    ///   Ordinarily, these workflowRecords are for grains that are active on
    ///   IndexWorkflowQueue's silo. (This may not be true for short periods when
    ///   a grain migrates to another silo or after the silo recovers from failure).
    /// 
    /// + The IndexWorkflowQueue grain Q has a dictionary updatesOnWait is an
    ///   in-memory dictionary that maps each grain G to the workflowRecords for G
    ///   that are waiting for be updated
    /// </summary>
    internal class IndexWorkflowQueue : SystemTarget, IIndexWorkflowQueue
    {
        //updates that must be propagated to indexes.
        IndexWorkflowRecordNode workflowRecordsHead;

        //maps each grain G to the workflowRecords for G
        //that are waiting for be updated
        IDictionary<IIndexableGrain, IndexWorkflowRecordNode> updatedOnWait;

        internal IndexWorkflowQueue(GrainId g, SiloAddress silo) : base(g, silo)
        {
            
        }
    }

    /// <summary>
    /// A node in the linked list of workflowRecords.
    /// 
    /// This linked list makes the traversal more efficient.
    /// </summary>
    internal class IndexWorkflowRecordNode
    {
        IndexWorkflowRecord workflowRecord;

        IndexWorkflowRecordNode prev;
        IndexWorkflowRecordNode next;
    }
}
