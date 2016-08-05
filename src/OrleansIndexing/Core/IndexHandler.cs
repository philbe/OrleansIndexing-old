using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        internal static async Task<bool> ApplyIndexUpdates(IList<Type> iGrainTypes, IIndexableGrain updatedGrain, IDictionary<string, IMemberUpdate> updates, SiloAddress siloAddress, bool updateIndexesEagerly)
        {
            if(updateIndexesEagerly)
            {
                return await ApplyIndexUpdatesEagerly(iGrainTypes, updatedGrain, updates, siloAddress, updateIndexesEagerly);
            }
            //TODO not implemented yet!
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<bool> ApplyIndexUpdatesEagerly(IList<Type> iGrainTypes, IIndexableGrain updatedGrain, IDictionary<string, IMemberUpdate> updates, SiloAddress siloAddress, bool updateIndexesEagerly)
        {
            Task<bool>[] updateTasks = new Task<bool>[iGrainTypes.Count()];
            int i = 0;
            foreach (Type iGrainType in iGrainTypes)
            {
                updateTasks[i++] = ApplyIndexUpdatesEagerly(iGrainType, updatedGrain, updates, siloAddress, updateIndexesEagerly);
            }
            return CombineResults(await Task.WhenAll(updateTasks));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<bool> ApplyIndexUpdatesEagerly(Type iGrainType, IIndexableGrain updatedGrain, IDictionary<string, IMemberUpdate> updates, SiloAddress siloAddress, bool updateIndexesEagerly)
        {
            var idxs = GetIndexes(iGrainType);
            if (!updates.Keys.ToSet().SetEquals(idxs.Keys)) return false;
            IList<Task<bool>> updateIndexTasks = new List<Task<bool>>();
            foreach (KeyValuePair<string, IMemberUpdate> updt in updates)
            {
                var idxInfo = idxs[updt.Key];
                if (updt.Value.GetOperationType() != IndexOperationType.None)
                {
                    updateIndexTasks.Add(((IndexInterface)idxInfo.Item1).ApplyIndexUpdate(updatedGrain, updt.Value.AsImmutable(), ((IndexMetaData)idxInfo.Item2).IsUniqueIndex(), siloAddress));
                }
            }

            bool[] updateResults = await Task.WhenAll(updateIndexTasks);
            bool allSuccessful = CombineResults(updateResults);
            if (!allSuccessful)
            {
                //TODO: we should do something about the failed index updates
                throw new Exception(string.Format("Not all index updates where successful"));
            }
            return allSuccessful;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CombineResults(bool[] updateResults)
        {
            bool allSuccessful = true;
            foreach (bool updateRes in updateResults)
            {
                allSuccessful &= updateRes;
                if (!allSuccessful) break;
            }

            return allSuccessful;
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

        internal static IndexInterface GetIndex(Type iGrainType, string indexName)
        {
            Tuple<object, object, object> index;
            if (GetIndexes(iGrainType).TryGetValue(indexName, out index))
            {
                return (IndexInterface)index.Item1;
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

        internal static IndexInterface GetIndex<T>(string indexName) where T : IIndexableGrain
        {
            return GetIndex(typeof(T), indexName);
        }

        internal static IndexInterface<K,V> GetIndex<K,V>(string indexName) where V : IIndexableGrain
        {
            return (IndexInterface<K,V>)GetIndex(typeof(V), indexName);
        }
    }
}
