using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    [Reentrant]
    public class IndexFinder : Grain, IIndexFinder
    {
        public Task<IIndex> GetIndex(Type iGrainType, string indexName)
        {
            return Task.FromResult(IndexHandler.GetIndex(iGrainType, indexName));
        }
    }
}
