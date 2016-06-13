using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    [StatelessWorker]
    [StorageProvider(ProviderName = "IndexingStore")]
    public class IndexRegistry<T> : Grain<IndexRegistryState>, IIndexRegistry<T> where T : Grain
    {
        public override async Task OnActivateAsync()
        {
            await ReadStateAsync();
            await base.OnActivateAsync();
        }

        public Task<IDictionary<string, IIndex>> GetIndexes()
        {
            return Task.FromResult(State.indexes);
        }
    }
}
