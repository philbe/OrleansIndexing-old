using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for IndexWorkflowQueue system-target.
    /// </summary>
    [Unordered]
    internal interface IIndexWorkflowQueueHandler : ISystemTarget
    {
        Task<bool> HandleWorkflowsUntilPunctuation(Immutable<IndexWorkflowRecordNode> immutable);
    }
}