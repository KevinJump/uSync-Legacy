using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

namespace Jumoo.uSync.Snapshots
{
    [PluginController("uSync")]
    public class SnapshotServiceController : UmbracoAuthorizedJsonController
    {
        [HttpGet]
        public IEnumerable<SnapshotInfo> GetSnapshots()
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);

            return snapshotManager.GetSnapshots();
        }

        [HttpGet]
        public SnapshotInfo CreateSnapshot(string name)
        {
            LogHelper.Info<SnapshotServiceController>("Createsnap shot: {0}", () => name);

            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);

            return snapshotManager.CreateSnapshot(name);
        }
    }
}
