using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for IndexWorkflowQueueSystemTarget system-target.
    /// </summary>
    [Unordered]
    internal interface IIndexWorkflowQueueHandler : ISystemTarget
    {
        Task HandleWorkflowsUntilPunctuation(Immutable<IndexWorkflowRecordNode> workflowRecordsHead);
    }
}