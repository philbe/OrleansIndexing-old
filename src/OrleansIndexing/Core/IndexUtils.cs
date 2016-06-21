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
    /// A utility class for the low-level operations related to indexes
    /// </summary>
    public class IndexUtils
    {
        /// <summary>
        /// Gets the index handler for a given grain interface type
        /// </summary>
        /// <typeparam name="T">the indexed grain interface type</typeparam>
        /// <returns>the index handler for a given grain interface type</returns>
        internal static IIndexHandler<T> GetIndexHandler<T>() where T : IIndexableGrain
        {
            return GrainClient.GrainFactory.GetGrain<IIndexHandler<T>>(TypeUtils.GetFullName(typeof(T)));
        }

        /// <summary>
        /// Gets the index handler for a given grain interface type
        /// </summary>
        /// <typeparam name="T">the indexed grain interface type</typeparam>
        /// <returns>the index handler for a given grain interface type</returns>
        internal static IIndexHandler<T> GetIndexHandler<T>(IGrainFactory gf) where T : IIndexableGrain
        {
            return gf.GetGrain<IIndexHandler<T>>(TypeUtils.GetFullName(typeof(T)));
        }


        /// <summary>
        /// Gets the index handler for a given grain interface type
        /// </summary>
        /// <param name="iGrainType">the indexed grain interface type</param>
        /// <returns>the index handler for a given grain interface type</returns>
        internal static IIndexHandler GetIndexHandler(Type iGrainType)
        {
            Type typedIndexHandlerType = typeof(IIndexHandler<>).MakeGenericType(iGrainType);
            return GrainClient.GrainFactory.GetGrain<IIndexHandler<IIndexableGrain>>(TypeUtils.GetFullName(iGrainType), typedIndexHandlerType);
        }

        /// <summary>
        /// Gets the index registry for a given grain interface type
        /// </summary>
        /// <typeparam name="T">the indexed grain interface type</typeparam>
        /// <returns>the index registry for a given grain interface type</returns>
        internal static IIndexRegistry<T> GetIndexRegistry<T>() where T : IIndexableGrain
        {
            return GrainClient.GrainFactory.GetGrain<IIndexRegistry<T>>(TypeUtils.GetFullName(typeof(T)));
        }

        /// <summary>
        /// A utility function for getting the index grainID,
        /// which is a simple concatenation of the grain
        /// interface type and indexName
        /// </summary>
        /// <param name="grainType">the grain interface type</param>
        /// <param name="indexName">the name of the index, which
        /// is the identifier of the index</param>
        /// <returns>index grainID</returns>
        internal static string GetIndexGrainID(Type grainType, string indexName)
        {
            return string.Format("{0}-{1}", TypeUtils.GetFullName(grainType), indexName);
        }

        /// <summary>
        /// This method extracts the name of an index grain from its primary key
        /// </summary>
        /// <param name="index">the given index grain</param>
        /// <returns>the name of the index</returns>
        public static string GetIndexNameFromIndexGrain(IIndex index)
        {
            string key = index.GetPrimaryKeyString();
            return key.Substring(key.LastIndexOf("-") + 1);
        }

        /// <summary>
        /// This method find the index update generator of a given index
        /// identified by the indexed grain interface type and the name of the index
        /// </summary>
        /// <typeparam name="T">type of the indexed grain interface</typeparam>
        /// <param name="gf">the grain factory instance</param>
        /// <param name="indexName">>the name of the index</param>
        /// <returns>the index update generator of the index</returns>
        internal static async Task<IIndexUpdateGenerator> GetIndexUpdateGenerator<T>(IGrainFactory gf, string indexName) where T : IIndexableGrain
        {
            IIndexUpdateGenerator output;
            (await GetIndexHandler<T>(gf).GetIndexUpdateGenerators()).Value.TryGetValue(indexName, out output);
            return output;
        }
    }
}
