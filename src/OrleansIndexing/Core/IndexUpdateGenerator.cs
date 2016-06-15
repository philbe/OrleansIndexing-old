using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// Default implementation of IIndexUpdateGenerator<K,V>
    /// that genericly implements CreateMemberUpdate
    /// </summary>
    /// <typeparam name="K">the key type of the index</typeparam>
    /// <typeparam name="V">the value type of the index</typeparam>
    [Serializable]
    public abstract class IndexUpdateGenerator<K,V> : IIndexUpdateGenerator<K,V> where V : Grain
    {
        public override IMemberUpdate CreateMemberUpdate(V g, K befImg)
        {
            K aftImg = ExtractIndexImage(g);
            return new MemberUpdate(befImg, aftImg);
        }
    }
}
