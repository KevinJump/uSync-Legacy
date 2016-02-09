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
        public string Folder { get; set; }

        public int FileCount { get; set; }

        public List<string> Items { get; set; }

        public SnapshotInfo(string folder)
        {
            Folder = folder;

            Name = folder.Substring(folder.LastIndexOf('_') + 1);

            var dateBit = folder.Substring(
                            folder.LastIndexOf('\\') + 1,
                            folder.LastIndexOf('_') - folder.LastIndexOf('\\') - 1);

            Name = Name; // + " [" + dateBit + "]";

            DateTime when;
            if (DateTime.TryParseExact(dateBit, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out when))
            {
                Created = when;
            }

            FileCount = CountFiles(folder);
            Items = new List<string>();
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
