using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    public abstract class IndexUpdateGenerator<K,V> : IIndexUpdateGenerator<K,V> where V : Grain
    {
        public override IMemberUpdate CreateMemberUpdate(V g, K befImg)
        {
            K aftImg = ExtractIndexImage(g);
            return new MemberUpdate(aftImg, befImg);
        }
    }
}
