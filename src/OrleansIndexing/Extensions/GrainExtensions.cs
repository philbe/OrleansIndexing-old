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
        /// <summary>
        /// Converts this grain to a specific grain interface.
        /// </summary>
        /// <typeparam name="TGrainInterface">The type of the grain interface.</typeparam>
        /// <param name="grain">The grain to convert.</param>
        /// <returns>A strongly typed <c>GrainReference</c> of grain interface type TGrainInterface.</returns>
        public static TGrainInterface AsReference<TGrainInterface>(this IAddressable grain, IGrainFactory gf)
        {
            if (grain == null)
            {
                throw new ArgumentNullException("grain", "Cannot pass null as an argument to AsReference");
            }

            return ((GrainFactory)gf).Cast<TGrainInterface>(grain.AsWeaklyTypedReference());
        }

        private const string WRONG_GRAIN_ERROR_MSG = "Passing a half baked grain as an argument. It is possible that you instantiated a grain class explicitly, as a regular object and not via Orleans runtime or via proper test mocking";

        internal static GrainReference AsWeaklyTypedReference(this IAddressable grain)
        {
            var reference = grain as GrainReference;
            // When called against an instance of a grain reference class, do nothing
            if (reference != null) return reference;

            var grainBase = grain as Grain;
            if (grainBase != null)
            {
                if (grainBase.Data == null || grainBase.Data.GrainReference == null)
                {
                    throw new ArgumentException(WRONG_GRAIN_ERROR_MSG, "grain");
                }
                return grainBase.Data.GrainReference;
            }

            var systemTarget = grain as ISystemTargetBase;
            if (systemTarget != null)
                return GrainReference.FromGrainId(systemTarget.GrainId, null, systemTarget.Silo);

            throw new ArgumentException(String.Format("AsWeaklyTypedReference has been called on an unexpected type: {0}.", grain.GetType().FullName), "grain");
        }
    }
}
