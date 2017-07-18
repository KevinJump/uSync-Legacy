﻿

namespace Jumoo.uSync.BackOffice {
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Umbraco.Core;
    using Umbraco.Core.IO;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Services;

    public class uSyncApplicationEventHandler : ApplicationEventHandler {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext) {
            LogHelper.Info<uSyncApplicationEventHandler>("Initializing uSync 73");

            var onUaaS = AppDomain.CurrentDomain.GetAssemblies()
                            .Any(a => a.FullName.StartsWith("Concorde.Messaging.Web"));
            if (onUaaS) {
                LogHelper.Warn<uSyncApplicationEventHandler>("uSync doesn't run on UaaS, so it will just stop now");
                return;
            }



            if (!ApplicationContext.Current.IsConfigured) {
                // install and upgrade block, we don't run if umbraco hasn't been configured 
                LogHelper.Warn<uSyncApplicationEventHandler>("Umbraco isn't configured - uSync aborting");
                return;
            }

            // this version of usync only runs on umbraco 7.3+ 
            //
            var version = Umbraco.Core.Configuration.UmbracoVersion.Current;
            if (version.Major > 7 || (version.Major == 7 && version.Minor >= 3)) {
                Setup();
            } else {
                LogHelper.Warn<uSyncApplicationEventHandler>("This version of uSync isn't compatible with this version of Umbraco.");
            }
        }

        private void Setup() {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            LogHelper.Info<uSyncApplicationEventHandler>("Firing up uSync");

            FileService.SavedTemplate += FileService_SavedTemplate;

            // just to make the code readable...
            var uSyncBackOffice = uSyncBackOfficeContext.Instance;
            uSyncBackOffice.Init();

            var settings = uSyncBackOffice.Configuration.Settings;

            try {
                uSyncEvents.fireStarting(new uSyncEventArgs());

                List<uSyncAction> setupActions = new List<uSyncAction>();

                // some settings based decisions...
                if (settings.Import) {
                    setupActions.AddRange(uSyncBackOffice.ImportAll());
                }

                if (settings.ExportAtStartup ||
                    (settings.ExportOnSave && !Directory.Exists(settings.MappedFolder()))) {
                    setupActions.AddRange(uSyncBackOffice.ExportAll());
                }

                if (settings.ExportOnSave) {
                    uSyncBackOffice.SetupEvents();
                }

                if (settings.WatchForFileChanges) {
                    uSyncFileWatcher.Init(settings.MappedFolder());
                }

                uSyncEvents.fireInitilized(new uSyncEventArgs());

                uSyncActionLogger.LogActions(setupActions);

                // we write a log - when there have been changes, a zero run doesn't get
                // a file written to disk.
                if (setupActions.Any(x => x.Change > ChangeType.NoChange))
                    uSyncActionLogger.SaveActionLog("Startup", setupActions);


                sw.Stop();
                LogHelper.Info<uSyncApplicationEventHandler>("uSync Complete ({0}ms)", () => sw.ElapsedMilliseconds);
            } catch (Exception ex) {

                if (settings.DontThrowErrors) {
                    LogHelper.Info<uSyncApplicationEventHandler>("No Throw errors is set, so uSync won't YSOD");
                    LogHelper.Error<uSyncApplicationEventHandler>("Error During Setup:", ex);
                } else {
                    LogHelper.Warn<uSyncApplicationEventHandler>("Errors during Sync: {0} {1}", () => ex.Message, () => ex.Source);
                    LogHelper.Warn<uSyncApplicationEventHandler>("Errors during Sync:\n {0}", () => ex.StackTrace);
                    throw new ApplicationException(
                        "uSync encounted an error during setup (which is probibily from an import)\n" +
                        "     You can set DontThrowErrors = True in the uSyncBackOffice.Config file and umbraco will not throw the YSOD (usync will just log errors in the log file)", ex);
                }
            }
        }

        private void FileService_SavedTemplate(IFileService sender, global::Umbraco.Core.Events.SaveEventArgs<global::Umbraco.Core.Models.ITemplate> e) {
            foreach (var template in e.SavedEntities) {
                String foundTemplate = FindTemplate(template.Alias);
                String GeneratedTemplate = IOHelper.MapPath(string.Format(SystemDirectories.MvcViews + "/{0}.cshtml", template.Alias.ToSafeFileName()));
                if (!String.IsNullOrEmpty(foundTemplate) && File.ReadAllText(foundTemplate).Equals(template.Content) && File.Exists(GeneratedTemplate) && File.ReadAllText(foundTemplate).Equals(File.ReadAllText(GeneratedTemplate))) {
                    File.Delete(GeneratedTemplate);
                }
            }
        }

        private String FindTemplate(String alias) {
            var templatePath = "";

            if (!File.Exists(templatePath)) {
                var viewsPath = IOHelper.MapPath(SystemDirectories.MvcViews);
                var directories = Directory.GetDirectories(viewsPath);

                foreach (var directory in directories.Where(x => !x.ToLower().Contains("partials"))) {
                    var folder = Path.GetFileName(directory);
                    String relativeFileUrl = string.Format(SystemDirectories.MvcViews + "/{0}/{1}.cshtml", folder, alias.ToSafeFileName());
                    if (File.Exists(IOHelper.MapPath(relativeFileUrl))) {
                        templatePath = IOHelper.MapPath(relativeFileUrl);
                    }
                }
            }

            return templatePath;
        }

    }
}
