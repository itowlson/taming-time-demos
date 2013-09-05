using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxPlayground
{
    public static class Implementation
    {
        public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)
        {
            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(
                    item => { if (predicate(item)) observer.OnNext(item); }
                );
            });
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item)) yield return item;
            }
        }

        public static IObservable<U> Select<T, U>(this IObservable<T> source, Func<T, U> selector)
        {
            return Observable.Create<U>(observer =>
            {
                return source.Subscribe(
                    item => { observer.OnNext(selector(item)); }
                );
            });
        }

        public static IEnumerable<U> Select<T, U>(this IEnumerable<T> source, Func<T, U> selector)
        {
            foreach (var item in source)
            {
                yield return selector(item);
            }
        }

        public static IObservable<U> SelectMany<T, U>(this IObservable<T> source, Func<T, IObservable<U>> selector)
        {
            return Observable.Create<U>(observer =>
            {
                return source.Subscribe(item1 =>
                {
                    var expands = selector(item1);
                    expands.Subscribe(item2 =>
                    {
                        observer.OnNext(item2);
                    });
                });
            });
        }

        public static IEnumerable<U> SelectMany<T, U>(this IEnumerable<T> source, Func<T, IEnumerable<U>> selector)
        {
            foreach (var item1 in source)
            {
                var expands = selector(item1);
                foreach (var item2 in expands)
                {
                    yield return item2;
                }
            }
        }

        public static IObservable<T> Aggregate<T>(this IObservable<T> source, Func<T, T, T> accumulator)
        {
            return Observable.Create<T>(observer =>
            {
                bool started = false;
                T acc = default(T);

                return source.Subscribe(
                    onNext: item =>
                    {
                        if (!started)
                        {
                            acc = item;
                            started = true;
                        }
                        else
                        {
                            acc = accumulator(acc, item);
                        }
                    },
                    onCompleted: () =>
                    {
                        if (started)
                        {
                            observer.OnNext(acc);
                        }
                        observer.OnCompleted();
                    });
            });
        }

        public static IEnumerable<T> Aggregate<T>(this IEnumerable<T> source, Func<T, T, T> accumulator)  // stay in the monad
        {
            bool started = false;
            T acc = default(T);

            foreach (var item in source)
            {
                if (!started)
                {
                    acc = item;
                    started = true;
                }
                else
                {
                    acc = accumulator(acc, item);
                }
            }

            if (started)
            {
                yield return acc;
            }
        }

        public static IObservable<T> First<T>(this IObservable<T> source)
        {
            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(item =>
                {
                    observer.OnNext(item);
                    observer.OnCompleted();
                });
            });
        }

        public static IEnumerable<T> First<T>(this IEnumerable<T> source)  // stay in the monad
        {
            foreach (var item in source)
            {
                yield return item;
                yield break;
            }
        }

        // Given a small set of primitives (SelectMany, Return, Empty) you can build up
        // a lot of operators using the same code regardless of whether you're dealing
        // with Observables or Enumerables.

        public static IObservable<T> Where_SM<T>(this IObservable<T> source, Func<T, bool> predicate)
        {
            return source.SelectMany(item => predicate(item) ? Observable.Return(item) : Observable.Empty<T>());
        }

        public static IEnumerable<T> Where_SM<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return source.SelectMany(item => predicate(item) ? EnumerableEx.Return(item) : Enumerable.Empty<T>());
        }

        public static IObservable<U> Select_SM<T, U>(this IObservable<T> source, Func<T, U> selector)
        {
            return source.SelectMany(item => Observable.Return(selector(item)));
        }

        public static IEnumerable<U> Select_SM<T, U>(this IEnumerable<T> source, Func<T, U> selector)
        {
            return source.SelectMany(item => EnumerableEx.Return(selector(item)));
        }

        public static IObservable<T> Concat_SM<T>(this IObservable<IObservable<T>> source)
        {
            return source.SelectMany(item => item);
        }

        public static IEnumerable<T> Concat_SM<T>(this IEnumerable<IEnumerable<T>> source)
        {
            return source.SelectMany(item => item);
        }
    }
}
