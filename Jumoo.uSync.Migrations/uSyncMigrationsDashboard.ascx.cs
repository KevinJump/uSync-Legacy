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
            string snapshotRoot = "~/uSync/Migrations";

            var snapshotMgr = new MigrationManager(snapshotRoot);
            var info = snapshotMgr.CreateMigration(txtSnapshotName.Text);

            if (info.FileCount == 0)
            {
                lbStatus.Text = "Migration contained no changes, so no folder has been created";
            }
            else
            {
                lbStatus.Text = "Migration created";
            }

            GetSnapShots();
        }

        private void GetSnapShots()
        {
            var snapshotMgr = new MigrationManager("~/uSync/migrations");
            snapshotList.DataSource = snapshotMgr.ListMigrations();
            snapshotList.DataBind();
        }

        protected void btnApplySnapshot_Click(object sender, EventArgs e)
        {
            var snapshotMgr = new MigrationManager("~/uSync/migrations");
            snapshotMgr.ApplyMigrations();
        }
    }
}