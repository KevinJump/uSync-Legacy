using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.BackOffice
{
    /// <summary>
    ///  Watches the uSync folder for changes, when some happen
    ///  sets a time for 8 seconds later, if no more changes 
    ///  happen, then that triggers an import. 
    /// 
    ///  allows you to do changes on the fly using uSync
    /// </summary>
    public class uSyncFileWatcher
    {
        private static FileSystemWatcher watcher;
        private static System.Timers.Timer _waitTimer;
        private static int _lockCount = 0;
        private static object _watcherLock = new object();

        public static void Init(string path)
        {
            LogHelper.Info<uSyncFileWatcher>("uSync is watching for file changes : {0}", () => path);

            watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastAccess |
                                NotifyFilters.LastWrite |
                                NotifyFilters.FileName |
                                NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                Filter = "*.config"
            };

            watcher.Changed += new FileSystemEventHandler(FileWatcherChangeEvent);
            watcher.Created += new FileSystemEventHandler(FileWatcherChangeEvent);
            watcher.Deleted += new FileSystemEventHandler(FileWatcherChangeEvent);
            watcher.Renamed += new RenamedEventHandler(FileWatcherRenameEvent);

            _waitTimer = new System.Timers.Timer(8128); // wait a perfect amount of time
            _waitTimer.Elapsed += ChangeTimerElapsed;
        }

        private static void ChangeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock(_watcherLock)
            {
                Pause();

                Stopwatch sw = new Stopwatch();
                sw.Start();

                uSyncEvents.Paused = true;

                LogHelper.Info<uSyncFileWatcher>("FileChanges - starting import");
                uSyncBackOfficeContext.Instance.ImportAll();

                uSyncEvents.Paused = false; 

                sw.Stop();
                LogHelper.Info<uSyncFileWatcher>("FileChanges - Import Complete: {0}ms", ()=> sw.ElapsedMilliseconds);
                Start();
            }
            
        }

        private static void Pause()
        {
            if (watcher != null)
            {
                Interlocked.Increment(ref _lockCount);
                LogHelper.Debug<uSyncFileWatcher>("Watcher Lock: {0}", () => _lockCount);

                if (watcher.EnableRaisingEvents)
                {
                    LogHelper.Debug<uSyncFileWatcher>("Pause");
                    watcher.EnableRaisingEvents = false; 
                }
            }
        }

        public static void Start()
        {
            if (watcher != null)
            {
                if (_lockCount > 0)
                {
                    Interlocked.Decrement(ref _lockCount);
                }

                LogHelper.Debug<uSyncFileWatcher>("Watcher Lock: {0}", () => _lockCount);

                if (_lockCount <= 0)
                {
                    LogHelper.Debug<uSyncFileWatcher>("Start");
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        private static void FileWatcherRenameEvent(object sender, RenamedEventArgs e)
        {
            LogHelper.Info<uSyncFileWatcher>("Rename Detected - but we don't do anything with this yet.");
        }

        private static void FileWatcherChangeEvent(object sender, FileSystemEventArgs e)
        {
            LogHelper.Info<uSyncFileWatcher>("File Change Detected: {0} {1}", () => e.ChangeType.ToString(), () => e.FullPath);

            if (_waitTimer != null)
            {
                _waitTimer.Stop();
                _waitTimer.Start();
            }
        }
    }
}
