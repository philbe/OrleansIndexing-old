using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Orleans.Runtime;

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

        /// <summary>
        /// Determines whether the index grain is a stateless worker
        /// or not. This piece of information can impact the relationship
        /// between index handlers and the index. 
        /// </summary>
        /// <returns>the result of whether the current index is
        /// a stateless worker or not</returns>
        public bool IsIndexStatelessWorker()
        {
            return IsStatelessWorker(Type.GetType(TypeCodeMapper.GetImplementation(_indexType).GrainClass));
        }

        /// <summary>
        /// A helper function that determines whether a given grain type
        /// is annotated with StatelessWorker annotation or not.
        /// </summary>
        /// <param name="grainType">the grain type to be tested</param>
        /// <returns>true if the grain type has StatelessWorker annotation,
        /// otherwise false.</returns>
        private static bool IsStatelessWorker(Type grainType)
        {
            return grainType.GetCustomAttributes(typeof(StatelessWorkerAttribute), true).Length > 0 ||
                grainType.GetInterfaces()
                    .Any(i => i.GetCustomAttributes(typeof(StatelessWorkerAttribute), true).Length > 0);
        }
    }
}
