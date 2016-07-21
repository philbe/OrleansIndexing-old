using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;
using System.Threading;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    [Reentrant]
    //[StatelessWorker]
    //TODO: because of a bug in OrleansStreams, this grain cannot be StatelessWorker. It should be fixed later.
    public class HashIndexPartitionedPerSilo<K, V> : Grain, IHashIndexPartitionedPerSilo<K, V> where V : IIndexableGrain
    {
        private IndexStatus _status;
        public static void InitPerSilo(Silo silo, string indexName, bool isUnique)
        {
            silo.RegisterSystemTarget(new HashIndexPartitionedPerSiloBucket(
                indexName,
                GetGrainID(indexName),
                silo.SiloAddress
            ));
        }

        public override Task OnActivateAsync()
        {
            _status = IndexStatus.Available;

            return base.OnActivateAsync();
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

        public Task Lookup(IOrleansQueryResult<V> result, K key)
        {
            return ((IIndex)this).Lookup(result.Cast<IIndexableGrain>(), key);
        }

        public async Task<V> LookupUnique(K key)
        {
            var result = new OrleansFirstQueryResult<V>();
            var taskCompletionSource = new TaskCompletionSource<V>();
            Task<V> tsk = taskCompletionSource.Task;
            Action<V> responseHandler = taskCompletionSource.SetResult;
            await result.SubscribeAsync(new QueryFirstResultObserver<V>(responseHandler));
            await Lookup(result, key);
            return await tsk;
        }

        public async Task Dispose()
        {
            _status = IndexStatus.Disposed;
            //get all silos
            Dictionary<SiloAddress, SiloStatus> hosts = await SiloUtils.GetHosts(true);
            var numHosts = hosts.Keys.Count;

            Task[] disposeToSilos = new Task[numHosts];

            int i = 0;
            IList<IOrleansQueryResult<V>> result = new List<IOrleansQueryResult<V>>();
            GrainId grainID = GetGrainID(IndexUtils.GetIndexNameFromIndexGrain(this));
            foreach (SiloAddress siloAddress in hosts.Keys)
            {
                //dispose the index on each silo
                disposeToSilos[i] = InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<IHashIndexPartitionedPerSiloBucket>(
                    grainID,
                    siloAddress
                ).Dispose();
                ++i;
            }
            await Task.WhenAll(disposeToSilos);
        }

        public Task<bool> IsAvailable()
        {
            return Task.FromResult(_status == IndexStatus.Available);
        }

        async Task IIndex.Lookup(IOrleansQueryResult<IIndexableGrain> result, object key)
        {
            //get all silos
            Dictionary<SiloAddress, SiloStatus> hosts = await SiloUtils.GetHosts(true);

            //Task[] queriesToSilos = new Task[hosts.Keys.Count];
            ISet<Task<IEnumerable<IIndexableGrain>>> queriesToSilos = new HashSet<Task<IEnumerable<IIndexableGrain>>>();

            int i = 0;
            GrainId grainID = GetGrainID(IndexUtils.GetIndexNameFromIndexGrain(this));
            foreach (SiloAddress siloAddress in hosts.Keys)
            {
                //query each silo
                queriesToSilos.Add(InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<IHashIndexPartitionedPerSiloBucket>(
                    grainID,
                    siloAddress
                ).LookupWholeResult(/*result, */key)); //TODO: because of a bug in OrleansStream, a SystemTarget cannot work with streams. It should be fixed later.
                ++i;
            }

            //TODO: After fixing the problem with OrleansStream, this part is not needed anymore
            while (queriesToSilos.Count > 0)
            {
                // Identify the first task that completes.
                Task<IEnumerable<IIndexableGrain>> firstFinishedTask = await Task.WhenAny(queriesToSilos);

                // ***Remove the selected task from the list so that you don't
                // process it more than once.
                queriesToSilos.Remove(firstFinishedTask);

                // Await the completed task.
                IEnumerable<IIndexableGrain> partialResult = await firstFinishedTask;

                await result.OnNextBatchAsync(partialResult);
            }
            await result.OnCompletedAsync();
        }
    }
}
