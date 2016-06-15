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
        public static OutputGrainInterfaceType GetGrain<OutputGrainInterfaceType>(this IGrainFactory gf, string grainID, Type grainInterfaceType) where OutputGrainInterfaceType : IGrain
        {
            //return GrainFactoryBase.MakeGrainReference_FromType(
            //        baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, grainID, grainInterfaceType),
            //        grainInterfaceType).AsReference<OutputGrainInterfaceType>();
            return GetGrain<OutputGrainInterfaceType>(gf, grainID, grainInterfaceType, typeof(OutputGrainInterfaceType));
        }
        public static SuperOutputGrainInterfaceType GetGrain<SuperOutputGrainInterfaceType>(this IGrainFactory gf, string grainID, Type grainInterfaceType, Type outputGrainInterfaceType)
        {
            return (SuperOutputGrainInterfaceType)((GrainFactory)gf).Cast(GrainFactoryBase.MakeGrainReference_FromType(
                    baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, grainID, grainInterfaceType),
                    grainInterfaceType), outputGrainInterfaceType);
        }
    }
}
