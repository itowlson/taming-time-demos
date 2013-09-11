using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
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
    /// Interaction logic for FileSystemDemo.xaml
    /// </summary>
    public partial class FileSystemDemo : Window
    {
        private readonly FileSystemDemoViewModel _viewModel = new FileSystemDemoViewModel();

        public FileSystemDemo()
        {
            InitializeComponent();

            DataContext = _viewModel;

            var tempFolder = "c:\\temp2";
            IObservable<FileSystemEventArgs> tempFolderEvents =
                FileSystem.WhatsHappeningIn(tempFolder);

            var myFileCreationEvents = tempFolderEvents
                .Where(f => f.ChangeType == WatcherChangeTypes.Created)
                .Select(f => new { f.ChangeType, f.Name })
                .Where(c => c.Name.StartsWith("my"));




            tempFolderEvents
                .Select(e => e.ChangeType + ": " + e.Name)
                .Subscribe(s => { _viewModel.LastChange = s; });

            //tempFolderEvents
            //    .Select(e => e.ChangeType + ": " + e.Name)
            //    .Subscribe(s => _viewModel.Events.Add(s));

            //tempFolderEvents
            //    .Select(e => e.ChangeType + ": " + e.Name)
            //    .Subscribe(s => fileSystemEventList.Items.Add(s));
        }
    }

    public class FileSystemDemoViewModel : ViewModelBase
    {
        private readonly ObservableCollection<string> _events = new ObservableCollection<string>();

        public ICollection<string> Events
        {
            get { return _events; }
        }

        private string _lastChange;

        public string LastChange
        {
            get { return _lastChange; }
            set { Set(ref _lastChange, value, "LastChange"); }
        }
    }
}
