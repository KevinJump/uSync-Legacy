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
            Name = Path.GetFileName(folder);
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
