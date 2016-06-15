using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// IndexableGrain class is the super-class of all grains that
    /// need to have indexing capability.
    /// 
    /// To make a grain indexable, two steps should be taken:
    ///     1- the grain class should extend IndexableGrain
    ///     2- the grain class is reponsible for calling UpdateIndexes
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
        
        public override async Task OnActivateAsync()
        {
            IIndexHandler handler = GetIndexHandler();
            _iUpdateGens = await handler.GetIndexUpdateGenerators();
            _beforeImages = new Dictionary<string, object>().AsImmutable<IDictionary<string, object>>();
            AddMissingBeforeImages();
            await base.OnActivateAsync();
        }

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

        protected virtual IIndexHandler GetIndexHandler()
        {
            return GrainFactory.GetGrain<IIndexHandler>(string.Format("IndexHandler<{0}>", GetType().Name));
        }

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
