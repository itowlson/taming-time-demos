using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxPlayground
{
    public class ValueOrError<T>
    {
        public enum Kind { Value, Error };

        public Kind Kind { get; set; }

        public T Value { get; set; }
        public Exception Error { get; set; }
    }

    public class ValueOrEndOrError<T>
    {
        public enum Kind { Value, End, Error };

        public Kind Kind { get; set; }

        public T Value { get; set; }
        public Exception Error { get; set; }
    }

    // Enumerable ---------------------------------------------------

    public interface MyEnumerable<T>
    {
        MyEnumerator1<T> GetEnumerator();
    }

    public interface MyEnumerator1<T>
    {
        bool MoveNext();
        T GetCurrent();  // throws
    }

    // ...so GetCurrent() can return either T or Exception
    public interface MyEnumerator2<T>
    {
        bool MoveNext();
        ValueOrError<T> GetCurrent();
    }

    // ...so MoveNext()+GetCurrent() can return either T or 'finished' or Exception
    public interface MyEnumerator3<T>
    {
        ValueOrEndOrError<T> Next();
    }

    // Reversing the arrows ---------------------------------------------------

    public interface AntiEnumerator1<T>
    {
        // ValueOrEndOrError<T> Next(void);
        void AntiNext(ValueOrEndOrError<T> item);
    }

    // ...but can split the three cases apart into separate methods
    public interface AntiEnumerator2<T>
    {
        void AntiNext_Value(T item);
        void AntiNext_End();
        void AntiNext_Error(Exception ex);
    }

    public interface AntiEnumerable<T>
    {
        // MyEnumerator<T> GetEnumerator(void)
        void AntiGetEnumerator(AntiEnumerator2<T> antiEnumerator);
    }

    // Renaming ------------------------------------------------------------

    public interface MyObserver<T>
    {
        void OnValue(T item);
        void OnEnd();
        void OnError(Exception ex);
    }

    public interface MyObservable<T>
    {
        void SetObserver(MyObserver<T> observer);
    }
}
