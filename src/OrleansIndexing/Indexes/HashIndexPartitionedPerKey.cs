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
    /// A simple implementation of a partitioned in-memory hash-index
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    public abstract class HashIndexPartitionedPerKey<K, V, BucketT> : HashIndexInterface<K, V> where V : class, IIndexableGrain where BucketT : HashIndexPartitionedPerKeyBucketInterface<K, V>, IGrainWithStringKey
    {
        private string _indexName;
        //private bool _isUnique;

        public HashIndexPartitionedPerKey(string indexName, bool isUniqueIndex)
        {
            _indexName = indexName;
            //_isUnique = isUniqueIndex;
        }

        public async Task<bool> ApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, SiloAddress siloAddress)
        {
            IMemberUpdate update = iUpdate.Value;
            OperationType opType = update.GetOperationType();
            if (opType == OperationType.Update)
            {
                int befImgHash = update.GetBeforeImage().GetHashCode();
                int aftImgHash = update.GetAfterImage().GetHashCode();
                BucketT befImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<BucketT>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                );
                if (befImgHash == aftImgHash)
                {
                    return await befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex);
                }
                else
                {
                    BucketT aftImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<BucketT>(
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
                BucketT aftImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<BucketT>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + aftImgHash
                );
                return await aftImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex);
            }
            else if(opType == OperationType.Delete)
            {
                int befImgHash = update.GetBeforeImage().GetHashCode();
                BucketT befImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<BucketT>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                );
                return await befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex);
            }
            return true;
        }

        public Task Lookup(IOrleansQueryResultStream<V> result, K key)
        {
            BucketT targetBucket = RuntimeClient.Current.InternalGrainFactory.GetGrain<BucketT>(
                IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + key.GetHashCode()
            );
            return targetBucket.Lookup(result, key);
        }

        public async Task<V> LookupUnique(K key)
        {
            var result = new OrleansFirstQueryResultStream<V>();
            var taskCompletionSource = new TaskCompletionSource<V>();
            Task<V> tsk = taskCompletionSource.Task;
            Action<V> responseHandler = taskCompletionSource.SetResult;
            await result.SubscribeAsync(new QueryFirstResultStreamObserver<V>(responseHandler));
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

        Task IIndex.Lookup(IOrleansQueryResultStream<IIndexableGrain> result, object key)
        {
            return Lookup(result.Cast<V>(), (K)key);
        }

        public Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            BucketT targetBucket = RuntimeClient.Current.InternalGrainFactory.GetGrain<BucketT>(
                   IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + key.GetHashCode()
               );
            return targetBucket.Lookup(key);
        }

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return await Lookup((K)key);
        }
    }
}
