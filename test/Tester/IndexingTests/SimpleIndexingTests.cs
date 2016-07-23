﻿using System;
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

    public class SimpleIndexingTests : HostedTestClusterEnsureDefaultStarted
    {

        private readonly ITestOutputHelper output;

        public SimpleIndexingTests(DefaultClusterFixture fixture, ITestOutputHelper output)
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

        //[Fact, TestCategory("BVT"), TestCategory("Indexing")]
        //public async Task Test_Indexing_GetIndex()
        //{
        //    bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx3");
        //    Assert.IsTrue(isLocIndexCreated);

        //    IIndex<string, IPlayerGrain> locIdx = await GrainClient.GrainFactory.GetIndex<string, IPlayerGrain>("locIdx3");

        //    Assert.IsNotNull(locIdx);
        //}

        //[Fact, TestCategory("BVT"), TestCategory("Indexing")]
        //public async Task Test_Indexing_IndexLookup1()
        //{
        //    bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx4");
        //    Assert.IsTrue(isLocIndexCreated);

        //    IIndex<string, IPlayerGrain> locIdx = await GrainClient.GrainFactory.GetIndex<string, IPlayerGrain>("locIdx4");

        //    while (!await locIdx.IsAvailable()) Thread.Sleep(50);

        //    IOrleansQueryResultStream<IPlayerGrain> result = await locIdx.Lookup("Seattle");
        //    int counter = 0;
        //    result.Subscribe(p => counter += 1);
        //    result.Dispose();
        //    Assert.Equal(0, counter);
        //}

        //[Fact, TestCategory("BVT"), TestCategory("Indexing")]
        //public async Task Test_Indexing_IndexLookup2()
        //{
        //    IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(1);
        //    await p1.SetLocation("Redmond");

        //    bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("locIdx5");
        //    Assert.IsTrue(isLocIndexCreated);

        //    IPlayerGrain p2 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(2);
        //    IPlayerGrain p3 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(3);

        //    await p2.SetLocation("Redmond");
        //    await p3.SetLocation("Bellevue");

        //    IIndex<string, IPlayerGrain> locIdx = await GrainClient.GrainFactory.GetIndex<string, IPlayerGrain>("locIdx5");

        //    while (!await locIdx.IsAvailable()) Thread.Sleep(50);

        //    IOrleansQueryResultStream<IPlayerGrain> result = await locIdx.Lookup("Redmond");
        //    int counter = 0;
        //    result.Subscribe(async entry =>
        //    {
        //        counter++;
        //        output.WriteLine("guid = {0}, location = {1}, primary key = {2}", entry, await entry.GetLocation(), entry.GetPrimaryKeyLong());
        //    });
        //    result.Dispose();

        //    Assert.Equal(2, counter);
        //}

        //[Fact, TestCategory("BVT"), TestCategory("Indexing")]
        //public async Task Test_Indexing_IndexLookup3()
        //{
        //    await GrainClient.GrainFactory.DropAllIndexes<IPlayerGrain>();

        //    IPlayerGrain p1 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(1);
        //    await p1.SetLocation("Lausanne");

        //    bool isLocIndexCreated = await GrainClient.GrainFactory.CreateAndRegisterIndex<HashIndexSingleBucketInterface<string, IPlayerGrain>, PlayerLocIndexGen>("__GetLocation");
        //    Assert.IsTrue(isLocIndexCreated);

        //    IPlayerGrain p2 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(2);
        //    IPlayerGrain p3 = GrainClient.GrainFactory.GetGrain<IPlayerGrain>(3);

        //    await p2.SetLocation("Lausanne");
        //    await p3.SetLocation("Geneva");

        //    IIndex<string, IPlayerGrain> locIdx = await GrainClient.GrainFactory.GetIndex<string, IPlayerGrain>("__GetLocation");

        //    while (!await locIdx.IsAvailable()) Thread.Sleep(50);

        //    IOrleansQueryResultStream<IPlayerGrain> result = await GrainClient.GrainFactory.GetActiveGrains<IPlayerGrain>(p => (p.GetLocation().Result) == "Lausanne");

        //    int counter = 0;
        //    result.Subscribe(async entry =>
        //    {
        //        counter++;
        //        output.WriteLine("guid = {0}, location = {1}, primary key = {2}", entry, await entry.GetLocation(), entry.GetPrimaryKeyLong());
        //    });
        //    result.Dispose();

        //    Assert.Equal(2, counter);
        //}

        private async Task<int> CountPlayersStreamingIn<TIGrain, TProperties>(string city) where TIGrain : IPlayerGrain, IIndexableGrain where TProperties : PlayerProperties
        {
            var taskCompletionSource = new TaskCompletionSource<int>();
            Task<int> tsk = taskCompletionSource.Task;
            Action<int> responseHandler = taskCompletionSource.SetResult;

            IOrleansQueryable<TIGrain, TProperties> q = from player in GrainClient.GrainFactory.GetActiveGrains<TIGrain, TProperties>()
                                                        where player.Location == city
                                                        select player;
            
            int counter = 0;
            var _ = q.ObserveResults(new QueryResultStreamObserver<TIGrain>(async entry =>
            {
                counter++;
                output.WriteLine("guid = {0}, location = {1}, primary key = {2}", entry, await entry.GetLocation(), entry.GetPrimaryKeyLong());
            }, () => {
                responseHandler(counter);
                return TaskDone.Done;
            }));

            int finalCount = await tsk;

            Assert.Equal(finalCount, await CountPlayersBlockingIn(q));

            return finalCount;
        }

        private async Task<int> CountPlayersBlockingIn<TIGrain, TProperties>(IOrleansQueryable<TIGrain, TProperties> q) where TIGrain : IPlayerGrain, IIndexableGrain where TProperties : PlayerProperties
        {
            IOrleansQueryResult<TIGrain> result = await q.GetResults();

            return result.Count();
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucker
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup4()
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

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer1Grain, Player1Properties>("Seattle"));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.Equal(1, await CountPlayersStreamingIn<IPlayer1Grain, Player1Properties>("Seattle"));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer1Grain>(2);
            Assert.Equal("Seattle", await p2.GetLocation());

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer1Grain, Player1Properties>("Seattle"));
        }

        /// <summary>
        /// Tests basic functionality of AHashIndexPartitionedPerSiloImpl with 1 Silo
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup5()
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

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Tehran"));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.Equal(1, await CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Tehran"));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(2);
            Assert.Equal("Tehran", await p2.GetLocation());

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Tehran"));
        }

        /// <summary>
        /// Tests basic functionality of AHashIndexPartitionedPerSiloImpl with 2 Silos
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup6()
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

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Seattle"));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.Equal(1, await CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Seattle"));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer2Grain>(2);
            Assert.Equal("Seattle", await p2.GetLocation());

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer2Grain, Player2Properties>("Seattle"));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_IndexLookup7()
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

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer3Grain, Player3Properties>("Seattle"));

            await p2.Deactivate();

            Thread.Sleep(1000);

            Assert.Equal(1, await CountPlayersStreamingIn<IPlayer3Grain, Player3Properties>("Seattle"));

            p2 = GrainClient.GrainFactory.GetGrain<IPlayer3Grain>(2);
            Assert.Equal("Seattle", await p2.GetLocation());

            Assert.Equal(2, await CountPlayersStreamingIn<IPlayer3Grain, Player3Properties>("Seattle"));
        }
    }
}
