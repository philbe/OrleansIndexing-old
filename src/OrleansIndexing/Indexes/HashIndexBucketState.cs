using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// Contains the state that should be stored for each HashIndexSingleBucket
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Serializable]
    public class HashIndexBucketState<K, V> where V : IIndexableGrain
    {
        //public string Name { set; get; }

        /// <summary>
        /// Determines whether there is a uniqueness constraint on this hash-index
        /// </summary>
        //public bool IsUnique { set; get; }

        /// <summary>
        /// The actual storage of the indexed values
        /// </summary>
        public Dictionary<K, HashIndexSingleBucketEntry<V>> IndexMap { set; get; }

        /// <summary>
        /// Contains the status of the index regarding
        /// its population process, which can be either
        /// UnderConstruction or Available. Available means
        /// that the index has already been populated.
        /// </summary>
        public IndexStatus IndexStatus {set; get;}
    }

    /// <summary>
    /// Represent an index entry in the hash-index
    /// </summary>
    /// <typeparam name="T">the type of elements stored in
    /// the entry</typeparam>
    [Serializable]
    public sealed class HashIndexSingleBucketEntry<T>
    {
        /// <summary>
        /// The set of values associated with a single key
        /// of the hash-index. The hash-set can contain more
        /// than one value if there is no uniqueness constraint
        /// on the hash-index
        /// </summary>
        public HashSet<T> Values = new HashSet<T>();

        public const byte TENTATIVE_TYPE_NONE = 0;
        public const byte TENTATIVE_TYPE_DELETE = 1;
        public const byte TENTATIVE_TYPE_INSERT = 2;
        public byte tentativeOperationType = TENTATIVE_TYPE_NONE;

        public void Remove(T item, bool isTentative)
        {
            if (isTentative)
            {
                setTentativeDelete();
            }
            else
            {
                clearTentativeFlag();
                Values.Remove(item);
            }
        }

        public void Add(T item, bool isTentative)
        {
            Values.Add(item);
            if (isTentative)
            {
                setTentativeInsert();
            }
            else
            {
                clearTentativeFlag();
            }
        }

        internal bool isTentative()
        {
            return isTentativeDelete() || isTentativeInsert();
        }

        internal bool isTentativeDelete()
        {
            return tentativeOperationType == TENTATIVE_TYPE_DELETE;
        }

        internal bool isTentativeInsert()
        {
            return tentativeOperationType == TENTATIVE_TYPE_INSERT;
        }

        internal void setTentativeDelete()
        {
            tentativeOperationType = TENTATIVE_TYPE_DELETE;
        }

        internal void setTentativeInsert()
        {
            tentativeOperationType = TENTATIVE_TYPE_INSERT;
        }

        internal void clearTentativeFlag()
        {
            tentativeOperationType = TENTATIVE_TYPE_NONE;
        }
    }
}