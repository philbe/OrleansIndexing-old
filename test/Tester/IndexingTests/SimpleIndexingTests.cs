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
using Xunit.Abstractions;
using System.Threading;

namespace UnitTests.IndexingTests
{
    [Serializable]
    public class PlayerLocIndexGen : IndexUpdateGenerator<string, IPlayerGrain>
    {
        public override string ExtractIndexImage(IPlayerGrain g)
        {
            return g.GetLocation().Result;
        }
    }

    [Serializable]
    public class PlayerScoreIndexGen : IndexUpdateGenerator<int, IPlayerGrain>
    {
        public override int ExtractIndexImage(IPlayerGrain g)
        {
            return g.GetScore().Result;
        }
    }

    public class SimpleIndexingTests : HostedTestClusterEnsureDefaultStarted
    {

        private readonly ITestOutputHelper output;

        public SimpleIndexingTests(DefaultClusterFixture fixture, ITestOutputHelper output)
            : base(fixture)
        {
            this.output = output;
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_AddOneIndex()
        {
            bool isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx1");
            Assert.True(isLocIndexCreated);
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_AddTwoIndexes()
        {
            bool isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx2");
            bool isScoreIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<int, IPlayerGrain>, PlayerScoreIndexGen>("scoreIdx2");

            Assert.True(isLocIndexCreated);
            Assert.True(isScoreIndexCreated);
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_GetIndex()
        {
            bool isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx3");
            Assert.True(isLocIndexCreated);

            IIndex<string, IPlayerGrain> locIdx = await IndexFactory.GetIndex<string, IPlayerGrain>("locIdx3");

            Assert.NotNull(locIdx);
            Assert.Equal(IndexUtils.GetIndexNameFromIndexGrain(locIdx), "locIdx3");
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup1()
        {
            bool isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx4");
            Assert.True(isLocIndexCreated);

            IIndex<string, IPlayerGrain> locIdx = await IndexFactory.GetIndex<string, IPlayerGrain>("locIdx4");

            while (!await locIdx.IsAvailable()) Thread.Sleep(50);
            Assert.Equal(IndexUtils.GetIndexNameFromIndexGrain(locIdx), "locIdx4");

            IEnumerable<IPlayerGrain> result = await locIdx.Lookup("Seattle");
            Assert.Equal(0, result.AsQueryable().Count());
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup2()
        {
            IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(1);
            await p1.SetLocation("Redmond");

            bool isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx5");
            Assert.True(isLocIndexCreated);

            IPlayerGrain p2 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(2);
            IPlayerGrain p3 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(3);

            await p2.SetLocation("Redmond");
            await p3.SetLocation("Bellevue");

            IIndex<string, IPlayerGrain> locIdx = await IndexFactory.GetIndex<string, IPlayerGrain>("locIdx5");

            while (!await locIdx.IsAvailable()) Thread.Sleep(50);
            Assert.Equal(IndexUtils.GetIndexNameFromIndexGrain(locIdx), "locIdx5");

            IEnumerable<IPlayerGrain> result = await locIdx.Lookup("Redmond");
            foreach (IPlayerGrain entry in result)
            {
                output.WriteLine("guid = {0}, location = {1}, primary key = {2}", entry, await entry.GetLocation(), entry.GetPrimaryKeyLong());
            }

            Assert.Equal(2, result.AsQueryable().Count());
        }
    }
}
