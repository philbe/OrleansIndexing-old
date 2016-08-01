using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnitTests.GrainInterfaces;
using UnitTests.Tester;
using Xunit;
using Tester;
using Orleans;
using Orleans.Runtime;
using Orleans.Indexing;
using UnitTests.Grains;

namespace UnitTests.IndexingTests
{

    public class NoIndexingTests : HostedTestClusterEnsureDefaultStarted
    {
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_NoIndex()
        {
            IPlayer1GrainNonFaultTolerant p100 = GrainClient.GrainFactory.GetGrain<IPlayer1GrainNonFaultTolerant>(100);
            IPlayer1GrainNonFaultTolerant p200 = GrainClient.GrainFactory.GetGrain<IPlayer1GrainNonFaultTolerant>(200);
            IPlayer1GrainNonFaultTolerant p300 = GrainClient.GrainFactory.GetGrain<IPlayer1GrainNonFaultTolerant>(300);

            await p100.SetLocation("Tehran");
            await p200.SetLocation("Tehran");
            await p300.SetLocation("Yazd");

            Assert.Equal("Tehran", await p100.GetLocation());
            Assert.Equal("Tehran", await p200.GetLocation());
            Assert.Equal("Yazd", await p300.GetLocation());
        }
    }
}
