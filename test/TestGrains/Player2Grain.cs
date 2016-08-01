using System;
using Orleans.Providers;
using UnitTests.GrainInterfaces;

namespace UnitTests.Grains
{
    [Serializable]
    public class Player2GrainState : Player2Properties, PlayerGrainState
    {
        public string Email { get; set; }
    }

    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = "MemoryStore")]
    public class Player2Grain : PlayerGrainNonFaultTolerant<Player2GrainState, Player2Properties>, IPlayer2Grain
    {
    }
}
