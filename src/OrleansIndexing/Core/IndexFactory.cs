using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.CodeGeneration;

namespace Orleans.Indexing
{
    /// <summary>
    /// A utility class for the index operations
    /// </summary>
    public class IndexFactory
    {
        /// <summary>
        /// Gets an IIndex<K,V> given its name
        /// </summary>
        /// <typeparam name="K">key type of the index</typeparam>
        /// <typeparam name="V">value type of the index, which is
        /// the grain being indexed</typeparam>
        /// <param name="indexName">the name of the index, which
        /// is the identifier of the index</param>
        /// <returns></returns>
        public static async Task<IIndex<K, V>> GetIndex<K, V>(string indexName) where V : IIndexableGrain
        {
            return (await IndexUtils.GetIndexHandler<V>().GetIndex(indexName)).AsReference<IIndex<K,V>>();
        }

        /// <summary>
        /// Creates an index grain, given its type and
        /// the type of its IndexUpdateGenerator.
        /// 
        /// The created index grain is not registered and will not do anything 
        /// unless it is registered by calling IndexFactory.RegisterIndex.
        /// </summary>
        /// <typeparam name="IIdxType">the type of grain interface
        /// of the index</typeparam>
        /// <typeparam name="IndexUpdateGenType">the type of
        /// IndexUpdateGenerator of the index</typeparam>
        /// <param name="indexName">the name of the index, which
        /// is the identifier of the index</param>
        /// <returns>the created index grain</returns>
        public static IIdxType CreateIndexGrain<IIdxType>(string indexName) where IIdxType : IIndex
        {
            Type idxType = typeof(IIdxType);
            Type iIndexType = idxType.GetGenericType(typeof(IIndex<,>));
            if (iIndexType != null)
            {
                Type[] indexTypeArgs = iIndexType.GetGenericArguments();
                //Type keyType = indexTypeArgs[0];
                Type grainType = indexTypeArgs[1];

                IIdxType indexGrain = GrainClient.GrainFactory.GetGrain<IIdxType>(IndexUtils.GetIndexGrainID(grainType, indexName));
                return indexGrain;
            }
            else
            {
                throw new NotSupportedException(string.Format("Adding an index that does not implement IIndex<K,V> is not supported yet. Your requested index ({0}) is invalid.", idxType.ToString()));
            }
        }

        /// <summary>
        /// Registers the given index with the given name
        /// into the Orleans Indexing runtime.
        /// </summary>
        /// <typeparam name="IIdxType">the type of the index to
        /// be registered</typeparam>
        /// <param name="indexName">the name of the index, which
        /// is the identifier of the index</param>
        /// <param name="index">the index grain to be registered</param>
        /// <returns>whether the registration of the index was
        /// successful or not.</returns>
        public static async Task<bool> RegisterIndex<IIdxType, IndexUpdateGenType>(string indexName, IIdxType index) where IIdxType : IIndex where IndexUpdateGenType : IIndexUpdateGenerator, new()
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
                bool isRegistered = await indexReg.RegisterIndex(indexName, index, new IndexMetaData(typeof(IIdxType), typeof(IndexUpdateGenType)));
                bool indexesReloaded = false;
                if (isRegistered)
                {
                    var reloadIndexes = ReloadIndexes(iGrainType);
                    await reloadIndexes;
                    indexesReloaded = reloadIndexes.Status == TaskStatus.RanToCompletion;
                    if (indexesReloaded)
                    {
                        Type indexBuilderType = typeof(IIndexBuilder<>).MakeGenericType(new Type[] { iGrainType });
                        IIndexBuilder indexBuilder = GrainClient.GrainFactory.GetGrain<IIndexBuilder<IIndexableGrain>>(IndexUtils.GetIndexGrainID(iGrainType, indexName), indexBuilderType);
                        var _ = indexBuilder.BuildIndex(indexName, index, new IndexUpdateGenType()).ConfigureAwait(false); //builds the index on its own without coming back here
                    }
                }
                return isRegistered && indexesReloaded;
            }
            else
            {
                throw new NotSupportedException(string.Format("Registering indexes that do not implement IIndex<K,V> is not supported yet. Your requested index ({0}) is invalid.", idxType.ToString()));
            }
        }

        /// <summary>
        /// A ccall to CreateIndexGrain followed by a call to RegisterIndex.
        /// </summary>
        /// <typeparam name="IIdxType">the type of the index to
        /// be registered</typeparam>
        /// <typeparam name="IndexUpdateGenType">the type of
        /// IndexUpdateGenerator of the index</typeparam>
        /// <param name="indexName">the name of the index, which
        /// is the identifier of the index</param>
        /// <returns>whether the creation and registration of the
        /// index was successful or not.</returns>
        public static Task<bool> CreateAndRegisterIndex<IIdxType, IndexUpdateGenType>(string indexName) where IIdxType : IIndex where IndexUpdateGenType : IIndexUpdateGenerator, new()
        {
            IIdxType index = CreateIndexGrain<IIdxType>(indexName);
            return RegisterIndex<IIdxType, IndexUpdateGenType>(indexName, index);
        }

        /// <summary>
        /// Explicitly reloads the indexes on the current index handler instance.
        /// </summary>
        /// <typeparam name="IGrainType">the grain interface type that its
        /// interfaces are going to be loaded.</typeparam>
        public static Task ReloadIndexes<IGrainType>() where IGrainType : IIndexableGrain
        {
            return IndexUtils.GetIndexHandler<IGrainType>().ReloadIndexes();
        }
        
        /// <summary>
        /// Explicitly reloads the indexes on the current index handler instance.
        /// </summary>
        /// <typeparam name="IGrainType">the grain interface type that its
        /// interfaces are going to be loaded.</typeparam>
        //public static Task ReloadIndexes<IGrainType>(IGrainFactory gf) where IGrainType : IIndexableGrain
        //{
        //    return GetIndexHandler<IGrainType>(gf).ReloadIndexes();
        //}

        /// <summary>
        /// Explicitly reloads the indexes on the current index handler instance.
        /// </summary>
        /// <typeparam name="IGrainType">the grain interface type that its
        /// interfaces are going to be loaded.</typeparam>
        internal static Task ReloadIndexes(Type iGrainType)
        {
            return IndexUtils.GetIndexHandler(iGrainType).ReloadIndexes();
        }

        /// <summary>
        /// Drops all the indexes defined for a given grain interface.
        /// </summary>
        /// <typeparam name="IGrainType">the given grain interface</typeparam>
        public static async Task DropAllIndexes<IGrainType>() where IGrainType : IIndexableGrain
        {
            await IndexUtils.GetIndexRegistry<IGrainType>().DropAllIndexes();
            await ReloadIndexes<IGrainType>();
        }

        /// <summary>
        /// Drops an index defined for a given grain interface provided its name.
        /// </summary>
        /// <typeparam name="IGrainType">the given grain interface</typeparam>
        /// <param name="indexName">the name of the index</param>
        public static async Task DropIndex<IGrainType>(string indexName) where IGrainType : IIndexableGrain
        {
            await IndexUtils.GetIndexRegistry<IGrainType>().DropIndex(indexName);
            await ReloadIndexes<IGrainType>();
        }
    }
}
