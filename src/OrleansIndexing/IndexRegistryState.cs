﻿using System.Collections.Generic;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class stores the list of indexes for a given grain type.
    /// </summary>
    public class IndexRegistryState
    {
        public IDictionary<string, IIndex> indexes { set; get; }
    }
}