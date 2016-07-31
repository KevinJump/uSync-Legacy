using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.BackOffice.Controllers;

namespace Jumoo.uSync.Content
{
    public class ContentEdition : IuSyncAddOn, IuSyncTab
    {
        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab()
            {
                name = "Content",
                template = "/app_plugins/uSync.Content/uSyncDashboardContent.html"
            };
        }

        public string GetVersionInfo()
        {
            return string.Format("uSync.Content: {0}", typeof(Jumoo.uSync.Content.ContentEdition)
              .Assembly.GetName().Version.ToString());
        }
    }
}
