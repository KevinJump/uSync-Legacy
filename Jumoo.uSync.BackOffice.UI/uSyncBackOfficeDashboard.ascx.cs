using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Jumoo.uSync.BackOffice.UI
{
    public partial class uSyncBackOfficeDashboard : System.Web.UI.UserControl
    {
        protected string TypeString(object type)
        {
            var typeName = type.ToString();
            return typeName.Substring(typeName.LastIndexOf('.')+1);
        }

        protected string ChangeClass(object change)
        {
            var changeType = (Core.ChangeType)change;

            if (changeType > Core.ChangeType.Fail)
                return "error";
            return "";
        }

        protected string ResultIcon(object result)
        {
            var r = (bool)result;

            if (r)
                return "<i class=\"icon-checkbox\"></i>";
            else
                return "<i class=\"icon-checkbox-empty\"></i>";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if ( !IsPostBack )
                SetupPage();

            WriteSettings();
        }

        private void WriteSettings()
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;

            chkExport.Checked = settings.ExportAtStartup;
            chkImport.Checked = settings.Import;
            chkEvents.Checked = settings.ExportOnSave;
            chkFiles.Checked = settings.WatchForFileChanges;

        }

        private void SetupPage()
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;


            uSyncVersionNumber.Text = uSyncBackOfficeContext.Instance.Version;
            uSyncCoreVersion.Text = Jumoo.uSync.Core.uSyncCoreContext.Instance.Version;

            var handlers = uSyncBackOfficeContext.Instance.Handlers;
            uSyncHandlerCount.Text = handlers.Count.ToString(); ;

            foreach (var handler in handlers)
            {
                var handlerConfig = settings.Handlers.Where(x => x.Name == handler.Name)
                    .FirstOrDefault();

                string enabledText = " (enabled) ";

                if (handlerConfig != null && !handlerConfig.Enabled)
                    enabledText = " (disabled) ";

                var item = new ListItem(handler.Name + enabledText);
                uSyncHandlers.Items.Add(item);
            }


            uSyncOtherSettings.Items.Add(string.Format("Save Location:   {0}", settings.Folder));
            uSyncOtherSettings.Items.Add(string.Format("Archive on Save: {0}", settings.ArchiveVersions));

            usyncFolder.Text = settings.Folder;
            usyncFolder1.Text = settings.Folder;
            uSyncFolder2.Text = settings.Folder;
        }

        protected void btnFullImport_Click(object sender, EventArgs e)
        {
            PerformImport(true);
        }

        protected void btnFullExport_Click(object sender, EventArgs e)
        {
            var folder = uSyncBackOfficeContext.Instance.Configuration.Settings.Folder;
            if (System.IO.Directory.Exists(folder))
                System.IO.Directory.Delete(folder, true);

            var actions = uSyncBackOfficeContext.Instance.ExportAll(folder);
            if (actions.Any())
            {
                uSyncStatus.DataSource = actions;
                uSyncStatus.DataBind();
            }

            ShowResultHeader("Export", "All items have been exported");
        }

        protected void btnSaveSettings_Click(object sender, EventArgs e)
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;

            settings.ExportAtStartup = chkExport.Checked;
            settings.ExportOnSave = chkEvents.Checked;
            settings.WatchForFileChanges = chkFiles.Checked;
            settings.Import = chkImport.Checked;

            uSyncBackOfficeContext.Instance.Configuration.SaveSettings(settings);

        }

        protected void btnSyncImport_Click(object sender, EventArgs e)
        {
            Backup();
            PerformImport(false);
        }

        protected void btnReport_Click(object sender, EventArgs e)
        {
            var changeMessage = "These are the changes if you ran an import now";
            var actions = uSyncBackOfficeContext.Instance.ImportReport(
                uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder());

            if (actions.Any())
            {
                changeMessage = string.Format("if you ran an import now their would be {0} items processed and {1} changes made",
                    actions.Count(), actions.Count(x => x.Change > Core.ChangeType.NoChange));

                uSyncStatus.DataSource = actions.Where(x => x.Change > Core.ChangeType.NoChange);
                uSyncStatus.DataBind();
            }

            ShowResultHeader("Change Report", changeMessage);
        }

        private void Backup()
        {
            var backupFolder = string.Format("~/app_data/uSync/Backups/{0}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            if (System.IO.Directory.Exists(backupFolder))
                System.IO.Directory.Delete(backupFolder, true);

            uSyncBackOfficeContext.Instance.ExportAll(backupFolder);


        }
        private void PerformImport(bool force)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            uSyncEvents.Paused = true;
            var actions = uSyncBackOfficeContext.Instance.ImportAll(uSyncBackOfficeContext.Instance.Configuration.Settings.Folder, force);
            uSyncEvents.Paused = false;

            sw.Stop();

            ShowResultHeader("Import processed", string.Format("uSync Import Complete: ({0}ms) processed {1} items and made {2} changes",
                    sw.ElapsedMilliseconds, actions.Count(), actions.Where(x => x.Change > Core.ChangeType.NoChange).Count()));

            if (actions.Any())
            {
                uSyncStatus.DataSource = actions.Where(x => x.Change > Core.ChangeType.NoChange);
                uSyncStatus.DataBind();
            }

        }

        private void ShowResultHeader(string title, string message)
        {
            uSyncResultPlaceHolder.Visible = true;
            resultHeader.Text = title;
            resultStatus.Text = message;
        }
    }
}