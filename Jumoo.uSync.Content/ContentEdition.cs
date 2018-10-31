using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.BackOffice.Controllers;
using Jumoo.uSync.Core;
using Umbraco.Core;
using Umbraco.Web.Editors;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web;

namespace Jumoo.uSync.Content
{
    public class ContentEdition : IuSyncAddOn, IuSyncTab
    {
        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab()
            {
                name = "Content",
                template = UriUtility.ToAbsolute("/app_plugins/uSync.Content/uSyncDashboardContent.html")
            };
        }

        public string GetVersionInfo()
        {
            return string.Format("uSync.Content: {0}", typeof(Jumoo.uSync.Content.ContentEdition)
              .Assembly.GetName().Version.ToString());
        }

    }

    [PluginController("uSync")]
    public class ContentEditionApiController : UmbracoAuthorizedJsonController
    {
        [HttpGet]
        public List<uSyncContentMapping> GetMappers()
        {
            return uSyncCoreContext.Instance.Configuration.Settings.ContentMappings
                .OrderBy(x => x.EditorAlias).ToList();
        }

    }
}
