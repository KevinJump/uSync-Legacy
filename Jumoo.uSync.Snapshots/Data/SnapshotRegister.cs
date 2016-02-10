using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;

namespace Jumoo.uSync.Snapshots.Data
{
    public class SnapshotRegister : ApplicationEventHandler 
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            var dbContext = applicationContext.DatabaseContext;
            var db = new DatabaseSchemaHelper(dbContext.Database,
                applicationContext.ProfilingLogger.Logger, dbContext.SqlSyntax);

            if (!db.TableExist("uSyncSnapshotAudit"))
            {
                LogHelper.Info<SnapshotRegister>("Setting up Snapshot Audit Table");
                db.CreateTable<SnapshotLog>(false);
            }
        }
    }
}
