using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;

namespace UnitTests.GrainInterfaces
{
    [Serializable]
    public class PlayerProperties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }
    }

    public interface IPlayerGrain : IGrainWithIntegerKey, IIndexableGrain<PlayerProperties>
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetEmail(string email);
        Task SetLocation(string location);
        Task SetScore(int score);
    }
}
