using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Orleans.Runtime;

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
    public abstract class IndexableGrain<T> : Grain<T>, IIndexableGrain<T>
    {
        /// <summary>
        /// an immutable cached version of IIndexUpdateGenerator instances
        /// for the current indexes on the grain.
        /// </summary>
        private Immutable<IDictionary<string, IIndexUpdateGenerator>> _iUpdateGens;

        /// <summary>
        /// an immutable copy of before-images of the indexed fields
        /// </summary>
        private Immutable<IDictionary<string, object>> _beforeImages;

        /// <summary>
        /// a cached grain interface type, which
        /// is cached on the first call to getIGrainType()
        /// </summary>
        private Type _iGrainType = null;

        /// <summary>
        /// Upon activation, the list of index update generators
        /// is retrieved from the index handler. It is cached in
        /// this grain for use in creating before-images, and also
        /// for later calls to UpdateIndexes.
        /// 
        /// Then, the before-images are created and stored in memory.
        /// </summary>
        public override async Task OnActivateAsync()
        {
            IIndexHandler handler = GetIndexHandler();
            _iUpdateGens = await handler.GetIndexUpdateGenerators();
            _beforeImages = new Dictionary<string, object>().AsImmutable<IDictionary<string, object>>();
            AddMissingBeforeImages();
            await base.OnActivateAsync();
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
        protected async Task UpdateIndexes()
        {
            IIndexHandler handler = GetIndexHandler();

            bool success = false;
            do
            {
                IDictionary<string, IMemberUpdate> updates = new Dictionary<string, IMemberUpdate>();
                IDictionary<string, IIndexUpdateGenerator> iUpdateGens = _iUpdateGens.Value;
                if (iUpdateGens.Count == 0) return;

                IDictionary<string, object> befImgs = _beforeImages.Value;
                foreach (KeyValuePair<string, IIndexUpdateGenerator> kvp in iUpdateGens)
                {
                    IMemberUpdate mu = kvp.Value.CreateMemberUpdate(this, befImgs[kvp.Key]);
                    updates.Add(kvp.Key, mu);
                }

                success = await handler.ApplyIndexUpdates(this.AsReference<IIndexableGrain>(), updates.AsImmutable());
                if (success)
                {
                    UpdateBeforeImages(updates);
                }
                else
                {
                    // assume that IndexHandler returned false because our list of indexes is invalid
                    _iUpdateGens = await handler.GetIndexUpdateGenerators(); // retry
                    iUpdateGens = _iUpdateGens.Value;
                    AddMissingBeforeImages();
                }

            } while (!success);
        }
        
        /// <returns>IndexHandler for the current grain</returns>
        private IIndexHandler GetIndexHandler()
        {
            Type thisIGrainType = getIGrainType();
            Type typedIndexHandlerType = typeof(IIndexHandler<>).MakeGenericType(thisIGrainType);
            return GrainFactory.GetGrain<IIndexHandler<IndexableGrain>>(TypeUtils.GetFullName(thisIGrainType), typedIndexHandlerType);
        }

        /// <summary>
        /// This method finds the IGrain interface that is the lowest one in the 
        /// interface type hierarchy of the current grain
        /// </summary>
        /// <returns>lowest IGrain interface in the hierarchy
        /// that the current class implements</returns>
        private Type getIGrainType()
        {
            if (_iGrainType == null)
            {
                Type iGrainTp = typeof(IGrain);
                Type iIndexableGrainTp = typeof(IIndexableGrain);
                Type typedIIndexableGrainTp = typeof(IIndexableGrain<T>);

                Type[] interfaces = this.GetType().GetInterfaces();
                int numInterfaces = interfaces.Length;

                Type thisIGrainType = iGrainTp;
                for (int i = 0; i < numInterfaces; ++i)
                {
                    Type otherIGrainType = interfaces[i];

                    //iIndexableGrainTp and typedIIndexableGrainTp are ignored when
                    //checking the descendants of IGrain, because there is no guarantee
                    //user defined grain interfaces extend these interfaces
                    if (otherIGrainType == iIndexableGrainTp || otherIGrainType == typedIIndexableGrainTp)
                        continue;
                    if (thisIGrainType.IsAssignableFrom(otherIGrainType))
                    {
                        thisIGrainType = otherIGrainType;
                    }
                }
                _iGrainType = thisIGrainType == iGrainTp ? typedIIndexableGrainTp : thisIGrainType;
            }
            return _iGrainType;
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
            IDictionary<string, IIndexUpdateGenerator> iUpdateGens = _iUpdateGens.Value;
            IDictionary<string, object> oldBefImgs = _beforeImages.Value;
            IDictionary<string, object> newBefImgs = new Dictionary<string, object>();
            foreach (KeyValuePair<string, IIndexUpdateGenerator> idxOp in iUpdateGens)
            {
                var indexID = idxOp.Key;
                if (!oldBefImgs.ContainsKey(indexID))
                {
                    newBefImgs.Add(indexID, idxOp.Value.ExtractIndexImage(this));
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
            IDictionary<string, IIndexUpdateGenerator> iUpdateGens = _iUpdateGens.Value;
            IDictionary<string, object> befImgs = _beforeImages.GetCopy();
            foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
            {
                var indexID = updt.Key;
                var opType = updt.Value.GetOperationType();
                if (opType == OperationType.Update || opType == OperationType.Insert)
                {
                    befImgs[indexID] = iUpdateGens[indexID].ExtractIndexImage(this);
                }
                else if(opType == OperationType.Delete)
                {
                    befImgs[indexID] = null;
                }
            }
            _beforeImages = befImgs.AsImmutable();
        }
    }

    /// <summary>
    /// This stateless IndexableGrain is the super class of all stateless 
    /// indexable-grains. But as multiple-inheritance (from both Grain and 
    /// IndexableGrain<T>) is not allowed, this class extends IndexableGrain<object>
    /// and disables the storage functionality of Grain<T>
    /// </summary>
    public abstract class IndexableGrain : IndexableGrain<object>, IIndexableGrain
    {
        protected override Task ClearStateAsync()
        {
            return TaskDone.Done;
        }

        protected override Task WriteStateAsync()
        {
            return TaskDone.Done;
        }

        protected override Task ReadStateAsync()
        {
            return TaskDone.Done;
        }
    }
}
