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
    /// IndexableGrainNonFaultTolerant class is the super-class of all grains that
    /// need to have indexing capability.
    /// 
    /// To make a grain indexable, two steps should be taken:
    ///     1- the grain class should extend IndexableGrainNonFaultTolerant
    ///     2- the grain class is responsible for calling UpdateIndexes
    ///        whenever one or more indexes need to be updated
    /// </summary>
    public abstract class IndexableGrainNonFaultTolerant<TState, TProperties> : Grain<TState>, IIndexableGrain<TProperties> where TProperties: new()
    {
        /// <summary>
        /// an immutable cached version of IIndexUpdateGenerator instances
        /// for the current indexes on the grain.
        /// The tuple contains Index, IndexMetaData, IndexUpdateGenerator
        /// </summary>
        private IDictionary<string, Tuple<object, object, object>> _iUpdateGens;

        /// <summary>
        /// an immutable copy of before-images of the indexed fields
        /// </summary>
        private Immutable<IDictionary<string, object>> _beforeImages;

        /// <summary>
        /// a cached grain interface type, which
        /// is cached on the first call to getIGrainType()
        /// </summary>
        private IList<Type> _iGrainTypes = null;

        private TProperties _props;

        protected virtual TProperties Properties { get { return defaultCreatePropertiesFromState(); } }

        private TProperties defaultCreatePropertiesFromState()
        {
            Type propsType = typeof(TProperties);
            Type stateType = typeof(TState);

            if (propsType.IsAssignableFrom(stateType)) return (TProperties)(object)State;

            if (_props == null) _props = new TProperties();

            foreach (PropertyInfo p in propsType.GetProperties())
            {
                p.SetValue(_props, stateType.GetProperty(p.Name).GetValue(State));
            }
            return _props;
        }

        /// <summary>
        /// Upon activation, the list of index update generators
        /// is retrieved from the index handler. It is cached in
        /// this grain for use in creating before-images, and also
        /// for later calls to UpdateIndexes.
        /// 
        /// Then, the before-images are created and stored in memory.
        /// </summary>
        public override Task OnActivateAsync()
        {
            _iUpdateGens = IndexHandler.GetIndexes(getIIndexableGrainTypes()[0]);
            _beforeImages = new Dictionary<string, object>().AsImmutable<IDictionary<string, object>>();
            AddMissingBeforeImages();
            return Task.WhenAll(InsertIntoActiveIndexes(), base.OnActivateAsync());
        }

        public override Task OnDeactivateAsync()
        {
            return Task.WhenAll(RemoveFromActiveIndexes(), base.OnDeactivateAsync());
        }

        /// <summary>
        /// Inserts the current grain to the active indexes only
        /// if it already has a persisted state
        /// </summary>
        protected Task InsertIntoActiveIndexes()
        {
            //check if it contains anything to be indexed
            if (_beforeImages.Value.Values.Any(e => e != null))
            {
                return UpdateActiveIndexes(true, Properties);
            }
            return TaskDone.Done;
        }

        /// <summary>
        /// Removes the current grain from active indexes
        /// </summary>
        protected Task RemoveFromActiveIndexes()
        {
            //check if it has anything indexed
            if (_beforeImages.Value.Values.Any(e => e != null))
            {
                return UpdateActiveIndexes(false, default(TProperties));
            }
            return TaskDone.Done;
        }

        /// <summary>
        /// After some changes were made to the grain, and the grain is 
        /// in a consistent state, this method is called to update the 
        /// indexes defined on this grain type.
        /// 
        /// A call to this method first creates the member updates, and
        /// then sends them to ApplyIndexUpdates of the index handler.
        /// 
        /// The only reason that this method can receive a negative result from 
        /// a call to ApplyIndexUpdates is that the list of indexes might have
        /// changed. In this case, it updates the list of member update and tries
        /// again. In the case of a positive result from ApplyIndexUpdates,
        /// the list of before-images is replaced by the list of after-images.
        /// </summary>
        protected Task UpdateIndexes(bool isOnActivate, TProperties props)
        {
            return Task.WhenAll(UpdateActiveIndexes(isOnActivate, props), UpdateInitializedIndexes(isOnActivate, props));
        }

        /// <summary>
        /// Updates A-indexes.
        /// </summary>
        protected async Task UpdateActiveIndexes(bool isOnActivate, TProperties props)
        {
            IList<Type> iGrainTypes = getIIndexableGrainTypes();
            bool success = false;
            do
            {
                IDictionary<string, IMemberUpdate> updates = new Dictionary<string, IMemberUpdate>();
                IDictionary<string, Tuple<object, object, object>> iUpdateGens = _iUpdateGens;
                {
                    if (iUpdateGens.Count == 0) return;

                    IDictionary<string, object> befImgs = _beforeImages.Value;
                    foreach (KeyValuePair<string, Tuple<object, object, object>> kvp in iUpdateGens)
                    {
                        IMemberUpdate mu = isOnActivate ? ((IIndexUpdateGenerator)kvp.Value.Item3).CreateMemberUpdate(befImgs[kvp.Key])
                                                        : ((IIndexUpdateGenerator)kvp.Value.Item3).CreateMemberUpdate(props, befImgs[kvp.Key]);
                        updates.Add(kvp.Key, mu);
                    }
                }

                success = await IndexHandler.ApplyIndexUpdates(iGrainTypes, this.AsReference<IIndexableGrain>(GrainFactory), updates.AsImmutable(), RuntimeAddress);
                if (success)
                {
                    UpdateBeforeImages(updates);
                }
                else
                {
                    // assume that IndexHandler returned false because our list of indexes is invalid
                    _iUpdateGens = IndexHandler.GetIndexes(iGrainTypes[0]); // retry
                    iUpdateGens = _iUpdateGens;
                    AddMissingBeforeImages();
                }

            } while (!success);
        }

        /// <summary>
        /// Updates I-indexes.
        /// </summary>
        protected Task UpdateInitializedIndexes(bool isOnActivate, TProperties props)
        {
            return TaskDone.Done;
        }

        /// <summary>
        /// This method finds the IGrain interface that is the lowest one in the 
        /// interface type hierarchy of the current grain
        /// </summary>
        /// <returns>lowest IGrain interface in the hierarchy
        /// that the current class implements</returns>
        private IList<Type> getIIndexableGrainTypes()
        {
            if (_iGrainTypes == null)
            {
                _iGrainTypes = new List<Type>();
                Type iIndexableGrainTp = typeof(IIndexableGrain<TProperties>);

                Type[] interfaces = GetType().GetInterfaces();
                int numInterfaces = interfaces.Length;
                
                for (int i = 0; i < numInterfaces; ++i)
                {
                    Type otherIGrainType = interfaces[i];

                    //iIndexableGrainTp and typedIIndexableGrainTp are ignored when
                    //checking the descendants of IGrain, because there is no guarantee
                    //user defined grain interfaces extend these interfaces
                    if (iIndexableGrainTp != otherIGrainType && iIndexableGrainTp.IsAssignableFrom(otherIGrainType))
                    {
                        _iGrainTypes.Add(otherIGrainType);
                    }
                }
            }
            return _iGrainTypes;
        }

        /// <summary>
        /// This method checks the list of cached indexes, and if
        /// any index does not have a before-image, it will create
        /// one for it. As before-images are stored as an immutable
        /// field, a new map is created in this process.
        /// 
        /// This method is called on activation of the grain, and when the
        /// UpdateIndexes method detects an inconsistency between the indexes
        /// in the index handler and the cached indexes of the current grain.
        /// </summary>
        private void AddMissingBeforeImages()
        {
            IDictionary<string, Tuple<object, object, object>> iUpdateGens = _iUpdateGens;
            IDictionary<string, object> oldBefImgs = _beforeImages.Value;
            IDictionary<string, object> newBefImgs = new Dictionary<string, object>();
            foreach (KeyValuePair<string, Tuple<object, object, object>> idxOp in iUpdateGens)
            {
                var indexID = idxOp.Key;
                if (!oldBefImgs.ContainsKey(indexID))
                {
                    newBefImgs.Add(indexID, ((IIndexUpdateGenerator)idxOp.Value.Item3).ExtractIndexImage(Properties));
                }
                else
                {
                    newBefImgs.Add(indexID, oldBefImgs[indexID]);
                }
            }
            _beforeImages = newBefImgs.AsImmutable();
        }

        /// <summary>
        /// This method assumes that a set of changes is applied to the
        /// indexes, and then it replaces the current before-images with
        /// after-images produced by the update.
        /// </summary>
        /// <param name="updates">the member updates that were successfully
        /// applied to the current indexes</param>
        private void UpdateBeforeImages(IDictionary<string, IMemberUpdate> updates)
        {
            IDictionary<string, Tuple<object, object, object>> iUpdateGens = _iUpdateGens;
            IDictionary<string, object> befImgs = _beforeImages.GetCopy();
            foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
            {
                var indexID = updt.Key;
                var opType = updt.Value.GetOperationType();
                if (opType == IndexOperationType.Update || opType == IndexOperationType.Insert)
                {
                    befImgs[indexID] = ((IIndexUpdateGenerator)iUpdateGens[indexID].Item3).ExtractIndexImage(Properties);
                }
                else if(opType == IndexOperationType.Delete)
                {
                    befImgs[indexID] = null;
                }
            }
            _beforeImages = befImgs.AsImmutable();
        }

        protected override async Task WriteStateAsync()
        {
            //base.WriteStateAsync should be done before UpdateIndexes, in order to ensure
            //that only the successfully persisted bits get to be indexed, so we cannot do
            //these two tasks in parallel
            //await Task.WhenAll(base.WriteStateAsync(), UpdateIndexes());

            // during WriteStateAsync for a stateful indexable grain,
            // the indexes get updated concurrently while base.WriteStateAsync is done.
            await Task.WhenAll(base.WriteStateAsync(), UpdateIndexes(false, Properties));
        }

        Task<object> IIndexableGrain.ExtractIndexImage(IIndexUpdateGenerator iUpdateGen)
        {
            return Task.FromResult(iUpdateGen.ExtractIndexImage(Properties));
        }
    }

    /// <summary>
    /// This stateless IndexableGrainNonFaultTolerant is the super class of all stateless 
    /// indexable-grains. But as multiple-inheritance (from both Grain and 
    /// IndexableGrainNonFaultTolerant<T>) is not allowed, this class extends IndexableGrainNonFaultTolerant<object>
    /// and disables the storage functionality of Grain<T>
    /// </summary>
    public abstract class IndexableGrainNonFaultTolerant<TProperties> : IndexableGrainNonFaultTolerant<object, TProperties>, IIndexableGrain<TProperties> where TProperties : new()
    {
        protected override Task ClearStateAsync()
        {
            return TaskDone.Done;
        }

        protected override Task WriteStateAsync()
        {
            // The only thing that should be done during
            // WriteStateAsync for a stateless indexable grain
            // is to update its indexes
            return UpdateIndexes(false, Properties);
        }

        protected override Task ReadStateAsync()
        {
            return TaskDone.Done;
        }
    }
}
