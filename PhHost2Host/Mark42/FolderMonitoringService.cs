using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Mark42
{
    /// <summary>
    /// Monitors a folder (including sub-folders and files) and raises events if it created, changed, renamed or deleted.
    /// </summary>
    public class FolderMonitoringService : IService
    {
        #region Private members

        protected readonly ILogger _log;
        protected readonly string _directoryAbsolutePath;
        protected readonly string _searchPattern;
        protected readonly bool _recursive;
        protected FileSystemWatcher _directoryWatcher;

        private Dictionary<string, string> _folderHashes;

        #endregion Private members

        #region Constructor

        public FolderMonitoringService(ILogger log, string folderToMonitor, string patternToMonitor, bool recursive)
        {
            _log = log;
            _directoryAbsolutePath = Helper.GetAbsolutePath(folderToMonitor);
            _searchPattern = patternToMonitor.ToLower();
            _recursive = recursive;

            _folderHashes = new Dictionary<string, string>();

            _directoryWatcher = new FileSystemWatcher
            {
                Path = _directoryAbsolutePath,
                //Filter = _searchPattern,
                IncludeSubdirectories = _recursive,
                NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite
            };
        }

        #endregion Constructor

        #region Properties

        public string MonitoringFolder
        {
            get
            {
                return _directoryAbsolutePath;
            }
        }

        #endregion

        #region IService

        public virtual bool IsRunning
        {
            get { return _directoryWatcher.EnableRaisingEvents; }
        }

        public virtual void Start()
        {
            _directoryWatcher.Created += DirectoryWatcherOnCreated;
            _directoryWatcher.Changed += DirectoryWatcherOnChanged;
            _directoryWatcher.Renamed += DirectoryWatcherOnRenamed;
            _directoryWatcher.Deleted += DirectoryWatcherOnDeleted;
            _directoryWatcher.EnableRaisingEvents = true;

            Refresh();
        }

        public void Refresh()
        {
            var directories = Directory.EnumerateDirectories(_directoryWatcher.Path, "*.*", (_recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)).Where(f => IsMatchPattern(f));
            foreach (string directory in directories)
            {
                DirectoryWatcherOnCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(directory), Path.GetFileName(directory)));
            }
        }

        public virtual void Stop()
        {
            _directoryWatcher.EnableRaisingEvents = false;
            _directoryWatcher.Created -= DirectoryWatcherOnCreated;
            _directoryWatcher.Changed -= DirectoryWatcherOnChanged;
            _directoryWatcher.Renamed -= DirectoryWatcherOnRenamed;
            _directoryWatcher.Deleted -= DirectoryWatcherOnDeleted;
        }

        #endregion IService

        #region Events

        public event Action<string> FolderCreated = (folderPath) => { };

        public event Action<string> FolderChanged = (folderPath) => { };

        public event Action<string, string> FolderRenamed = (oldFolderPath, newFolderPath) => { };

        public event Action<string> FolderDeleted = (folderPath) => { };

        #endregion Protected methods

        #region Private methods

        private object _lock = new object();

        private void DirectoryWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
				try{
	                //if file event comes
	                if (Path.HasExtension(e.FullPath)) return;

	                _log.Trace("FolderMonitoringService: file '{0}' created.", e.Name);

	                string folderPath = e.FullPath;
	                if (IsMatchPattern(e.Name) && FolderHashIsChanged(folderPath))
	                {
	                    Thread.Sleep(500);//need to wait until files copying finishes(avoid additional 'change' event)
	                    AddFolderHash(folderPath);
	                    FolderCreated(folderPath);
	                }
				}catch(Exception ex){

					_log.Trace("[ERROR] FolderMonitoringService: file created.", ex.StackTrace);

				}
            }
        }

        private void DirectoryWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
				try{
	                //if file event comes
	                if (Path.HasExtension(e.FullPath)) return;

	                _log.Trace("FolderMonitoringService: file '{0}' changed.", e.Name);

	                string folderPath = e.FullPath;
	                if (IsMatchPattern(e.Name) && FolderHashIsChanged(folderPath))
	                {
	                    UpdateFolderHash(folderPath);
	                    FolderChanged(folderPath);
	                }
				}catch(Exception ex){

					_log.Trace("[ERROR] FolderMonitoringService: file created.", ex.StackTrace);

				}
            }
        }

        private void DirectoryWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            lock (_lock)
            {
				try{
	                //if file event comes
	                if (Path.HasExtension(e.FullPath)) return;

	                _log.Trace("FolderMonitoringService: file '{0}' renamed.", e.Name);

	                string oldFolderPath = e.OldFullPath;
	                string newFolderPath = e.FullPath;
	                if (IsMatchPattern(e.Name))
	                {
	                    RemoveFolderHash(oldFolderPath);
	                    AddFolderHash(newFolderPath);
	                    FolderRenamed(oldFolderPath, newFolderPath);
	                }
				}catch(Exception ex){

					_log.Trace("[ERROR] FolderMonitoringService: file created.", ex.StackTrace);

				}
            }
        }

        private void DirectoryWatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
				try{
	                //if file event comes
	                if (Path.HasExtension(e.FullPath)) return;

	                _log.Trace("FolderMonitoringService: file '{0}' deleted.", e.Name);

	                string folderPath = e.FullPath;
	                if (IsMatchPattern(e.Name))
	                {
	                    RemoveFolderHash(folderPath);
	                    FolderDeleted(folderPath);
	                }
				}catch(Exception ex){

					_log.Trace("[ERROR] FolderMonitoringService: file created.", ex.StackTrace);

				}
            }
        }

        private bool IsMatchPattern(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            return _searchPattern.Contains(extension);
        }

        private void AddFolderHash(string folderPath)
        {
            string hash = GenerateFolderHash(folderPath);
            _folderHashes.Add(folderPath, hash);
        }

        private void UpdateFolderHash(string folderPath)
        {
            string hash = GenerateFolderHash(folderPath);
            _folderHashes[folderPath] = hash;
        }

        private bool FolderHashIsChanged(string folderPath)
        {
            string newHash = GenerateFolderHash(folderPath);

            string oldHash;
            if (!_folderHashes.TryGetValue(folderPath, out oldHash)) return true;

            return !oldHash.Equals(newHash);
        }

        private void RemoveFolderHash(string folderPath)
        {
            if (_folderHashes.ContainsKey(folderPath))
            {
                _folderHashes.Remove(folderPath);
            }
        }

        /// <summary>
        /// Creates MD5 for folder by composing hashes of all its files that matches search pattern (excluding sub-folders).
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        private string GenerateFolderHash(string folderPath)
        {
            try
            {
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                              .Where(f => IsMatchPattern(f))
                                              .OrderBy(p => p).ToList();

                MD5 md5 = MD5.Create();

                string folderName = Path.GetFileName(folderPath);
                byte[] folderNameBytes = Encoding.UTF8.GetBytes(folderName.ToLower());
                if (files.Count != 0)
                {
                    md5.TransformBlock(folderNameBytes, 0, folderNameBytes.Length, folderNameBytes, 0);
                }
                else
                {
                    md5.TransformFinalBlock(folderNameBytes, 0, folderNameBytes.Length);
                }

                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];

                    // hash path
                    string relativePath = file.Substring(folderPath.Length + 1);
                    byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    // hash contents
                    byte[] contentBytes = File.ReadAllBytes(file);
                    if (i == files.Count - 1)
                        md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                    else
                        md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
            catch
            {
                return GenerateFolderHash(folderPath);
            }
        }

        #endregion Private methods
    }
}
