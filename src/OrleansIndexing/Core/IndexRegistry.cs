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
    //[StorageProvider(ProviderName = "IndexingStore")]
    public class IndexRegistry<T> : Grain<IndexRegistryState>, IIndexRegistry<T> where T : IGrain
    {
        public override async Task OnActivateAsync()
        {
            //await ReadStateAsync();
            if (State.indexes == null) State.indexes = new Dictionary<string, Tuple<IIndex, IndexMetaData>>();
            await base.OnActivateAsync();
        }

        public Task<IDictionary<string, Tuple<IIndex, IndexMetaData>>> GetIndexes()
        {
            return Task.FromResult(State.indexes);
        }

        public async Task<bool> RegisterIndex(string indexName, IIndex index, IndexMetaData indexMetaData)
        {
            if (State.indexes.ContainsKey(indexName))
            {
                throw new Exception(string.Format("Index with name ({0}) and type ({1}) already exists.", indexName, index.GetType()));
            }
            State.indexes.Add(indexName, Tuple.Create((IIndex)index, indexMetaData));
            await base.WriteStateAsync();
            return true;
        }
    }
}
