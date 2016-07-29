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
    /// IndexableGrain class is the super-class of all grains that
    /// need to have indexing capability.
    /// 
    /// To make a grain indexable, two steps should be taken:
    ///     1- the grain class should extend IndexableGrain
    ///     2- the grain class is responsible for calling UpdateIndexes
    ///        whenever one or more indexes need to be updated
    /// </summary>
    public abstract class IndexableGrain<TState, TProperties> : IndexableGrainNonFaultTolerant<IndexableExtendedState<TState>, TProperties> where TProperties: new()
    {
        protected override TProperties Properties { get { return defaultCreatePropertiesFromState(); } }

        private TProperties defaultCreatePropertiesFromState()
        {
            Type propsType = typeof(TProperties);
            Type stateType = typeof(TState);

            if (propsType.IsAssignableFrom(stateType)) return (TProperties)(object)(State.State);

            if (_props == null) _props = new TProperties();

            foreach (PropertyInfo p in propsType.GetProperties())
            {
                p.SetValue(_props, stateType.GetProperty(p.Name).GetValue(State.State));
            }
            return _props;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    [Serializable]
    public class IndexableExtendedState<TState>
    {
        internal List<int> ActiveWorkflowsList;
        internal int WorkflowQueueSeqNum;
        internal SiloAddress WorkflowQueueSilo;
        public TState State;
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
