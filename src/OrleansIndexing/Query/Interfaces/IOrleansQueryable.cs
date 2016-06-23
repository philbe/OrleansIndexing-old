using Orleans.Runtime;
using Orleans.Runtime.MembershipService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// Extension for the built-in <see cref="IOrderedQueryable"/> allowing for Orleans specific operations
    /// </summary>
    //public interface IOrleansQueryable : IOrderedQueryable
    //{
    //}

    /// <summary>
    /// Extension for the built-in <see cref="IOrderedQueryable{T}"/> allowing for Orleans specific operations
    /// </summary>
    public interface IOrleansQueryable<T> : /*IOrleansQueryable, */ IOrderedQueryable<T> where T : IIndexableGrain
    {
        Task<IOrleansQueryResult<T>> GetResults();
    }
}
