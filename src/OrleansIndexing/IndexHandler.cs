using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    [StatelessWorker]
    public class IndexHandler<T> : Grain, IIndexHandler<T> where T : IGrain
    {
        private Immutable<IDictionary<string, Tuple<IIndex,IndexMetaData>>> _indexes;
        private Immutable<IDictionary<string, IIndexUpdateGenerator>> _iUpdateGens;
        private IIndexRegistry<T> _indexRegistry;

        public override async Task OnActivateAsync()
        {
            _indexRegistry = GrainFactory.GetGrain<IIndexRegistry<T>>(TypeUtils.GetFullName(typeof(T)));
            await Task.WhenAll(ReloadIndexes(), base.OnActivateAsync());
        }

        public async Task<bool> ApplyIndexUpdates(IIndexableGrain updatedGrain, Immutable<IDictionary<string, IMemberUpdate>> iUpdates)
        {
            var updates = iUpdates.Value;
            var idxs = _indexes.Value;
            if (!updates.Keys.ToSet().SetEquals(idxs.Keys)) return false;
            IList<Task<bool>> updateIndexTasks = new List<Task<bool>>();
            foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
            {
                updateIndexTasks.Add(idxs[updt.Key].Item1.ApplyIndexUpdate(updatedGrain, updt.Value.AsImmutable()));
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

        public Task<Immutable<IDictionary<string, IIndexUpdateGenerator>>> GetIndexUpdateGenerators()
        {
            return Task.FromResult(_iUpdateGens);
        }

        public Task<Immutable<IDictionary<string, Tuple<IIndex, IndexMetaData>>>> GetIndexes()
        {
        return Task.FromResult(_indexes);
        }

        public async Task ReloadIndexes()
        {
            _indexes = (await _indexRegistry.GetIndexes()).AsImmutable();
            IDictionary<string, IIndexUpdateGenerator> iUpdateGens = new Dictionary<string, IIndexUpdateGenerator>();
            foreach (KeyValuePair<string, Tuple<IIndex, IndexMetaData>> idx in _indexes.Value)
            {
                iUpdateGens.Add(idx.Key, await idx.Value.Item1.GetIndexUpdateGenerator());
            }
            _iUpdateGens = iUpdateGens.AsImmutable();
        }
    }
}
