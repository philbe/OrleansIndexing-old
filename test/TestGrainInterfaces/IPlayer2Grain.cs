using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    public class Player2Properties
    {
        public int Score { get; set; }

        [Index(typeof(HashIndexPartitionedPerSilo<string, IPlayer2Grain>))]
        public string Location { get; set; }
    }

    public interface IPlayer2Grain : IGrainWithIntegerKey, IIndexableGrain<Player2Properties>
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetEmail(string email);
        Task SetLocation(string location);
        Task SetScore(int score);
    }
}
