using Orleans.Concurrency;
using Orleans.Core;
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
    [Reentrant]
    internal class IndexWorkflowQueueHandler : SystemTarget, IIndexWorkflowQueueHandler
    {
        private IIndexWorkflowQueue __workflowQueue;
        private IIndexWorkflowQueue WorkflowQueue { get { return __workflowQueue == null ? InitIndexWorkflowQueue() : __workflowQueue; } }

        private int _queueSeqNum;
        private Type _iGrainType;

        private bool _isDefinedAsFaultTolerantGrain;
        private bool _hasAnyIIndex;
        private bool HasAnyIIndex { get { if (__indexes == null) { InitIndexes(); } return _hasAnyIIndex; } }
        private bool IsFaultTolerant { get { return _isDefinedAsFaultTolerantGrain && HasAnyIIndex; } }

        private IDictionary<string, Tuple<object, object, object>> __indexes;

        private IDictionary<string, Tuple<object, object, object>> Indexes { get { return __indexes == null ? InitIndexes() : __indexes; } }

        internal IndexWorkflowQueueHandler(Type iGrainType, int queueSeqNum, SiloAddress silo, bool isDefinedAsFaultTolerantGrain) : base(CreateIndexWorkflowQueueHandlerGrainId(iGrainType, queueSeqNum), silo)
        {
            _iGrainType = iGrainType;
            _queueSeqNum = queueSeqNum;
            _isDefinedAsFaultTolerantGrain = isDefinedAsFaultTolerantGrain;
            _hasAnyIIndex = false;
            __indexes = null;
            __workflowQueue = null;
        }

        public Task<bool> HandleWorkflowsUntilPunctuation(Immutable<IndexWorkflowRecordNode> workflowRecords)
        {
            var _ = Task.Factory.StartNew(async input =>
            {
                var workflows = ((Immutable<IndexWorkflowRecordNode>)input);

                while (workflows.Value != null)
                {
                    Dictionary<IIndexableGrain, HashSet<int>> grainsToActiveWorkflows = null;
                    if(IsFaultTolerant)
                    {
                        grainsToActiveWorkflows = await GetActiveWorkflowsListsFromGrains(workflows);
                    }
                    var updatesToIndexes = CreateAMapForUpdatesToIndexes();
                    PopulateUpdatesToIndexes(workflows, updatesToIndexes, grainsToActiveWorkflows);
                    await Task.WhenAll(PrepareIndexUpdateTasks(updatesToIndexes));
                    workflows = await WorkflowQueue.GiveMoreWorkflowsOrSetAsIdle();
                }
            }, workflowRecords);

            return Task.FromResult(true);
        }

        private IList<Task<bool>> PrepareIndexUpdateTasks(Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes)
        {
            IList<Task<bool>> updateIndexTasks = new List<Task<bool>>();
            foreach (var indexEntry in Indexes)
            {
                var idxInfo = indexEntry.Value;
                var updatesToIndex = updatesToIndexes[indexEntry.Key];
                if (updatesToIndex.Count() > 0)
                {
                    updateIndexTasks.Add(((IndexInterface)idxInfo.Item1).ApplyIndexUpdateBatch(updatesToIndex.AsImmutable(), ((IndexMetaData)idxInfo.Item2).IsUniqueIndex(), Silo));
                }
            }

            return updateIndexTasks;
        }

        private void PopulateUpdatesToIndexes(Immutable<IndexWorkflowRecordNode> workflows, Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes, Dictionary<IIndexableGrain, HashSet<int>> grainsToActiveWorkflows)
        {
            bool faultTolerant = IsFaultTolerant;
            IndexWorkflowRecordNode currentWorkflow = workflows.Value;
            while (!currentWorkflow.IsPunctuation())
            {
                IndexWorkflowRecord workflowRec = currentWorkflow.WorkflowRecord;
                IIndexableGrain g = workflowRec.Grain;
                bool existsInActiveWorkflows = false;
                if (faultTolerant)
                {
                    HashSet<int> activeWorkflowRecs = null;
                    if (grainsToActiveWorkflows.TryGetValue(g, out activeWorkflowRecs))
                    {
                        if (activeWorkflowRecs.Contains(workflowRec.SeqNum))
                        {
                            existsInActiveWorkflows = true;
                        }
                    }
                }

                foreach (var updates in currentWorkflow.WorkflowRecord.MemberUpdates)
                {
                    IMemberUpdate updt = updates.Value;
                    if (updt.GetOperationType() != IndexOperationType.None)
                    {
                        string index = updates.Key;
                        var updatesToIndex = updatesToIndexes[index];
                        IList<IMemberUpdate> updatesList;
                        if (!updatesToIndex.TryGetValue(g, out updatesList))
                        {
                            updatesList = new List<IMemberUpdate>();
                            updatesToIndex.Add(g, updatesList);
                        }

                        if (!faultTolerant || existsInActiveWorkflows)
                        {
                            updatesList.Add(updt);
                        }
                        else if (((IndexMetaData)Indexes[index].Item2).IsUniqueIndex())
                        {
                            //reverse a possible remaining tentative record from the index
                            updatesList.Add(new MemberUpdateReverseTentative(updt));
                        }
                    }
                }
                currentWorkflow = currentWorkflow.Next;
            }
        }

        private static HashSet<int> EMPTY_HASHSET = new HashSet<int>();
        private async Task<Dictionary<IIndexableGrain, HashSet<int>>> GetActiveWorkflowsListsFromGrains(Immutable<IndexWorkflowRecordNode> workflows)
        {
            IndexWorkflowRecordNode currentWorkflow = workflows.Value;
            var result = new Dictionary<IIndexableGrain, HashSet<int>>();
            var grains = new List<IIndexableGrain>();
            var activeWorkflowsListTasks = new List<Task<Immutable<List<int>>>>();
            Immutable<List<int>>[] activeWorkflowsLists;

            while (!currentWorkflow.IsPunctuation())
            {
                IIndexableGrain g = currentWorkflow.WorkflowRecord.Grain;
                foreach (var updates in currentWorkflow.WorkflowRecord.MemberUpdates)
                {
                    IMemberUpdate updt = updates.Value;
                    if (updt.GetOperationType() != IndexOperationType.None && !result.ContainsKey(g))
                    {
                        result.Add(g, EMPTY_HASHSET);
                        grains.Add(g);
                        activeWorkflowsListTasks.Add(g.AsReference<IIndexableGrain>(InsideRuntimeClient.Current.GrainFactory, _iGrainType).GetActiveWorkflowIdsList());
                    }
                }
                currentWorkflow = currentWorkflow.Next;
            }

            if (activeWorkflowsListTasks.Count() > 0)
            {
                activeWorkflowsLists = await Task.WhenAll(activeWorkflowsListTasks);
                for(int i = 0; i < activeWorkflowsLists.Length; ++i)
                {
                    result[grains[i]] = activeWorkflowsLists[i].Value.ToSet();
                }
            }

            return result;
        }

        private Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> CreateAMapForUpdatesToIndexes()
        {
            var updatesToIndexes = new Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>>();
            foreach (string index in Indexes.Keys)
            {
                updatesToIndexes.Add(index, new Dictionary<IIndexableGrain, IList<IMemberUpdate>>());
            }

            return updatesToIndexes;
        }
        
        private IDictionary<string, Tuple<object, object, object>> InitIndexes()
        {
            __indexes = IndexHandler.GetIndexes(_iGrainType);
            foreach(var idxInfo in __indexes.Values)
            {
                if(idxInfo.Item1 is InitializedIndex)
                {
                    _hasAnyIIndex = true;
                    return __indexes;
                }
            }
            return __indexes;
        }

        private IIndexWorkflowQueue InitIndexWorkflowQueue()
        {
            return __workflowQueue = InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<IIndexWorkflowQueue>(IndexWorkflowQueue.CreateIndexWorkflowQueueGrainId(_iGrainType, _queueSeqNum), Silo);
        }

        public static GrainId CreateIndexWorkflowQueueHandlerGrainId(Type grainInterfaceType, int queueSeqNum)
        {
            return GrainId.GetSystemTargetGrainId(Constants.INDEX_WORKFLOW_QUEUE_HANDLER_SYSTEM_TARGET_TYPE_CODE,
                                                  IndexWorkflowQueue.CreateIndexWorkflowQueuePrimaryKey(grainInterfaceType, queueSeqNum));
        }
    }
}
