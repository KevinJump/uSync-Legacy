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
        public IEnumerable<uSyncAction> Export()
        {
            var folder = uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder();

            if (System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.Delete(folder, true);
            }


            var actions = uSyncBackOfficeContext.Instance.ExportAll();

            // we write a log - when there have been changes, a zero run doesn't get
            // a file written to disk.
            if (actions.Any(x => x.Change > ChangeType.NoChange))
                uSyncActionLogger.SaveActionLog("Export", actions);

            return actions;
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

            var l = new GoodwillLicence();

            var settings = new BackOfficeSettings()
            {
                backOfficeVersion = uSyncBackOfficeContext.Instance.Version,
                coreVersion = uSyncCoreContext.Instance.Version,
                addOns = addOnString,
                settings = uSyncBackOfficeContext.Instance.Configuration.Settings,
                licenced = l.IsLicenced()
            };

            return settings;
        }

        [HttpGet]
        public bool UpdateSyncMode(string mode)
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;

            switch (mode.ToLower())
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

        public bool licenced { get; set; }
    }

 
    public interface IuSyncAddOn
    {
        string GetVersionInfo();
    }
}
