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
    /// Implements <see cref="IOrleansQueryable"/>
    /// </summary>
    public class QueryIndexedGrainsNode<TIGrain, TProperties> : QueryGrainsNode<TIGrain, TProperties> where TIGrain : IIndexableGrain
    {
        private string _indexName;

        private object _param;

        public QueryIndexedGrainsNode(IGrainFactory grainFactory, string indexName, object param) : base(grainFactory)
        {
            _indexName = indexName;
            _param = param;
        }
        public override async Task<IOrleansQueryResult<TIGrain>> GetResults()
        {
            return (IOrleansQueryResult<TIGrain>)await (await GetGrainFactory().GetIndex(_indexName, typeof(TIGrain))).Lookup(_param);
        }
    }
}
