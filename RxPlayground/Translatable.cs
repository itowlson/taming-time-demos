using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RxPlayground
{
    public class ProcessEvent
    {
        public string Name { get; set; }
    }

    public class Processes : IQbservable<ProcessEvent> //, IQbservableProvider
    {
        private readonly ProcessesQuery<ProcessEvent> _provider;

        public Processes()
        {
            _provider = new ProcessesQuery<ProcessEvent>(Expression);
        }

        public Type ElementType
        {
            get { return typeof(ProcessEvent); }
        }

        public Expression Expression
        {
            get { return Expression.Constant(this); }
        }

        public IQbservableProvider Provider
        {
            get { return _provider; }
        }

        public IDisposable Subscribe(IObserver<ProcessEvent> observer)
        {
            return _provider.Subscribe(observer);
            //return new ProcessesQuery<ProcessEvent>(Expression).Subscribe(observer);
        }

        //public IQbservable<TResult> CreateQuery<TResult>(Expression expression)
        //{
        //    if (typeof(TResult) != typeof(ProcessEvent))
        //    {
        //        throw new InvalidOperationException();
        //    }
        //    return (IQbservable<TResult>)(object)(new ProcessesQuery<ProcessEvent>(expression));
        //}
    }

    public class ProcessesQuery<T> : IQbservable<T>, IQbservableProvider
    {
        private readonly Expression _expression;
        private ProcessesQueryBuilder _queryBuilder;

        public ProcessesQuery(Expression expression)
        {
            _expression = expression;
        }

        public Type ElementType
        {
            get { return typeof(ProcessEvent); }
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public IQbservableProvider Provider
        {
            get { return this; }
        }

        private List<IObserver<T>> _observers = new List<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            EnsureQuerySetUp();

            _observers.Add(observer);
            return Disposable.Create(() => _observers.Remove(observer));
        }

        private void EnsureQuerySetUp()
        {
            if (_queryBuilder == null)
            {
                SetUpQuery();
            }
        }

        private void SetUpQuery()
        {
            _queryBuilder = new ProcessesQueryBuilder();
            _queryBuilder.Visit(_expression);

            var selection = "*";

            if (_queryBuilder.Selects.Count > 0)
            {
                selection = String.Join(", ", _queryBuilder.Selects
                                                           .Select(s => "TargetInstance." + s));
            }

            EventQuery q = new EventQuery();

            // TODO: also support __InstanceDeletionEvent and __InstanceModificationEvent based on
            // say a Kind flag in the ProcessEvent type and Where(pi => pi.Kind == ...) filters.

            q.QueryString = "SELECT " + selection + " FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\"";

            foreach (var where in _queryBuilder.Wheres)
            {
                q.QueryString += " AND TargetInstance." + where.Item1 + " = \"" + where.Item2.ToString() + "\"";
            }

            Console.WriteLine(q.QueryString);

            ManagementScope scope = new ManagementScope(@"\\localhost\root\cimv2");

            ManagementEventWatcher w = new ManagementEventWatcher(scope, q);
            w.Options.Timeout = TimeSpan.FromSeconds(60);
            w.Start();

            w.EventArrived += w_EventArrived;
        }

        public IQbservable<TResult> CreateQuery<TResult>(Expression expression)
        {
            return (IQbservable<TResult>)(object)(new ProcessesQuery<TResult>(expression));
        }

        void w_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var mbo = e.NewEvent;

            foreach (var o in _observers)
            {
                o.OnNext((T)(Project(mbo)));
            }
        }

        private object Project(ManagementBaseObject mbo)
        {
            var targetInstance = (ManagementBaseObject)(mbo["TargetInstance"]);

            if (typeof(T) == typeof(ProcessEvent))
            {
                return new ProcessEvent { Name = (string)(targetInstance["Name"]) };
            }
            else if (typeof(T) == typeof(string))
            {
                var propname = _queryBuilder.Selects.Single();

                return (string)(targetInstance[propname]);
            }

            throw new NotSupportedException("we don't currently handle projecting into " + typeof(T));
        }
    }


    public class ProcessesQueryBuilder : ExpressionVisitor
    {
        private readonly List<Tuple<string, object>> _where = new List<Tuple<string, object>>();
        private readonly List<string> _select = new List<string>();

        public IReadOnlyCollection<Tuple<string, object>> Wheres
        {
            get { return _where; }
        }

        public IReadOnlyCollection<string> Selects
        {
            get { return _select; }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Qbservable))
            {
                if (node.Method.Name == "Where")
                {
                    TranslateWhereExpressionToFilters(node);
                }
                if (node.Method.Name == "Select")
                {
                    TranslateSelectExpressionToProjections(node);
                }
            }
            return base.VisitMethodCall(node);
        }

        private void TranslateWhereExpressionToFilters(MethodCallExpression node)
        {
            var criterion_ = node.Arguments[1];
            if (criterion_.NodeType == ExpressionType.Quote)
            {
                criterion_ = ((UnaryExpression)criterion_).Operand;
            }

            Expression<Func<ProcessEvent, bool>> criterion = (Expression<Func<ProcessEvent, bool>>)criterion_;
            var body = criterion.Body;

            if (body.NodeType == ExpressionType.Quote)
            {
                body = ((UnaryExpression)body).Operand;
            }

            // TODO: Handle And and Or queries

            if (body.NodeType == ExpressionType.Equal)
            {
                BinaryExpression eqexpr = (BinaryExpression)(body);
                MemberExpression mem = (MemberExpression)(eqexpr.Left);
                object val = Eval(eqexpr.Right);

                _where.Add(Tuple.Create(mem.Member.Name, val));
            }
            else
            {
                throw new NotSupportedException("can't handle where clause " + node.ToString());
            }
        }

        private void TranslateSelectExpressionToProjections(MethodCallExpression node)
        {
            var projection_ = node.Arguments[1];
            if (projection_.NodeType == ExpressionType.Quote)
            {
                projection_ = ((UnaryExpression)projection_).Operand;
            }

            Expression<Func<ProcessEvent, string>> projection = (Expression<Func<ProcessEvent, string>>)projection_;  // assume string for now
            var body = projection.Body;

            if (body.NodeType == ExpressionType.Quote)
            {
                body = ((UnaryExpression)body).Operand;
            }

            MemberExpression mem = (MemberExpression)body;
            _select.Add(mem.Member.Name);
        }

        private static object Eval(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Constant)
            {
                return ((ConstantExpression)expr).Value;
            }
            else if (expr.NodeType == ExpressionType.MemberAccess)
            {
                var container = ((MemberExpression)expr).Expression;
                var containerVal = Eval(container);

                var mem = ((MemberExpression)expr).Member;
                var fld = mem as FieldInfo;
                if (fld != null)
                {
                    return fld.GetValue(containerVal);
                }
                throw new NotSupportedException("couldn't get val for " + mem);
            }

            throw new NotSupportedException("can't eval " + expr);
        }
    }
}
