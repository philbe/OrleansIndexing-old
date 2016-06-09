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
        private Immutable<bool> _isUpdated;

        public MemberUpdate(object befImg, object aftImg, bool isUpdated)
        {
            _isUpdated = isUpdated.AsImmutable();
            if (isUpdated)
            {
                _befImg = befImg.AsImmutable();
                _aftImg = aftImg.AsImmutable();
            }
        }

        public MemberUpdate(object befImg, object aftImg) : this(befImg, aftImg, befImg.Equals(aftImg))
        {
        }

        /// <summary>
        /// Exposes the stored before-image.
        /// </summary>
        /// <returns>the before-image of the indexed attribute(s)
        /// that is before applying the current update</returns>
        public object GetBefImg()
        {
            return _isUpdated.Value ? _befImg.Value : null;
        }

        public object GetAftImg()
        {
            return _isUpdated.Value ? _aftImg.Value : null;
        }

        public bool IsUpdated()
        {
            return _isUpdated.Value;
        }
    }
}
