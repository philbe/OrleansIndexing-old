using System;
using Orleans.Providers;
using UnitTests.GrainInterfaces;

namespace UnitTests.Grains
{
    [Serializable]
    public class Player3GrainState : Player3Properties, PlayerGrainState
    {
        public string Email { get; set; }
    }

    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = "MemoryStore")]
    public class Player3Grain : PlayerGrainNonFaultTolerant<Player3GrainState, Player3Properties>, IPlayer3Grain
    {
    }
}
