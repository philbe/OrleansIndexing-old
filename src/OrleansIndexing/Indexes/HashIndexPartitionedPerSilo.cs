using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    [Reentrant]
    [StatelessWorker]
    public class HashIndexPartitionedPerSilo<K, V> : Grain, IHashIndexPartitionedPerSilo<K, V> where V : IIndexableGrain
    {
        public static void InitPerSilo(Silo silo, string indexName, bool isUnique)
        {
            silo.RegisterSystemTarget(new HashIndexPartitionedPerSiloBucket(
                indexName,
                GetGrainID(indexName),
                silo.SiloAddress
            ));
        }

        public Task<bool> ApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, bool isUniqueIndex, SiloAddress siloAddress)
        {
            IHashIndexPartitionedPerSiloBucket bucketInCurrentSilo = InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<IHashIndexPartitionedPerSiloBucket>(
                GetGrainID(IndexUtils.GetIndexNameFromIndexGrain(this)),
                siloAddress
            );
            return bucketInCurrentSilo.ApplyIndexUpdate(g, iUpdate, isUniqueIndex/*, siloAddress*/);
        }

        private static GrainId GetGrainID(string indexName)
        {
            return GrainId.GetSystemTargetGrainId(Constants.HASH_INDEX_PARTITIONED_PER_SILO_BUCKET_SYSTEM_TARGET_TYPE_CODE,
                                               IndexUtils.GetIndexGrainID(typeof(V), indexName));
        }

        public Task<bool> IsUnique()
        {
            return Task.FromResult(false);
        }

        public async Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            Dictionary<SiloAddress, SiloStatus> hosts = await SiloUtils.GetHosts(true);
            var numHosts = hosts.Keys.Count;

            Task<IOrleansQueryResult<IIndexableGrain>>[] queriesToSilos = new Task<IOrleansQueryResult<IIndexableGrain>>[numHosts];

            int i = 0;
            IList<IOrleansQueryResult<V>> result = new List<IOrleansQueryResult<V>>();
            GrainId grainID = GetGrainID(IndexUtils.GetIndexNameFromIndexGrain(this));
            foreach (SiloAddress siloAddress in hosts.Keys)
            {
                queriesToSilos[i] = InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<IHashIndexPartitionedPerSiloBucket>(
                    grainID,
                    siloAddress
                ).Lookup(key);
                ++i;
            }

            await Task.WhenAll(queriesToSilos);

            for(i = 0; i< numHosts; ++i)
            {
                result.Add(new OrleansQueryResult<V>(queriesToSilos[i].Result));
            }
            return new OrleansQueryResult<V>(result);
        }

        public async Task<V> LookupUnique(K key)
        {
            return (await Lookup(key)).GetFirst();
        }

        public Task Dispose()
        {
            return TaskDone.Done;
        }

        public Task<bool> IsAvailable()
        {
            return Task.FromResult(true);
        }

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndex.Lookup(object key)
        {
            return (IOrleansQueryResult<IIndexableGrain>)await Lookup((K)key);
        }
    }
}
