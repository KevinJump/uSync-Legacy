using Jumoo.uSync.BackOffice;
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

        [HttpGet]
        public uSyncSnapshotSettings GetSnapshotSettings()
        {
            return uSyncSnapshots.Instance.Configuration.Settings;
        }

        [HttpGet]
        public IEnumerable<uSyncAction> Report(string snapshotName)
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);
            return snapshotManager.Report(snapshotName);
        }

        [HttpGet]
        public IEnumerable<uSyncAction> ReportAll()
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);
            return snapshotManager.Report();
        }

        [HttpGet]
        public IEnumerable<uSyncAction> Apply(string snapshotName)
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);
            return snapshotManager.Apply(snapshotName);
        }

        [HttpGet]
        public IEnumerable<uSyncAction> ApplyAll()
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);
            return snapshotManager.ApplySnapshots();
        }

        [HttpGet]
        public bool Delete(string snapshotName)
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            SnapshotManager snapshotManager = new SnapshotManager(root);
            return snapshotManager.Delete(snapshotName);
        }
    }
}
