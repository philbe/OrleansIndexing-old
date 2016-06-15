using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// Contains the state that should be stored for each HashIndexInMemory
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Serializable]
    public class HashIndexInMemoryState<K, V> where V : IGrain
    {
        //public string Name { set; get; }
        public bool IsUnique { set; get; }

        public IDictionary<K, HashIndexInMemoryEntry<V>> IndexMap { set; get; }

        public IIndexUpdateGenerator IndexUpdateGen { set; get; }
    }
    public sealed class HashIndexInMemoryEntry<T>
    {
        public ISet<T> Values = new HashSet<T>();
    }
}