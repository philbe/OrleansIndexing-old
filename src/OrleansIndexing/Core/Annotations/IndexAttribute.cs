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

        public IndexAttribute(bool IsEager) : this(typeof(AHashIndexSingleBucket<,>), IsEager, false)
        {
        }

        public IndexAttribute(Type IndexType, bool IsEager = false, bool IsUnique = false)
        {
            this.IndexType = IndexType;
            this.IsUnique = IsUnique;
            this.IsEager = IsEager;
        }
    }
}
