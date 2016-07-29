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
using Xunit.Abstractions;
using System.Threading;
using Orleans.TestingHost;

namespace UnitTests.IndexingTests
{
    //[Serializable]
    //public class PlayerLocIndexGen : IndexUpdateGenerator<string, PlayerProperties>
    //{
    //    public override string ExtractIndexImage(PlayerProperties g)
    //    {
    //        return g.Location;
    //    }
    //}

    //[Serializable]
    //public class PlayerScoreIndexGen : IndexUpdateGenerator<int, PlayerProperties>
    //{
    //    public override int ExtractIndexImage(PlayerProperties g)
    //    {
    //        return g.Score;
    //    }
    //}

    public class FaultTolerantIndexingTests : HostedTestClusterEnsureDefaultStarted
    {

        private readonly ITestOutputHelper output;

        public FaultTolerantIndexingTests(DefaultClusterFixture fixture, ITestOutputHelper output)
            : base(fixture)
        {
            this.output = output;
        }

        //[Fact, TestCategory("BVT"), TestCategory("Indexing")]
        //public async Task Test_Indexing_AddOneIndex()
        //{
        //    bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx1");
        //    Assert.IsTrue(isLocIndexCreated);
        //}

        //[Fact, TestCategory("BVT"), TestCategory("Indexing")]
        //public async Task Test_Indexing_AddTwoIndexes()
        //{
        //    bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx2");
        //    bool isScoreIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<int, IPlayerGrain>, PlayerScoreIndexGen>("scoreIdx2");

        //    Assert.IsTrue(isLocIndexCreated);
        //    Assert.IsTrue(isScoreIndexCreated);
        //}

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucker
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup1()
        {
            //await GrainClient.GrainFactory.DropAllIndexes<IPlayerGrain>();

            IPlayer1Grain p1 = GrainClient.GrainFactory.GetGrain<IPlayer1Grain>(1);
            await p1.SetLocation("Seattle");

            //bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("__GetLocation");
            //Assert.IsTrue(isLocIndexCreated);

            IPlayer1Grain p2 = GrainClient.GrainFactory.GetGrain<IPlayer1Grain>(2);
            IPlayer1Grain p3 = GrainClient.GrainFactory.GetGrain<IPlayer1Grain>(3);

            await p2.SetLocation("Seattle");
            await p3.SetLocation("San Fransisco");

            IIndex<string, IPlayer1Grain> locIdx = GrainClient.GrainFactory.GetIndex<string, IPlayer1Grain>("__Location");

            while (!await locIdx.IsAvailable()) Thread.Sleep(50);



            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer1Grain, Player1Properties>("Seattle", output));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.AreEqual(1, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer1Grain, Player1Properties>("Seattle", output));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer1Grain>(2);
            Assert.AreEqual("Seattle", await p2.GetLocation());

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer1Grain, Player1Properties>("Seattle", output));
        }

        /// <summary>
        /// Tests basic functionality of AHashIndexPartitionedPerSiloImpl with 1 Silo
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup2()
        {
            if (HostedCluster.SecondarySilos.Count > 0)
            {
                HostedCluster.StopSecondarySilos();
                await HostedCluster.WaitForLivenessToStabilizeAsync();
            }
            //await GrainClient.GrainFactory.DropAllIndexes<IPlayerGrain>();

            IPlayer2Grain p1 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(1);
            await p1.SetLocation("Tehran");

            //bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("__GetLocation");
            //Assert.IsTrue(isLocIndexCreated);

            IPlayer2Grain p2 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(2);
            IPlayer2Grain p3 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(3);

            await p2.SetLocation("Tehran");
            await p3.SetLocation("Yazd");

            IIndex<string, IPlayer2Grain> locIdx = GrainClient.GrainFactory.GetIndex<string, IPlayer2Grain>("__Location");

            while (!await locIdx.IsAvailable()) Thread.Sleep(50);

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Tehran", output));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.AreEqual(1, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Tehran", output));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(2);
            Assert.AreEqual("Tehran", await p2.GetLocation());

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Tehran", output));
        }

        /// <summary>
        /// Tests basic functionality of AHashIndexPartitionedPerSiloImpl with 2 Silos
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup3()
        {
            //await GrainClient.GrainFactory.DropAllIndexes<IPlayerGrain>();

            if (HostedCluster.SecondarySilos.Count == 0)
            {
                HostedCluster.StartAdditionalSilo();
                await HostedCluster.WaitForLivenessToStabilizeAsync();
            }

            IPlayer2Grain p1 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(1);
            await p1.SetLocation("Seattle");

            //bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("__GetLocation");
            //Assert.IsTrue(isLocIndexCreated);

            IPlayer2Grain p2 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(2);
            IPlayer2Grain p3 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(3);

            await p2.SetLocation("Seattle");
            await p3.SetLocation("San Fransisco");

            IIndex<string, IPlayer2Grain> locIdx = GrainClient.GrainFactory.GetIndex<string, IPlayer2Grain>("__Location");

            while (!await locIdx.IsAvailable()) Thread.Sleep(50);

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Seattle", output));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.AreEqual(1, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Seattle", output));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(2);
            Assert.AreEqual("Seattle", await p2.GetLocation());

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Seattle", output));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup4()
        {
            //await GrainClient.GrainFactory.DropAllIndexes<IPlayerGrain>();

            IPlayer3Grain p1 = GrainClient.GrainFactory.GetGrain<IPlayer3Grain>(1);
            await p1.SetLocation("Seattle");

            //bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("__Location");
            //Assert.IsTrue(isLocIndexCreated);

            IPlayer3Grain p2 = GrainClient.GrainFactory.GetGrain<IPlayer3Grain>(2);
            IPlayer3Grain p3 = GrainClient.GrainFactory.GetGrain<IPlayer3Grain>(3);

            await p2.SetLocation("Seattle");
            await p3.SetLocation("San Fransisco");

            IIndex<string, IPlayer3Grain> locIdx = GrainClient.GrainFactory.GetIndex<string, IPlayer3Grain>("__Location");

            while (!await locIdx.IsAvailable()) Thread.Sleep(50);

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer3Grain, Player3Properties>("Seattle", output));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.AreEqual(1, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer3Grain, Player3Properties>("Seattle", output));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer3Grain>(2);
            Assert.AreEqual("Seattle", await p2.GetLocation());

            Assert.AreEqual(2, await IndexingTestUtils.CountPlayersStreamingIn<IPlayer3Grain, Player3Properties>("Seattle", output));
        }
    }
}
