﻿using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The grain interface for the IndexableGrain grain.
    /// </summary>
    public interface IIndexableGrain : IGrain
    {
    }

    /// <summary>
    /// The grain interface for the IndexableGrain grain,
    /// which indexes a single grain.
    /// </summary>
    public interface IIndexableGrain<T> : IIndexableGrain
    {
    }
}