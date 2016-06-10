using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public abstract class IndexOps<K,V> : IIndexOps<K,V> where V : Grain
    {
        public override IMemberUpdate CreateTypedMemberUpdate(V g, K befImg)
        {
            K aftImg = ExtractTypedIndexImage(g);
            return new MemberUpdate(aftImg, befImg);
        }
    }
}
