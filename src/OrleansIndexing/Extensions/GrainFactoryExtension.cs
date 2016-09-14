﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    public static class GrainFactoryExtensions
    {
        /// <summary>
        /// This method extends GrainFactory by adding a new GetGrain method
        /// that can get the runtime type of the grain interface along
        /// with its primary key, and return the grain casted to an interface
        /// that the given grainInterfaceType extends.
        /// 
        /// The main use-case is when you want to get a grain whose type
        /// is unknown at compile time.
        /// </summary>
        /// <typeparam name="OutputGrainInterfaceType">The output type of
        /// the grain</typeparam>
        /// <param name="gf">the grainFactory instance that this method
        /// is extending its functionality</param>
        /// <param name="grainID">the primary key of the grain</param>
        /// <param name="grainInterfaceType">the runtime type of the grain
        /// interface</param>
        /// <returns>the requested grain with the given grainID and grainInterfaceType</returns>
        public static OutputGrainInterfaceType GetGrain<OutputGrainInterfaceType>(this IGrainFactory gf, string grainID, Type grainInterfaceType) where OutputGrainInterfaceType : IGrain
        {
            return GrainFactoryBase.MakeGrainReference_FromType(
                    baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, grainID, grainInterfaceType),
                    grainInterfaceType).AsReference<OutputGrainInterfaceType>(gf);
            //return GetGrain<OutputGrainInterfaceType>(gf, grainID, grainInterfaceType, typeof(OutputGrainInterfaceType));
        }

        /// <summary>
        /// This method extends GrainFactory by adding a new GetGrain method
        /// to it that can get the runtime type of the grain interface along
        /// with its primary key, and return the grain casted to an interface
        /// that the given grainInterfaceType extends it.
        /// 
        /// The main use-case is when you want to get a grain that its type
        /// is unknown at compile time.
        /// </summary>
        /// <typeparam name="OutputGrainInterfaceType">The output type of
        /// the grain</typeparam>
        /// <param name="gf">the grainFactory instance that this method
        /// is extending its functionality</param>
        /// <param name="grainID">the primary key of the grain</param>
        /// <param name="grainInterfaceType">the runtime type of the grain
        /// interface</param>
        /// <returns>the requested grain with the given grainID and grainInterfaceType</returns>
        public static OutputGrainInterfaceType GetGrain<OutputGrainInterfaceType>(this IGrainFactory gf, Guid grainID, Type grainInterfaceType) where OutputGrainInterfaceType : IGrain
        {
            return GrainFactoryBase.MakeGrainReference_FromType(
                    baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, grainID, grainInterfaceType),
                    grainInterfaceType).AsReference<OutputGrainInterfaceType>(gf);
            //return GetGrain<OutputGrainInterfaceType>(gf, grainID, grainInterfaceType, typeof(OutputGrainInterfaceType));
        }

        /// <summary>
        /// This method extends GrainFactory by adding a new GetGrain method
        /// to it that can get the runtime type of the grain interface along
        /// with its primary key, and return the grain casted to an interface
        /// that the given grainInterfaceType extends it.
        /// 
        /// The main use-case is when you want to get a grain that its type
        /// is unknown at compile time, and also SuperOutputGrainInterfaceType
        /// is non-generic, while outputGrainInterfaceType is a generic type.
        /// </summary>
        /// <typeparam name="SuperOutputGrainInterfaceType">The output type of
        /// the grain</typeparam>
        /// <param name="gf">the grainFactory instance that this method
        /// is extending its functionality</param>
        /// <param name="grainID">the primary key of the grain</param>
        /// <param name="grainInterfaceType">the runtime type of the grain
        /// interface</param>
        /// <param name="outputGrainInterfaceType">the type of grain interface
        /// that should be returned</param>
        /// <returns></returns>
        //public static SuperOutputGrainInterfaceType GetGrain<SuperOutputGrainInterfaceType>(this IGrainFactory gf, string grainID, Type grainInterfaceType, Type outputGrainInterfaceType)
        //{
        //    return (SuperOutputGrainInterfaceType)((GrainFactory)gf).Cast(GrainFactoryBase.MakeGrainReference_FromType(
        //            baseTypeCode => TypeCodeMapper.ComposeGrainId(baseTypeCode, grainID, grainInterfaceType),
        //            grainInterfaceType), outputGrainInterfaceType);
        //}
    }
}
