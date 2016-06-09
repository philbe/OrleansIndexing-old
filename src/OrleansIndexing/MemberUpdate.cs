using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansIndexing
{
    /// <summary>
    /// MemberUpdate is an generic
    /// </summary>
    public class MemberUpdate : IMemberUpdate
    {
        private object _befImg;
        private object _aftImg;
        private bool _isUpdated;

        public MemberUpdate(object befImg, object aftImg, bool isUpdated)
        {
            _isUpdated = isUpdated;
            if (_isUpdated)
            {
                _befImg = befImg;
                _aftImg = aftImg;
            }
        }

        public MemberUpdate(object befImg, object aftImg) : this(befImg, aftImg, befImg.Equals(aftImg))
        {
        }

        public object GetBefImg()
        {
            return _isUpdated ? _befImg : null;
        }

        public object GetAftImg()
        {
            return _isUpdated ? _aftImg : null;
        }

        public bool IsUpdated()
        {
            return _isUpdated;
        }
    }
}
