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
    //[StorageProvider(ProviderName = "IndexingStore")]
    public class IndexRegistry<T> : Grain<IndexRegistryState>, IIndexRegistry<T> where T : IGrain
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

        public async Task<bool> RegisterIndex(string indexName, IIndex index)
        {
            if (State.indexes == null) State.indexes = new Dictionary<string, IIndex>();
            if (State.indexes.ContainsKey(indexName))
            {
                throw new Exception(string.Format("Index with name ({0}) and type ({1}) already exists.", indexName, index.GetType()));
            }
            State.indexes.Add(indexName, index);
            await base.WriteStateAsync();
            return true;
        }
    }
}
