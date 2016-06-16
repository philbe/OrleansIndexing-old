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
        private Type _indexUpdateGeneratorType;

        /// <summary>
        /// Constructs an IndexMetaData, which currently only
        /// consists of the type of the index
        /// </summary>
        /// <param name="indexType">the type of the index</param>
        /// <param name="indexUpdateGenerator">the type of the
        /// index update generator</param>
        public IndexMetaData(Type indexType, Type indexUpdateGenerator)
        {
            _indexType = indexType;
            _indexUpdateGeneratorType = indexUpdateGenerator;
        }
        
        /// <returns>the type of the index</returns>
        public Type getIndexType()
        {
            return _indexType;
        }

        /// <returns>the type of the index update
        /// generator</returns>
        public Type getIndexUpdateGeneratorType()
        {
            return _indexUpdateGeneratorType;
        }

        /// <summary>
        /// Each index has an IIndexUpdateGenerator specific to it, which this
        /// method returns an instance of it.
        /// </summary>
        /// <returns>the IIndexUpdateGenerator instance of the current index.</returns>
        public IIndexUpdateGenerator getIndexUpdateGeneratorInstance()
        {
            return (IIndexUpdateGenerator)Activator.CreateInstance(_indexUpdateGeneratorType);
        }
    }
}
