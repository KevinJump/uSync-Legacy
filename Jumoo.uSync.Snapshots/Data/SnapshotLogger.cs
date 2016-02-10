using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Persistence;

namespace Jumoo.uSync.Snapshots.Data
{
    /// <summary>
    /// petapoco classes to store the status of a snapshot
    /// in the site db - this way we can work out what 
    /// has been applied to this site. 
    /// </summary>
    public class SnapshotLogger
    {
        UmbracoDatabase db;
        DateTime MinDateTime = new DateTime(2000, 01, 01);

        public SnapshotLogger()
        {
            db = ApplicationContext.Current.DatabaseContext.Database;
        }

        public IEnumerable<SnapshotLog> GetSnapshots()
        {
            var sql = new Sql()
                .Select("*")
                .From();
            var items = db.Fetch<SnapshotLog>(sql);
            return items;
        }

        public SnapshotLog AddSnapshot(SnapshotInfo info, bool local)
        {
            var folder = Path.GetFileName(info.Folder);
            var log = GetSnapshot(folder);
            if (log == null)
            {
                log = new SnapshotLog()
                {
                    Name = Path.GetFileName(info.Folder),
                    IsLocal = local,
                    Applied = MinDateTime
                };

                db.Insert(log);
            }

            return log;
        }

        public SnapshotLog GetSnapshot(string folderName)
        {
            return db.SingleOrDefault<SnapshotLog>("WHERE name = @0", folderName);
        }

        // updates the applied date on a snapshot.
        public void ApplySnapshot(SnapshotInfo info, bool force = false)
        {
            var log = GetSnapshot(Path.GetFileName(info.Folder));
            if (log == null)
                log = AddSnapshot(info, false);

            if (force || log.Applied == null || log.Applied.Value <= MinDateTime)
            {
                log.Applied = DateTime.Now;
                db.Update(log);
            }
        }

        public void DeleteSnapshot(string folderName)
        {
            var log = GetSnapshot(folderName);
            if (log != null)
            {
                db.Delete(log);
            }
        }
    }
}
