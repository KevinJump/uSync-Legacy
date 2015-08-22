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

        protected string ResultIcon(object result)
        {
            var r = (bool)result;

            if (r)
                return "<i class=\"icon-checkbox\"></i>";
            else
                return "<i class=\"icon-checkbox-dotted\"></i>";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                SetupPage();
                WriteSettings();
            }

            
        }

        private void WriteSettings()
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;

            rbAutoSync.Checked = false;
            rbTarget.Checked = false;
            rbManual.Checked = false;
            rbOther.Checked = false; 

            if (settings.Import == true)
            {
                if (settings.ExportOnSave == true)
                {
                    rbAutoSync.Checked = true;
                }
                else
                {
                    rbTarget.Checked = true;
                }
            }
            else if (settings.ExportOnSave == false && settings.ExportAtStartup == false)
            {
                rbManual.Checked = true;
            }
            else
            {
                rbOther.Checked = true;
            }

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

            usyncFolder.Text = settings.Folder;
        }

        protected void btnFullImport_Click(object sender, EventArgs e)
        {
            PerformImport(true);
        }

        protected void btnFullExport_Click(object sender, EventArgs e)
        {
            // events shoudn't fire when you export, 
            // but we are pausing just incase.
            uSyncEvents.Paused = true;

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
            uSyncEvents.Paused = false;
        }

        protected void btnSaveSettings_Click(object sender, EventArgs e)
        {
            var settings = uSyncBackOfficeContext.Instance.Configuration.Settings;

            var mode = "no change";
            if (rbAutoSync.Checked == true)
            {
                settings.ExportOnSave = true;
                settings.Import = true;
                settings.ExportAtStartup = false;

                mode = "AutoSync";

            }
            else if (rbTarget.Checked == true)
            {
                settings.ExportOnSave = false;
                settings.Import = true;
                settings.ExportAtStartup = false;

                mode = "Sync Target";
            }
            else if (rbManual.Checked == true)
            {
                settings.ExportOnSave =false;
                settings.Import = false;
                settings.ExportAtStartup = false;

                mode = "Manual";

            }
            uSyncBackOfficeContext.Instance.Configuration.SaveSettings(settings);

            ShowResultHeader("Settings Updated",
                string.Format("Mode = {0} (requires a restart to take effect)", mode));

            WriteSettings();

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
                changeMessage = string.Format("if you ran an import now: {0} items would be processed and {1} changes would be made",
                    actions.Count(), actions.Count(x => x.Change > Core.ChangeType.NoChange));

                uSyncStatus.DataSource = actions.Where(x => x.Change > Core.ChangeType.NoChange);
                uSyncStatus.DataBind();
            }

            ShowResultHeader("Change Report", changeMessage);
        }

        private void Backup()
        {
            uSyncEvents.Paused = true;

            var backupFolder = string.Format("~/app_data/uSync/Backups/{0}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            if (System.IO.Directory.Exists(backupFolder))
                System.IO.Directory.Delete(backupFolder, true);

            uSyncBackOfficeContext.Instance.ExportAll(backupFolder);

            uSyncEvents.Paused = false;


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