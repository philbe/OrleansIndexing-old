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
    public abstract class QueryGrainsNode<T> : QueryGrainsNode, IOrleansQueryable<T> where T : IIndexableGrain
    {

        public QueryGrainsNode(IGrainFactory gf) : base(gf)
        {
        }

        public virtual Type ElementType
        {
            get
            {
                return typeof(T);
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
                return new OrleansQueryProvider<T>();
            }
        }

        /// <summary>
        /// This method gets the result of executing the query
        /// on this query object
        /// </summary>
        /// <returns>the query result</returns>
        public abstract Task<IOrleansQueryResult<T>> GetResults();

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotSupportedException("GetEnumerator is not supported on QueryGrainsNode. User ");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
