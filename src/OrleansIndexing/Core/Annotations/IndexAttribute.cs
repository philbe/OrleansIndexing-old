using System;

namespace Orleans.Indexing
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IndexAttribute : Attribute
    {
        public Type IndexType { get; private set; }

        public IndexAttribute()
        {
            IndexType = typeof(IHashIndexSingleBucket<,>);
        }

        public IndexAttribute(Type indexType)
        {
            IndexType = indexType;
        }
    }
}
