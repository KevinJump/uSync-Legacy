using Jumoo.uSync.BackOffice.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Snapshots
{
    public class uSyncSnapshots : IuSyncAddOn, IuSyncTab
    {

        public uSyncSnapshots() {
            Initialize();
        }

        public uSyncSnapshots(bool init) {

            if (init)
                Initialize();
        }
        private static uSyncSnapshots _instance;
        public static uSyncSnapshots Instance
        {
            get { return _instance ?? (_instance = new uSyncSnapshots(true)); }
        }

        public SnapshotConfig Configuration { get; set; }

        private void Initialize()
        {
            Configuration = new SnapshotConfig();
        }

        public string GetVersionInfo()
        {
            return string.Format("uSync.Snapshots: {0}", typeof(Jumoo.uSync.Snapshots.uSyncSnapshots)
              .Assembly.GetName().Version.ToString());
        }

        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab()
            {
                name = "Snapshots",
                template = "/app_plugins/usyncsnapshots/snapshotdashboard.html"
            };
        }
    }
}
