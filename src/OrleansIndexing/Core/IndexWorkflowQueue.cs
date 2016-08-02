using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    [Reentrant]
    internal class IndexWorkflowQueue : SystemTarget, IIndexWorkflowQueue
    {
        //the persistent state of IndexWorkflowQueue, including:
        // - doubly linked list of workflowRecordds
        // - the identity of the IndexWorkflowQueue system target
        protected IndexWorkflowQueueState State;

        //the tail of workflowRecords doubly linked list
        internal IndexWorkflowRecordNode workflowRecordsTail;

        //maps each grain G to the workflowRecords for G
        //that are waiting for be updated
        IDictionary<IIndexableGrain, IndexWorkflowRecordNode> updatesOnWait;

        //the storage provider for index work-flow queue
        private IStorageProvider storageProvider;

        internal IndexWorkflowQueue(GrainId g, SiloAddress silo, IStorageProvider indexWorkflowQueueStorage) : base(g, silo)
        {
            State = new IndexWorkflowQueueState(g, silo);
            workflowRecordsTail = null;
            updatesOnWait = new Dictionary<IIndexableGrain, IndexWorkflowRecordNode>();
            storageProvider = indexWorkflowQueueStorage;
        }

        public Task AddToQueue(Immutable<IndexWorkflowRecord> workflow)
        {
            IndexWorkflowRecord newWorkflow = workflow.Value;
            IndexWorkflowRecordNode newWorkflowNode = new IndexWorkflowRecordNode(newWorkflow);
            IndexWorkflowRecordNode appendToRecord;
            if (updatesOnWait.TryGetValue(newWorkflow.Grain, out appendToRecord))
            {
                IIndexableGrain appendToRecordGrain = appendToRecord.WorkflowRecord.Grain;
                IndexWorkflowRecordNode tmp = appendToRecord.Next;
                while (tmp != null && tmp.WorkflowRecord.Grain == appendToRecordGrain)
                {
                    appendToRecord = tmp;
                    tmp = tmp.Next;
                }
                appendToRecord.Append(newWorkflowNode, ref workflowRecordsTail);
            }
            else if (workflowRecordsTail == null) //if the list is empty
            {
                workflowRecordsTail = newWorkflowNode;
                State.State.WorkflowRecordsHead = newWorkflowNode;
            }
            else // otherwise append to the end of the list
            {
                workflowRecordsTail.Append(newWorkflowNode, ref workflowRecordsTail);
            }
            return storageProvider.WriteStateAsync("Orleans.Indexing.IndexWorkflowQueue-" + TypeUtils.GetFullName(newWorkflow.IGrainType), this.AsWeaklyTypedReference(), State);
        }

        public Task RemoveFromQueue(IndexWorkflowRecordNode workflowsHead, int numWorkflows)
        {
            if (numWorkflows == 0) return TaskDone.Done;

            Type iGrainType = workflowsHead.WorkflowRecord.IGrainType;
            IndexWorkflowRecordNode tmpNext;
            int i = 0;
            do
            {
                tmpNext = workflowsHead.Next;
                if (i == 0 || i == (numWorkflows - 1))
                {
                    workflowsHead.Remove(ref workflowRecordsTail, ref State.State.WorkflowRecordsHead);
                }
                else
                {
                    workflowsHead.Clean();
                }
                workflowsHead = tmpNext;
            } while (++i < numWorkflows);
            return storageProvider.WriteStateAsync("Orleans.Indexing.IndexWorkflowQueue-" + TypeUtils.GetFullName(iGrainType), this.AsWeaklyTypedReference(), State);
        }
    }

    /// <summary>
    /// A node in the linked list of workflowRecords.
    /// 
    /// This linked list makes the traversal more efficient.
    /// </summary>
    internal class IndexWorkflowRecordNode
    {
        internal IndexWorkflowRecord WorkflowRecord;

        internal IndexWorkflowRecordNode Prev = null;
        internal IndexWorkflowRecordNode Next = null;

        public IndexWorkflowRecordNode(IndexWorkflowRecord workflow)
        {
            WorkflowRecord = workflow;
        }

        public void Append(IndexWorkflowRecordNode elem, ref IndexWorkflowRecordNode tail)
        {
            var tmpNext = Next;
            if (tmpNext != null)
            {
                elem.Next = tmpNext;
                tmpNext.Prev = elem;
            }
            elem.Prev = this;
            Next = elem;

            if (tail == this)
            {
                tail = elem;
            }
        }

        public void Remove(ref IndexWorkflowRecordNode head, ref IndexWorkflowRecordNode tail)
        {
            if (Prev == null) head = Next;
            else Prev.Next = Next;

            if (Next == null) tail = Prev;
            else Next.Prev = Prev;

            Clean();
        }

        /// <summary>
        /// This method gathers all the IndexWorkflowRecords that belong to the
        /// same grain and are continuously one after the other
        /// </summary>
        /// <returns>the continuous list of IndexWorkflowRecords for the same grain</returns>
        public IList<IndexWorkflowRecord> GetContinousListForTheSameGrain()
        {
            IList<IndexWorkflowRecord> res = new List<IndexWorkflowRecord>();
            res.Add(WorkflowRecord);

            IIndexableGrain thisGrain = WorkflowRecord.Grain;
            IndexWorkflowRecordNode tmp = Next;
            while (tmp != null && tmp.WorkflowRecord.Grain == thisGrain)
            {
                res.Add(tmp.WorkflowRecord);
                tmp = tmp.Next;
            }

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clean()
        {
            WorkflowRecord = null;
            Next = null;
            Prev = null;
        }
    }

    /// <summary>
    /// All the information stored for a single IndexWorkflowQueue
    /// </summary>
    internal class IndexWorkflowQueueEntry
    {
        //updates that must be propagated to indexes.
        internal IndexWorkflowRecordNode WorkflowRecordsHead;

        internal GrainId QueueId;

        internal SiloAddress Silo;

        public IndexWorkflowQueueEntry(GrainId queueId, SiloAddress silo)
        {
            WorkflowRecordsHead = null;
            QueueId = queueId;
            Silo = silo;
        }
    }

    /// <summary>
    /// The persistent unit for storing the information for a IndexWorkflowQueue
    /// </summary>
    internal class IndexWorkflowQueueState : IGrainState
    {
        public IndexWorkflowQueueEntry State;

        public IndexWorkflowQueueState(GrainId g, SiloAddress silo)
        {
            State = new IndexWorkflowQueueEntry(g, silo);
            ETag = null;
        }

        public string ETag { get; set; }

        object IGrainState.State
        {
            get
            {
                return State;
            }

            set
            {
                State = (IndexWorkflowQueueEntry) value;
            }
        }
    }
}
