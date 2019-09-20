using Jumoo.uSync.BackOffice;
using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Jumoo.uSync.BackOffice.Helpers;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using System.Diagnostics;

namespace Jumoo.uSync.Content
{
    abstract public class BaseContentHandler<T>
        where T : IContentBase
    {
        internal IContentService _contentService;
        internal IMediaService _mediaService; 

        internal const string mediaFolderName = "_uSyncMedia";
        internal string _exportFileName = "content";

        private bool _ignorePathSettingOn;
        private bool _rootPathSettingOn;
        private bool _levelPathsOn;

        public BaseContentHandler(string fileName)
        {
            _contentService = ApplicationContext.Current.Services.ContentService;
            _mediaService = ApplicationContext.Current.Services.MediaService;
            _exportFileName = fileName;

            _ignorePathSettingOn = false;
            _rootPathSettingOn = false;
            _levelPathsOn = false; 

            // short Id Setting, means we save with id.config not {{name}}.config
            handlerSettings = new BaseContentHandlerSettings();
            handlerSettings.UseShortName = uSyncBackOfficeContext.Instance.Configuration.Settings.UseShortIdNames;
        }

        #region BaseImport

        abstract public SyncAttempt<T> Import(string file, int parentId, bool force = false);
        virtual public SyncAttempt<T> ImportRedirect(string file, bool force = false)
        {
            return SyncAttempt<T>.Succeed(file, ChangeType.NoChange);
        }
        virtual public void ImportSecondPass(string file, T item) {}

        public IEnumerable<uSyncAction> ImportAll(string folder, bool force)
        {
            var typeName = typeof(T).Name;
            var sw = Stopwatch.StartNew();
            LogHelper.Info<Logging>("<< Import: [{0}] {1}",
                () => typeName,
                () => Path.GetFileName(folder));

            Dictionary<string, T> updates = new Dictionary<string, T>();

            List<uSyncAction> actions = new List<uSyncAction>();

            string mappedFolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            actions.AddRange(ProcessActions());

            actions.AddRange(ImportFolder(mappedFolder, -1, force, updates));

            if (updates.Any())
            {
                foreach(var update in updates)
                {
                    ImportSecondPass(update.Key, update.Value);
                }
            }

            sw.Stop();
            LogHelper.Info<Logging>(">> Import [{0}] Complete: {1} Items {2} changes {3} failures ({4}ms)",
                () => typeName,
                () => actions.Count(),
                () => actions.Count(x => x.Change > ChangeType.NoChange),
                () => actions.Count(x => x.Change > ChangeType.Fail),
                () => sw.ElapsedMilliseconds);


            return actions;
        }

        private IEnumerable<uSyncAction> ImportFolder(string folder, int parentId, bool force, Dictionary<string, T> updates)
        {
            LogHelper.Debug<ContentHandler>("Import Folder: {0} {1}", () => folder, () => parentId);
            int itemId = parentId;
            List<uSyncAction> actions = new List<uSyncAction>();

            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, string.Format("{0}.config", _exportFileName)))
                {
                    var attempt = Import(file, parentId, force);
                    if (attempt.Success && attempt.Change > ChangeType.NoChange && attempt.Item != null)
                    {
                        updates.Add(file, attempt.Item);
                    }

                    if (attempt.Item != null)
                        itemId = ((IContentBase)attempt.Item).Id;

                    actions.Add(uSyncActionHelper<T>.SetAction(attempt, file));
                }

                // redirects...
                foreach(string file in Directory.GetFiles(folder, "redirect.config"))
                {
                    var attempt = ImportRedirect(file, force);
                    actions.Add(uSyncActionHelper<T>.SetAction(attempt, file));
                }

                foreach (var child in Directory.GetDirectories(folder))
                {
                    if (!Path.GetFileName(child).Equals("_uSyncMedia", StringComparison.OrdinalIgnoreCase))
                    {
                        actions.AddRange(ImportFolder(child, itemId, force, updates));
                    }
                }
            }
            return actions;
        }

        private IEnumerable<uSyncAction> ProcessActions()
        {
            List<uSyncAction> syncActions = new List<uSyncAction>();

            var actions = uSyncBackOfficeContext.Instance.Tracker.GetActions(typeof(T));

            if (actions != null && actions.Any())
            {
                foreach(var action in actions)
                {
                    switch(action.Action)
                    {
                        case SyncActionType.Delete:
                            syncActions.Add(DeleteItem(action.Key, action.Name));
                            break;
                    }
                }
            }

            return syncActions;
        }

        virtual public uSyncAction DeleteItem(Guid key, string keyString)
        {
            return new uSyncAction();
        }

        #endregion

        #region  Base Export


        #endregion

        #region Base Report
        public IEnumerable<uSyncAction> Report(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            string mappedFolder = folder; 
            if (folder.StartsWith("~"))
                mappedFolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            if (Directory.Exists(mappedFolder))
            {
                foreach(var file in Directory.GetFiles(mappedFolder, string.Format("{0}.config", _exportFileName)))
                {
                    actions.Add(ReportItem(file));
                }

                foreach(var child in Directory.GetDirectories(mappedFolder))
                {
                    actions.AddRange(Report(child));
                }

            }

            return actions;
        }

        abstract public uSyncAction ReportItem(string file);
        #endregion

        #region Base Settings Load

        //
        // implimenting ISyncHanlderConfig for both media and content. 

        private List<uSyncHandlerSetting> _settings;


        protected BaseContentHandlerSettings handlerSettings { get; set; }


        public void LoadHandlerConfig(IEnumerable<uSyncHandlerSetting> settings)
        {
            LogHelper.Debug<ContentHandler>("Loading Handler Settings {0}", () => settings.Count());

            _settings = settings.ToList();

            if (_settings.Any())
            {
                foreach(var setting in _settings)
                {
                    switch(setting.Key.ToLower())
                    {
                        case "useshortidnames":
                            bool idNameVal = false;
                            if (bool.TryParse(setting.Value, out idNameVal))
                                handlerSettings.UseShortName = idNameVal;
                            break;
                        case "include":
                        case "root":
                            handlerSettings.Root = setting.Value.ToDelimitedList();
                            _rootPathSettingOn = handlerSettings.Root != null && handlerSettings.Root.Any();
                            LogHelper.Debug<ContentHandler>("Root Setting: {0}", () => handlerSettings.Root);
                            break;
                        case "ignore":
                            handlerSettings.Ignore = setting.Value.ToDelimitedList();
                            _ignorePathSettingOn = handlerSettings.Ignore != null && handlerSettings.Ignore.Any();
                            LogHelper.Debug<ContentHandler>("Ignore Setting: {0}", () => string.Join(",", handlerSettings.Ignore));
                            break;
                        case "levelpath":
                            bool.TryParse(setting.Value, out _levelPathsOn);
                            LogHelper.Debug<ContentHandler>("Level Paths : {0}", ()=> _levelPathsOn);
                            break;
                        case "deleteactions":
                            bool delete = false;
                            if (bool.TryParse(setting.Value, out delete))
                                handlerSettings.DeleteActions = delete;
                            break;
                        case "rulesonexport":
                            bool rulesOnExport = false;
                            if (bool.TryParse(setting.Value, out rulesOnExport))
                                handlerSettings.UseRulesOnExport = true;
                            break;
                    }
                }
            }
        }


        #endregion


        /// <summary>
        ///  will either return the path (as expected) or a path that
        ///  uses the letters of the key, and the level to make a short
        ///  path..
        /// </summary>
        /// <param name="item"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string GetSavePath(IContentBase item, string path)
        {
            if (!_levelPathsOn)
            {
                return path;
            }
            else
            {
                return item.Level.ToString("000") + "\\" + GetItemFileName(item);
            }
        }

        protected string GetItemFileName(IContentBase item)
        {
            if (item != null)
            {
                if (handlerSettings.UseShortName)
                    return uSyncIOHelper.GetShortGuidPath(item.Key);

                return item.Name.ToSafeFileName();
            }

            // we should never really get here, but if for
            // some reason we do - just return a guid.
            return uSyncIOHelper.GetShortGuidPath(Guid.NewGuid());
        }

        protected bool IncludeItem(int contentId)
        {
            if (!_ignorePathSettingOn && !_rootPathSettingOn)
                return true;

            var content = _contentService.GetById(contentId);
            return IncludeItem(content.Path);
        }

        protected bool IncludeItem(string path, IContentBase item)
        {
            if (!_ignorePathSettingOn && !_rootPathSettingOn)
                return true;

            if (item == null)
                return true;
           
            var itemPath = Path.Combine(path, item.Name.ToSafeFileName());

            return IncludeItem(itemPath);
        }


        protected bool IncludeItem(string path)
        {
            if ((!_ignorePathSettingOn && !_rootPathSettingOn) || string.IsNullOrWhiteSpace(path))
                return true;

            if (_ignorePathSettingOn)
            {
                LogHelper.Debug<ContentHandler>("Checking : {0} in ignore path settings", () => path);
                // if the path starts with the ignore thing, then we don't include it.
                if (handlerSettings.Ignore != null) 
                {
                    foreach(var item in handlerSettings.Ignore)
                    {
                        if (path.InvariantContains(item))
                        {
                            LogHelper.Debug<ContentHandler>("Ignoring: {0} ({1})", () => path, () => string.Join(",", handlerSettings.Ignore));
                            return false;
                        }
                    }
                }
            }

            if (_rootPathSettingOn)
            {
                LogHelper.Debug<ContentHandler>("Checking : {0} in root path settings", () => path);
                // if root is set but the path DOESN'T start with it we don't include it.
                if (handlerSettings.Root != null && handlerSettings.Root.Any())
                {
                    foreach(var item in handlerSettings.Root)
                    {
                        if (path.InvariantContains(item)) {
                            return true;
                        }
                    }

                    LogHelper.Debug<ContentHandler>("Not under root: {0} ({1})", () => path, () => string.Join(",", handlerSettings.Root));
                    return false;
                }
            }

            return true;
        }


        protected class BaseContentHandlerSettings
        {
            public bool UseShortName { get; set; }

            public IList<string> Root { get; set; }
            public IList<string> Ignore { get; set; }

            public bool UseRulesOnExport { get; set; }

            public bool DeleteActions { get; set; }
        }
    }

    
}
