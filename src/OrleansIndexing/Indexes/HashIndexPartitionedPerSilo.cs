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
    [Serializable]
    public class HashIndexPartitionedPerSilo<K, V> : IHashIndex<K, V> where V : IIndexableGrain
    {
        private string _name;
        //private IGrainFactory _gf;

        public HashIndexPartitionedPerSilo(string indexName, Silo silo)
        {
            _name = indexName;
            //_gf = null;
            silo.RegisterSystemTarget(new HashIndexPartitionedPerSiloBucket(
                indexName,
                GetGrainID(),
                silo.SiloAddress
            ));
        }

        public Task<bool> ApplyIndexUpdate(IIndexableGrain g, Immutable<IMemberUpdate> iUpdate, SiloAddress siloAddress)
        {
            IHashIndexPartitionedPerSiloBucket bucketInCurrentSilo = InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<IHashIndexPartitionedPerSiloBucket>(
                GetGrainID(),
                siloAddress
            );
            return bucketInCurrentSilo.ApplyIndexUpdate(g, iUpdate/*, siloAddress*/);
        }

        private GrainId GetGrainID()
        {
            return GrainId.GetSystemTargetGrainId(Constants.HASH_INDEX_PARTITIONED_PER_SILO_BUCKET_SYSTEM_TARGET_TYPE_CODE,
                                               IndexUtils.GetIndexGrainID(typeof(V), _name));
        }

        public Task<bool> IsUnique()
        {
            return Task.FromResult(false);
        }

        public async Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            IGrainFactory gf = GrainClient.GrainFactory;
            //if (_gf == null) gf = GrainClient.GrainFactory;
            //else gf = _gf;

            Dictionary<SiloAddress, SiloStatus> hosts = await gf.GetGrain<IManagementGrain>(/*RuntimeInterfaceConstants.SYSTEM_MANAGEMENT_ID*/ 1).GetHosts(true);
            var numHosts = hosts.Keys.Count;

            Task<IOrleansQueryResult<IIndexableGrain>>[] queriesToSilos = new Task<IOrleansQueryResult<IIndexableGrain>>[numHosts];

            int i = 0;
            IList<IOrleansQueryResult<V>> result = new List<IOrleansQueryResult<V>>();
            foreach (SiloAddress siloAddress in hosts.Keys)
            {
                queriesToSilos[i] = ((GrainFactory)gf).GetSystemTarget<IHashIndexPartitionedPerSiloBucket>(
                    GetGrainID(),
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
            //State.IndexMap.Clear();
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

        public string GetName()
        {
            return _name;
        }

        //public void SetGrainFactory(IGrainFactory gf)
        //{
        //    _gf = gf;
        //}
    }
}
