using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncAndAsync
{
    public static class Streams
    {
        static IEnumerable<int> xs = new[] { 1, 2, 3, 4, 5 };
        static Func<int, string> f = n => n.ToString();
        static Func<int, Task<string>> fAsync = n => Task.FromResult(n.ToString());
        static Func<int, bool> cond = n => n % 2 == 0;
        static Func<string, bool> cond2 = s => s.Length > 4;

        public static void Sync()
        {
            var ys = xs.Select(x => f(x));
            var zs = xs.Where(x => cond(x));
        }

        public static void Async<T, U>()
        {
            // ???

            // var ts = xs.Select(async x => await fAsync(x));  // NO! NO! NO! (pulls from synchronous stream, rather than being pushed by asynchronous stream)
        }

        public static void Sync2()
        {
            var ys = xs.Where(x => cond(x))
                       .Select(x => f(x))
                       .Where(y => cond2(y));
        }

        public static async void /* <-- ??? */ Async2a_AsynchronousGenerator()
        {
            // How would we call this?  What would it return?

            while (true)
            {
                var x = await GetNextX();
                if (cond(x))
                {
                    var y = f(x);
                    if (cond2(y))
                    {
                        // push y somehow
                    }
                }
                // somehow break if no more data can be expected
            }
        }

        public static void Async2b_Events()
        {
            MyDataSource src = null;

            src.DataAvailable += (o, e) =>
                {
                    if (cond(e.Data))
                    {
                        var y = f(e.Data);
                        if (cond2(y))
                        {
                            RaiseTransformedDataAvailable(y);
                        }
                    }
                };

            // now consumers interested in the filtered, transformed data attach
            // to Streams.TransformedDataAvailable instead of MyDataSource.DataAvailable

            // Notice that we can't return the event itself because events aren't first
            // class citizens.
        }

        private static void RaiseTransformedDataAvailable(string transformedData)
        {
            TransformedDataAvailable(null, new TransformedDataEventArgs { Data = transformedData });
        }

        public static event EventHandler<TransformedDataEventArgs> TransformedDataAvailable;

        public class TransformedDataEventArgs : EventArgs
        {
            public string Data { get; set; }
        }

        private class MyDataSource
        {
            public event EventHandler<MyDataEventArgs> DataAvailable;
        }

        private class MyDataEventArgs : EventArgs
        {
            public int Data { get; set; }
        }

        private static int _i = 0;

        public static Task<int> GetNextX()
        {
            return Task.FromResult(++_i);
        }
    }






    public interface IAsyncStream_UsingEvents<T>
    {
        event Action<T> DataAvailable;
    }

    public static class AsyncStream_UsingEvents
    {
        public static IAsyncStream_UsingEvents<T> Where<T>(this IAsyncStream_UsingEvents<T> source, Func<T, bool> predicate)
        {
            return new FilteringAsyncStream_UsingEvents<T>(source, predicate);
        }

        private class FilteringAsyncStream_UsingEvents<T> : IAsyncStream_UsingEvents<T>
        {
            private readonly IAsyncStream_UsingEvents<T> _source;
            private readonly Func<T, bool> _predicate;

            public FilteringAsyncStream_UsingEvents(IAsyncStream_UsingEvents<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;

                _source.DataAvailable += item => { if (_predicate(item)) DataAvailable(item); };
            }

            public event Action<T> DataAvailable;
        }
    }

    public static class Test_AsyncStream_UsingEvent
    {
        public static void PrintEvens()
        {
            var ints = new AllIntsStream();
            var evens = ints.Where(i => i % 2 == 0);
            evens.DataAvailable += i => Console.WriteLine(i);

            ints.Start();
        }

        private class AllIntsStream : IAsyncStream_UsingEvents<int>
        {
            private int _current = 0;

            public void Start()
            {
                while (true)
                {
                    ++_current;
                    DataAvailable(_current);
                }
            }

            public event Action<int> DataAvailable;
        }
    }



    public interface IAsyncStream_UsingSubscriptions<T>
    {
        void Subscribe(Action<T> subscriber);
    }

    public static class AsyncStream_UsingSubscriptions
    {
        public static IAsyncStream_UsingSubscriptions<T> Where<T>(this IAsyncStream_UsingSubscriptions<T> source, Func<T, bool> predicate)
        {
            return new FilteringAsyncStream_UsingSubscriptions<T>(source, predicate);
        }

        public static IAsyncStream_UsingSubscriptions<U> Select<T, U>(this IAsyncStream_UsingSubscriptions<T> source, Func<T, U> selector)
        {
            return new MappingAsyncStream_UsingSubscriptions<T, U>(source, selector);
        }

        private class FilteringAsyncStream_UsingSubscriptions<T> : IAsyncStream_UsingSubscriptions<T>
        {
            private readonly IAsyncStream_UsingSubscriptions<T> _source;
            private readonly Func<T, bool> _predicate;

            public FilteringAsyncStream_UsingSubscriptions(IAsyncStream_UsingSubscriptions<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public void Subscribe(Action<T> subscriber)
            {
                _source.Subscribe(item =>
                    {
                        if (_predicate(item))
                        {
                            subscriber(item);
                        }
                    });
            }
        }

        private class MappingAsyncStream_UsingSubscriptions<T, U> : IAsyncStream_UsingSubscriptions<U>
        {
            private readonly IAsyncStream_UsingSubscriptions<T> _source;
            private readonly Func<T, U> _selector;

            public MappingAsyncStream_UsingSubscriptions(IAsyncStream_UsingSubscriptions<T> source, Func<T, U> selector)
            {
                _source = source;
                _selector = selector;
            }

            public void Subscribe(Action<U> subscriber)
            {
                _source.Subscribe(item =>
                {
                    subscriber(_selector(item));
                });
            }
        }
    }

    public static class Test_AsyncStream_UsingSubscriptions
    {
        static Func<int, string> f = n => n.ToString();
        static Func<int, bool> cond = n => n % 2 == 0;
        static Func<string, bool> cond2 = s => s.Length > 4;

        public static void PrintEvens()
        {
            var ints = new AllIntsStream();
            var evens = ints.Where(i => i % 2 == 0);
            evens.Subscribe(i => Console.WriteLine(i));
        }

        public static void PrintSquaredEvens()
        {
            var ints = new AllIntsStream();
            var evensSquared = ints.Where(i => i % 2 == 0)
                                   .Select(i => i * i);
            evensSquared.Subscribe(i => Console.WriteLine(i));
        }

        public static void ForComparison()
        {
            var xs = new AllIntsStream();
            var ys = xs.Where(x => cond(x))
                       .Select(x => f(x))
                       .Where(y => cond2(y));

            ys.Subscribe(y => Console.WriteLine(y));
        }

        private class AllIntsStream : IAsyncStream_UsingSubscriptions<int>
        {
            private int _current = 0;

            public void Subscribe(Action<int> subscriber)
            {
                while (true)
                {
                    ++_current;
                    subscriber(_current);
                }
            }
        }
    }


}
