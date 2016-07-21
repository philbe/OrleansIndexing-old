using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class casts IOrleansQueryResult{FromTP} to IOrleansQueryResult{ToTP}.
    /// 
    /// As IOrleansQueryResult{T} cannot be a covariant type (because it extends IAsyncObservable),
    /// this class is required when a conversion between two IOrleansQueryResult types is required.
    /// 
    /// It is not possible to subscribe to an instance of this class directly.
    /// One should use the original IOrleansQueryResult{FromTP} for subscription.
    /// </summary>
    /// <typeparam name="FromTP">type of grain for input IOrleansQueryResult</typeparam>
    /// <typeparam name="ToTP">type of grain for output IOrleansQueryResult</typeparam>

    [Serializable]
    public class OrleansQueryResultCaster<FromTP, ToTP> : IOrleansQueryResult<ToTP> where FromTP : IIndexableGrain where ToTP : IIndexableGrain
    {
        // List of observers
        //private IList<IObserver<T>> _queryResultObservers;

        //Currently, the whole result is stored here, but it is
        //just a simple implementation. This implementation should
        //be replaced with a more sophisticated approach to asynchronously
        //read the results on demand

        protected IOrleansQueryResult<FromTP> _stream;

        //public OrleansQueryResult()
        //{
        //    //_stream = stream;
        //    throw new NotImplementedException();
        //}

        // Accept a queryResult instance which we shall observe
        public OrleansQueryResultCaster(IOrleansQueryResult<FromTP> stream)
        {
            _stream = stream;
        }

        public IOrleansQueryResult<TOGrain> Cast<TOGrain>() where TOGrain : IIndexableGrain
        {
            if (typeof(TOGrain) == typeof(FromTP)) return (IOrleansQueryResult<TOGrain>)_stream;
            return new OrleansQueryResultCaster<FromTP, TOGrain>(_stream);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        //public async Task<TIGrain> GetFirst()
        //{
        //    var taskCompletionSource = new TaskCompletionSource<TIGrain>();
        //    Task<TIGrain> tsk = taskCompletionSource.Task;
        //    Action<TIGrain> responseHandler = taskCompletionSource.SetResult;
        //    await _stream.SubscribeAsync(new QueryFirstResultObserver<TIGrain>(responseHandler));
        //    return await tsk;
        //}

        public Task OnCompletedAsync()
        {
            return _stream.OnCompletedAsync();
        }

        public Task OnErrorAsync(Exception ex)
        {
            return _stream.OnErrorAsync(ex);
        }

        public virtual Task OnNextAsync(ToTP item, StreamSequenceToken token = null)
        {
            return _stream.OnNextAsync(item.AsReference<FromTP>(), token);
        }

        public virtual Task OnNextBatchAsync(IEnumerable<ToTP> batch, StreamSequenceToken token = null)
        {
            return Task.WhenAll(batch.Select(item => (_stream.OnNextAsync(item.AsReference<FromTP>(), token))));
            //TODO: replace with the code below, as soon as stream.OnNextBatchAsync is supported.
            //return _stream.OnNextBatchAsync(batch.Select(x => x.AsReference<FromTP>), token); //not supported yet!
        }

        public Task<StreamSubscriptionHandle<ToTP>> SubscribeAsync(IAsyncObserver<ToTP> observer)
        {
            throw new NotSupportedException();
        }

        public Task<StreamSubscriptionHandle<ToTP>> SubscribeAsync(IAsyncObserver<ToTP> observer, StreamSequenceToken token, StreamFilterPredicate filterFunc = null, object filterData = null)
        {
            throw new NotSupportedException();
        }
    }
}
