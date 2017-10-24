using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Audit.Persistance;
using Umbraco.Core;

namespace Jumoo.uSync.Audit.EventHandlers
{
    public class AudtiDBLogger : ISyncAuditHandler
    {
        private readonly ApplicationContext _applicationContext;

        private uSyncAuditService _auditService;

        public AudtiDBLogger(ApplicationContext appContext)
        {
            _applicationContext = appContext;
        }

        public bool Activate()
        {

            // db logger is on by default. 
            var configValue = ConfigurationManager.AppSettings["Audit.DBLogger"];
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                bool active;
                if (bool.TryParse(configValue, out active))
                {
                    if (!active)
                    {
                        return false;
                    }
                }
            }

            _auditService = new uSyncAuditService(
                _applicationContext.DatabaseContext,
                _applicationContext.ProfilingLogger.Logger);

            uSyncAudit.Changed += uSyncAudit_Changed;
            return true;
        }

        private void uSyncAudit_Changed(uSyncAudit sender, uSyncChangesEventArgs e)
        {
            _auditService.SaveChangeGroup(e.Changes);
        }
    }
}
