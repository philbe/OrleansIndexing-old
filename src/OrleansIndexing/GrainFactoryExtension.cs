using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    public static class GrainExtensions
    {
        public static IGrainType GetGrain<IGrainType>(this IGrainFactory gf, string grainID, Type grainInterfaceType)
        {
            return GrainFactoryBase.MakeGrainReference_FromType(
                    baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, grainID, grainInterfaceType),
                    grainInterfaceType).AsReference<IGrainType>();
        }
    }
}
