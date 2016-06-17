using System;
using System.Threading.Tasks;
using Orleans;

namespace UnitTests.GrainInterfaces
{
    public interface IPlayerGrain : IGrainWithIntegerKey
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetEmail(string email);
        Task SetLocation(string location);
        Task SetScore(int score);
    }
}
