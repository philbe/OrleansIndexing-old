using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class stores the list of indexes for a given grain type.
    /// </summary>
    [Serializable]
    public class IndexRegistryState
    {
        public IDictionary<string, Tuple<IIndex,IndexMetaData>> indexes { set; get; }
    }
}