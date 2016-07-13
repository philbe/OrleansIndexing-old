using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class encapsulates the result of a query.
    /// </summary>
    /// <typeparam name="TIGrain"></typeparam>
    [Serializable]
    public class OrleansQueryResult<TIGrain> : IOrleansQueryResult<TIGrain> where TIGrain : IIndexableGrain
    {
        // List of observers
        //private IList<IObserver<T>> _queryResultObservers;

        //Currently, the whole result is stored here, but it is
        //just a simple implementation. This implementation should
        //be replaced with a more sophisticated approach to asynchronously
        //read the results on demand
        protected IEnumerable<TIGrain> _queryResult;

        // Accept a queryResult instance which we shall observe
        public OrleansQueryResult(IEnumerable<TIGrain> queryResult)
        {
            //_queryResultObservers = new List<IObserver<T>>();
            _queryResult = queryResult;

        }
        public OrleansQueryResult(IOrleansQueryResult<IIndexableGrain> queryResult)
        {
            _queryResult = new List<TIGrain>();
            queryResult.Subscribe(new QueryResultObserver<IIndexableGrain, TIGrain>((IList<TIGrain>)_queryResult));

        }
        public OrleansQueryResult(IList<IOrleansQueryResult<TIGrain>> queryResults)
        {
            _queryResult = new List<TIGrain>();
            foreach (IOrleansQueryResult<TIGrain> res in queryResults)
            {
                res.Subscribe(new QueryResultObserver<TIGrain, TIGrain>((IList<TIGrain>)_queryResult));
            }

        }

        // This method allows the observers to attach themselves. It returns a disposer object to the observer
        // which the observer can utilize to unsubscribe
        public IDisposable Subscribe(IObserver<TIGrain> observer)
        {
            //if (!_queryResultObservers.Contains(observer))
            //{
            //    _queryResultObservers.Add(observer);
                inspectQueryResult(observer);
            //}

            return new Disposer(/*_queryResultObservers, */observer);
        }

        // This method is used to inspect the query result.
        public void inspectQueryResult(IObserver<TIGrain> observer)
        {
            foreach (TIGrain elem in _queryResult)
            {
                observer.OnNext(elem);
            }
        }

        public void Dispose()
        {
            //_queryResultObservers.Clear();
            //_queryResultObservers = null;
            _queryResult = null;
        }

        //
        // Private class Disposer: Implements the IDisposable. Observable returns an instance to the observer for easy unsubscription
        //
        private class Disposer : IDisposable
        {
            // The observers list received from the observable
            //private IList<IObserver<T>> _queryResultObservers;
            // The observer instance to unsubscribe
            //private IObserver<T> _observer;

            public Disposer(/*IList<IObserver<T>> _subObservers,*/ IObserver<TIGrain> observer)
            {
                //_queryResultObservers = _subObservers;
                //_observer = observer;
            }

            public void Dispose()
            {
                //if (_queryResultObservers.Contains(_observer))
                //{
                //    _queryResultObservers.Remove(_observer);
                //}
            }
        }
    }

    internal class QueryResultObserver<TIn, TOut> : IObserver<TIn> where TIn : IAddressable
    {
        private IList<TOut> _queryResult;

        public QueryResultObserver(IList<TOut> _queryResult)
        {
            this._queryResult = _queryResult;
        }

        public void OnCompleted()
        {
            //throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(TIn value)
        {
            //throw new NotImplementedException();
            _queryResult.Add(value.AsReference<TOut>());
        }
    }
}
