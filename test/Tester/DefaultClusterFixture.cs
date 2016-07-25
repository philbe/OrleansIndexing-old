using Orleans.Runtime.Configuration;
using Orleans.TestingHost;

namespace Tester
{
    public class DefaultClusterFixture : BaseTestClusterFixture
    {
        protected override TestCluster CreateTestCluster()
        {
            var options = new TestClusterOptions();
            options.ClusterConfiguration.AddMemoryStorageProvider("Default");
            options.ClusterConfiguration.AddMemoryStorageProvider("MemoryStore");

            //Required for IndexingTests
            options.ClusterConfiguration.AddMemoryStorageProvider("PubSubStore");
            options.ClusterConfiguration.AddMemoryStorageProvider("IndexingStorageProvider");
            options.ClusterConfiguration.AddSimpleMessageStreamProvider("IndexingStreamProvider");
            options.ClientConfiguration.AddSimpleMessageStreamProvider("IndexingStreamProvider");
            return new TestCluster(options);
        }
    }
}
