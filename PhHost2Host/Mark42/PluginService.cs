using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mark42
{
    public class PluginService<TPlugin> : LoopService
        where TPlugin : class
    {
        #region Private members
        private FolderMonitoringService _folderMonitoringService;
        private Dictionary<string, List<TPlugin>> _plugins = new Dictionary<string, List<TPlugin>>();
        private ConcurrentQueue<Action> _fileEventsQueue = new ConcurrentQueue<Action>();
        #endregion

        #region Constructor

        public PluginService(string pluginsFolder)
            : this(pluginsFolder, "*.dll")
        {
        }

        public PluginService(string pluginsFolder, string searchPattern)
            : this(pluginsFolder, searchPattern, false)
        {
        }

        public PluginService(string pluginsFolder, string searchPattern, bool recursive)
            : this(new StubLogger(), pluginsFolder, searchPattern, recursive)
        {
        }

        public PluginService(ILogger log, string pluginsFolder, string searchPattern, bool recursive)
            : base(log, "PluginServiceThread", 100)
        {
            _folderMonitoringService = new FolderMonitoringService(_log, pluginsFolder, searchPattern, recursive);
            _folderMonitoringService.FolderCreated += FolderCreated;
            _folderMonitoringService.FolderChanged += FolderChanged;
            _folderMonitoringService.FolderRenamed += FolderRenamed;
            _folderMonitoringService.FolderDeleted += FolderDeleted;
        }

        #endregion

        #region Properties

        public string PluginsFolder
        {
            get
            {
                return _folderMonitoringService.MonitoringFolder;
            }
        }

        public List<TPlugin> Plugins
        {
            get
            {
                List<TPlugin> result = new List<TPlugin>();
                _plugins.Values.ToList().ForEach(i => result.AddRange(i));
                return result;
            }
        }

        #endregion

        #region Events
        public event Action<PluginService<TPlugin>, List<TPlugin>> PluginsAdded = (sender, plugins) => { };
        public event Action<PluginService<TPlugin>, List<TPlugin>, List<TPlugin>> PluginsChanged = (sender, oldPlugins, newPlugins) => { };
        public event Action<PluginService<TPlugin>, List<TPlugin>> PluginsRemoved = (sender, plugins) => { };
        #endregion

        #region Virtual methods

        /// <summary>
        /// Override this method in case need to load plugins with parameterized constructor.
        /// </summary>
        /// <param name="mefLoader"></param>
        /// <returns></returns>
        protected virtual List<TPlugin> LoadPlugins(MefLoader mefLoader)
        {
            return mefLoader.Load<TPlugin>();
        }

        #endregion

        #region FolderMonitoringService

        private object _lock = new object();

        private void FolderCreated(string folderPath)
        {
            _fileEventsQueue.Enqueue(() => Created(folderPath));
        }

        private void Created(string folderPath)
        {
            lock (_lock)
            {
                _log.Debug("PluginService: folder '{0}' created.", folderPath);
                var mefLoader = SeparateAppDomain.CreateInstance<MefLoader>(folderPath, folderPath);
                var plugins = LoadPlugins(mefLoader);
                if (plugins.Count == 0)
                {
                    SeparateAppDomain.Delete(folderPath);
                    return;
                }

                _plugins.Add(folderPath, plugins);
                PluginsAdded(this, plugins);
            }
        }

        private void FolderChanged(string folderPath)
        {
            _fileEventsQueue.Enqueue(() => Changed(folderPath));
        }

        private void Changed(string folderPath)
        {
            lock (_lock)
            {
                _log.Debug("PluginService: folder '{0}' changed.", folderPath);

                //try remove old
                AppDomain oldAppDomain = null;
                List<TPlugin> oldPlugins = new List<TPlugin>();
                if (_plugins.ContainsKey(folderPath))
                {
                    oldPlugins = _plugins[folderPath];
                    _plugins.Remove(folderPath);
                    oldAppDomain = SeparateAppDomain.Extract(folderPath);
                }

                //try create new
                var mefLoader = SeparateAppDomain.CreateInstance<MefLoader>(folderPath, folderPath);
                var newPlugins = LoadPlugins(mefLoader);
                if (newPlugins.Count != 0)
                {
                    _plugins.Add(folderPath, newPlugins);
                }

                //raise event
                PluginsChanged(this, oldPlugins, newPlugins);

                if (newPlugins.Count == 0) SeparateAppDomain.Delete(folderPath);
                if (oldAppDomain != null) AppDomain.Unload(oldAppDomain);
            }
        }

        private void FolderRenamed(string oldFolderPath, string newFolderPath)
        {
            _fileEventsQueue.Enqueue(() => Renamed(oldFolderPath, newFolderPath));
        }

        private void Renamed(string oldFolderPath, string newFolderPath)
        {
            lock (_lock)
            {
                _log.Debug("PluginService: folder '{0}' renamed into '{1}'.", oldFolderPath, newFolderPath);

                //try remove old
                AppDomain oldAppDomain = null;
                List<TPlugin> oldPlugins = new List<TPlugin>();
                if (_plugins.ContainsKey(oldFolderPath))
                {
                    oldPlugins = _plugins[oldFolderPath];
                    _plugins.Remove(oldFolderPath);
                    oldAppDomain = SeparateAppDomain.Extract(oldFolderPath);
                }

                //try create new
                var mefLoader = SeparateAppDomain.CreateInstance<MefLoader>(newFolderPath, newFolderPath);
                var newPlugins = LoadPlugins(mefLoader);
                if (newPlugins.Count != 0)
                {
                    _plugins.Add(newFolderPath, newPlugins);
                }

                //raise event
                PluginsChanged(this, oldPlugins, newPlugins);

                if (newPlugins.Count == 0) SeparateAppDomain.Delete(newFolderPath);
                if (oldAppDomain != null) AppDomain.Unload(oldAppDomain);
            }
        }

        private void FolderDeleted(string folderPath)
        {
            _fileEventsQueue.Enqueue(() => Deleted(folderPath));
        }

        private void Deleted(string folderPath)
        {
            lock (_lock)
            {
                _log.Debug("PluginService: folder '{0}' deleted.", folderPath);

                //try remove old
                AppDomain oldAppDomain = null;
                List<TPlugin> oldPlugins = new List<TPlugin>();
                if (_plugins.ContainsKey(folderPath))
                {
                    oldPlugins = _plugins[folderPath];
                    _plugins.Remove(folderPath);
                    oldAppDomain = SeparateAppDomain.Extract(folderPath);
                }

                //raise event
                PluginsRemoved(this, oldPlugins);

                if (oldAppDomain != null) AppDomain.Unload(oldAppDomain);
            }
        }

        #endregion

        #region IService

        public override void Start()
        {
            base.Start();
            _folderMonitoringService.Start();
        }

        public override void Stop()
        {
            _folderMonitoringService.Stop();
            base.Stop();
        }

        #endregion

        #region LoopService

        protected override void Action()
        {
            Action fileEvent;
            if (_fileEventsQueue.TryDequeue(out fileEvent))
            {
                fileEvent();
            }
        }

        #endregion
    }
}
