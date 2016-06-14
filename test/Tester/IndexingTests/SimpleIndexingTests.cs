using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using UnitTests.GrainInterfaces;
using UnitTests.Tester;
using Xunit;
using Tester;
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
            var isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx1");
            Assert.IsTrue(isLocIndexCreated);

        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_AddTwoIndexes()
        {
            var isLocIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx2");
            var isScoreIndexCreated = await IndexFactory.CreateAndRegisterIndex<IHashIndexInMemory<int, IPlayerGrain>, PlayerScoreIndexGen>("scoreIdx2");

            Assert.IsTrue(isLocIndexCreated);
            Assert.IsTrue(isScoreIndexCreated);

        }
    }
}
