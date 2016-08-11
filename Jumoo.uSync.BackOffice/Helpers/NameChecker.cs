using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;

using Umbraco.Core.Logging;

namespace Jumoo.uSync.BackOffice.Helpers
{
    /// <summary>
    ///  this is our technical debt for choosing to store things on the disk
    ///  with logical names, when things get renamed, we run the risk of 
    ///  ending up with multiple files. 
    /// 
    ///  this class manages the renames - by searching the folder(s) for the key
    ///  of a recently saved item, if it finds that key somewhere else. then 
    ///  it removes the rouge file. it also handels the fact that child folders
    ///  might need moving. 
    /// </summary>
    public class NameChecker
    {
        public static void ManageOrphanFiles(string typeFolder, Guid Key, string newFile)
        {
            string path = Path.Combine(uSyncBackOfficeContext.Instance.Configuration.Settings.MappedFolder(), typeFolder);
            CheckFolder(path, Key, newFile);
        }

        private static void CheckFolder(string folder, Guid Key, string newFile)
        {
            LogHelper.Debug<NameChecker>("Checking Folder: {0}", () => folder);

            if (!Directory.Exists(folder))
                return;

            foreach(var file in Directory.GetFiles(folder, "*.config"))
            {
                if (!file.Equals(newFile, StringComparison.OrdinalIgnoreCase))
                {
                    var fileKey = GetKey(file);
                    if (fileKey != Guid.Empty && fileKey == Key)
                    {
                        // this file matches our new one, we need to do all the things.
                        ManageOrphan(file, newFile);
                    }
                }
            }


            // it is possible (or ineed likely) that if we find the file, ManageOrphan
            // might have deleted the folder.
            if (Directory.Exists(folder))
            {
                foreach (var directory in Directory.GetDirectories(folder))
                {
                    CheckFolder(directory, Key, newFile);
                }
            }
        }


        private static Guid GetKey(string file)
        {
            if (!File.Exists(file))
                return Guid.Empty;

            XElement node = XElement.Load(file);
            if (node == null)
                return Guid.Empty;


            var fileKey = node.Element("Key").ValueOrDefault(Guid.Empty);

            if (fileKey == Guid.Empty && node.Element("Info") != null)
                fileKey = node.Element("Info").Element("Key").ValueOrDefault(Guid.Empty);

            if (fileKey == Guid.Empty)
                fileKey = node.Attribute("Key").ValueOrDefault(Guid.Empty);

            if (fileKey == Guid.Empty)
                fileKey = node.Attribute("guid").ValueOrDefault(Guid.Empty);

            return fileKey;
        }


        /// <summary>
        ///  deletes the orphan file, and moves any subfolders to their new place. 
        ///  then it deletes the parent folder if it's empty.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newFile"></param>
        private static void ManageOrphan(string file, string newFile)
        {
            LogHelper.Info<NameChecker>("Managing Orphaned File: {0}", () => file);

            var orphanDir = Path.GetDirectoryName(file);
            var targetDir = Path.GetDirectoryName(newFile);

            // move any child folders
            if (Directory.Exists(orphanDir) && Directory.Exists(targetDir))
            {
                foreach (var subDir in Directory.GetDirectories(orphanDir))
                {
                    var targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                    Directory.Move(subDir, targetSubDir);
                }
            }

            // delete the file
            if (File.Exists(file))
                File.Delete(file);


            // redirectcheck
            var redirect = Path.Combine(Path.GetDirectoryName(file), "redirect.config");
            LogHelper.Debug<NameChecker>("Checking for Redirect: {0}", () => redirect);
            if (File.Exists(redirect))
                File.Delete(redirect);



            // delete if empty
            var folder = new DirectoryInfo(orphanDir);
            if (folder.GetFileSystemInfos().Length == 0)
            {
                folder.Delete();
            }
        }
    }
}
