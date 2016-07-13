﻿using Orleans;
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
    public class HashIndexSingleBucket<K, V> : Grain<HashIndexBucketState<K,V>>, IHashIndexSingleBucket<K, V> where V : IIndexableGrain
    {
        //private Func<K, K, bool> _equalsLambda = ((k1,k2) => k1.Equals(k2));
        //private Func<K, long> _hashLambda = (k => k.GetHashCode());

        public override async Task OnActivateAsync()
        {
            //await ReadStateAsync();
            if (State.IndexMap == null) State.IndexMap = new Dictionary<K, HashIndexSingleBucketEntry<V>>();
            State.IndexStatus = IndexStatus.Available;
            if (State.IndexStatus == IndexStatus.UnderConstruction)
            {
                //var _ = GetIndexBuilder().BuildIndex(indexName, this, IndexUtils.GetIndexUpdateGenerator<V>(GrainFactory, IndexUtils.GetIndexNameFromIndexGrain(this)));
            }
            await base.OnActivateAsync();
        }

        public async Task<bool> ApplyIndexUpdate(IGrainFactory gf, IIndexableGrain g, Immutable<IMemberUpdate> iUpdate)
        {
            //the index can start processing update as soon as it becomes
            //visible to index handler and does not have to wait for any
            //further event regarding index builder, so it is not necessary
            //to have a Created state
            //if (State.IndexStatus == IndexStatus.Created) return true;

            var updatedGrain = g.AsReference<V>(gf);
            var updt = (MemberUpdate)iUpdate.Value;
            var opType = updt.GetOperationType();
            HashIndexSingleBucketEntry<V> befEntry;
            HashIndexSingleBucketEntry<V> aftEntry;
            if (opType == OperationType.Update)
            {
                K befImg = (K)updt.GetBeforeImage();
                K aftImg = (K)updt.GetAfterImage();
                if (State.IndexMap.TryGetValue(befImg, out befEntry) && befEntry.Values.Contains(updatedGrain))
                {   //Delete and Insert
                    if (State.IndexMap.TryGetValue(aftImg, out aftEntry))
                    {
                        if (aftEntry.Values.Contains(updatedGrain))
                        {
                            befEntry.Values.Remove(updatedGrain);
                        }
                        else
                        {
                            if (State.IsUnique && aftEntry.Values.Count > 0)
                            {
                                throw new Exception(string.Format("The uniqueness property of index is violated after an update operation for before-image = {0}, after-image = {1} and grain = {2}", befImg, aftImg, updatedGrain.GetPrimaryKey()));
                            }
                            befEntry.Values.Remove(updatedGrain);
                            aftEntry.Values.Add(updatedGrain);
                        }
                    }
                    else
                    {
                        aftEntry = new HashIndexSingleBucketEntry<V>();
                        befEntry.Values.Remove(updatedGrain);
                        aftEntry.Values.Add(updatedGrain);
                        State.IndexMap.Add(aftImg, aftEntry);
                    }
                }
                else
                { // Insert
                    if (State.IndexMap.TryGetValue(aftImg, out aftEntry))
                    {
                        if (!aftEntry.Values.Contains(updatedGrain))
                        {
                            if (State.IsUnique && aftEntry.Values.Count > 0)
                            {
                                throw new Exception(string.Format("The uniqueness property of index is violated after an update operation for (not found before-image = {0}), after-image = {1} and grain = {2}", befImg, aftImg, updatedGrain.GetPrimaryKey()));
                            }
                            aftEntry.Values.Add(updatedGrain);
                        }
                    }
                    else
                    {
                        aftEntry = new HashIndexSingleBucketEntry<V>();
                        aftEntry.Values.Add(updatedGrain);
                        State.IndexMap.Add(aftImg, aftEntry);
                    }
                }
            }
            else if (opType == OperationType.Insert)
            { // Insert
                K aftImg = (K)updt.GetAfterImage();
                if (State.IndexMap.TryGetValue(aftImg, out aftEntry))
                {
                    if (!aftEntry.Values.Contains(updatedGrain))
                    {
                        if (State.IsUnique && aftEntry.Values.Count > 0)
                        {
                            throw new Exception(string.Format("The uniqueness property of index is violated after an insert operation for after-image = {1} and grain = {2}", aftImg, updatedGrain.GetPrimaryKey()));
                        }
                        aftEntry.Values.Add(updatedGrain);
                    }
                }
                else
                {
                    aftEntry = new HashIndexSingleBucketEntry<V>();
                    aftEntry.Values.Add(updatedGrain);
                    State.IndexMap.Add(aftImg, aftEntry);
                }
            }
            else if (opType == OperationType.Delete)
            { // Delete
                K befImg = (K)updt.GetBeforeImage();

                if (State.IndexMap.TryGetValue(befImg, out befEntry) && befEntry.Values.Contains(updatedGrain))
                {
                    befEntry.Values.Remove(updatedGrain);
                    if (State.IndexStatus != IndexStatus.Available)
                    {
                        State.IndexStatus = await GetIndexBuilder().AddTombstone(updatedGrain) ? IndexStatus.Available : State.IndexStatus;
                        Task writeTask = null;
                        if (State.IndexStatus == IndexStatus.Available) writeTask = base.WriteStateAsync();
                        if (State.IndexMap.TryGetValue(befImg, out befEntry) && befEntry.Values.Contains(updatedGrain))
                        {
                            befEntry.Values.Remove(updatedGrain);
                            var isAvailable = await GetIndexBuilder().AddTombstone(updatedGrain);
                            if(State.IndexStatus != IndexStatus.Available && isAvailable)
                            {
                                State.IndexStatus = IndexStatus.Available;
                                writeTask = base.WriteStateAsync();
                            }
                        }
                        await writeTask;
                    }
                }
            }
            return true;
        }

        public Task<bool> IsUnique()
        {
            return Task.FromResult(State.IsUnique);
        }

        public async Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            if (!(State.IndexStatus == IndexStatus.Available || await IsAvailable()))
            {
                var e = new Exception(string.Format("Index is not still available."));
                GetLogger().Log((int)ErrorCode.IndexingIndexIsNotReadyYet, Severity.Error, "Index is not still available.", null, e);
                throw e;
            }
            HashIndexSingleBucketEntry<V> entry;
            if (State.IndexMap.TryGetValue(key, out entry))
            {
                return new OrleansQueryResult<V>(entry.Values);
            }
            else
            {
                return new OrleansQueryResult<V>(Enumerable.Empty<V>());
            }
        }

        public async Task<V> LookupUnique(K key)
        {
            if (!(State.IndexStatus == IndexStatus.Available || await IsAvailable()))
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
                    return entry.Values.GetEnumerator().Current;
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

        private IIndexBuilder<V> GetIndexBuilder()
        {
            return GrainFactory.GetGrain<IIndexBuilder<V>>(IndexUtils.GetIndexGrainID(typeof(V), IndexUtils.GetIndexNameFromIndexGrain(this)));
        }

        public async Task<bool> IsAvailable()
        {
            if (State.IndexStatus == IndexStatus.Available) return true;
            var isDone = await GetIndexBuilder().IsDone();
            if(isDone)
            {
                State.IndexStatus = IndexStatus.Available;
                await base.WriteStateAsync();
            }
            return isDone;
        }

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return (IOrleansQueryResult<IIndexableGrain>)await Lookup((K)key);
        }

        public Task SetName(string name)
        {
            return TaskDone.Done;
        }

        /// <summary>
        /// Each hash-index needs a hash function, and a user can specify
        /// the hash function via a call to this method.
        /// 
        /// This method should be used internally by the index grain and
        /// should not be invoked from other grains.
        /// </summary>
        /// <param name="hashLambda">hash function that should be used
        /// for this hash-index</param>
        //void SetHashLambda(Func<K, long> hashLambda)
        //{
        //    _hashLambda = hashLambda;
        //}

        /// <summary>
        /// Each hash-index needs a function for checking equality,
        /// a user can specify the equality-check function via a call
        /// to this method.
        /// 
        /// This method should be used internally by the index grain and
        /// should not be invoked from other grains.
        /// </summary>
        /// <param name="equalsLambda">equality check function that
        /// should be used for this hash-index</param>
        //void SetEqualsLambda(Func<K, K, bool> equalsLambda)
        //{
        //    _equalsLambda = equalsLambda;
        //}
    }
}