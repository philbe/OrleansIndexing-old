using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using UnitTests.GrainInterfaces;
using Orleans.Indexing;

namespace UnitTests.Grains
{
    [Serializable]
    public class PlayerGrainState
    {
        public string Email { get; set; }

        public string Location { get; set; }

        public int Score { get; set; }

    }

    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = "MemoryStore")]
    public class PlayerGrain : IndexableGrain<PlayerGrainState>, IPlayerGrain
    {
        private Logger logger;
        
        public string Email { get { return State.Email; } }
        public string Location { get { return State.Location; } }
        public int Score { get { return State.Score; } }

        public override Task OnActivateAsync()
        {
            logger = GetLogger("PlayerGrain-" + IdentityString);
            return base.OnActivateAsync();
        }

        public Task<string> GetLocation()
        {
            return Task.FromResult(Location);
        }

        public Task SetLocation(string location)
        {
            State.Location = location;
            //return TaskDone.Done;
            return base.WriteStateAsync();
        }

        public Task<int> GetScore()
        {
            return Task.FromResult(Score);
        }

        public Task SetScore(int score)
        {
            State.Score = score;
            //return TaskDone.Done;
            return base.WriteStateAsync();
        }

        public Task<string> GetEmail()
        {
            return Task.FromResult(Email);
        }

        public Task SetEmail(string email)
        {
            State.Email = email;
            //return TaskDone.Done;
            return base.WriteStateAsync();
        }
    }
}
