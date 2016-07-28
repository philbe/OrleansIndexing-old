﻿using Orleans.Concurrency;
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
    [Serializable]
    public class MemberUpdate : IMemberUpdate
    {
        private Immutable<object> _befImg;
        private Immutable<object> _aftImg;
        private Immutable<IndexOperationType> _opType;

        public MemberUpdate(object befImg, object aftImg, IndexOperationType opType)
        {
            _opType = opType.AsImmutable();
            if (opType == IndexOperationType.Update || opType == IndexOperationType.Delete)
            {
                _befImg = befImg.AsImmutable();
            }
            if (opType == IndexOperationType.Update || opType == IndexOperationType.Insert)
            {
                _aftImg = aftImg.AsImmutable();
            }
        }

        public MemberUpdate(object befImg, object aftImg) : this(befImg, aftImg, GetOperationType(befImg, aftImg))
        {
        }

        private static IndexOperationType GetOperationType(object befImg, object aftImg)
        {
            if(befImg == null)
            {
                if (aftImg == null) return IndexOperationType.None;
                else return IndexOperationType.Insert;
            }
            else
            {
                if (aftImg == null) return IndexOperationType.Delete;
                else if(befImg.Equals(aftImg)) return IndexOperationType.None;
                else return IndexOperationType.Update;
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
            return (opType == IndexOperationType.Update || opType == IndexOperationType.Delete) ? _befImg.Value : null;
        }

        public object GetAfterImage()
        {
            var opType = _opType.Value;
            return (opType == IndexOperationType.Update || opType == IndexOperationType.Insert) ? _aftImg.Value : null;
        }

        public IndexOperationType GetOperationType()
        {
            return _opType.Value;
        }

        public IMemberUpdate Merge(params IMemberUpdate[] updates)
        {
            var len = updates.Length;
            if (len == 0) return null;
            //IndexOperationType combinedOperationType = updates[0].GetOperationType();
            //for(int i = 1; i < len; ++i)
            //{
            //    combinedOperationType = combinedOperationType.CombineWith(updates[i].GetOperationType());
            //}
            return new MemberUpdate(updates[0].GetBeforeImage(), updates[len-1].GetAfterImage()/*, combinedOperationType*/);
        }
    }
}
