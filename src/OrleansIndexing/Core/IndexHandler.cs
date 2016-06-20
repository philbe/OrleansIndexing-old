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
    /// <summary>
    /// IndexHandler is responsible for updating the indexes defined
    /// for a grain interface type. It  also communicates with the grain
    /// instances by telling them about the list of available indexes.
    /// 
    /// The fact that IndexHandler is a StatelessWorker makes it
    /// very scalable, but at the same time should stay in sync
    /// with index registry to be aware of the available indexes.
    /// </summary>
    /// <typeparam name="T">the type of grain interface type of
    /// the grain that is handled by this index handler</typeparam>
    [StatelessWorker]
    public class IndexHandler<T> : Grain, IIndexHandler<T> where T : IIndexableGrain
    {
        private Immutable<IDictionary<string, Tuple<IIndex,IndexMetaData>>> _indexes;
        private Immutable<IDictionary<string, IIndexUpdateGenerator>> _iUpdateGens;
        private IIndexRegistry<T> _indexRegistry;

        /// <summary>
        /// Upon activation, the list of indexes are read and
        /// cached from the corresponding index registry
        /// </summary>
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
                iUpdateGens.Add(idx.Key, idx.Value.Item2.getIndexUpdateGeneratorInstance());
            }
            _iUpdateGens = iUpdateGens.AsImmutable();
        }

        public async Task<IIndex> GetIndex(string indexName)
        {
            Tuple<IIndex, IndexMetaData> index;
            if (_indexes.Value.TryGetValue(indexName, out index))
            {
                return index.Item1;
            }
            else
            {
                //this part of code is commented out, because it should
                //never happen that the indexes are not loaded, if the
                //index is registered in the index registry
                //await ReloadIndexes();
                //if (_indexes.Value.TryGetValue(indexName, out index))
                //{
                //    return index.Item1;
                //}
                //else
                //{
                    throw new Exception(string.Format("Index \"{0}\" does not exist for {1}.", indexName, typeof(T)));
                //}
            }
        }
    }
}
