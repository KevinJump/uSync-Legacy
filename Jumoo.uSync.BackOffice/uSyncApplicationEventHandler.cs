

namespace Jumoo.uSync.BackOffice
{
    using System.IO;
    using System.Diagnostics;

    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using System;

    public class uSyncApplicationEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            LogHelper.Info<uSyncApplicationEventHandler>("Initializing uSync 73");

            var onUaaS = AppDomain.CurrentDomain.GetAssemblies()
                            .Any(a => a.FullName.StartsWith("Concorde.Messaging.Web"));
            if (onUaaS)
            {
                LogHelper.Warn<uSyncApplicationEventHandler>("uSync doesn't run on UaaS, so it will just stop now");
                return;
            }



            if (!ApplicationContext.Current.IsConfigured)
            {
                // install and upgrade block, we don't run if umbraco hasn't been configured 
                LogHelper.Warn<uSyncApplicationEventHandler>("Umbraco isn't configured - uSync aborting");
                return;
            }

            // this version of usync only runs on umbraco 7.3+ 
            //
            var version = Umbraco.Core.Configuration.UmbracoVersion.Current;
            if (version.Major > 7 || (version.Major == 7 && version.Minor >= 3))
            {
                Setup();
            }
            else
            {
                LogHelper.Warn<uSyncApplicationEventHandler>("This version of uSync isn't compatible with this version of Umbraco.");
            }
        }

        private void Setup()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            LogHelper.Info<uSyncApplicationEventHandler>("Firing up uSync");

            // just to make the code readable...
            var uSyncBackOffice = uSyncBackOfficeContext.Instance;
            uSyncBackOffice.Init();

            var settings = uSyncBackOffice.Configuration.Settings;

            uSyncEvents.fireStarting(new uSyncEventArgs());

            List<uSyncAction> setupActions = new List<uSyncAction>();

            // some settings based decisions...
            if (settings.Import)
            {
                setupActions.AddRange(uSyncBackOffice.ImportAll());
            }

            if (settings.ExportAtStartup ||
                (settings.ExportOnSave && !Directory.Exists(settings.MappedFolder())))
            {
                setupActions.AddRange(uSyncBackOffice.ExportAll());
            }

            if (settings.ExportOnSave)
            {
                uSyncBackOffice.SetupEvents();
            }

            if (settings.WatchForFileChanges)
            {
                uSyncFileWatcher.Init(settings.MappedFolder());
            }

            uSyncEvents.fireInitilized(new uSyncEventArgs());

            uSyncActionLogger.LogActions(setupActions);

            // we write a log - when there have been changes, a zero run doesn't get
            // a file written to disk.
            if ( setupActions.Any(x => x.Change > ChangeType.NoChange))
                uSyncActionLogger.SaveActionLog("Startup", setupActions);


            sw.Stop();
            LogHelper.Info<uSyncApplicationEventHandler>("uSync Complete ({0}ms)", () => sw.ElapsedMilliseconds);
        }


      
    }
}
