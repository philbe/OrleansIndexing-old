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
        private IIndexWorkflowQueue _workflowQueue { get { return __workflowQueue == null ? InitIndexWorkflowQueue() : __workflowQueue; } }

        private int _queueSeqNum;
        private Type _iGrainType;

        private bool _isDefinedAsFaultTolerantGrain;

        private bool _hasAnyIIndex;

        private bool _isFaultTolerant { get { return _isDefinedAsFaultTolerantGrain && _hasAnyIIndex; } }

        private IDictionary<string, Tuple<object, object, object>> __indexes;

        private IDictionary<string, Tuple<object, object, object>> _indexes { get { return __indexes == null ? InitIndexes() : __indexes; } }

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
                    var updatesToIndexes = CreateAMapForUpdatesToIndexes();
                    PopulateUpdatesToIndexes(workflows, updatesToIndexes);
                    await Task.WhenAll(PrepareIndexUpdateTasks(updatesToIndexes));
                    workflows = await _workflowQueue.GiveMoreWorkflowsOrSetAsIdle();
                }
            }, workflowRecords);

            return Task.FromResult(true);
        }

        private IList<Task<bool>> PrepareIndexUpdateTasks(Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes)
        {
            IList<Task<bool>> updateIndexTasks = new List<Task<bool>>();
            foreach (var indexEntry in _indexes)
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

        private static void PopulateUpdatesToIndexes(Immutable<IndexWorkflowRecordNode> workflows, Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes)
        {
            IndexWorkflowRecordNode currentWorkflow = workflows.Value;
            while (!currentWorkflow.IsPunctuation())
            {
                IIndexableGrain g = currentWorkflow.WorkflowRecord.Grain;
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
                        updatesList.Add(updt);
                    }
                }
                currentWorkflow = currentWorkflow.Next;
            }
        }

        private Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> CreateAMapForUpdatesToIndexes()
        {
            var updatesToIndexes = new Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>>();
            foreach (string index in _indexes.Keys)
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
