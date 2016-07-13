using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Runtime.MembershipService;

namespace Orleans.Indexing
{
    /// <summary>
    /// A utility class for the low-level operations related to silos
    /// </summary>
    internal static class SiloUtils
    {
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

        internal static SiloAddress[] GetSiloAddresses(SiloAddress[] silos)
        {
            if (silos != null && silos.Length > 0)
                return silos;

            return InsideRuntimeClient.Current.Catalog.SiloStatusOracle
                .GetApproximateSiloStatuses(true).Select(s => s.Key).ToArray();
        }

        internal static ISiloControl GetSiloControlReference(SiloAddress silo)
        {
            return InsideRuntimeClient.Current.InternalGrainFactory.GetSystemTarget<ISiloControl>(Constants.SiloControlId, silo);
        }

        #endregion
    }
}
