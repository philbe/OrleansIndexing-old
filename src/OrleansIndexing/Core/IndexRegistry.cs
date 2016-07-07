using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public static class IndexRegistry
    {

        //internal static Task<bool> RegisterIndex(Type iGrainType, string indexName, IIndex index, IndexMetaData indexMetaData)
        //{
        //    if (GetIndexes(iGrainType).ContainsKey(indexName))
        //    {
        //        throw new Exception(string.Format("Index with name ({0}) and type ({1}) already exists.", indexName, index.GetType()));
        //    }
        //    GetIndexes(iGrainType).Add(indexName, Tuple.Create((object)index, (object)indexMetaData, (object)indexMetaData.getIndexUpdateGeneratorInstance()));
        //    return Task.FromResult(true);
        //}

        //internal static Task<bool> RegisterIndex<T>(string indexName, IIndex index, IndexMetaData indexMetaData) where T : IIndexableGrain
        //{
        //    var iGrainType = typeof(T);
        //    return RegisterIndex(iGrainType, indexName, index, indexMetaData);
        //}

        //internal static async Task<bool> DropIndex<T>(string indexName) where T : IIndexableGrain
        //{
        //    var iGrainType = typeof(T);
        //    Tuple<object, object, object> index;
        //    GetIndexes(iGrainType).TryGetValue(indexName, out index);
        //    if (index != null)
        //    {
        //        await ((IIndex)index.Item1).Dispose();
        //        return GetIndexes(iGrainType).Remove(indexName);
        //    }
        //    else
        //    {
        //        throw new Exception(string.Format("Index with name ({0}) does not exist for type ({1}).", indexName, TypeUtils.GetFullName(typeof(T))));
        //    }
        //}

        //internal static async Task DropAllIndexes<T>() where T : IIndexableGrain
        //{
        //    var iGrainType = typeof(T);
        //    IList<Task> disposeTasks = new List<Task>();
        //    foreach (KeyValuePair<string, Tuple<object, object, object>> index in GetIndexes(iGrainType))
        //    {
        //        disposeTasks.Add(((IIndex)index.Value.Item1).Dispose());
        //    }
        //    await Task.WhenAll(disposeTasks);
        //    GetIndexes(iGrainType).Clear();
        //}

        internal static IDictionary<string, Tuple<object, object, object>> GetIndexes(Type iGrainType)
        {
            if (InsideRuntimeClient.Current.Indexes == null) InsideRuntimeClient.Current.Indexes = new Dictionary<Type, IDictionary<string, Tuple<object, object, object>>>();
            IDictionary<string, Tuple<object, object, object>> indexes;
            if (!InsideRuntimeClient.Current.Indexes.TryGetValue(iGrainType, out indexes))
            {
                indexes = new Dictionary<string, Tuple<object, object, object>>();
                InsideRuntimeClient.Current.Indexes.Add(iGrainType, indexes);
            }
            return indexes;
        }

        internal static IDictionary<string, Tuple<object, object, object>> GetIndexes<T>() where T : IIndexableGrain
        {
            return GetIndexes(typeof(T));
        }
    }
}
