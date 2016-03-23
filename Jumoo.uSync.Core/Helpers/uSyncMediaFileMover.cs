using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Jumoo.uSync.Core.Interfaces;

using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using System.Xml.Linq;

namespace Jumoo.uSync.Core.Helpers
{
    public class uSyncMediaFileMover : ISyncFileHander2<IMedia>
    {

        [Obsolete("use ImportFileValue(XElement, IMedia, string")]
        public bool ImportFile(IMedia item, string folder)
        {
            return false; 
        }

        public bool ImportFileValue(XElement node, IMedia item, string folder)
        {
            // 
            // if we have move media = false, we don't actually move 
            // the media file, we just let the user move the Media folder
            // so the internal umbracoFile values will be fine ?
            //
            if (!uSyncCoreContext.Instance.Configuration.Settings.MoveMedia) {
                LogHelper.Debug<uSyncMediaFileMover>("Media moving is off - media file not being moved");
                return true;
            }

            LogHelper.Debug<uSyncMediaFileMover>("\n--------------------- FILE MOVE ---------------");


            bool changes = false; 
            Guid guid = item.Key;

            if (!Directory.Exists(folder))
                return false;

            if (!item.HasProperty("umbracoFile"))
                return false;

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

            var umbracoFileValue = "";
            if (node.Element("umbracoFile") != null)
            {
                umbracoFileValue = node.Element("umbracoFile").Value;
            }

            foreach(var file in Directory.GetFiles(folder, "*.*"))
            {
                if (currentFile != null)
                {
                    // compare current...
                    if (!FilesAreEqual(currentFile, new FileInfo(file)))
                    {
                        string sourceFile = Path.GetFileName(file);

                        using (FileStream s = new FileStream(file, FileMode.Open))
                        {
                            item.SetValue("umbracoFile", sourceFile, s);
                            changes = true;
                        }

                        // if we've created a new file in umbraco, it will be in a new folder
                        // and the old current file will need to be deleted.
                        if (Directory.Exists(currentFile.DirectoryName))
                           
                                Directory.Delete(currentFile.DirectoryName, true);
                           
                    }
                }
                else
                {
                    // this is a new file.
                    using (FileStream s = new FileStream(file, FileMode.Open))
                    {
                        item.SetValue("umbracoFile", Path.GetFileName(file), s);
                        changes = true;
                    }

                }
            }

            if (changes)
            {
                // if we are using image cropper then umbracoFile value will have been blasted a bit by the upload
                // we need to set it back here...
                // var newUmbracoFileValue = item.GetValue<string>("umbracoFile");

                if (IsJson(umbracoFileValue))
                {
                    var newUmbracoFileValue = item.GetValue<string>("umbracoFile");

                    var oldObj = JsonConvert.DeserializeObject<dynamic>(umbracoFileValue);
                    var newSrc = newUmbracoFileValue;
                    if (IsJson(newUmbracoFileValue))
                    {
                        newSrc = JsonConvert.DeserializeObject<dynamic>(newUmbracoFileValue).src;
                    }
                    oldObj.src = newSrc;

                    var fileVal = JsonConvert.SerializeObject(oldObj);
                    LogHelper.Debug<uSyncMediaFileMover>("JSON Value: {0}", ()=> fileVal);
                    IContentBase baseItem = (IContentBase)item;
                    baseItem.SetValue("umbracoFile", fileVal );
                }

                ApplicationContext.Current.Services.MediaService.Save(item);
            }
            
            return changes;
        }


        public bool ExportFile(IMedia item, string folder)
        {
            if (!uSyncCoreContext.Instance.Configuration.Settings.MoveMedia)
            {
                LogHelper.Debug<uSyncMediaFileMover>("Media moving is off - media file not being moved");
                return true;
            }

            foreach (var fileProperty in item.Properties.Where(p => p.Alias == "umbracoFile"))
            {
                if (fileProperty == null || fileProperty.Value == null)
                    continue;

                var umbracoFile = fileProperty.Value.ToString();

                var filePath = umbracoFile;
                if (IsJson(umbracoFile))
                {
                    filePath = JsonConvert.DeserializeObject<dynamic>(umbracoFile).src;
                }

                string uSyncFolder = folder; 
                string uSyncFile = Path.Combine(uSyncFolder, Path.GetFileName(filePath));
                string sourceFile = IOHelper.MapPath(string.Format("~{0}", filePath));

                if (System.IO.File.Exists(sourceFile))
                {
                    if (!Directory.Exists(uSyncFolder))
                        Directory.CreateDirectory(uSyncFolder);

                    System.IO.File.Copy(sourceFile, uSyncFile, true);
                }
            }
            return true;
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
