using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class HashIndexInMemory<K, V> : Grain<HashIndexInMemoryState<K,V>>, IHashIndexInMemory<K, V> where V : IGrain
    {
        //private Func<K, K, bool> _equalsLambda = ((k1,k2) => k1.Equals(k2));
        //private Func<K, long> _hashLambda = (k => k.GetHashCode());

        public override async Task OnActivateAsync()
        {
            //await ReadStateAsync();
            if (State.IndexMap == null) State.IndexMap = new Dictionary<K, HashIndexEntry<V>>();
            await base.OnActivateAsync();
        }

        //public Task<string> GetIndexName()
        //{
        //    return Task.FromResult(State.Name);
        //}

        //public Task SetIndexName(string indexName)
        //{
        //    State.Name = indexName;
        //    return base.WriteStateAsync();
        //}

        public Task<bool> ApplyIndexUpdate(IGrain g, Immutable<IMemberUpdate> iUpdate)
        {
            var updatedGrain = g.AsReference<V>();
            var updt = (MemberUpdate)iUpdate.Value;
            var opType = updt.GetOperationType();
            if (opType == OperationType.Update)
            {
                K befImg = (K)updt.GetBeforeImage();

                if (State.IndexMap.ContainsKey(befImg))
                {
                    HashIndexEntry<V> befEntry = State.IndexMap[befImg];
                    if(befEntry.Values.Contains(updatedGrain))
                    {
                        K aftImg = (K)updt.GetAfterImage();
                        if(State.IndexMap.ContainsKey(aftImg))
                        {
                            HashIndexEntry<V> aftEntry = State.IndexMap[aftImg];
                            if(State.IsUnique && aftEntry.Values.Count > 0)
                            {
                                throw new Exception(string.Format("The uniqueness property of index is violated after an update operation for before-image = {0}, after-image = {1} and grain = {2}", befImg, aftImg, updatedGrain.GetPrimaryKey()));
                            }
                            befEntry.Values.Remove(updatedGrain);
                            aftEntry.Values.Add(updatedGrain);
                        }
                        else
                        {
                            HashIndexEntry<V> aftEntry = new HashIndexEntry<V>();
                            aftEntry.Values.Add(updatedGrain);
                            State.IndexMap.Add(aftImg, aftEntry);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("The index entry does not exist for before-image = {0} and grain = {1}", befImg, updatedGrain.GetPrimaryKey()));
                    }
                }
                else
                {
                    throw new Exception(string.Format("The index entry does not exist for before-image = {0} and grain = {1}", befImg, updatedGrain.GetPrimaryKey()));
                }
            }
            else if (opType == OperationType.Insert)
            {
                K aftImg = (K)updt.GetAfterImage();
                if (State.IndexMap.ContainsKey(aftImg))
                {
                    HashIndexEntry<V> aftEntry = State.IndexMap[aftImg];
                    if (State.IsUnique && aftEntry.Values.Count > 0)
                    {
                        throw new Exception(string.Format("The uniqueness property of index is violated after an insert operation for after-image = {1} and grain = {2}", aftImg, updatedGrain.GetPrimaryKey()));
                    }
                    aftEntry.Values.Add(updatedGrain);
                }
                else
                {
                    HashIndexEntry<V> aftEntry = new HashIndexEntry<V>();
                    aftEntry.Values.Add(updatedGrain);
                    State.IndexMap.Add(aftImg, aftEntry);
                }
            }
            else if (opType == OperationType.Delete)
            {
                K befImg = (K)updt.GetBeforeImage();

                if (State.IndexMap.ContainsKey(befImg))
                {
                    HashIndexEntry<V> befEntry = State.IndexMap[befImg];
                    if (befEntry.Values.Contains(updatedGrain))
                    {
                        befEntry.Values.Remove(updatedGrain);
                    }
                    else
                    {
                        throw new Exception(string.Format("The index entry does not exist for before-image = {0} and grain = {1}", befImg, updatedGrain.GetPrimaryKey()));
                    }
                }
                else
                {
                    throw new Exception(string.Format("The index entry does not exist for before-image = {0} and grain = {1}", befImg, updatedGrain.GetPrimaryKey()));
                }
            }
            return Task.FromResult(true);
        }

        public Task<IIndexUpdateGenerator> GetIndexUpdateGenerator()
        {
            return Task.FromResult(State.IndexUpdateGen);
        }

        public Task SetIndexUpdateGenerator(IIndexUpdateGenerator iUpdateGen)
        {
            State.IndexUpdateGen = iUpdateGen;
            return base.WriteStateAsync();
        }

        public Task<bool> IsUnique()
        {
            return Task.FromResult(State.IsUnique);
        }

        public Task<IEnumerable<V>> Lookup(K key)
        {
            if (State.IndexMap.ContainsKey(key))
            {
                return Task.FromResult((IEnumerable<V>)State.IndexMap[key].Values);
            }
            else
            {
                return Task.FromResult(Enumerable.Empty<V>());
            }
        }

        public async Task<V> LookupUnique(K key)
        {
            return (await Lookup(key)).GetEnumerator().Current;
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
    public sealed class HashIndexEntry<T>
    {
        public ISet<T> Values = new HashSet<T>();
    }
}
