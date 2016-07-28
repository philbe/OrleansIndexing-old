﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Providers;
using System.Collections.Concurrent;
using System.Threading;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-bucket in-memory hash-index
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Reentrant]
    public abstract class HashIndexPartitionedPerKeyBucket<K, V> : Grain<HashIndexBucketState<K,V>>, HashIndexPartitionedPerKeyBucketInterface<K, V> where V : class, IIndexableGrain
    {

        public override Task OnActivateAsync()
        {
            if (State.IndexMap == null) State.IndexMap = new ConcurrentDictionary<K, HashIndexSingleBucketEntry<V>>();
            State.IndexStatus = IndexStatus.Available;

            writeRequestIdGen = 0;
            pendingWriteRequests = new ConcurrentDictionary<int, byte>();
            return base.OnActivateAsync();
        }

        #region Multi-threaded Index Update
        #region Multi-threaded Index Update Variables
        /// <summary>
        /// This lock is used to synchronize the access
        /// to the shared map that stores the index information
        /// </summary>
        private object modify_lock = new object();

        /// <summary>
        /// This lock is used to queue all the writes to the storage
        /// and do them in a single batch, i.e., group commit
        /// 
        /// Works hand-in-hand with pendingWriteRequests and writeRequestIdGen.
        /// </summary>
        private AsyncLock write_lock = new AsyncLock();

        /// <summary>
        /// Creates a unique ID for each write request to the storage.
        /// 
        /// The values generated by this ID generator are used in pendingWriteRequests
        /// </summary>
        private volatile int writeRequestIdGen;

        /// <summary>
        /// All the write requests that are waiting behind write_lock are accumulated
        /// in this data structure, and all of them will be done at once.
        /// </summary>
        private volatile ConcurrentDictionary<int, byte> pendingWriteRequests;

        #endregion Multi-threaded Index Update Variables

        public Task<bool> ApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, SiloAddress siloAddress)
        {
            IMemberUpdate updt = iUpdate.Value;
            return ApplyIndexUpdate(g, updt, isUniqueIndex, updt.GetOperationType());
        }

        public async Task<bool> ApplyIndexUpdate(IIndexableGrain g, IMemberUpdate iUpdate, bool isUniqueIndex, IndexOperationType opType)
        {
            //the index can start processing update as soon as it becomes
            //visible to index handler and does not have to wait for any
            //further event regarding index builder, so it is not necessary
            //to have a Created state
            //if (State.IndexStatus == IndexStatus.Created) return true;

            //this variable determines whether index was still unavailable
            //when we received a delete operation
            bool fixIndexUnavailableOnDelete = false;

            //the target grain that is updated
            V updatedGrain = g.AsReference<V>(GrainFactory);

            K befImg;
            HashIndexSingleBucketEntry<V> befEntry;
            lock (modify_lock)
            {
                //Updates the index bucket
                HashIndexBucketUtils.UpdateBucket(updatedGrain, iUpdate, opType, State, isUniqueIndex, out befImg, out befEntry, out fixIndexUnavailableOnDelete);
            }
            //if the index was still unavailable
            //when we received a delete operation
            if (fixIndexUnavailableOnDelete)
            {
                State.IndexStatus = await GetIndexBuilder().AddTombstone(updatedGrain) ? IndexStatus.Available : State.IndexStatus;
                if (State.IndexMap.TryGetValue(befImg, out befEntry) && befEntry.Values.Contains(updatedGrain))
                {
                    befEntry.Values.Remove(updatedGrain);
                    var isAvailable = await GetIndexBuilder().AddTombstone(updatedGrain);
                    if (State.IndexStatus != IndexStatus.Available && isAvailable)
                    {
                        State.IndexStatus = IndexStatus.Available;
                    }
                }
            }

            //create a write-request ID, which is used for group commit
            int writeRequestId = Interlocked.Increment(ref writeRequestIdGen);
            //add the write-request ID to the pending write requests
            while (!pendingWriteRequests.TryAdd(writeRequestId, default(byte))) ;
            //wait before any previous write is done
            using (await write_lock.LockAsync())
            {
                //if the write request was not already handled
                //by a previous group write attempt
                if (pendingWriteRequests.ContainsKey(writeRequestId))
                {
                    //clear all pending write requests, as this attempt will do them all.
                    pendingWriteRequests.Clear();
                    //write the index state back to the storage
                    await base.WriteStateAsync();
                }
                //else
                //{
                //    Nothing! It's already been done by a previous worker.
                //}
            }
            return true;
        }

        private IIndexBuilder<V> GetIndexBuilder()
        {
            return GrainFactory.GetGrain<IIndexBuilder<V>>(this.GetPrimaryKeyString());
        }
        #endregion Multi-threaded Index Update

        //public Task<bool> IsUnique()
        //{
        //    return Task.FromResult(State.IsUnique);
        //}

        public async Task Lookup(IOrleansQueryResultStream<V> result, K key)
        {
            if (!(State.IndexStatus == IndexStatus.Available))
            {
                var e = new Exception(string.Format("Index is not still available."));
                GetLogger().Error((int)ErrorCode.IndexingIndexIsNotReadyYet, "Index is not still available.", e);
                throw e;
            }
            HashIndexSingleBucketEntry<V> entry;
            if (State.IndexMap.TryGetValue(key, out entry))
            {
                await result.OnNextBatchAsync(entry.Values);
                await result.OnCompletedAsync();
            }
            else
            {
                await result.OnCompletedAsync();
            }
        }

        public Task<V> LookupUnique(K key)
        {
            if (!(State.IndexStatus == IndexStatus.Available))
            {
                var e = new Exception(string.Format("Index is not still available."));
                GetLogger().Error((int)ErrorCode.IndexingIndexIsNotReadyYet, e.Message, e);
                throw e;
            }
            HashIndexSingleBucketEntry<V> entry;
            if (State.IndexMap.TryGetValue(key, out entry))
            {
                if (entry.Values.Count() == 1)
                {
                    return Task.FromResult(entry.Values.GetEnumerator().Current);
                }
                else
                {
                    var e = new Exception(string.Format("There are {0} values for the unique lookup key \"{1}\" does not exist on index \"{2}\".", entry.Values.Count(), key, IndexUtils.GetIndexNameFromIndexGrain(this)));
                    GetLogger().Error((int)ErrorCode.IndexingIndexIsNotReadyYet, e.Message, e);
                    throw e;
                }
            }
            else
            {
                var e = new Exception(string.Format("The lookup key \"{0}\" does not exist on index \"{1}\".", key, IndexUtils.GetIndexNameFromIndexGrain(this)));
                GetLogger().Error((int)ErrorCode.IndexingIndexIsNotReadyYet, e.Message, e);
                throw e;
            }
        }

        public Task Dispose()
        {
            State.IndexMap.Clear();
            return TaskDone.Done;
        }

        private IIndexBuilder<V> GetIndexBuilder(IGrainFactory gf)
        {
            return gf.GetGrain<IIndexBuilder<V>>(IndexUtils.GetIndexGrainID(typeof(V), IndexUtils.GetIndexNameFromIndexGrain(this)));
        }

        public Task<bool> IsAvailable()
        {
            return Task.FromResult(State.IndexStatus == IndexStatus.Available);
        }

        Task IIndex.Lookup(IOrleansQueryResultStream<IIndexableGrain> result, object key)
        {
            return Lookup(result.Cast<V>(), (K)key);
        }

        public Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            if (!(State.IndexStatus == IndexStatus.Available))
            {
                var e = new Exception(string.Format("Index is not still available."));
                GetLogger().Error((int)ErrorCode.IndexingIndexIsNotReadyYet, "Index is not still available.", e);
                throw e;
            }
            HashIndexSingleBucketEntry<V> entry;
            if (State.IndexMap.TryGetValue(key, out entry))
            {
                return Task.FromResult((IOrleansQueryResult<V>)new OrleansQueryResult<V>(entry.Values));
            }
            else
            {
                return Task.FromResult((IOrleansQueryResult<V>)new OrleansQueryResult<V>(Enumerable.Empty<V>()));
            }
        }

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return await Lookup((K)key);
        }
    }
}
