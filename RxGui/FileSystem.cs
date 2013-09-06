using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxGui
{
    public static class FileSystem
    {
        public static IObservable<FileSystemEventArgs> WhatsHappeningIn(string path)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = path;

            var changes = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(a => new FileSystemEventHandler((o, e) => a(e)), h => watcher.Changed += h, h => watcher.Changed -= h);
            var creations = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(a => new FileSystemEventHandler((o, e) => a(e)), h => watcher.Created += h, h => watcher.Created -= h);
            var deletions = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(a => new FileSystemEventHandler((o, e) => a(e)), h => watcher.Deleted += h, h => watcher.Deleted -= h);
            var renamings = Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(a => new RenamedEventHandler((o, e) => a(e)), h => watcher.Renamed += h, h => watcher.Renamed -= h);
            var events = changes.Merge(creations).Merge(deletions).Merge(renamings);

            watcher.EnableRaisingEvents = true;

            return events;
        }

    }
}
