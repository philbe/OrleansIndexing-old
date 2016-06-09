using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace OrleansIndexing
{
    /// <summary>
    /// IndexableGrain class is the super-class of all grains that
    /// need to have indexing capability.
    /// 
    /// For making a grain indexable, two steps should be taken:
    ///     1- the grain should extend IndexableGrain
    ///     2- the grain is reponsible for calling UpdateIndexes
    ///        whenever the indexes should be updated
    /// </summary>
    public abstract class IndexableGrain : Grain
    {
        /// <summary>
        /// an immutable cached version of IIndexOps instances
        /// for the current indexes on the grain.
        /// </summary>
        private Immutable<IDictionary<string, IIndexOps>> _indexOps;
        /// <summary>
        /// an immutable copy of before images of the indexed fields
        /// </summary>
        private Immutable<IDictionary<string, object>> _beforeImages;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override async Task OnActivateAsync()
        {
            IIndexHandler handler = GetIndexHandler();
            _indexOps = await handler.GetIndexOps();
            _beforeImages = new Dictionary<string, object>().AsImmutable<IDictionary<string, object>>();
            AddMissingBeforeImages();
        }

        protected async Task UpdateIndexes()
        {
            IIndexHandler handler = GetIndexHandler();

            bool success = false;
            do
            {
                IDictionary<string, IMemberUpdate> updates = new Dictionary<string, IMemberUpdate>();
                IDictionary<string, IIndexOps> idxOps = _indexOps.Value;
                IDictionary<string, object> befImgs = _beforeImages.Value;
                foreach (KeyValuePair<string, IIndexOps> kvp in idxOps)
                {
                    IMemberUpdate mu = kvp.Value.CreateMemberUpdate(this, befImgs[kvp.Key]);
                    updates.Add(kvp.Key, mu);
                }

                success = await handler.ApplyIndexUpdates(updates.AsImmutable());
                if (success)
                {
                    UpdateBeforeImages(updates);
                }
                else
                {
                    // assume that IndexHandler returned false because our list of indexes is invalid
                    _indexOps = await handler.GetIndexOps(); // retry
                    AddMissingBeforeImages();
                }

            } while (!success);
        }

        protected virtual IIndexHandler GetIndexHandler()
        {
            return GrainFactory.GetGrain<IIndexHandler>("IndexHandler<" + GetType().Name + ">");
        }

        private void AddMissingBeforeImages()
        {
            IDictionary<string, IIndexOps> idxOps = _indexOps.Value;
            IDictionary<string, object> oldBefImgs = _beforeImages.Value;
            IDictionary<string, object> newBefImgs = new Dictionary<string, object>();
            foreach (KeyValuePair<string, IIndexOps> idxOp in idxOps)
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
            IDictionary<string, IIndexOps> idxOps = _indexOps.Value;
            IDictionary<string, object> befImgs = _beforeImages.GetCopy();
            foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
            {
                var indexID = updt.Key;
                if (updt.Value.IsUpdated()) {
                    befImgs.Add(indexID, idxOps[indexID].ExtractIndexImage(this));
                }
            }
            _beforeImages = befImgs.AsImmutable();
        }
    }
}
