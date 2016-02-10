using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Snapshots
{
    public class uSyncSnapshots
    {
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
    }
}
