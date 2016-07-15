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
    public class IHashIndexPartitionedPerKey<K, V> : IHashIndex<K, V> where V : IIndexableGrain
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
                    return await befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex).ConfigureAwait(false);
                }
                else
                {
                    IHashIndexPartitionedPerKeyBucket<K, V> aftImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                        IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                    );
                    var befTask = befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex, OperationType.Delete);
                    var aftTask = aftImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex, OperationType.Insert);
                    await Task.WhenAll(befTask, aftTask).ConfigureAwait(false);
                    return befTask.Result && aftTask.Result;
                }
            }
            else if(opType == OperationType.Insert)
            {
                int aftImgHash = update.GetAfterImage().GetHashCode();
                IHashIndexPartitionedPerKeyBucket<K, V> aftImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + aftImgHash
                );
                return await aftImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex).ConfigureAwait(false);
            }
            else if(opType == OperationType.Delete)
            {
                int befImgHash = update.GetBeforeImage().GetHashCode();
                IHashIndexPartitionedPerKeyBucket<K, V> befImgBucket = InsideRuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                    IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + befImgHash
                );
                return await befImgBucket.ApplyIndexUpdate(g, iUpdate, isUniqueIndex).ConfigureAwait(false);
            }
            return true;
        }

        public Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            IHashIndexPartitionedPerKeyBucket<K, V> targetBucket = RuntimeClient.Current.InternalGrainFactory.GetGrain<IHashIndexPartitionedPerKeyBucket<K, V>>(
                IndexUtils.GetIndexGrainID(typeof(V), _indexName) + "_" + key.GetHashCode()
            );
            return targetBucket.Lookup(key);
        }

        public async Task<V> LookupUnique(K key)
        {
            return (await Lookup(key).ConfigureAwait(false)).GetFirst();
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

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return (IOrleansQueryResult<IIndexableGrain>)await Lookup((K)key).ConfigureAwait(false);
        }
    }
}
