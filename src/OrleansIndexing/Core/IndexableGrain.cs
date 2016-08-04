using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Orleans.Runtime;
using System.Reflection;
using System.Linq;

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

        private string GetWorkflowQueue()
        {
            return TypeUtils.GetFullName(typeof(TState)) + "-" + GetWorkflowQueueId();
        }

        private int GetWorkflowQueueId()
        {
            //does it have a designated IndexWorkflowQueue?
            if (base.State.workflowQueueSeqNum > 0)
            {
                return base.State.workflowQueueSeqNum;
            }
            else
            {
                //TODO: find the designated IndexWorkflowQueue sequence number on this silo
                return 1;
            }
        }

        private SiloAddress GetWorkflowQueueSilo()
        {
            //does it have a designated IndexWorkflowQueue?
            if (base.State.workflowQueueSeqNum > 0)
            {
                return base.State.workflowQueueSilo;
            }
            else
            {
                return RuntimeAddress;
            }
        }

        private string GetReincarnatedWorkflowQueueId()
        {
            return GetWorkflowQueue() + "/" + GetWorkflowQueueSilo().ToLongString();
        }

        public override Task<Immutable<List<int>>> GetActiveWorkflowIdsList()
        {
            return Task.FromResult(base.State.activeWorkflowsList.AsImmutable());
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
        internal List<int> activeWorkflowsList = null;
        internal int workflowQueueSeqNum = 0;
        internal SiloAddress workflowQueueSilo = null;

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
