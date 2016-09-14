﻿using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Providers;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-bucket persistent hash-index
    /// 
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [StorageProvider(ProviderName = Constants.INDEXING_STORAGE_PROVIDER_NAME)]
    public class IHashIndexPartitionedPerKeyBucketImplNonIncremental<K, V> : HashIndexPartitionedPerKeyBucket<K, V>, IHashIndexPartitionedPerKeyBucketNonIncremental<K, V> where V : class, IIndexableGrain
    {
    }
}
