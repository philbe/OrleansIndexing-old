using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for IndexWorkflowQueue system-target.
    /// </summary>
    internal interface IIndexWorkflowQueueHandler
    {
        Task HandleWorkflowsUntilPunctuation(IndexWorkflowRecordNode immutable);
    }
}