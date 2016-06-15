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

        /// <summary>
        /// Determines whether there is a unique
        /// constraint on this hash-index
        /// </summary>
        public bool IsUnique { set; get; }

        /// <summary>
        /// The actual storage of the indexed values
        /// </summary>
        public IDictionary<K, HashIndexInMemoryEntry<V>> IndexMap { set; get; }

        /// <summary>
        /// The update generator for the current hash-index
        /// </summary>
        public IIndexUpdateGenerator IndexUpdateGen { set; get; }
    }

    /// <summary>
    /// Represent an index entry in the hash-index
    /// </summary>
    /// <typeparam name="T">the type of elements stored in
    /// the entry</typeparam>
    [Serializable]
    public sealed class HashIndexInMemoryEntry<T>
    {
        /// <summary>
        /// The set of values associated with a single key
        /// of the hash-index. The hash-set can contain more
        /// than one value if there is no unique constraint
        /// on the hash-index
        /// </summary>
        public ISet<T> Values = new HashSet<T>();
    }
}