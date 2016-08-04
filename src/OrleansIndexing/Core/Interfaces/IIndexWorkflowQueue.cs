using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for IndexWorkflowQueue system-target.
    /// </summary>
    [Unordered]
    internal interface IIndexWorkflowQueue : ISystemTarget
    {
        /// <summary>
        /// Adds a workflowRecord, created by an indexable grain, to the queue
        /// </summary>
        Task AddToQueue(Immutable<IndexWorkflowRecord> workflowRecord);

        /// <summary>
        /// If there is more work to do, hands it to the queue handler,
        /// otherwise sets the status of queue handler as idle.
        /// </summary>
        Task<Immutable<IndexWorkflowRecordNode>> GiveMoreWorkflowsOrSetAsIdle();
    }
}