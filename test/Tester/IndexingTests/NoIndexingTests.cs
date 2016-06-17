using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
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
            IPlayerGrain p100 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(100);
            IPlayerGrain p200 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(200);
            IPlayerGrain p300 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(300);

            await p100.SetLocation("Redmond");
            await p200.SetLocation("Redmond");
            await p300.SetLocation("Bellevue");

            Assert.AreEqual("Redmond", await p100.GetLocation());
            Assert.AreEqual("Redmond", await p200.GetLocation());
            Assert.AreEqual("Bellevue", await p300.GetLocation());
        }
    }
}
