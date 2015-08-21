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
            PerformImport(false);
        }

        protected void btnReport_Click(object sender, EventArgs e)
        {
            ShowResultHeader("Change Report", "Below shows what will happen if you run an import now");

            var actions = uSyncBackOfficeContext.Instance.ImportReport(
                uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder());

            if (actions.Any())
            {
                uSyncStatus.DataSource = actions;
                uSyncStatus.DataBind();
            }

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
                uSyncStatus.DataSource = actions;
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