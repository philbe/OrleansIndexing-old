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
    internal class IndexWorkflowQueueHandler : IIndexWorkflowQueueHandler
    {
        private IndexWorkflowQueue WorkflowQueue { get; set; }
        
        private Type _iGrainType;

        private bool IsFaultTolerant { get { return WorkflowQueue.IsFaultTolerant; } }

        private SiloAddress Silo { get; set; }

        private IDictionary<string, Tuple<object, object, object>> __indexes;

        private IDictionary<string, Tuple<object, object, object>> Indexes { get { return __indexes == null ? InitIndexes() : __indexes; } }

        internal IndexWorkflowQueueHandler(Type iGrainType, IndexWorkflowQueue workflowQueue, bool isDefinedAsFaultTolerantGrain, SiloAddress silo)
        {
            _iGrainType = iGrainType;
            WorkflowQueue = workflowQueue;
            Silo = silo;
            __indexes = null;
        }

        public async Task HandleWorkflowsUntilPunctuation(IndexWorkflowRecordNode workflows)
        {
            while (workflows != null)
            {
                Dictionary<IIndexableGrain, HashSet<Guid>> grainsToActiveWorkflows = null;
                if(IsFaultTolerant)
                {
                    grainsToActiveWorkflows = await GetActiveWorkflowsListsFromGrains(workflows);
                }
                var updatesToIndexes = CreateAMapForUpdatesToIndexes();
                PopulateUpdatesToIndexes(workflows, updatesToIndexes, grainsToActiveWorkflows);
                await Task.WhenAll(PrepareIndexUpdateTasks(updatesToIndexes));
                workflows = await WorkflowQueue.GiveMoreWorkflowsOrSetAsIdle();
            }
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

        private void PopulateUpdatesToIndexes(IndexWorkflowRecordNode currentWorkflow, Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes, Dictionary<IIndexableGrain, HashSet<Guid>> grainsToActiveWorkflows)
        {
            bool faultTolerant = IsFaultTolerant;
            while (!currentWorkflow.IsPunctuation())
            {
                IndexWorkflowRecord workflowRec = currentWorkflow.WorkflowRecord;
                IIndexableGrain g = workflowRec.Grain;
                bool existsInActiveWorkflows = false;
                if (faultTolerant)
                {
                    HashSet<Guid> activeWorkflowRecs = null;
                    if (grainsToActiveWorkflows.TryGetValue(g, out activeWorkflowRecs))
                    {
                        if (activeWorkflowRecs.Contains(workflowRec.WorkflowId))
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

        private static HashSet<Guid> EMPTY_HASHSET = new HashSet<Guid>();
        private async Task<Dictionary<IIndexableGrain, HashSet<Guid>>> GetActiveWorkflowsListsFromGrains(IndexWorkflowRecordNode currentWorkflow)
        {
            var result = new Dictionary<IIndexableGrain, HashSet<Guid>>();
            var grains = new List<IIndexableGrain>();
            var activeWorkflowsListTasks = new List<Task<Immutable<List<Guid>>>>();
            Immutable<List<Guid>>[] activeWorkflowsLists;

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
                    return __indexes;
                }
            }
            return __indexes;
        }
    }
}
