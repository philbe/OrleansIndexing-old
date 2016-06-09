using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    public abstract class IndexOps<T>
    {
        abstract public object ExtractIndexImage(T g);

        public IMemberUpdate CreateMemberUpdate(T g, object befImg)
        {
            var aftImg = ExtractIndexImage(g);
            return new MemberUpdate(aftImg, befImg);
        }
    }
}
