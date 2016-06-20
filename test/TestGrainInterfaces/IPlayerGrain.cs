using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;

namespace UnitTests.GrainInterfaces
{
    public interface IPlayerGrain : IGrainWithIntegerKey, IIndexableGrain
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetEmail(string email);
        Task SetLocation(string location);
        Task SetScore(int score);
    }
}
