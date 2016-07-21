using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    [Serializable]
    public class IHashIndexPartitionedPerKey<K, V> : IHashIndex<K, V> where V : class, IIndexableGrain
    {
        private string _indexName;
        //private bool _isUnique;

        public IHashIndexPartitionedPerKey(string indexName, bool isUniqueIndex)
        {
            _indexName = indexName;
            //_isUnique = isUniqueIndex;
        }

        public async Task<bool> ApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, SiloAddress siloAddress)
        {
            MemberUpdate update = (MemberUpdate)iUpdate.Value;
            OperationType opType = update.GetOperationType();
            if (opType == OperationType.Update)
            {
                int befImgHash = update.GetBeforeImage().GetHashCode();
                int aftImgHash = update.GetAfterImage().GetHashCode();
                IHashIndexPartitionedPerKeyBucket<K, V> befImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                );
                if (befImgHash == aftImgHash)
                {
                    return await befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex);
                }
                else
                {
                    IHashIndexPartitionedPerKeyBucket<K, V> aftImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                        IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                    );
                    var befTask = befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex, OperationType.Delete);
                    var aftTask = aftImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex, OperationType.Insert);
                    bool[] results = await Task.WhenAll(befTask, aftTask);
                    return results[0] && results[1];
                }
            }
            else if(opType == OperationType.Insert)
            {
                int aftImgHash = update.GetAfterImage().GetHashCode();
                IHashIndexPartitionedPerKeyBucket<K, V> aftImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + aftImgHash
                );
                return await aftImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex);
            }
            else if(opType == OperationType.Delete)
            {
                int befImgHash = update.GetBeforeImage().GetHashCode();
                IHashIndexPartitionedPerKeyBucket<K, V> befImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                );
                return await befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex);
            }
            return true;
        }

        public Task Lookup(IOrleansQueryResult<V> result, K key)
        {
            IHashIndexPartitionedPerKeyBucket<K, V> targetBucket = RuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + key.GetHashCode()
            );
            return targetBucket.Lookup(result, key);
        }

        public async Task<V> LookupUnique(K key)
        {
            var result = new OrleansFirstQueryResult<V>();
            var taskCompletionSource = new TaskCompletionSource<V>();
            Task<V> tsk = taskCompletionSource.Task;
            Action<V> responseHandler = taskCompletionSource.SetResult;
            await result.SubscribeAsync(new QueryFirstResultObserver<V>(responseHandler));
            await Lookup(result, key);
            return await tsk;
        }

        public Task Dispose()
        {
            //right now, we cannot do anything.
            //we need to know the list of buckets
            return TaskDone.Done;
        }

        public Task<bool> IsAvailable()
        {
            return Task.FromResult(true);
        }

        Task IIndex.Lookup(IOrleansQueryResult<IIndexableGrain> result, object key)
        {
            return Lookup(result.Cast<V>(), (K)key);
        }

        public Task<IEnumerable<V>> Lookup(K key)
        {
            IHashIndexPartitionedPerKeyBucket<K, V> targetBucket = RuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                   IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + key.GetHashCode()
               );
            return targetBucket.Lookup(key);
        }

        async Task<IEnumerable<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return await Lookup((K)key);
        }
    }
}
