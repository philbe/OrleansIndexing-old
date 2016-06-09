using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    [StatelessWorker]
    [StorageProvider(ProviderName = "IndexingStore")]
    public class IndexHandler<T> : Grain<IndexHandlerState>, IIndexHandler where T : Grain
    {
        private Immutable<IDictionary<string, IIndex>> _indexes;
        private Immutable<IDictionary<string, IIndexOps>> _indexOps;

        public override async Task OnActivateAsync()
        {
            await ReadStateAsync();
            _indexes = State.indexes.AsImmutable();
            IDictionary<string, IIndexOps> idxOps = new Dictionary<string, IIndexOps>();
            foreach (KeyValuePair<string, IIndex> idx in State.indexes)
            {
                idxOps.Add(idx.Key, await idx.Value.GetIndexOps());
            }
            _indexOps = idxOps.AsImmutable();
        }

        public async Task<bool> ApplyIndexUpdates(Immutable<IDictionary<string, IMemberUpdate>> iUpdates)
        {
            var updates = iUpdates.Value;
            var idxs = _indexes.Value;
            if (!updates.Keys.Equals(idxs.Keys)) return false;
            IList<Task<bool>> updateIndexTasks = new List<Task<bool>>();
            foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
            {
                updateIndexTasks.Add(idxs[updt.Key].ApplyIndexUpdates(updt.Value.AsImmutable()));
            }
            await Task.WhenAll(updateIndexTasks);
            bool allSuccessful = true;
            foreach (Task<bool> utask in updateIndexTasks)
            {
                allSuccessful = allSuccessful && (await utask);
            }
            if(!allSuccessful)
            {
                //TODO: we should do something about the failed index updates
            }
            return true;
        }

        public Task<Immutable<IDictionary<string, IIndexOps>>> GetIndexOps()
        {
            return Task.FromResult(_indexOps);
        }

        public Task<Immutable<IDictionary<string, IIndex>>> GetIndexes()
        {
        return Task.FromResult(_indexes);
        }
    }
}
