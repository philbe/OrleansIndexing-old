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
    /// The top-level class for query objects
    /// </summary>
    public abstract class QueryGrainsNode
    {
        private IGrainFactory _grainFactory;

        public QueryGrainsNode(IGrainFactory gf)
        {
            _grainFactory = gf;
        }

        public IGrainFactory GetGrainFactory()
        {
            return _grainFactory;
        }
    }
    /// <summary>
    /// The top-level class for query objects, which implements <see cref="IOrleansQueryable{T}"/>
    /// </summary>
    public abstract class QueryGrainsNode<TIGrain, TProperties> : QueryGrainsNode, IOrleansQueryable<TIGrain, TProperties> where TIGrain : IIndexableGrain
    {

        public QueryGrainsNode(IGrainFactory gf) : base(gf)
        {
        }

        public virtual Type ElementType
        {
            get
            {
                return typeof(TIGrain);
            }
        }

        public virtual Expression Expression
        {
            get
            {
                return Expression.Constant(this);
            }
        }

        public virtual IQueryProvider Provider
        {
            get
            {
                return new OrleansQueryProvider<TIGrain, TProperties>();
            }
        }

        /// <summary>
        /// This method gets the result of executing the query
        /// on this query object
        /// </summary>
        /// <returns>the query result</returns>
        public abstract Task<IOrleansQueryResult<TIGrain>> GetResults();

        public IEnumerator<TProperties> GetEnumerator()
        {
            throw new NotSupportedException("GetEnumerator is not supported on QueryGrainsNode. User ");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
