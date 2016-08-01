using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using UnitTests.GrainInterfaces;
using Orleans.Indexing;

namespace UnitTests.Grains
{
    public interface PlayerGrainState
    {
        int Score { get; set; }
        string Location { get; set; }
        string Email { get; set; }
    }
}
