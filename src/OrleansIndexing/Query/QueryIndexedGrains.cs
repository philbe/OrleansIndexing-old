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
    public class QueryIndexedGrains<T> : QueryGrains<T> where T : IIndexableGrain
    {
        private string _indexName;

        private object _param;

        private IGrainFactory _grainFactory;

        public QueryIndexedGrains(IGrainFactory grainFactory, string indexName, object param) : base(grainFactory)
        {
            _indexName = indexName;
            _param = param;
            _grainFactory = grainFactory;
        }
        public override async Task<IOrleansQueryResult<T>> GetResults()
        {
            return (IOrleansQueryResult<T>)await (await _grainFactory.GetIndex(_indexName, typeof(T))).Lookup(_param);
        }
    }
}
