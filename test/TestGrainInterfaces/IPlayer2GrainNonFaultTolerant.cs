using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    public class Player2PropertiesNonFaultTolerant : PlayerProperties
    {
        public int Score { get; set; }

        [Index(typeof(AHashIndexPartitionedPerSilo<string, IPlayer2GrainNonFaultTolerant>), IsEager: true)]
        public string Location { get; set; }
    }

    public interface IPlayer2GrainNonFaultTolerant : IPlayerGrain, IIndexableGrain<Player2PropertiesNonFaultTolerant>
    {
    }
}
