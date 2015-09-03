using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Migrations.Helpers
{
    /// <summary>
    ///  somewhere to put all the little IO things we do
    ///  
    ///  mostly this is around merging 
    ///  and comparing folders
    /// </summary>
    public class SnapshotIO
    {

        public static void MergeFolder(string target, string source)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            FileInfo[] files = sourceDir.GetFiles();
            foreach(var file in files)
            {
                var targetFile = Path.Combine(target, file.Name);
                file.CopyTo(targetFile, true);
            }

            foreach(var subFolder in sourceDir.GetDirectories())
            {
                string targetPath = Path.Combine(target, subFolder.Name);
                MergeFolder(targetPath, subFolder.FullName);
            }
        }

        public static void RemoveDuplicates(string target, string source)
        {
            DirectoryInfo targetDir = new DirectoryInfo(target);
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            var targetList = targetDir.GetFiles("*.*", SearchOption.AllDirectories);
            var sourceList = sourceDir.GetFiles("*.*", SearchOption.AllDirectories);

            FileCompare fileCompare = new FileCompare();

            List<string> duplicates = new List<string>();

            var matches = targetList.Intersect(sourceList, fileCompare);

            if (matches.Any())
            {
                foreach(var file in matches)
                {
                    duplicates.Add(file.FullName);
                }
            }

            if (duplicates.Any())
            {
                foreach(var file in duplicates)
                {
                    File.Delete(file);
                }
            }

            RemoveEmptyDirectories(target);

        }


        public static void RemoveEmptyDirectories(string folder)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);

            if (dir.GetDirectories().Any())
            {
                foreach(var subFolder in dir.GetDirectories())
                {
                    RemoveEmptyDirectories(subFolder.FullName);
                }
            }

            dir.Refresh();

            if(!dir.GetFileSystemInfos().Any())
            {
                dir.Delete();
            }
        }
    }


    class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals(FileInfo x, FileInfo y)
        {
            if (x.Name == y.Name && x.Length == y.Length)
            {
                var xhash = GetFileHash(x.FullName, x.Name);
                var yhash = GetFileHash(y.FullName, y.Name);
                return xhash.Equals(yhash);
            }
            return false;
        }

        public int GetHashCode(FileInfo x)
        {
            return GetFileHash(x.FullName, x.Name).GetHashCode();
        }

        private string GetFileHash(string path, string name)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)) + name;
                }
            }
        }
    }
}