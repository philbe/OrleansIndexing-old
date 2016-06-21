using Orleans.Runtime;
using Orleans.Runtime.MembershipService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Orleans.Indexing
{
    public class ActiveGrainScanner
    {

        public static async Task<IEnumerable<T>> GetActiveGrains<T>() where T : IGrain
        {
            string grainTypeName = TypeCodeMapper.GetImplementation(typeof(T)).GrainClass;

            IEnumerable<Tuple<GrainId, string, int>> activeGrainList = await GetGrainActivations();
            IEnumerable<T> filteredList = activeGrainList.Where(s => s.Item2.Equals(grainTypeName)).Select(s => GrainClient.GrainFactory.GetGrain<T>(s.Item1.GetPrimaryKey(), typeof(T)));
            return filteredList.ToList();
        }

        public static async Task<IEnumerable<T>> GetActiveGrains<T>(IGrainFactory gf, params SiloAddress[] hostsIds) where T : IGrain
        {
            string grainTypeName = TypeCodeMapper.GetImplementation(typeof(T)).GrainClass;

            IEnumerable<Tuple<GrainId, string, int>> activeGrainList = await GetGrainActivations(hostsIds);
            IEnumerable<T> filteredList = activeGrainList.Where(s => s.Item2.Equals(grainTypeName)).Select(s => gf.GetGrain<T>(s.Item1.GetPrimaryKey(), typeof(T)));
            return filteredList.ToList();
        }

        private static async Task<IEnumerable<Tuple<GrainId, string, int>>> GetGrainActivations()
        {
            Dictionary<SiloAddress, SiloStatus> hosts = await GetHosts(true);
            SiloAddress[] silos = hosts.Keys.ToArray();
            return await GetGrainActivations(silos);
        }

        internal static async Task<IEnumerable<Tuple<GrainId, string, int>>> GetGrainActivations(params SiloAddress[] hostsIds)
        {
            List<Task<List<Tuple<GrainId, string, int>>>> all = GetSiloAddresses(hostsIds).Select(s => GetSiloControlReference(s).GetGrainStatistics()).ToList();
            await Task.WhenAll(all);
            return all.SelectMany(s => s.Result);
        }


        #region copy & paste from ManagementGrain.cs

        internal static async Task<Dictionary<SiloAddress, SiloStatus>> GetHosts(bool onlyActive = false)
        {
            var mTable = await GetMembershipTable();
            var table = await mTable.ReadAll();

            var t = onlyActive ?
                table.Members.Where(item => item.Item1.Status.Equals(SiloStatus.Active)).ToDictionary(item => item.Item1.SiloAddress, item => item.Item1.Status) :
                table.Members.ToDictionary(item => item.Item1.SiloAddress, item => item.Item1.Status);
            return t;
        }

        private static async Task<IMembershipTable> GetMembershipTable()
        {
            IMembershipTable membershipTable = null;
            var factory = new MembershipFactory();
            membershipTable = factory.GetMembershipTable(Silo.CurrentSilo.GlobalConfig.LivenessType, Silo.CurrentSilo.GlobalConfig.MembershipTableAssembly);

            await membershipTable.InitializeMembershipTable(Silo.CurrentSilo.GlobalConfig, false,
                TraceLogger.GetLogger(membershipTable.GetType().Name));

            return membershipTable;
        }

        private static SiloAddress[] GetSiloAddresses(SiloAddress[] silos)
        {
            if (silos != null && silos.Length > 0)
                return silos;

            return InsideRuntimeClient.Current.Catalog.SiloStatusOracle
                .GetApproximateSiloStatuses(true).Select(s => s.Key).ToArray();
        }

        private static ISiloControl GetSiloControlReference(SiloAddress silo)
        {
            return InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<ISiloControl>(Constants.SiloControlId, silo);
        }

        #endregion
    }
}
