using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    public class Player1Properties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }
    }

    public interface IPlayer1Grain : IGrainWithIntegerKey, IIndexableGrain<Player1Properties>
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetEmail(string email);
        Task SetLocation(string location);
        Task SetScore(int score);
    }
}
