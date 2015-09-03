using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace Jumoo.uSync.Migrations
{
    public class MigrationInfo
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public string Path { get; set; }
        public int FileCount { get; set; }

        public MigrationInfo(string path)
        {
            Path = path;

            Name = path.Substring(path.LastIndexOf('_') + 1);

            var dateBit = path.Substring(
                            path.LastIndexOf('\\') + 1,
                            path.LastIndexOf('_') - path.LastIndexOf('\\')-1);

            Name = Name; // + " [" + dateBit + "]";

            DateTime when;
            if (DateTime.TryParseExact(dateBit, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out when))
            {
                Time = when;
            }

            FileCount = CountFiles(path);
        }

        private int CountFiles(string folder)
        {
            int count = 0;
            if (Directory.Exists(folder))
            {
                count = Directory.GetFiles(folder).Count();

                foreach (var dir in Directory.GetDirectories(folder))
                {
                    count += CountFiles(dir);
                }
            }
            return count;
        }

    }
}