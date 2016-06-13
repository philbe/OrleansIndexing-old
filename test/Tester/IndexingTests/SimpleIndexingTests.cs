using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using UnitTests.GrainInterfaces;
using UnitTests.Tester;
using Xunit;
using Tester;

namespace UnitTests.General
{
    public class SimpleIndexingTests : HostedTestClusterEnsureDefaultStarted
    {
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task TestCreatingHashIndexInMemory()
        {
            
        }
    }
}
