﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The grain interface for the index registry grain.
    /// </summary>
    public interface IIndexRegistry : IGrainWithStringKey
    {
        /// <summary>
        /// Exposes the indexes defined under this index registry
        /// </summary>
        /// <returns>the dictionary from indexID to index grain for all
        /// the indexes defined under this index registry</returns>
        Task<IDictionary<string, Tuple<IIndex, IndexMetaData>>> GetIndexes();

        /// <summary>
        /// Registers a new index in this index registry
        /// </summary>
        /// <param name="indexName">name of the index</param>
        /// <param name="index">a reference to the index grain</param>
        /// <param name="indexMetaData">the meta data for the index</param>
        Task<bool> RegisterIndex(string indexName, IIndex index, IndexMetaData indexMetaData);
        
        /// <summary>
        /// Drops the index identified by the given name that
        /// is registered under the current index registry
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        Task<bool> DropIndex(string indexName);

        /// <summary>
        /// Drops all the indexes registered under the
        /// current index registry
        /// </summary>
        Task DropAllIndexes();
    }

    /// <summary>
    /// The grain interface for the index registry grain,
    /// which contains the indexes for a single grain.
    /// </summary>
    public interface IIndexRegistry<T> : IIndexRegistry where T : IGrain
    {
    }
}