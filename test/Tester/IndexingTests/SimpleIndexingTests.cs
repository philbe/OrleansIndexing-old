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

namespace UnitTests.General
{
    [Serializable]
    public class PlayerLocIndexGen : IndexUpdateGenerator<string, PlayerGrain>
    {
        public override string ExtractIndexImage(PlayerGrain g)
        {
            return g.Location;
        }
    }

    [Serializable]
    public class PlayerScoreIndexGen : IndexUpdateGenerator<int, PlayerGrain>
    {
        public override int ExtractIndexImage(PlayerGrain g)
        {
            return g.Score;
        }
    }

    public class SimpleIndexingTests : HostedTestClusterEnsureDefaultStarted
    {
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

            IIndex<string, IPlayerGrain> locIdx = IndexFactory.GetIndex<string, IPlayerGrain>("locIdx3");

            Assert.NotNull(locIdx);
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup()
        {
            bool isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx4");
            Assert.True(isLocIndexCreated);

            IIndex<string, IPlayerGrain> locIdx = IndexFactory.GetIndex<string, IPlayerGrain>("locIdx4");

            IEnumerable<IPlayerGrain> result = await locIdx.Lookup("Redmond");
            Assert.Equal(result.AsQueryable().Count(), 0);
        }
    }
}
