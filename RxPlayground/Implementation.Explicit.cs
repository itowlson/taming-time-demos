using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxPlayground
{
    public static class Implementation_Explicit
    {
        public static IObservable<T> Where_Explicit<T>(this IObservable<T> source, Func<T, bool> predicate)
        {
            return new WhereObservable<T>(source, predicate);
        }

        private class WhereObservable<T> : IObservable<T>
        {
            private readonly IObservable<T> _source;
            private readonly Func<T, bool> _predicate;

            public WhereObservable(IObservable<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var whereObserver = new WhereObserver<T>(observer, _predicate);
                return _source.Subscribe(whereObserver);
            }
        }

        // An observer which simply passes events on to an inner observer, with filtering

        private class WhereObserver<T> : IObserver<T>
        {
            private readonly IObserver<T> _inner;
            private readonly Func<T, bool> _predicate;

            public WhereObserver(IObserver<T> inner, Func<T, bool> predicate)
            {
                _inner = inner;
                _predicate = predicate;
            }

            public void OnCompleted()
            {
                _inner.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _inner.OnError(error);
            }

            public void OnNext(T value)
            {
                if (_predicate(value))
                {
                    _inner.OnNext(value);
                }
            }
        }


    }
}
