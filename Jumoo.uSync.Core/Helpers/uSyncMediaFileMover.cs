using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core.Models;
using Umbraco.Core.IO;
using Newtonsoft.Json;

namespace Jumoo.uSync.Core.Helpers
{
    public class uSyncMediaFileMover
    {
        private string _mediaFolder;

        public uSyncMediaFileMover(string folder)
        {
            _mediaFolder = folder;
        }

        public void ImportFile(Guid guid, IMedia item)
        {
            string sourceFolder = string.Format("{0}\\{1}", _mediaFolder, guid.ToString());

            if (!Directory.Exists(sourceFolder))
                return;

            if (!item.HasProperty("umbracoFile"))
                return;

            FileInfo currentFile = null;

            var filePath = item.GetValue<string>("umbracoFile");

            if (!string.IsNullOrEmpty(filePath))
            {
                if (IsJson(filePath))
                {
                    filePath = JsonConvert.DeserializeObject<dynamic>(filePath).src;
                }

                if (filePath.StartsWith("/media/")) // safety catch - we only do media
                {
                    string fullPath = IOHelper.MapPath(string.Format("~{0}", filePath));
                    if (System.IO.File.Exists(fullPath))
                    {
                        currentFile = new FileInfo(fullPath);
                    }

                }
            }

            foreach(var file in Directory.GetFiles(sourceFolder, "*.*"))
            {
                if (currentFile != null)
                {
                    // compare current...
                    if (!FilesAreEqual(currentFile, new FileInfo(file)))
                    {
                        string sourceFile = Path.GetFileName(file);

                        using (FileStream s = new FileStream(sourceFile, FileMode.Open))
                        {
                            item.SetValue("umbracoFile", sourceFile, s);
                        }

                        // if we've created a new file in umbraco, it will be in a new folder
                        // and the old current file will need to be deleted.
                        if (Directory.Exists(currentFile.DirectoryName))
                            Directory.Delete(currentFile.DirectoryName);
                    }
                }
                else
                {
                    // this is a new file.
                    using (FileStream s = new FileStream(file, FileMode.Open))
                    {
                        item.SetValue("umbracoFile", Path.GetFileName(file), s);
                    }

                }
            }
        }


        public void ExportMediaFile(string umbracoFile, Guid guid)
        {
            var filePath = umbracoFile;
            if (IsJson(umbracoFile))
            {
                filePath = JsonConvert.DeserializeObject<dynamic>(umbracoFile).src;
            }

            string uSyncFolder = Path.Combine(_mediaFolder, guid.ToString());
            string uSyncFile = Path.Combine(uSyncFolder, Path.GetFileName(filePath));
            string sourceFile = IOHelper.MapPath(string.Format("~{0}", filePath));

            if (System.IO.File.Exists(sourceFile))
            {
                if (!Directory.Exists(uSyncFolder))
                    Directory.CreateDirectory(uSyncFolder);

                System.IO.File.Copy(sourceFile, uSyncFile, true);
            }
        }


        private bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}"))
                || (input.StartsWith("[") && input.EndsWith("]"));
        }

        const int BYTES_TO_READ = sizeof(Int64);

        private bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }
            return true;
        }

    }
}
