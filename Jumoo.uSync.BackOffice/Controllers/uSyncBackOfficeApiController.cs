using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Jumoo.uSync.BackOffice.Licence;
using Jumoo.uSync.BackOffice.Helpers;

namespace Jumoo.uSync.BackOffice.Controllers
{
    [PluginController("uSync")]
    public class uSyncApiController : UmbracoAuthorizedJsonController
    {

        [HttpGet]
        public IEnumerable<uSyncAction> Report()
        {
            var actions = uSyncBackOfficeContext.Instance.ImportReport();
            return actions;
        }

        [HttpGet]
        public IEnumerable<uSyncAction> Export(bool deleteAction = false)
        {
            var folder = uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder();

            if (System.IO.Directory.Exists(folder))
            {
                // delete the sub foldes (this will leave the uSync.Action file)
                // we try three times, usally its because someone has something open
                // so we can't delete a folder. 
                var attempt = 0;
                var success = false;
                while (attempt < 3 && success == false)
                {
                    success = CleanFolder(folder);
                    attempt++;
                }
            }

            if (deleteAction)
            {
                var action = System.IO.Path.Combine(folder, "uSyncActions.config");
                if (System.IO.File.Exists(action))
                    System.IO.File.Delete(action);
            }


            var actions = uSyncBackOfficeContext.Instance.ExportAll();

            // we write a log - when there have been changes, a zero run doesn't get
            // a file written to disk.
            if (actions.Any(x => x.Change > ChangeType.NoChange))
                uSyncActionLogger.SaveActionLog("Export", actions);

            return actions;
        }

        private bool CleanFolder(string folder)
        {
            try
            {
                foreach (var child in System.IO.Directory.GetDirectories(folder))
                {
                    System.IO.Directory.Delete(child, true);
                }
                return true;
            }
            catch (System.IO.IOException ex)
            {
                Logger.Warn<Events>("Cannot Clean Folder - will try three times: {0}", () => ex.Message);
                return false;
            }
        }

        [HttpGet]
        public IEnumerable<uSyncAction> Import(bool force)
        {
            var actions = uSyncBackOfficeContext.Instance.ImportAll(force: force);

            // we write a log - when there have been changes, a zero run doesn't get
            // a file written to disk.
            if (actions.Any(x => x.Change > ChangeType.NoChange))
                uSyncActionLogger.SaveActionLog("Import", actions);

            return actions;
        }

        [HttpGet]
        public BackOfficeSettings GetSettings()
        {
            string addOnString = "";
            List<BackOfficeTab> addOnTabs = new List<BackOfficeTab>();

            var types = TypeFinder.FindClassesOfType<IuSyncAddOn>();
            foreach (var t in types)
            {
                var typeInstance = Activator.CreateInstance(t) as IuSyncAddOn;
                if (typeInstance != null)
                {
                    LogHelper.Debug<Events>("Loading AddOn Versions: {0}", () => typeInstance.GetVersionInfo());
                    addOnString = string.Format("{0} [{1}]", addOnString, typeInstance.GetVersionInfo());
                }
            }

            var tabTypes = TypeFinder.FindClassesOfType<IuSyncTab>();
            foreach(var t in tabTypes)
            {
                var inst = Activator.CreateInstance(t) as IuSyncTab;
                if (inst != null)
                {
                    addOnTabs.Add(inst.GetTabInfo());
                }
            }

            var l = new GoodwillLicence();

            var settings = new BackOfficeSettings()
            {
                backOfficeVersion = uSyncBackOfficeContext.Instance.Version,
                coreVersion = uSyncCoreContext.Instance.Version,
                addOns = addOnString,
                settings = uSyncBackOfficeContext.Instance.Configuration.Settings,
                licenced = l.IsLicenced(),
                addOnTabs = addOnTabs,
                Handlers = uSyncBackOfficeContext.Instance.Handlers.Select(x => x.Name)
            };

            return settings;
        }

        [HttpGet]
        public bool UpdateSyncMode(string mode)
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;

            switch (mode.ToLowerInvariant())
            {
                case "auto":
                    settings.ExportAtStartup = false;
                    settings.ExportOnSave = true;
                    settings.Import = true;
                    break;
                case "target":
                    settings.ExportAtStartup = false;
                    settings.ExportOnSave = false;
                    settings.Import = true;
                    break;
                case "source":
                    settings.ExportAtStartup = false;
                    settings.ExportOnSave = true;
                    settings.Import = false;
                    break;
                case "manual":
                    settings.ExportAtStartup = false;
                    settings.ExportOnSave = false;
                    settings.Import = false;
                    break;
                case "other":
                    return false; 
            }

            uSyncBackOfficeContext.Instance.Configuration.SaveSettings(settings);
            return true; 
        }


        [HttpGet]
        public IEnumerable<uSyncHistory> GetHistory()
        {
            return uSyncActionLogger.GetActionHistory(false);
        }

        [HttpGet]
        public int ClearHistory()
        {
            return uSyncActionLogger.ClearHistory();
        }

        [HttpGet]
        public IEnumerable<SyncAction> GetActions()
        {
            // gets the actions from the uSync Action file....
            var uSyncFolder = uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder();
            var Tracker = new Helpers.ActionTracker(uSyncFolder);
            return Tracker.GetAllActions();
        }

        [HttpGet]
        public bool RemoveAction(string name, string type)
        {
            // gets the actions from the uSync Action file....
            var uSyncFolder = uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder();
            var Tracker = new Helpers.ActionTracker(uSyncFolder);

            return Tracker.RemoveActions(name, type);
        }
    }

    public class BackOfficeSettings
    {
        public string backOfficeVersion { get; set; }
        public string coreVersion { get; set; }
        public string addOns { get; set; }
        public uSyncBackOfficeSettings settings { get; set; }

        public IEnumerable<string> Handlers { get; set; }

        public bool licenced { get; set; }
        public IEnumerable<BackOfficeTab> addOnTabs { get; set; }

    }

    public class BackOfficeTab
    {
        public string name { get; set; }
        public string template { get; set; }
    }

 
    public interface IuSyncAddOn
    {
        string GetVersionInfo();
    }

    public interface IuSyncTab
    { 
        BackOfficeTab GetTabInfo();
    }
}
