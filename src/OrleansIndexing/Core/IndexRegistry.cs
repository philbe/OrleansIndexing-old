using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    //[StorageProvider(ProviderName = "IndexingStore")]
    public class IndexRegistry<T> : Grain<IndexRegistryState>, IIndexRegistry<T> where T : IIndexableGrain
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
            var writeTask = base.WriteStateAsync();
            var reloadTask = IndexFactory.ReloadIndexes<T>();
            await Task.WhenAll(writeTask, reloadTask);
            return writeTask.Status == TaskStatus.RanToCompletion && reloadTask.Status == TaskStatus.RanToCompletion;
        }

        public async Task<bool> DropIndex(string indexName)
        {
            Tuple<IIndex, IndexMetaData> index;
            State.indexes.TryGetValue(indexName, out index);
            if (index != null)
            {
                await index.Item1.Dispose();
                return State.indexes.Remove(indexName);
            }
            else
            {
                throw new Exception(string.Format("Index with name ({0}) does not exist for type ({1}).", indexName, TypeUtils.GetFullName(typeof(T))));
            }
        }

        public async Task DropAllIndexes()
        {
            IList<Task> disposeTasks = new List<Task>();
            foreach (KeyValuePair<string, Tuple<IIndex, IndexMetaData>> index in State.indexes)
            {
                disposeTasks.Add(index.Value.Item1.Dispose());
            }
            await Task.WhenAll(disposeTasks);
            State.indexes.Clear();
        }
    }
}
