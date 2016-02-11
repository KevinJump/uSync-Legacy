using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

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
            var actions = uSyncBackOfficeContext.Instance.ExportAll();
            return actions;
        }

        [HttpGet]
        public IEnumerable<uSyncAction> Import(bool force)
        {
            var actions = uSyncBackOfficeContext.Instance.ImportAll(force: force);
            return actions;
        }

        [HttpGet]
        public BackOfficeSettings GetSettings()
        {
            var settings = new BackOfficeSettings()
            {
                backOfficeVersion = uSyncBackOfficeContext.Instance.Version,
                coreVersion = uSyncCoreContext.Instance.Version,
                settings = uSyncBackOfficeContext.Instance.Configuration.Settings,
            };

            return settings; 
        }
    }

    public class BackOfficeSettings
    {
        public string backOfficeVersion { get; set; }
        public string coreVersion { get; set; }
        public uSyncBackOfficeSettings settings { get; set; }
    }
}
