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
    public static class IndexHandler
    {
        internal static async Task<bool> ApplyIndexUpdates(IList<Type> iGrainTypes, IIndexableGrain updatedGrain, Immutable<IDictionary<string, IMemberUpdate>> iUpdates, SiloAddress siloAddress)
        {
            var updates = iUpdates.Value;
            foreach (Type iGrainType in iGrainTypes)
            {
                var idxs = GetIndexes(iGrainType);
                if (!updates.Keys.ToSet().SetEquals(idxs.Keys)) return false;
                IList<Task<bool>> updateIndexTasks = new List<Task<bool>>();
                foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
                {
                    var idxInfo = idxs[updt.Key];
                    updateIndexTasks.Add(((IIndex)idxInfo.Item1).ApplyIndexUpdate(updatedGrain, updt.Value.AsImmutable(), ((IndexMetaData)idxInfo.Item2).IsUniqueIndex(), siloAddress));
                }
                await Task.WhenAll(updateIndexTasks);
                bool allSuccessful = true;
                foreach (Task<bool> utask in updateIndexTasks)
                {
                    allSuccessful = allSuccessful && (await utask);
                }
                if (!allSuccessful)
                {
                    //TODO: we should do something about the failed index updates
                    throw new Exception(string.Format("Not all index updates where successful for updatedGrain = {1}", updatedGrain));
                }
            }
            return true;
        }

        //internal static Task<bool> ApplyIndexUpdates<T>(IIndexableGrain updatedGrain, Immutable<IDictionary<string, IMemberUpdate>> iUpdates) where T : IIndexableGrain
        //{
        //    return ApplyIndexUpdates(typeof(T), updatedGrain, iUpdates);
        //}

        internal static IDictionary<string, Tuple<object, object, object>> GetIndexes(Type iGrainType)
        {
            return IndexRegistry.GetIndexes(iGrainType);
        }

        internal static IDictionary<string, Tuple<object, object, object>> GetIndexes<T>() where T : IIndexableGrain
        {
            return IndexRegistry.GetIndexes<T>();
        }

        internal static IIndex GetIndex(Type iGrainType, string indexName)
        {
            Tuple<object, object, object> index;
            if (GetIndexes(iGrainType).TryGetValue(indexName, out index))
            {
                return (IIndex)index.Item1;
            }
            else
            {
                //this part of code is commented out, because it should
                //never happen that the indexes are not loaded, if the
                //index is registered in the index registry
                //await ReloadIndexes();
                //if (_indexes.Value.TryGetValue(indexName, out index))
                //{
                //    return Task.FromResult(index.Item1);
                //}
                //else
                //{
                throw new Exception(string.Format("Index \"{0}\" does not exist for {1}.", indexName, iGrainType));
                //}
            }
        }

        internal static IIndex GetIndex<T>(string indexName) where T : IIndexableGrain
        {
            return GetIndex(typeof(T), indexName);
        }

        internal static IIndex<K,V> GetIndex<K,V>(string indexName) where V : IIndexableGrain
        {
            return (IIndex<K,V>)GetIndex(typeof(V), indexName);
        }
    }
}
