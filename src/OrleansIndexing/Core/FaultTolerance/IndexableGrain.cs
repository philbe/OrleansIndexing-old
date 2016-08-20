using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Orleans.Runtime;
using System.Reflection;
using System.Linq;
using Orleans.Storage;

namespace Orleans.Indexing
{
    /// <summary>
    /// IndexableGrain class is the super-class of all fault-tolerant
    /// grains that need to have indexing capability.
    /// 
    /// To make a grain indexable, two steps should be taken:
    ///     1- the grain class should extend IndexableGrain
    ///     2- the grain class is responsible for calling UpdateIndexes
    ///        whenever one or more indexes need to be updated
    ///        
    /// Fault tolerance can be an optional feature for indexing, i.e.,
    /// IndexableGrain extends IndexableGrainNonFaultTolerant.
    /// By default, indexing is fault tolerant.
    /// 
    /// IndexableGrain creates a wrapper around the State class provided by
    /// the actual user-grain that extends it. It adds the following information to it:
    ///  - a list called activeWorkflowsList to the State,
    ///    which points to the in-flight indexing workflowsIds.
    ///  - There's a fixed mapping (e.g., a hash function) from grain id to IndexWorkflowQueue
    ///    instance. Each IndexableGrain G has a property workflowQueue whose value,
    ///    [grain-type-name + sequence number], identifies the IndexWorkflowQueue grain
    ///    that processes index updates on G's behalf.
    /// </summary>
    public abstract class IndexableGrain<TState, TProperties> : IndexableGrainNonFaultTolerant<IndexableExtendedState<TState>, TProperties>, IIndexableGrainFaultTolerant where TProperties: new()
    {
        protected new TState State
        {
            get { return base.State.UserState; }
            set { base.State.UserState = value; }
        }

        protected override TProperties Properties { get { return defaultCreatePropertiesFromState(); } }

        internal override IDictionary<Type, IIndexWorkflowQueue> WorkflowQueues
        {
            get { return base.State.workflowQueues; }
            set { base.State.workflowQueues = value; }
        }

        //0: uninitialized, 1: has some I-Indexes, 2: does not have any I-Index
        private sbyte __hasAnyIIndex;
        private bool HasAnyIIndex { get { return __hasAnyIIndex == 0 ? InitHasAnyIIndex() : __hasAnyIIndex > 0; } }

        public override Task OnActivateAsync()
        {
            //set it as un-initialized
            __hasAnyIIndex = 0;

            //if the list of active work-flows is null or empty
            //we can assume that we did not contact any work-flow
            //queue before in a possible prior activation
            if (base.State.activeWorkflowsSet == null || base.State.activeWorkflowsSet.Count() == 0)
            {
                WorkflowQueues = null;
            }
            //if there are some remaining active work-flows
            //they should be handled first
            else
            {
                PruneWorkflowQueuesForMissingTypes();

                return HandleRemainingWorkflows().ContinueWith(t => Task.WhenAll(PruneActiveWorkflowsSetFromAlreadyHandledWorkflows(t.Result), base.OnActivateAsync()));
            }
            return base.OnActivateAsync();
        }

        /// <summary>
        /// Applies a set of updates to the indexes defined on the grain
        /// </summary>
        /// <param name="updates">the dictionary of indexes to their corresponding updates</param>
        /// <param name="updateIndexesEagerly">whether indexes should be
        /// updated eagerly or lazily</param>
        /// <param name="onlyUniqueIndexesWereUpdated">a flag to determine whether
        /// only unique indexes were updated</param>
        /// <param name="numberOfUniqueIndexesUpdated">determine the number of
        /// updated unique indexes</param>
        /// <param name="writeStateIfConstraintsAreNotViolated">whether writing back
        /// the state to the storage should be done if no constraint is violated</param>
        protected override async Task ApplyIndexUpdates(IDictionary<string, IMemberUpdate> updates,
                                                       bool updateIndexesEagerly,
                                                       bool onlyUniqueIndexesWereUpdated,
                                                       int numberOfUniqueIndexesUpdated,
                                                       bool writeStateIfConstraintsAreNotViolated)
        {
            if (HasAnyIIndex)
            {
                //if there is any update to the indexes
                //we go ahead and updates the indexes
                if (updates.Count() > 0)
                {
                    IList<Type> iGrainTypes = GetIIndexableGrainTypes();
                    IIndexableGrain thisGrain = this.AsReference<IIndexableGrain>(GrainFactory);
                    Guid workflowId = GenerateUniqueWorkflowId();

                    //if indexes are updated eagerly
                    if (updateIndexesEagerly)
                    {
                        throw new InvalidOperationException("Fault tolerant indexes cannot be updated eagerly. This misconfiguration should have been cur on silo startup. Check SiloAssemblyLoader for the reason.");
                    }
                    //Otherwise, if indexes are updated lazily
                    else
                    {
                        //update the indexes lazily
                        //updating indexes lazily is the first step, because
                        //workflow record should be persisted in the workflow-queue first.
                        //The reason for waiting here is to make sure that the workflow record
                        //in the workflow queue is correctly persisted.
                        await ApplyIndexUpdatesLazily(updates, iGrainTypes, thisGrain, workflowId);
                    }

                    //if any unique index is defined on this grain and at least one of them is updated
                    if (numberOfUniqueIndexesUpdated > 0)
                    {
                        //try
                        //{
                        //    //update the unique indexes eagerly
                        //    //if there were more than one unique index, the updates to
                        //    //the unique indexes should be tentative in order not to
                        //    //become visible to readers before making sure that all
                        //    //uniqueness constraints are satisfied
                        await ApplyIndexUpdatesEagerly(iGrainTypes, thisGrain, updates, true, false, true);
                        //}
                        //catch (UniquenessConstraintViolatedException ex)
                        //{
                        //    //nothing should be done as tentative records are going to
                        //    //be removed by WorkflowQueueHandler
                        //    //the exception is thrown back to the user code.
                        //    throw ex;
                        //}
                    }

                    //final, the grain state is persisted if requested
                    if (writeStateIfConstraintsAreNotViolated)
                    {
                        //there is no constraint violation and the workflow ID
                        //can be a part of the list of active workflows
                        //Here, we add the work-flow to the list of committed/in-flight work-flows
                        AddWorkdlowIdToActiveWorkflows(workflowId);
                        await BaseWriteStateAsync();
                    }

                    //if everything was successful, the before images are updated
                    UpdateBeforeImages(updates);
                }
                //otherwise if there is no update to the indexes, we should
                //write back the state of the grain if requested
                else if (writeStateIfConstraintsAreNotViolated)
                {
                    await BaseWriteStateAsync();
                }
            }
            else
            {
                await base.ApplyIndexUpdates(updates, updateIndexesEagerly, onlyUniqueIndexesWereUpdated, numberOfUniqueIndexesUpdated, writeStateIfConstraintsAreNotViolated);
            }
        }

        /// <summary>
        /// Handles the remaining work-flows of the grain 
        /// </summary>
        /// <returns>the actual list of work-flow record IDs that were available in the queue(s)</returns>
        private Task<IEnumerable<Guid>> HandleRemainingWorkflows()
        {
            var copyOfWorkflowQueues = new Dictionary<Type, IIndexWorkflowQueue>(WorkflowQueues);
            var tasks = new List<Task<IEnumerable<Guid>>>();
            foreach (var wfqEntry in copyOfWorkflowQueues)
            {
                tasks.Add(HandleRemainingWorkflows(wfqEntry.Key, wfqEntry.Value));
            }
            return Task.WhenAll(tasks).ContinueWith(t => t.Result.SelectMany(res => res));
        }

        /// <summary>
        /// Handles the remaining work-flows of a specific grain interface of the grain
        /// </summary>
        /// <param name="iGrainType">the grain interface type being indexed</param>
        /// <param name="workflowQ">the previous work-flow queue responsible for handling the updates</param>
        /// <returns>the actual list of work-flow record IDs that were available in this queue</returns>
        private async Task<IEnumerable<Guid>> HandleRemainingWorkflows(Type iGrainType, IIndexWorkflowQueue workflowQ)
        {
            Immutable<List<IndexWorkflowRecord>> remainingWorkflows;
            try
            {
                remainingWorkflows = await workflowQ.GetRemainingWorkflowsIn(base.State.activeWorkflowsSet);
            }
            catch //the corresponding workflowQ is down, we should ask its reincarnated version
            {
                IIndexWorkflowQueue reincarnatedWorkflowQ = GetReincarnatedWorkflowQueue(workflowQ);
                remainingWorkflows = await reincarnatedWorkflowQ.GetRemainingWorkflowsIn(base.State.activeWorkflowsSet);
            }
            WorkflowQueues.Remove(iGrainType);
            if (remainingWorkflows.Value != null && remainingWorkflows.Value.Count() > 0)
            {
                IIndexWorkflowQueue newWorkflowQ = GetWorkflowQueue(iGrainType);
                await newWorkflowQ.AddAllToQueue(remainingWorkflows);
                return remainingWorkflows.Value.Select(w => w.WorkflowId);
            }
            return Enumerable.Empty<Guid>();
        }

        private IIndexWorkflowQueue GetReincarnatedWorkflowQueue(IIndexWorkflowQueue workflowQ)
        {
            throw new NotImplementedException();
        }

        private Task PruneActiveWorkflowsSetFromAlreadyHandledWorkflows(IEnumerable<Guid> workflowsInProgress)
        {
            var initialSize = base.State.activeWorkflowsSet.Count();
            base.State.activeWorkflowsSet.Clear();
            foreach (Guid workflowId in workflowsInProgress)
            {
                base.State.activeWorkflowsSet.Add(workflowId);
            }
            if (base.State.activeWorkflowsSet.Count() != initialSize)
            {
                return BaseWriteStateAsync();
            }
            return TaskDone.Done;
        }

        private void PruneWorkflowQueuesForMissingTypes()
        {
            var oldQueues = WorkflowQueues;
            WorkflowQueues = new Dictionary<Type, IIndexWorkflowQueue>();
            IList<Type> iGrainTypes = GetIIndexableGrainTypes();
            IIndexWorkflowQueue q;
            foreach (var grainType in iGrainTypes)
            {
                if(oldQueues.TryGetValue(grainType, out q))
                {
                    WorkflowQueues.Add(grainType, q);
                }
            }
        }

        public override Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsList()
        {
            var workflows = base.State.activeWorkflowsSet;
            if(workflows == null) return Task.FromResult(new HashSet<Guid>().AsImmutable());
            return Task.FromResult(workflows.AsImmutable());
        }

        public override Task RemoveFromActiveWorkflowIds(Guid removedWorkflowId)
        {
            if (base.State.activeWorkflowsSet != null && base.State.activeWorkflowsSet.Remove(removedWorkflowId))
            {
                return BaseWriteStateAsync();
            }
            else
            {
                return TaskDone.Done;
            }
        }

        /// <summary>
        /// Adds a workflow ID to the list of active workflows
        /// for this fault-tolerant indexable grain
        /// </summary>
        /// <param name="workflowId">the workflow ID to be added</param>
        private void AddWorkdlowIdToActiveWorkflows(Guid workflowId)
        {
            if (base.State.activeWorkflowsSet == null)
            {
                base.State.activeWorkflowsSet = new HashSet<Guid>();
            }
            base.State.activeWorkflowsSet.Add(workflowId);
        }

        /// <summary>
        /// Generates a unique Guid that does not exist in the
        /// list of active workflows.
        /// 
        /// Actually, there is a very unlikely possibility that
        /// we end up with a duplicate workflow ID in the following
        /// scenario:
        /// 1- IndexableGrain G is updated and assigned workflow ID = A
        /// 2- workflow record with ID = A is added to the index workflow queue
        /// 3- G fails and its state (including its active workflow list) is thrown away
        /// 4- G is re-activated and reads it state from storage (which does
        ///    not include A in its active workflow list)
        /// 5- G gets updated and a new workflow with ID = A is generated for it.
        ///    This ID is assumed to be unique, while it actually is not unique
        ///    and already exists in the workflow queue.
        /// 
        /// The only way to avoid it is using a centralized unique
        /// workflow ID generator, which can be added if necessary.
        /// </summary>
        /// <returns>a new unique workflow ID</returns>
        private Guid GenerateUniqueWorkflowId()
        {
            Guid workflowId = Guid.NewGuid();
            while (base.State.activeWorkflowsSet != null && base.State.activeWorkflowsSet.Contains(workflowId))
            {
                workflowId = Guid.NewGuid();
            }

            return workflowId;
        }

        private TProperties defaultCreatePropertiesFromState()
        {
            if (typeof(TProperties).IsAssignableFrom(typeof(TState))) return (TProperties)(object)(base.State.UserState);

            if (_props == null) _props = new TProperties();

            foreach (PropertyInfo p in typeof(TProperties).GetProperties())
            {
                p.SetValue(_props, typeof(TState).GetProperty(p.Name).GetValue(base.State.UserState));
            }
            return _props;
        }

        private bool InitHasAnyIIndex()
        {
            IList<Type> iGrainTypes = GetIIndexableGrainTypes();
            foreach (var iGrainType in iGrainTypes)
            {
                var indexes = IndexHandler.GetIndexes(iGrainType);
                foreach (var idxInfo in indexes.Values)
                {
                    if (idxInfo.Item1 is InitializedIndex)
                    {
                        __hasAnyIIndex = 1;
                        return true;
                    }
                }
            }
            __hasAnyIIndex = -1;
            return false;
        }
    }

    /// <summary>
    /// IndexableExtendedState{TState} is a wrapper around
    /// a user-defined state, TState, which adds the necessary
    /// information for fault-tolerant indexing
    /// </summary>
    /// <typeparam name="TState">the type of user state</typeparam>
    [Serializable]
    public class IndexableExtendedState<TState>
    {
        internal HashSet<Guid> activeWorkflowsSet = null;
        internal IDictionary<Type, IIndexWorkflowQueue> workflowQueues = null;

        public TState UserState = (TState)Activator.CreateInstance(typeof(TState));
    }

    /// <summary>
    /// This stateless IndexableGrainNonFaultTolerant is the super class of all stateless 
    /// indexable-grains.
    /// 
    /// Having a stateless fault-tolerant indexable-grain is meaningless,
    /// so it is the same as having a stateless non-fault-tolerant indexable grain
    /// </summary>
    public abstract class IndexableGrain<TProperties> : IndexableGrainNonFaultTolerant<TProperties> where TProperties : new()
    {
    }
}
