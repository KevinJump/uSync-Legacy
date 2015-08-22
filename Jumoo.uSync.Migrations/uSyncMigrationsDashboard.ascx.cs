using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Jumoo.uSync.BackOffice;

namespace Jumoo.uSync.Migrations
{
    public partial class uSyncMigrationsDashboard : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                GetSnapShots();
        }

        protected void btnSnapshot_Click(object sender, EventArgs e)
        {
            string snapshotRoot = "~/uSync/snapshots";

            var snapshotMgr = new SnapshotManager(snapshotRoot);
            snapshotMgr.CreateSnapshot(txtSnapshotName.Text);

            GetSnapShots();
        }

        private void GetSnapShots()
        {
            var snapshotMgr = new SnapshotManager("~/uSync/snapshots");
            snapshotList.DataSource = snapshotMgr.ListSnapshots();
            snapshotList.DataBind();
        }
    }
}