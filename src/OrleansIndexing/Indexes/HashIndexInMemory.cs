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
    public class HashIndexInMemory<K, V> : Grain<HashIndexInMemoryState<K,V>>, IHashIndexInMemory<K, V> where V : IIndexableGrain
    {
        //private Func<K, K, bool> _equalsLambda = ((k1,k2) => k1.Equals(k2));
        //private Func<K, long> _hashLambda = (k => k.GetHashCode());

        public override async Task OnActivateAsync()
        {
            //await ReadStateAsync();
            if (State.IndexMap == null) State.IndexMap = new Dictionary<K, HashIndexInMemoryEntry<V>>();
            if (State.IndexStatus == IndexStatus.UnderConstruction)
            {
                string indexName = IndexUtils.GetIndexNameFromIndexGrain(this);
                var _ = GetIndexBuilder().BuildIndex(indexName, this, await IndexUtils.GetIndexUpdateGenerator<V>(GrainFactory, indexName));
            }
            await base.OnActivateAsync();
        }

        public async Task<bool> ApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate)
        {
            //the index can start processing update as soon as it becomes
            //visible to index handler and does not have to wait for any
            //further event regarding index builder, so it is not necessary
            //to have a Created state
            //if (State.IndexStatus == IndexStatus.Created) return true;

            var updatedGrain = g.AsReference<V>(GrainFactory);
            var updt = (MemberUpdate)iUpdate.Value;
            var opType = updt.GetOperationType();
            HashIndexInMemoryEntry<V> befEntry;
            HashIndexInMemoryEntry<V> aftEntry;
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
                        aftEntry = new HashIndexInMemoryEntry<V>();
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
                        aftEntry = new HashIndexInMemoryEntry<V>();
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
                    aftEntry = new HashIndexInMemoryEntry<V>();
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

        public async Task<IEnumerable<V>> Lookup(K key)
        {
            if (!(State.IndexStatus == IndexStatus.Available || await IsAvailable()))
            {
                var e = new Exception(string.Format("Index is not still available."));
                GetLogger().Log((int)ErrorCode.IndexingIndexIsNotReadyYet, Severity.Error, "Index is not still available.", null, e);
                throw e;
            }
            HashIndexInMemoryEntry<V> entry;
            if (State.IndexMap.TryGetValue(key, out entry))
            {
                return entry.Values;
            }
            else
            {
                return Enumerable.Empty<V>();
            }
        }

        public async Task<V> LookupUnique(K key)
        {
            return (await Lookup(key)).GetEnumerator().Current;
        }

        public Task Dispose()
        {
            State.IndexMap.Clear();
            return TaskDone.Done;
        }

        private IIndexBuilder<V> GetIndexBuilder()
        {
            return GrainFactory.GetGrain<IIndexBuilder<V>>(((IIndex<K, V>)this).GetPrimaryKeyString());
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

        async Task<IEnumerable<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return (IEnumerable<IIndexableGrain>)await Lookup((K)key);
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
