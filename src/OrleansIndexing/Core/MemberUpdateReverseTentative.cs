using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is a wrapper around another IMemberUpdate, which reverses
    /// the operation in the actual update
    /// </summary>
    public class MemberUpdateReverseTentative : IMemberUpdate
    {
        private IMemberUpdate _update;
        public MemberUpdateReverseTentative(IMemberUpdate update)
        {
            _update = update;
        }
        public object GetBeforeImage()
        {
            return _update.GetAfterImage();
        }

        public object GetAfterImage()
        {
            return _update.GetBeforeImage();
        }

        public IndexOperationType GetOperationType()
        {
            IndexOperationType op = _update.GetOperationType();
            switch (op)
            {
                case IndexOperationType.Delete: return IndexOperationType.Insert;
                case IndexOperationType.Insert: return IndexOperationType.Delete;
                default: return op;
            }
        }
    }
}
