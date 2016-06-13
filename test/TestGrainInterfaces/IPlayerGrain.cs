using System;
using System.Threading.Tasks;
using Orleans;

namespace UnitTests.GrainInterfaces
{
    public interface IPlayerGrain : IGrainWithIntegerKey
    {
        Task<string> GetLocation();
        Task<int> GetScore();

        Task SetLocation(string location);
        Task SetScore(int score);
    }
}
