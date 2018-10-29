using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Web.HealthCheck;

using Jumoo.uSync.BackOffice;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;

namespace Jumoo.uSync.HealthChecker
{
    [HealthCheck("B8717B6A-F565-462C-ADD1-5C4096366699", "uSync",
        Description = "The health of usync for this install of Umbraco.", Group = "uSync")]
    public class uSyncHealthCheck : HealthCheck
    {
        public uSyncHealthCheck(HealthCheckContext healthCheckContext) 
            : base(healthCheckContext)
        { }

        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<HealthCheckStatus> GetStatus()
        {
            List<HealthCheckStatus> status = new List<HealthCheckStatus>();

            status.AddRange(CheckImports());
            status.AddRange(CheckReport());
            status.AddRange(CheckKeys());
            status.AddRange(CheckFolder());

            return status;
        }



        /// <summary>
        ///  checks the history to see how the last import went. 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<HealthCheckStatus> CheckImports()
        {
            List<HealthCheckStatus> status = new List<HealthCheckStatus>();

            LogHelper.Info<uSyncHealthCheck>("Checking Import History");

            var history = uSyncActionLogger.GetActionHistory(true);

            if (history.Any())
            {
                // first is last.
                var lastImport = history.FirstOrDefault(x => x.type == "Startup" || x.type == "Import");

                if (lastImport != null)
                {
                    var fails = lastImport.actions.Where(x => x.Change >= Core.ChangeType.Fail);
                    if (fails.Any())
                    {
                        var failStatus = new HealthCheckStatus(
                            string.Format("Last sync contained {0} errors", fails.Count()))
                            {
                                ResultType = StatusResultType.Warning,
                                Description = "The last uSync Import contained errors, you should look at the imports and see if you can fix it."
                            };

                        status.Add(failStatus);
                    }
                    else
                    {
                        var s = new HealthCheckStatus("Sync OK")
                        {
                            ResultType = StatusResultType.Success,
                            Description = "The last sync contained no errors"
                        };
                        status.Add(s);
                    }
                }
            }

            return status;
        }

        private IEnumerable<HealthCheckStatus> CheckReport()
        {
            List<HealthCheckStatus> actions = new List<HealthCheckStatus>();

            var uSyncActions = uSyncBackOfficeContext.Instance.ImportReport();


            var count = uSyncActions.Count();
            var err = uSyncActions.Count(x => x.Change >= Core.ChangeType.Fail);
            var changes = uSyncActions.Count(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Fail);
            var success = count - err;

            if (err > 0)
            {
                actions.Add(new HealthCheckStatus(string.Format("{0} Sync Errors", err))
                {
                    ResultType = StatusResultType.Error,
                    Description = "There are errors on the sync report, which means there is probibly something wrong with the sync"
                });
            }
            else if (changes > 0)
            {
                actions.Add(new HealthCheckStatus(string.Format("{0} Pending Changes", changes))
                {
                    ResultType = StatusResultType.Warning,
                    Description = string.Format("There are {0} pending changes, from the disk to uSync, you should run an import to sync this site", changes)
                });
            }
            else
            {
                actions.Add(new HealthCheckStatus("No pending changes")
                {
                    ResultType = StatusResultType.Success,
                    Description = string.Format("the uSync folder contains {0} items and is in sync with the database", count)
                });
            }

            return actions;

        }


        // some clever checking to see if the key in the file matches the ond on the db
        // although to be honest, the report check will do this, because they will
        // be marked as changes.
        private IEnumerable<HealthCheckStatus> CheckKeys()
        {
            List<HealthCheckStatus> status = new List<HealthCheckStatus>();
            LogHelper.Info<uSyncHealthCheck>("Checking Keys");


            return status;
        }

        private IEnumerable<HealthCheckStatus> CheckFolder()
        {
            List<HealthCheckStatus> actions = new List<HealthCheckStatus>();

            var folder = 
                IOHelper.MapPath(uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            try
            {
                if (!System.IO.Directory.Exists(folder)) {
                    System.IO.Directory.CreateDirectory(folder);
                }

                var testFile = "permissions.txt";
                var filePath = System.IO.Path.Combine(folder, testFile);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                using (var text = System.IO.File.CreateText(filePath))
                {
                    text.WriteLine("hello");
                }

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                actions.Add(new HealthCheckStatus("File Permissions OK")
                {
                    ResultType = StatusResultType.Success,
                    Description = "Files can be created and deleted in the uSync folder"
                });

            }
            catch (Exception ex)
            {
                actions.Add(new HealthCheckStatus("File permissions fail")
                {
                    ResultType = StatusResultType.Error,
                    Description = "uSync cannot write to the usync folder: " + ex.Message,
                });
            }

            return actions;

        }

    }
}
