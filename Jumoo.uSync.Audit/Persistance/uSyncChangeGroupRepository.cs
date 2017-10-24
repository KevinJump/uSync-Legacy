using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Audit.Persistance.Model;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Audit.Persistance
{
    internal class uSyncChangeGroupRepository
        : uSyncAuditRepositoryBase<uSyncChangeGroupDTO, uSyncChangeGroup>
    {
        public uSyncChangeGroupRepository(DatabaseContext dbContext, ILogger logger) 
            : base(dbContext, logger, "uSyncAudit_ChangeGroups")
        {
        }
    }
}
