using Orleans.Runtime;
using Orleans.Runtime.MembershipService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Linq.Expressions;

namespace Orleans.Indexing
{
    /// <summary>
    /// The query class for querying all active grains of a given type
    /// </summary>
    public class QueryActiveGrainsNode<TIGrain, TProperties> : QueryGrainsNode<TIGrain, TProperties> where TIGrain : IIndexableGrain
    {
        public QueryActiveGrainsNode(IGrainFactory gf) : base(gf)
        {
        }

        public override Task<IOrleansQueryResult<TIGrain>> GetResults()
        {
            throw new NotSupportedException(string.Format("Traversing over all the active grains of {0} is not supported.", typeof(TIGrain)));
        }
    }
}
