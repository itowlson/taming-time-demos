using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
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
    /// Interaction logic for MouseDemo.xaml
    /// </summary>
    public partial class MouseDemo : Window
    {
        private readonly MouseDemoViewModel _viewModel = new MouseDemoViewModel();

        public MouseDemo()
        {
            InitializeComponent();

            DataContext = _viewModel;

            var mouseMoves = Observable.FromEvent<MouseEventHandler, MouseEventArgs>(a => new MouseEventHandler((o, e) => a(e)), h => MouseMove += h, h => MouseMove -= h);
            var mouseDowns = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(a => new MouseButtonEventHandler((o, e) => a(e)), h => MouseDown += h, h => MouseDown -= h);
            var mouseUps = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(a => new MouseButtonEventHandler((o, e) => a(e)), h => MouseUp += h, h => MouseUp -= h);

            //var mouseMoves = Observable.FromEventPattern(this, "MouseMove")
            //                           .Select(e => e.EventArgs)
            //                           .Cast<MouseEventArgs>();

            mouseMoves.Select(e => e.GetPosition(this))
                      .Subscribe(_viewModel.SetPosition);

            //var mouseMovesWhileMouseDown = from d in mouseDowns
            //                               from m in mouseMoves.TakeUntil(mouseUps)
            //                               select m.GetPosition(this);

            //mouseMovesWhileMouseDown.Subscribe(_viewModel.SetPosition);
        }
    }

    public class MouseDemoViewModel : ViewModelBase
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
