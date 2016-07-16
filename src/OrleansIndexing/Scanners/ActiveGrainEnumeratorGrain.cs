using System.Threading.Tasks;
using Orleans;
using System.Collections.Generic;
using System;
using Orleans.Runtime;
using Orleans.Runtime.MembershipService;
using System.Linq;

namespace Orleans.Indexing
{
    /// <summary>
    /// Grain implementation class ActiveGrainEnumeratorGrain.
    /// </summary>
    public class ActiveGrainEnumeratorGrain : Grain, IActiveGrainEnumeratorGrain
    {

        private IMembershipTable membershipTable;

        public async Task<IEnumerable<Guid>> GetActiveGrains(string grainTypeName)
        {
            IEnumerable< Tuple < GrainId, string, int>> activeGrainList = await GetGrainActivations();
            IEnumerable<Guid> filteredList = activeGrainList.Where(s => s.Item2.Equals(grainTypeName)).Select(s => s.Item1.GetPrimaryKey());
            return filteredList.ToList();
        }

        public async Task<IEnumerable<IGrain>> GetActiveGrains(Type grainType) 
        {
            string grainTypeName = TypeCodeMapper.GetImplementation(grainType).GrainClass;
            
            IEnumerable<Tuple<GrainId, string, int>> activeGrainList = await GetGrainActivations();
            IEnumerable<IGrain> filteredList = activeGrainList.Where(s => s.Item2.Equals(grainTypeName)).Select(s => GrainFactory.GetGrain<IIndexableGrain>(s.Item1.GetPrimaryKey(),grainType));
            return filteredList.ToList();
        }


        private async Task<IEnumerable<Tuple<GrainId, string, int>>> GetGrainActivations()
        {
            Dictionary<SiloAddress, SiloStatus> hosts = await GetHosts(true).ConfigureAwait(false);
            SiloAddress[] silos = hosts.Keys.ToArray();
            return await GetGrainActivations(silos).ConfigureAwait(false);
        }

        private async Task<IEnumerable<Tuple<GrainId, string, int>>> GetGrainActivations(SiloAddress[] hostsIds)
        {
            IEnumerable<Task<List<Tuple<GrainId, string, int>>>> all = SiloUtils.GetSiloAddresses(hostsIds).Select(s => SiloUtils.GetSiloControlReference(s).GetGrainStatistics());
            List<Tuple<GrainId, string, int>>[] result = await Task.WhenAll(all).ConfigureAwait(false);
            return result.SelectMany(s => s);
        }


        #region copy & paste from ManagementGrain.cs

        private async Task<Dictionary<SiloAddress, SiloStatus>> GetHosts(bool onlyActive = false)
        {
            var mTable = await GetMembershipTable();
            var table = await mTable.ReadAll();

            var t = onlyActive ?
                table.Members.Where(item => item.Item1.Status.Equals(SiloStatus.Active)).ToDictionary(item => item.Item1.SiloAddress, item => item.Item1.Status) :
                table.Members.ToDictionary(item => item.Item1.SiloAddress, item => item.Item1.Status);
            return t;
        }

        private async Task<IMembershipTable> GetMembershipTable()
        {
            if (membershipTable == null)
            {
                var factory = new MembershipFactory();
                membershipTable = factory.GetMembershipTable(Silo.CurrentSilo.GlobalConfig.LivenessType, Silo.CurrentSilo.GlobalConfig.MembershipTableAssembly);

                await membershipTable.InitializeMembershipTable(Silo.CurrentSilo.GlobalConfig, false,
                    TraceLogger.GetLogger(membershipTable.GetType().Name));
            }
            return membershipTable;
        }

        private static SiloAddress[] GetSiloAddresses(SiloAddress[] silos)
        {
            if (silos != null && silos.Length > 0)
                return silos;

            return InsideRuntimeClient.Current.Catalog.SiloStatusOracle
                .GetApproximateSiloStatuses(true).Select(s => s.Key).ToArray();
        }

        private ISiloControl GetSiloControlReference(SiloAddress silo)
        {
            return InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<ISiloControl>(Constants.SiloControlId, silo);
        }

        #endregion
    }
}
