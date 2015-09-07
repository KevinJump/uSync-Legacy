namespace Jumoo.uSync.BackOffice
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Jumoo.uSync.Core;

    using Umbraco.Core;
    using Umbraco.Core.Logging;

    public class uSyncBackOfficeContext
    {
        private static uSyncBackOfficeContext _instance;
        private SortedList<int, ISyncHandler> handlers;

        public Helpers.ActionTracker Tracker; 

        public List<ISyncHandler> Handlers
        {
            get { return handlers.Select(x => x.Value).ToList(); }
        }

        private uSyncBackOfficeConfig _config;

        public string Version
        {
            get {
                return typeof(Jumoo.uSync.BackOffice.uSyncApplicationEventHandler)
                  .Assembly.GetName().Version.ToString();
            }
        }

        public uSyncBackOfficeContext() { }

        public static uSyncBackOfficeContext Instance
        {
            get { return _instance ?? (_instance = new uSyncBackOfficeContext()); }
        }

        public uSyncBackOfficeConfig Configuration
        {
            get { return _config ?? (_config = new uSyncBackOfficeConfig()); }
        }

        public void Init()
        {
            handlers = new SortedList<int, ISyncHandler>();

            var types = TypeFinder.FindClassesOfType<ISyncHandler>();

            LogHelper.Info<uSyncBackOfficeContext>("Loading up Sync Handlers : {0}", () => types.Count());
            foreach (var t in types)
            {
                var typeInstance = Activator.CreateInstance(t) as ISyncHandler;
                if (typeInstance != null)
                {
                    LogHelper.Debug<uSyncBackOfficeContext>("Adding Instance: {0}", () => typeInstance.Name);
                    handlers.Add(typeInstance.Priority, typeInstance);
                }
            }

            uSyncCoreContext.Instance.Init();
            _config = new uSyncBackOfficeConfig();

            Tracker = new Helpers.ActionTracker(_config.Settings.MappedFolder());
        }

        public void SetupEvents()
        {
            LogHelper.Info<uSyncApplicationEventHandler>("Setting up Events");
            foreach(var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name))
                {
                    handler.RegisterEvents();
                }
            }
        }

        public IEnumerable<uSyncAction> ImportAll(string folder = null, bool force = false)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Configuration.Settings.Folder;

            LogHelper.Info<uSyncApplicationEventHandler>("Running Full uSync Import");

            List<uSyncAction> importActions = new List<uSyncAction>();

            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name))
                {
                    var syncFolder = System.IO.Path.Combine(folder, handler.SyncFolder);

                    LogHelper.Debug<uSyncApplicationEventHandler>("# Import Calling Handler: {0}", () => handler.Name);
                    importActions.AddRange(handler.ImportAll(syncFolder, force));
                }
            }

            return importActions;
        }

        public IEnumerable<uSyncAction> ExportAll(string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Configuration.Settings.Folder;

            LogHelper.Info<uSyncApplicationEventHandler>("Running full Umbraco Export");

            List<uSyncAction> exportActions = new List<uSyncAction>();

            foreach(var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name))
                {
                    exportActions.AddRange(handler.ExportAll(folder));
                }
            }

            return exportActions;
        }

        /// <summary>
        ///  a report on what will change if we run a report
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public IEnumerable<uSyncAction> ImportReport(string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Configuration.Settings.Folder;

            LogHelper.Info<uSyncApplicationEventHandler>("Running Import Report");

            List<uSyncAction> reportActions = new List<uSyncAction>();

            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name))
                {
                    var syncFolder = System.IO.Path.Combine(folder, handler.SyncFolder);
                    reportActions.AddRange(handler.Report(syncFolder));
                }
            }

            return reportActions;
        }

        private bool HandlerEnabled(string handlerName)
        {
            var handlerConfig = Configuration.Settings.Handlers.Where(x => x.Name == handlerName).FirstOrDefault();

            if (handlerConfig != null && !handlerConfig.Enabled)
            {
                // this handler is off (on is default)
                LogHelper.Debug<uSyncApplicationEventHandler>("Handler: {0} is disabled by config", () => handlerName);
                return false;
            }

            return true;
        }
    }
}
