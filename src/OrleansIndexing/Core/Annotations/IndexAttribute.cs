using System;

namespace Orleans.Indexing
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IndexAttribute : Attribute
    {
        public Type IndexType { get; private set; }
        public bool IsUnique { get; private set; }
        public bool IsEager { get; private set; }

        public IndexAttribute()
        {
            IndexType = typeof(AHashIndexSingleBucket<,>);
        }

        public IndexAttribute(Type IndexType) : this(IndexType, false)
        {
        }

        public IndexAttribute(Type IndexType, bool IsUnique) : this(IndexType, false, false)
        {
        }

        public IndexAttribute(Type IndexType, bool IsUnique, bool IsEager)
        {
            this.IndexType = IndexType;
            this.IsUnique = IsUnique;
            this.IsEager = IsEager;
        }
    }
}
