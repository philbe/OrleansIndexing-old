﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for HashIndexPartitionedPerSiloBucket<K, V> system target,
    /// which is created in order to guide Orleans to find the grain instances
    /// more efficiently.
    /// 
    /// Generic SystemTargets are not supported yet, and that's why the
    /// interface is non-generic.
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    //internal interface IHashIndexPartitionedPerSiloBucket<K, V> : ISystemTarget, IHashIndex<K, V> where V : IIndexableGrain
    internal interface IHashIndexPartitionedPerSiloBucket : ISystemTarget, IHashIndex<object, IIndexableGrain>
    {
    }
}
