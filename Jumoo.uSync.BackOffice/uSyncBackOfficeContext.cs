namespace Jumoo.uSync.BackOffice
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Jumoo.uSync.Core;

    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using System.Collections.Specialized;
    using System.Diagnostics;

    public class uSyncBackOfficeContext
    {
        private static uSyncBackOfficeContext _instance;
        private SortedList<int, ISyncHandler> handlers;

        // private SortedList<int, ISyncPostImportHandler> postImportHandlers;

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
            uSyncCoreContext.Instance.Init();

            LoadAssemblyHandlers();

            //
            // Handlers can Impliment a post import handler, this is good for do things after everything has ran
            // at least once (example DataTypes need to run before and after DocTypes)
            //
            /*
            var postImportTypes = TypeFinder.FindClassesOfType<ISyncPostImportHandler>();
            LogHelper.Info<uSyncBackOfficeContext>("Loading up Post Import Handlers : {0}", () => postImportTypes.Count());

            foreach (var t in postImportTypes)
            {
                var typeInstance = Activator.CreateInstance(t) as ISyncPostImportHandler;
                if (typeInstance != null)
                {
                    LogHelper.Debug<uSyncBackOfficeContext>("Adding Instance: {0}", () => typeInstance.Name);
                    postImportHandlers.Add(typeInstance.Priority, typeInstance);
                }
            }
            */


            _config = new uSyncBackOfficeConfig();

            Tracker = new Helpers.ActionTracker(_config.Settings.MappedFolder());
        }


        private void LoadAssemblyHandlers()
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

                    if (typeInstance is ISyncHandlerConfig)
                    {
                        ((ISyncHandlerConfig)typeInstance).LoadHandlerConfig(HandlerSettings(typeInstance.Name));
                    }
                }
            }

        }

        public void SetupEvents()
        {
            LogHelper.Info<uSyncApplicationEventHandler>("Setting up Events");
            foreach(var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name, "events"))
                {
                    handler.RegisterEvents();
                }
            }
        }


        public IEnumerable<uSyncAction> ImportAll(string folder = null, bool force = false)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Configuration.Settings.Folder;

            // the default way uSync.BackOffice calls an import (on import all)
            return Import(Configuration.Settings.HandlerGroup, folder, force);
        }

        public IEnumerable<uSyncAction> ExportAll(string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Configuration.Settings.Folder;

            return Export(Configuration.Settings.HandlerGroup, folder);
        }

        public IEnumerable<uSyncAction> ImportReport(string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Configuration.Settings.Folder;

            return Report(Configuration.Settings.HandlerGroup, folder);
        }

        /// <summary>
        ///  Import - Run a import of stuff from a folder on the disk.
        ///  
        ///  this is the best method to call externally, as you can define 
        ///  the handler group, folder, and the force and default behaviors.
        ///  
        ///  the backoffice call is typically is "Default", "uSync/data", false, true
        /// 
        ///  this means, use the default handler group, 
        ///     on the usync folder
        ///     don't force the updates
        ///     handlers that are not explicity configured are considered enabled and in the group
        ///     
        ///  if you are not mimicing the back office, you don't need to worry about enableMissingHandlers
        ///  when you define your own groups you will want this to be false, because you will only
        ///  want handlers that are in your group. 
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="folder"></param>
        /// <param name="force"></param>
        /// <param name="enableMissingHandlers"></param>
        /// <returns></returns>

        public IEnumerable<uSyncAction> Import(string groupName, string folder, bool force)
        {

            // pause all saving etc. while we do an import
            uSyncEvents.Paused = true;

            LogHelper.Info<uSyncApplicationEventHandler>("Running uSync Import: Group = {0} Folder = {1} Force = {2}",
                () => groupName, () => folder, () => force);

            List<uSyncAction> actions = new List<uSyncAction>();

            if (IsStopped(folder, force))
            {
                LogHelper.Info<uSyncApplicationEventHandler>("usync.stop file exists, exiting");
                actions.Add(uSyncAction.Fail("uSync.Stop", typeof(String), "usync stop file: exiting import"));
                uSyncEvents.Paused = false;
                return actions;
            }


            // run through the valid handlers for this import and do the import
            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name, "import", groupName))
                {
                    var sw = Stopwatch.StartNew();

                    var syncFolder = System.IO.Path.Combine(folder, handler.SyncFolder);
                    LogHelper.Debug<uSyncApplicationEventHandler>("# Import Calling Handler: {0}", () => handler.Name);
                    actions.AddRange(handler.ImportAll(syncFolder, force));
                    sw.Stop();
                    LogHelper.Debug<uSyncApplicationEventHandler>("# Handler {0} Complete ({1}ms)", () => handler.Name, ()=> sw.ElapsedMilliseconds);
                }
            }

            // once imported, we can have things that require a second import, these are idenfiied by 
            // requiresPostProcessing, and are pushed through the avalible ISyncPostImportHandlers
            // 
            var postImports = actions.Where(x => x.Success && x.Change > ChangeType.NoChange && x.RequiresPostProcessing);

            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name, "import", groupName))
                {
                    if (handler is ISyncPostImportHandler)
                    {
                        var postHandler = (ISyncPostImportHandler)handler;

                        var syncFolder = System.IO.Path.Combine(folder, handler.SyncFolder);
                        LogHelper.Debug<uSyncApplicationEventHandler>("# Post Import Processing: {0}", () => handler.Name);
                        var postActions = postHandler.ProcessPostImport(syncFolder, postImports);
                        if (postActions != null)
                            actions.AddRange(postActions);
                    }
                }
            }


            // do the once file stuff if needed. 
            OnceCheck(folder);

            uSyncEvents.Paused = false; 
            return actions;
        }


        public IEnumerable<uSyncAction> Export(string groupName, string folder)
        {
            LogHelper.Info<uSyncApplicationEventHandler>("Running full Umbraco Export");

            List<uSyncAction> actions = new List<uSyncAction>();

            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name, "export", groupName))
                {
                    actions.AddRange(handler.ExportAll(folder));
                }
            }

            return actions;

        }


        public IEnumerable<uSyncAction> Report(string groupName, string folder)
        {
            LogHelper.Info<uSyncApplicationEventHandler>("Running Import Report");

            List<uSyncAction> actions = new List<uSyncAction>();

            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name, "import", groupName))
                {
                    var sw = Stopwatch.StartNew();
                    var syncFolder = System.IO.Path.Combine(folder, handler.SyncFolder);
                    actions.AddRange(handler.Report(syncFolder));
                    sw.Stop();
                    LogHelper.Debug<uSyncApplicationEventHandler>("Report Complete: {0} ({1}ms)", () => handler.Name, () => sw.ElapsedMilliseconds);

                }
            }

            return actions;

        }

        // checks for a stop file. tells you if it's there...
        private bool IsStopped(string folder, bool force)
        {
            var stopFile = Umbraco.Core.IO.IOHelper.MapPath(System.IO.Path.Combine(folder, "usync.stop"));
            return (!force && System.IO.File.Exists(stopFile));
        }

        // changes any once file into a stop file, so it will stop next time.
        private void OnceCheck(string folder)
        {
            var onceFile = Umbraco.Core.IO.IOHelper.MapPath(System.IO.Path.Combine(folder, "usync.once"));
            LogHelper.Debug<uSyncApplicationEventHandler>("Looking for once file: {0}", () => onceFile);
            if (System.IO.File.Exists(onceFile))
            {
                var stopFile = Umbraco.Core.IO.IOHelper.MapPath(System.IO.Path.Combine(folder, "usync.stop"));
                System.IO.File.Move(onceFile, stopFile);
                LogHelper.Debug<uSyncApplicationEventHandler>("Renamed once to stop, for next time");
            }

        }

        #region Handlers 

        private bool HandlerInGroup(string handlerName, string group)
        {
            LogHelper.Debug<uSyncBackOfficeConfig>("Looking for Handler {0} in Group {1}", () => handlerName, () => group);
            var hGroup = Configuration.Settings.Handlers.FirstOrDefault(x => x.Group.Equals(group, StringComparison.InvariantCultureIgnoreCase));
            if (hGroup != null)
            {
                return hGroup.Handlers.Any(x => x.Name.Equals(handlerName, StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        public bool HandlerEnabled(string handlerName, string action, string group = "default")
        {
            var validActions = new string[] { "all", action.ToLower() }; 

            var hGroup = Configuration.Settings.Handlers.FirstOrDefault(x => x.Group.Equals(group, StringComparison.OrdinalIgnoreCase));
            if (hGroup != null)
            {
                var handlerConfig = hGroup.Handlers.Where(x => x.Name == handlerName).FirstOrDefault();

                if (handlerConfig != null)
                {
                    if (handlerConfig.Enabled)
                    {
                        var actions = handlerConfig.Actions.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var valid = actions.Any(x => validActions.Contains(x));
                        LogHelper.Debug<uSyncApplicationEventHandler>("Handler: {0} {1} = {2}", () => handlerName, () => action, () => valid);
                        return valid;
                    }
                    else
                    {
                        LogHelper.Debug<uSyncApplicationEventHandler>("Handler: {0} is disabled by config", () => handlerName);
                        return false;
                    }
                }

                LogHelper.Debug<uSyncApplicationEventHandler>("Handler {0} is missing in group \"{1}\" and default setting is {2}", () => handlerName, ()=> group, () => hGroup.EnableMissing);
                // return the group default (i.e if true, we include handlers not in this group) 
                return hGroup.EnableMissing;
            }

            return false;
        }

        private IEnumerable<uSyncHandlerSetting> HandlerSettings(string handlerName, string group = "default")
        {
            var hGroup = Configuration.Settings.Handlers.FirstOrDefault(x => x.Group.Equals(group, StringComparison.InvariantCultureIgnoreCase));
            if (hGroup != null)
            {
                var handlerConfig = hGroup.Handlers.Where(x => x.Name == handlerName).FirstOrDefault();

                if (handlerConfig != null && handlerConfig.Settings != null)
                    return handlerConfig.Settings;
            }

            return new List<uSyncHandlerSetting>();
        }
        #endregion
    }
}
