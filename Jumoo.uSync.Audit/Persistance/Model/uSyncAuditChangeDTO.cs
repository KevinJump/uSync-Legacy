using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;

namespace Jumoo.uSync.Audit.Persistance.Model
{
    [TableName("uSyncAudit_ChangeGroups")]
    [PrimaryKey("id")]
    [ExplicitColumns]
    public class uSyncChangeGroupDTO
    {
        [Column("id")]
        [PrimaryKeyColumn]
        public int Id { get; set; }

        [Column("userId")]
        public string UserId { get; set; }

        [Column("userName")]
        public string UserName { get; set; }

        [Column("changeTime")]
        [Constraint(Default = SystemMethods.CurrentDateTime)]

        public DateTime ChangeTime { get; set; }

        [Column("itemType")]
        public string ItemType { get; set; }
    }

    [TableName("uSyncAudit_ChangeItems")]
    [PrimaryKey("id")]
    [ExplicitColumns]
    public class uSyncItemChangesDTO
    {
        [Column("id")]
        [PrimaryKeyColumn]
        public int Id { get; set; }

        [Column("changeGroupId")]
        public int ChangeGroupId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("key")]
        public Guid Key { get; set; }

        [Column("source")]
        public string Source { get; set; }

        [Column("changes")]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Changes { get; set; }
    }
}
