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
        private Type _indexType;

        /// <summary>
        /// Constructs an IndexMetaData, which currently only
        /// consists of the type of the index
        /// </summary>
        /// <param name="indexType"></param>
        public IndexMetaData(Type indexType)
        {
            _indexType = indexType;
        }

        /// <summary>
        /// Returns the type of the index
        /// </summary>
        /// <returns>the type of the index</returns>
        public Type getIndexType()
        {
            return _indexType;
        }
    }
}
