using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// MemberUpdate is a generic implementation of IMemberUpdate
    /// that relies on a copy of beforeImage and afterImage, without
    /// keeping any semantic information about the actual change that
    /// happened.
    /// This class assumes that befImg and aftImg passed to it won't be
    /// altered later on, so they are immutable.
    /// </summary>
    public class MemberUpdate : IMemberUpdate
    {
        private Immutable<object> _befImg;
        private Immutable<object> _aftImg;
        private Immutable<OperationType> _opType;

        public MemberUpdate(object befImg, object aftImg, OperationType opType)
        {
            _opType = opType.AsImmutable();
            if (opType == OperationType.Update || opType == OperationType.Delete)
            {
                _befImg = befImg.AsImmutable();
            }
            if (opType == OperationType.Update || opType == OperationType.Insert)
            {
                _aftImg = aftImg.AsImmutable();
            }
        }

        public MemberUpdate(object befImg, object aftImg) : this(befImg, aftImg, GetOperationType(befImg, aftImg))
        {
        }

        private static OperationType GetOperationType(object befImg, object aftImg)
        {
            if(befImg == null)
            {
                if (aftImg == null) return OperationType.None;
                else return OperationType.Insert;
            }
            else
            {
                if (aftImg == null) return OperationType.Delete;
                else if(befImg.Equals(aftImg)) return OperationType.None;
                else return OperationType.Update;
            }
        }

        /// <summary>
        /// Exposes the stored before-image.
        /// </summary>
        /// <returns>the before-image of the indexed attribute(s)
        /// that is before applying the current update</returns>
        public object GetBeforeImage()
        {
            var opType = _opType.Value;
            return (opType == OperationType.Update || opType == OperationType.Delete) ? _befImg.Value : null;
        }

        public object GetAfterImage()
        {
            var opType = _opType.Value;
            return (opType == OperationType.Update || opType == OperationType.Insert) ? _aftImg.Value : null;
        }

        public OperationType GetOperationType()
        {
            return _opType.Value;
        }
    }
}
