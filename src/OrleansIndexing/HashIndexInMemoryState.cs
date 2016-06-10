using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// Contains the state that should be stored for each HashIndexInMemory
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    public class HashIndexInMemoryState<K, V> where V : IGrain
    {
        public bool IsUnique { set; get; }

        public IDictionary<K, HashIndexEntry<V>> IndexMap { set; get; }

        public IIndexOps IndexOps { set; get; }
    }
}