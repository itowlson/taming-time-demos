using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RxGui
{
    /// <summary>
    /// Interaction logic for ManualDemo.xaml
    /// </summary>
    public partial class ManualDemo : Window
    {
        private readonly ManualDemoViewModel _viewModel = new ManualDemoViewModel();

        public ManualDemo()
        {
            InitializeComponent();

            DataContext = _viewModel;

            var locations = GetLocationStreamFromChunkyGenerator();

            //var locations = GetLocationStreamFromChunkyGenerator();

            //var locations = GetLocationStreamFromFineGrainedGenerator();

            //var locations = GetAngleStream()
            //                    .Select(a => Math.PI * a / 180)
            //                    .Select(a => new Point(150 + 100 * Math.Sin(a), 150 + 100 * Math.Cos(a)));

            //var locations = GetAngleStream()
            //                    .Select(a => new Point(70 + a * 1.2, 200 + 150 * Math.Sin(Math.PI * a / 180)));

            locations.SubscribeOn(ThreadPoolScheduler.Instance)
                     //.Select(p => Point.Multiply(p, new Matrix(0.7, 1.2, 1.5, 0.9, -40, -50)))
                     .Subscribe(_viewModel.SetPosition);
        }

        private static IObservable<Point> GetLocationStreamFromExplicitCreation()
        {
            var locations = Observable.Create<Point>(observer =>
            {
                for (int i = 0; i < 10; ++i)
                {
                    observer.OnNext(new Point(100, 100));
                    Thread.Sleep(250);
                    observer.OnNext(new Point(150, 100));
                    Thread.Sleep(250);
                    observer.OnNext(new Point(150, 150));
                    Thread.Sleep(250);
                    observer.OnNext(new Point(100, 150));
                    Thread.Sleep(250);
                }
                return Disposable.Empty;
            });
            return locations;
        }

        private static IObservable<Point> GetChunkyLocationStreamFromAtoms()
        {
            var wait = Observable.Empty<Point>().Delay(TimeSpan.FromMilliseconds(250));

            var points = new[] {
                new Point(100, 100),
                new Point(150, 100),
                new Point(150, 150),
                new Point(100, 150),
            };

            var locs = points.ToObservable(ThreadPoolScheduler.Instance)
                             .Select(pt => Observable.Return(pt).Concat(wait))
                             .Concat();

            //var locs = Observable.Return(new Point(100, 100))
            //          .Concat(wait)
            //          .Concat(Observable.Return(new Point(150, 100)))
            //          .Concat(wait)
            //          .Concat(Observable.Return(new Point(150, 150)))
            //          .Concat(wait)
            //          .Concat(Observable.Return(new Point(100, 150)))
            //          .Concat(wait);

            return locs.Repeat(10);
        }

        private static IObservable<Point> GetLocationStreamFromChunkyGenerator()
        {
            return Observable.Generate<int, Point>(
                0,
                i => i < 40,
                i => i + 1,
                i =>
                {
                    switch (i % 4)
                    {
                        case 0: return new Point(100, 100);
                        case 1: return new Point(150, 100);
                        case 2: return new Point(150, 150);
                        case 3: return new Point(100, 150);
                        default: throw new ArgumentException();
                    }
                },
                i => TimeSpan.FromMilliseconds(250)
            );
        }

        private static IObservable<Point> GetLocationStreamFromFineGrainedGenerator()
        {
            return Observable.Generate<double, Point>
            (
                0,
                _ => true,
                i => (i + 1) % 360,
                i => new Point(
                    150 + 100 * Math.Sin(Math.PI * i / 180),
                    150 + 100 * Math.Cos(Math.PI * i / 180)
                    ),
                _ => TimeSpan.FromMilliseconds(10),
                ThreadPoolScheduler.Instance
            );
        }

        private static IObservable<double> GetAngleStream()
        {
            return Observable.Generate<double, double>
            (
                0,
                _ => true,
                i => (i + 1) % 360,
                i => i,
                _ => TimeSpan.FromMilliseconds(10),
                ThreadPoolScheduler.Instance
            );
        }

        private static IObservable<double> GetAngleStreamByInterval(TimeSpan interval)
        {
            return Observable.Interval(interval, ThreadPoolScheduler.Instance)
                             .Select(i => (double)(i % 360));
        }
    }

    public class ManualDemoViewModel : ViewModelBase
    {
        private double _ballLeft = 80;

        public double BallLeft
        {
            get { return _ballLeft; }
            set { Set(ref _ballLeft, value, "BallLeft"); }
        }

        private double _ballTop = 80;

        public double BallTop
        {
            get { return _ballTop; }
            set { Set(ref _ballTop, value, "BallTop"); }
        }


        public void SetPosition(Point position)
        {
            BallLeft = position.X;
            BallTop = position.Y;
        }
    }
}
