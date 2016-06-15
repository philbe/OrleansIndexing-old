using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The meta data that is stored beside the index
    /// </summary>
    [Serializable]
    public class IndexMetaData
    {
        Type _indexType;

        public IndexMetaData(Type indexType)
        {
            _indexType = indexType;
        }

        public Type getIndexType()
        {
            return _indexType;
        }
    }
}
