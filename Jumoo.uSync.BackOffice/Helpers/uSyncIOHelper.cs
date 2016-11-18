

namespace Jumoo.uSync.BackOffice.Helpers
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using System.IO;

    using Umbraco.Core;
    using Umbraco.Core.Logging;

    public class uSyncIOHelper
    {
        public static string SavePath(string root, string type, string filePath)
        {
            return Umbraco.Core.IO.IOHelper.MapPath(
                Path.Combine(root, type, filePath + ".config"));
        }

        public static string SavePath(string root, string type, string path, string name)
        {
            return
                Umbraco.Core.IO.IOHelper.MapPath(
                    Path.Combine(root, type, path, name + ".config")
                    );
        }

        public static void SaveNode(XElement node, string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    ArchiveFile(path);

                    // remove
                    // File.Delete(path);
                }

                string folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                LogHelper.Debug<uSyncIOHelper>("Saving XML to Disk: {0}", () => path);

                uSyncEvents.fireSaving(new uSyncEventArgs { fileName = path });

                node.Save(path);

                uSyncEvents.fireSaved(new uSyncEventArgs { fileName = path });
            }
            catch(Exception ex)
            {
                LogHelper.Warn<uSyncEvents>("Failed to save node: ", () => ex.ToString());
            }
        }

        public static void ArchiveFile(string path)
        {
            LogHelper.Debug<uSyncIOHelper>("Archive: {0}", () => path);
            try
            {
                if (!uSyncBackOfficeContext.Instance.Configuration.Settings.ArchiveVersions)
                {
                    DeleteFile(path);
                    return;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                string folder = Path.GetDirectoryName(path);

                var root = uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder();
                var archiveRoot = uSyncBackOfficeContext.Instance.Configuration.Settings.ArchiveFolder;
                string filePath = path.Substring(root.Length);

                var archiveFile = string.Format("{0}\\{1}\\{2}_{3}.config",
                                        Umbraco.Core.IO.IOHelper.MapPath(archiveRoot),
                                        filePath, fileName.ToSafeFileName(),
                                        DateTime.Now.ToString("ddMMyy_HHmmss"));

                if (!Directory.Exists(Path.GetDirectoryName(archiveFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(archiveFile));

                if (File.Exists(path))
                {
                    if (File.Exists(archiveFile))
                        File.Delete(archiveFile);

                    File.Copy(path, archiveFile);

                    // archive does delete. because it is always called before a save, 
                    // calling archive without a save is just like deleting (but saving)
                    DeleteFile(path);
                }
            }
            catch(Exception ex)
            {
                LogHelper.Warn<uSyncEvents>("Failed to Archive the existing file (might be locked?) {0}", () => ex.ToString());
            }
        }

        public static void ArchiveRelativeFile(string type, string path, string name)
        {
            string fullpath = Path.Combine(type, path, name);
            ArchiveRelativeFile(fullpath);
        }

        public static void ArchiveRelativeFile(string path, string name)
        {
            string fullPath = Path.Combine(path, name);
            ArchiveRelativeFile(fullPath);
        }

        public static void ArchiveRelativeFile(string fullPath)
        {
            var uSyncFolder = uSyncBackOfficeContext.Instance.Configuration.Settings.Folder;
            var fullFolder = Path.Combine(uSyncFolder, fullPath + ".config");

            ArchiveFile(Umbraco.Core.IO.IOHelper.MapPath(fullFolder));
        }

        internal static void DeleteFile(string file)
        {
            uSyncEvents.fireDeleting(new uSyncEventArgs { fileName = file });

            if (!uSyncBackOfficeContext.Instance.Configuration.Settings.PreserveAllFiles)
            {

                var dir = Path.GetDirectoryName(file);
                if (Directory.Exists(dir))
                {
                    if (File.Exists(file))
                        File.Delete(file);

                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
            }
            else
            {
                // we don't actually delete files - we just make them usync archive files. 
                // that way they still exist in deployment 
                if (File.Exists(file))
                    MakeFileArchived(file, Path.GetFileNameWithoutExtension(file));
            }

            uSyncEvents.fireDeleted(new uSyncEventArgs { fileName = file });
        }

        private static void ClenseArchiveFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                int versions = uSyncBackOfficeContext.Instance.Configuration.Settings.MaxArchiveVersionCount;

                DirectoryInfo dir = new DirectoryInfo(folder);
                FileInfo[] fileList = dir.GetFiles("*.config");
                var files = fileList.OrderByDescending(f => f.CreationTime);

                foreach (var file in files.Skip(versions))
                {
                    file.Delete();
                }
            }
        }


        public static string GetShortGuidPath(Guid guid)
        {
            string encoded = Convert.ToBase64String(guid.ToByteArray());
            encoded = encoded
              .Replace("/", "_")
              .Replace("+", "-");
            return encoded.Substring(0, 22);
        }

        public static void MakeFileArchived(string path, string name)
        {
            XElement node = new XElement("uSyncArchive", new XAttribute("name", name));
            node.Save(path);
        }
    }
}
