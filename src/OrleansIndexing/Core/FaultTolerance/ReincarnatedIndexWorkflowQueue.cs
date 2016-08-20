using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// To minimize the number of RPCs, we process index updates for each grain
    /// on the silo where the grain is active. To do this processing, each silo
    /// has one or more IndexWorkflowQueueSystemTarget system-targets for each grain class,
    /// up to the number of hardware threads. A system-target is a grain that
    /// belongs to a specific silo.
    /// + Each of these system-targets has a queue of workflowRecords, which describe
    ///   updates that must be propagated to indexes.Each workflowRecord contains
    ///   the following information:
    ///    - workflowID: grainID + a sequence number
    ///    - memberUpdates: the updated values of indexed fields
    ///  
    ///   Ordinarily, these workflowRecords are for grains that are active on
    ///   IndexWorkflowQueueSystemTarget's silo. (This may not be true for short periods when
    ///   a grain migrates to another silo or after the silo recovers from failure).
    /// 
    /// + The IndexWorkflowQueueSystemTarget grain Q has a dictionary updatesOnWait is an
    ///   in-memory dictionary that maps each grain G to the workflowRecords for G
    ///   that are waiting for be updated
    /// </summary>
    [StorageProvider(ProviderName = Constants.INDEXING_WORKFLOWQUEUE_STORAGE_PROVIDER_NAME)]
    [Reentrant]
    internal class ReincarnatedIndexWorkflowQueue : Grain, IIndexWorkflowQueue
    {
        internal static TimeSpan ACTIVE_FOR_A_DAY = TimeSpan.FromDays(1);
        private IndexWorkflowQueueBase _base;

        public override Task OnActivateAsync()
        {
            DelayDeactivation(ACTIVE_FOR_A_DAY);
            return base.OnActivateAsync();
        }

        public Task Initialize(IIndexWorkflowQueue oldParentSystemTarget)
        {
            if (_base == null)
            {
                GrainReference oldParentSystemTargetRef = oldParentSystemTarget.AsWeaklyTypedReference();
                string[] parts = oldParentSystemTargetRef.GetPrimaryKeyString().Split('-');
                if (parts.Length != 2)
                {
                    throw new Exception("The primary key for IndexWorkflowQueueSystemTarget should only contain a single special character '-', while it contains multiple. The primary key is '" + oldParentSystemTargetRef.GetPrimaryKeyString() + "'");
                }

                Type grainInterfaceType = TypeUtils.ResolveType(parts[0]);
                int queueSequenceNumber = int.Parse(parts[1]);

                GrainReference thisRef = this.AsWeaklyTypedReference();
                _base = new IndexWorkflowQueueBase(grainInterfaceType, queueSequenceNumber, oldParentSystemTargetRef.SystemTargetSilo, true, thisRef.GrainId, thisRef);
            }
            return TaskDone.Done;
        }

        public Task AddAllToQueue(Immutable<List<IndexWorkflowRecord>> workflowRecords)
        {
            return _base.AddAllToQueue(workflowRecords);
        }

        public Task AddToQueue(Immutable<IndexWorkflowRecord> workflowRecord)
        {
            return _base.AddToQueue(workflowRecord);
        }

        public Task<Immutable<List<IndexWorkflowRecord>>> GetRemainingWorkflowsIn(HashSet<Guid> activeWorkflowsSet)
        {
            return _base.GetRemainingWorkflowsIn(activeWorkflowsSet);
        }

        public Task<Immutable<IndexWorkflowRecordNode>> GiveMoreWorkflowsOrSetAsIdle()
        {
            return _base.GiveMoreWorkflowsOrSetAsIdle();
        }

        public Task RemoveAllFromQueue(Immutable<List<IndexWorkflowRecord>> workflowRecords)
        {
            return _base.RemoveAllFromQueue(workflowRecords);
        }
    }
}
