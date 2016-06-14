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
    public class IndexFactory
    {
        public static IIndex<K, V> GetIndex<K, V>(string indexName) where V : IGrain
        {
            Type grainType = typeof(V);
            return GrainFactoryBase.MakeGrainReference_FromType(
                baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, GetIndexGrainID(grainType, indexName), typeof(IIndex)),
                typeof(IIndex)).AsReference<IIndex<K, V>>();
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
                IIdxType indexGrain =  GrainFactoryBase.MakeGrainReference_FromType(
                    baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, GetIndexGrainID(grainType, indexName), typeof(IIndex)),
                    typeof(IIndex)).AsReference<IIdxType>();
                var t1 = indexGrain.SetIndexUpdateGenerator(new IndexUpdateGenType());
                //var t2 = indexGrain.SetIndexName(indexName);
                await Task.WhenAll(new Task[] { t1/*, t2*/ });
                return indexGrain;
            }
            else
            {
                throw new NotSupportedException(string.Format("Adding indexes that do not implement IIndex<K,V> is not supported yet. Your requested index ({0}) is invalid.", idxType.ToString()));
            }
        }

        public static Task<bool> RegisterIndex(string indexName, IIndex index)
        {
            Type idxType = index.GetType();
            Type iIndexType = idxType.GetGenericType(typeof(IIndex<,>));
            if (iIndexType != null)
            {
                Type[] indexTypeArgs = iIndexType.GetGenericArguments();
                //Type keyType = indexTypeArgs[0];
                Type grainType = indexTypeArgs[1];
                IIndexRegistry indexReg = GrainClient.GrainFactory.GetGrain<IIndexRegistry>(TypeUtils.GetFullName(grainType));
                //string indexName = await index.GetIndexName();
                return indexReg.RegisterIndex(indexName, index);
            }
            else
            {
                throw new NotSupportedException(string.Format("Registering indexes that do not implement IIndex<K,V> is not supported yet. Your requested index ({0}) is invalid.", idxType.ToString()));
            }
        }

        public static async Task<IIdxType> CreateAndRegisterIndex<IIdxType, IndexUpdateGenType>(string indexName) where IIdxType : IIndex where IndexUpdateGenType : IIndexUpdateGenerator, new()
        {
            IIdxType index = await CreateIndex<IIdxType, IndexUpdateGenType>(indexName);
            await RegisterIndex(indexName, index);
            return index;
        }

        private static string GetIndexGrainID(Type grainType, string indexName)
        {
            return string.Format("{0}-{1}", TypeUtils.GetFullName(grainType), indexName);
        }
    }
}
