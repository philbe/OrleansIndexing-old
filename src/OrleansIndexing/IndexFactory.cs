﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.CodeGeneration;

namespace Orleans.Indexing
{
    public class IndexFactory
    {
        /// <summary>
        /// Gets an IIndex<K,V> given its name
        /// </summary>
        /// <typeparam name="K">key type of the index</typeparam>
        /// <typeparam name="V">value type of the index, which is
        /// the grain being indexed</typeparam>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public static async Task<IIndex<K, V>> GetIndex<K, V>(string indexName) where V : IGrain
        {
            return (await GetIndexHandler<V>().GetIndex(indexName)).AsReference<IIndex<K,V>>();
        }
        
        public static async Task<IIdxType> CreateIndex<IIdxType, IndexUpdateGenType>(string indexName) where IIdxType : IIndex where IndexUpdateGenType : IIndexUpdateGenerator, new()
        {
            Type idxType = typeof(IIdxType);
            Type iIndexType = idxType.GetGenericType(typeof(IIndex<,>));
            if (iIndexType != null)
            {
                Type[] indexTypeArgs = iIndexType.GetGenericArguments();
                //Type keyType = indexTypeArgs[0];
                Type grainType = indexTypeArgs[1];

                IIdxType indexGrain = GrainClient.GrainFactory.GetGrain<IIdxType>(GetIndexGrainID(grainType, indexName));
                await indexGrain.SetIndexUpdateGenerator(new IndexUpdateGenType());
                //var t1 = indexGrain.SetIndexUpdateGenerator(new IndexUpdateGenType());
                //var t2 = indexGrain.SetIndexName(indexName);
                //await Task.WhenAll(new Task[] { t1/*, t2*/ });
                return indexGrain;
            }
            else
            {
                throw new NotSupportedException(string.Format("Adding indexes that do not implement IIndex<K,V> is not supported yet. Your requested index ({0}) is invalid.", idxType.ToString()));
            }
        }

        public static Task<bool> RegisterIndex<IIdxType>(string indexName, IIdxType index) where IIdxType : IIndex
        {
            Type idxType = index.GetType();
            Type iIndexType = idxType.GetGenericType(typeof(IIndex<,>));
            if (iIndexType != null)
            {
                Type[] indexTypeArgs = iIndexType.GetGenericArguments();
                //Type keyType = indexTypeArgs[0];
                Type iGrainType = indexTypeArgs[1];

                Type indexRegType = typeof(IIndexRegistry<>).MakeGenericType(new Type[] { iGrainType } );

                IIndexRegistry indexReg = GrainClient.GrainFactory.GetGrain<IIndexRegistry<IIndexableGrain>>(TypeUtils.GetFullName(iGrainType), indexRegType);
                //string indexName = await index.GetIndexName();
                return indexReg.RegisterIndex(indexName, index, new IndexMetaData(typeof(IIdxType)));
            }
            else
            {
                throw new NotSupportedException(string.Format("Registering indexes that do not implement IIndex<K,V> is not supported yet. Your requested index ({0}) is invalid.", idxType.ToString()));
            }
        }

        public static async Task<bool> CreateAndRegisterIndex<IIdxType, IndexUpdateGenType>(string indexName) where IIdxType : IIndex where IndexUpdateGenType : IIndexUpdateGenerator, new()
        {
            IIdxType index = await CreateIndex<IIdxType, IndexUpdateGenType>(indexName);
            return await RegisterIndex(indexName, index);
        }

        public static Task ReloadIndexes<IGrainType>() where IGrainType : IGrain
        {
            return GetIndexHandler<IGrainType>().ReloadIndexes();
        }

        private static IIndexHandler<T> GetIndexHandler<T>() where T : IGrain
        {
            return GrainClient.GrainFactory.GetGrain<IIndexHandler<T>>(TypeUtils.GetFullName(typeof(T)));
        }

        private static string GetIndexGrainID(Type grainType, string indexName)
        {
            return string.Format("{0}-{1}", TypeUtils.GetFullName(grainType), indexName);
        }
    }
}
