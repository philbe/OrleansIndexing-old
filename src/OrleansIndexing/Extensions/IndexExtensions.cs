﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Concurrency;

namespace Orleans.Indexing
{
    public static class IndexExtensions
    {
        internal static Task<bool> ApplyIndexUpdate(this IndexInterface index, IIndexableGrain updatedGrain, Immutable<IMemberUpdate> update, bool isUniqueIndex, SiloAddress siloAddress = null)
        {
            Type indexType = index.GetType();
            if (indexType.Name.Contains("AHashIndexPartitionedPerSiloReference"))
            {
                AHashIndexPartitionedPerSiloBucket bucketInCurrentSilo = InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<AHashIndexPartitionedPerSiloBucket>(
                    GetGrainID(IndexUtils.GetIndexNameFromIndexGrain((IAddressable )index), indexType.GetGenericArguments()[1]),
                    siloAddress
                );
                return bucketInCurrentSilo.DirectApplyIndexUpdate(updatedGrain, update, isUniqueIndex/*, siloAddress*/);
            }
            return index.DirectApplyIndexUpdate(updatedGrain, update, isUniqueIndex, siloAddress);
        }

        
        private static GrainId GetGrainID(string indexName, Type iGrainType)
        {
            return GrainId.GetSystemTargetGrainId(Constants.HASH_INDEX_PARTITIONED_PER_SILO_BUCKET_SYSTEM_TARGET_TYPE_CODE,
                                               IndexUtils.GetIndexGrainID(iGrainType, indexName));
        }
    }
}
