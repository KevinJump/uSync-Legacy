using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Jumoo.uSync.BackOffice.Controllers
{
    [PluginController("uSync")]
    public class uSyncBackOfficeController : UmbracoAuthorizedApiController
    {
        public static object _apiLock;

        public void ExportAll()
        {
            lock(_apiLock)
            {
                uSyncEvents.Paused = true;

                uSyncBackOfficeContext.Instance.ExportAll();

                uSyncEvents.Paused = false;
            }
        }

        public void ImportAll()
        {
            lock (_apiLock)
            {
                uSyncEvents.Paused = true;

                uSyncBackOfficeContext.Instance.ImportAll();

                uSyncEvents.Paused = false;
            }
        }

        /// <summary>
        ///  An experiement, if we pass in a node,
       ///   can we just stream it right in?
        /// </summary>
        /// <param name="node"></param>
        public void ImportDocType(XElement node)
        {
            lock (_apiLock)
            {
                if (node != null)
                {
                    uSyncEvents.Paused = true;
                    uSyncCoreContext.Instance.ContentTypeSerializer.DeSerialize(node, true, true);
                    uSyncEvents.Paused = false;
                }
            }
        }
    }
}
