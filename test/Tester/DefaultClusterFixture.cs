using Orleans.Runtime;
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
            options.ClusterConfiguration.AddMemoryStorageProvider("IndexingWorkflowQueueStorageProvider");
            options.ClusterConfiguration.AddSimpleMessageStreamProvider("IndexingStreamProvider");
            options.ClientConfiguration.AddSimpleMessageStreamProvider("IndexingStreamProvider");

            //options.ClusterConfiguration.Defaults.DefaultTraceLevel = Severity.Verbose;
            //options.ClientConfiguration.DefaultTraceLevel = Severity.Verbose;
            //options.ClusterConfiguration.GetOrCreateNodeConfigurationForSilo("Primary").DefaultTraceLevel = Severity.Verbose;
            return new TestCluster(options);
        }
    }
}
