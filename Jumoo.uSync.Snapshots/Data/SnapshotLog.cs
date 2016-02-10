using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Jumoo.uSync.Snapshots.Data
{
    [TableName("uSyncSnapshotAudit")]
    [PrimaryKey("id", autoIncrement = true)]
    public class SnapshotLog
    {
        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement =true)]
        public int SnapshotId { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Applied")]
        public DateTime? Applied { get; set; }

        [Column("IsLocal")]
        public bool IsLocal { get; set; }
    }
}
