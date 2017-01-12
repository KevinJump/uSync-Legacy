using Jumoo.uSync.Snapshots.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Snapshots
{
    public class SnapshotInfo
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime Applied { get; set; }
        public bool Local { get; set; }
        public string Folder { get; set; }

        public int FileCount { get; set; }

        public List<string> Items { get; set; }

        public SnapshotInfo(string folder, bool local=false)
        {
            Folder = folder;

            Name = folder.Substring(folder.LastIndexOf('_') + 1);

            var dateBit = folder.Substring(
                            folder.LastIndexOf('\\') + 1,
                            folder.LastIndexOf('_') - folder.LastIndexOf('\\') - 1);


            DateTime when;
            if (DateTime.TryParseExact(dateBit, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out when))
            {
                Created = when;
            }

            FileCount = CountFiles(folder);
            Items = new List<string>();

            Local = local;

            SnapshotLogger logger = new SnapshotLogger();
            var log = logger.GetSnapshot(Path.GetFileName(folder));
            if (log != null)
            {
                Local = log.IsLocal;
                if (log.Applied != null && log.Applied > DateTime.MinValue)
                {
                    Applied = log.Applied.Value;
                }
            }
            else {
                logger.AddSnapshot(this, local);
            }
        }

        private int CountFiles(string folder)
        {
            int count = 0;
            if (Directory.Exists(folder))
            {
                count = Directory.GetFiles(folder).Count();
                foreach(var dir in Directory.GetDirectories(folder))
                {
                    count += CountFiles(dir);
                }
            }

            return count;
        }
    }
}
