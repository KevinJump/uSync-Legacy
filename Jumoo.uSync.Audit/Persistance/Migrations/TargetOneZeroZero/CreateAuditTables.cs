using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Audit.Persistance.Model;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Jumoo.uSync.Audit.Persistance.Migrations.TargetOneZeroZero
{
    [Migration("0.0.1", 1, "uSyncAudits")]
    public class CreateAuditTables : MigrationBase
    {
        public CreateAuditTables(ISqlSyntaxProvider sqlSyntax, ILogger logger) 
            : base(sqlSyntax, logger)
        {
        }

        public override void Down()
        {
            // 
        }

        public override void Up()
        {
            var tables = SqlSyntax.GetTablesInSchema(Context.Database).ToArray();

            if (!tables.InvariantContains("uSyncAudit_ChangeGroups"))
                Create.Table<uSyncChangeGroupDTO>();

            if (!tables.InvariantContains("uSyncAudit_ChangeItems"))
                Create.Table<uSyncItemChangesDTO>();

        }
    }
}
