using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Audit.EventHandlers
{
    public class AuditDiskLogger : ISyncAuditHandler
    {
        public AuditDiskLogger(ApplicationContext appContext)
        {
        }

        public bool Activate()
        {
            var diskLoggerValue = ConfigurationManager.AppSettings["Audit.DiskLogger"];

            if (!string.IsNullOrWhiteSpace(diskLoggerValue))
            {
                bool active; 
                if (bool.TryParse(diskLoggerValue, out active)) {
                    
                    if (active)
                    {
                        uSyncAudit.Changed += uSyncAudit_Changed;
                        return true;

                    }
                }
            }

            return false;
        }

        private void uSyncAudit_Changed(uSyncAudit sender, uSyncChangesEventArgs e)
        {
            WriteToDisk(e.Changes);
        }

        private void WriteToDisk(uSyncChangeGroup changes)
        {
            string fileName = string.Format("{0}_{1}.config",
                DateTime.Now.ToString("yyyyMMddHHmmssff"), changes.ItemType);
            var rootPath = IOHelper.MapPath(
                string.Format("~/uSync/AuditLog/{0}/{1}/{2}",
                                DateTime.Now.ToString("yyyy"),
                                DateTime.Now.ToString("MM"),
                                DateTime.Now.ToString("dd")));

            var filePath = Path.Combine(rootPath, fileName);

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                XmlSerializer serializer = new XmlSerializer(typeof(uSyncChangeGroup));
                using (StreamWriter w = new StreamWriter(filePath))
                {
                    serializer.Serialize(w, changes);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn<AuditDiskLogger>("Failed to save maintance mode settings file: {0}", () => ex);
            }
        }
    }
}
