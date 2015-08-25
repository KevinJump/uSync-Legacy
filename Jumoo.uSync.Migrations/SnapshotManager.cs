using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.IO;
using Umbraco.Core;
using Umbraco.Core.IO;
using Jumoo.uSync.BackOffice;
using Umbraco.Core.Logging;
using System.Security.Cryptography;

namespace Jumoo.uSync.Migrations
{
    public class SnapshotManager
    {
        private string _rootFolder;

        private List<string> _folders;

        public SnapshotManager(string folder)
        {
            _rootFolder = IOHelper.MapPath(folder);

            if (!Directory.Exists(_rootFolder))
                Directory.CreateDirectory(_rootFolder);

            _folders = new List<string>();

            _folders.Add("views");
            _folders.Add("css");
            _folders.Add("app_code");
            _folders.Add("scripts");
            _folders.Add("xslt");
            _folders.Add("fonts");
        }

        public List<SnapshotInfo> ListSnapshots()
        {
            List<SnapshotInfo> snapshots = new List<SnapshotInfo>();
            if (Directory.Exists(_rootFolder))
            {
                foreach(var dir in Directory.GetDirectories(_rootFolder))
                {
                    snapshots.Add(new SnapshotInfo(dir));
                }
            }

            return snapshots;
        }

        public void CreateSnapshot(string name)
        {
            var masterSnap = CombineSnapshots(_rootFolder);

            var snapshotFolder = Path.Combine(_rootFolder,
                string.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), name.ToSafeFileName()));

            uSyncBackOfficeContext.Instance.ExportAll(snapshotFolder);

            LogHelper.Info<SnapshotManager>("Export Complete");

            foreach(var folder in _folders)
            {
                var source = IOHelper.MapPath("~/" + folder);
                if (Directory.Exists(source))
                {
                    LogHelper.Info<SnapshotManager>("Including {0} in snapshot", () => source);
                    var target = Path.Combine(snapshotFolder, folder);
                    MergeFolder(source, target);
                }
            }

            LogHelper.Info<SnapshotManager>("Extra folders copied");

            // now we delete anything that is in any of the previous snapshots.
            if (!string.IsNullOrEmpty(masterSnap))
            {
                RemoveDuplicates(snapshotFolder, masterSnap);

                // todo - capture deletes since last snapshot?
                //          things in the master but not in our new one?
                          

                // Directory.Delete(masterSnap, true);
            }

            LogHelper.Info<SnapshotManager>("Cleaned Snapshot up..");

        }

        /// <summary>
        ///  takes everything in the snapshot folder, builds a master snapshot
        ///  and then runs it through an import
        /// </summary>
        public IEnumerable<uSyncAction> ApplySnapshots()
        {
            var snapshotImport = CombineSnapshots(_rootFolder);

            if (Directory.Exists(snapshotImport))
            {
                var actions = uSyncBackOfficeContext.Instance.ImportAll(snapshotImport);
                return actions;
            }

            return null;
        }

        #region Snapshot Creation
        /// <summary>
        ///  builds a master snapshot, of all existing
        ///  snapshots, this can then be used as the import folder
        ///  meaning we import just once. 
        /// 
        ///  also good when creating snapshots, we only create stuff
        ///  that is not in our existing snapshot folders.
        /// </summary>
        /// <param name="snapshotFolder"></param>
        /// <returns></returns>
        private string CombineSnapshots(string snapshotFolder)
        {
            var tempRoot = IOHelper.MapPath(Path.Combine(SystemDirectories.Data, "temp", "usync", "snapshots"));

            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);

            Directory.CreateDirectory(tempRoot);

            DirectoryInfo root = new DirectoryInfo(snapshotFolder);

            var snapshots = root.GetDirectories().OrderBy(x => x.Name);

            if (snapshots.Any())
            {
                foreach(var snapshot in snapshots)
                {
                    MergeFolder(snapshot.FullName, tempRoot);
                }

                return tempRoot;
            }

            return string.Empty;
        }

        private void MergeFolder(string source, string dest)
        {
            DirectoryInfo dir = new DirectoryInfo(source);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
                return;

            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            FileInfo[] files = dir.GetFiles();
            foreach (var file in files)
            {
                var destPath = Path.Combine(dest, file.Name);
                file.CopyTo(destPath, true);
            }

            foreach (var subDirectory in dirs)
            {
                string destPath = Path.Combine(dest, subDirectory.Name);
                MergeFolder(subDirectory.FullName, destPath);
            }
        }

        private void RemoveDuplicates(string target, string source)
        {
            DirectoryInfo targetDir = new DirectoryInfo(target);
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            var targetList = targetDir.GetFiles("*.*", SearchOption.AllDirectories);
            var sourceList = sourceDir.GetFiles("*.*", SearchOption.AllDirectories);

            FileCompare fileComp = new FileCompare();

            List<string> duplicates = new List<string>();
            var matches = targetList.Intersect(sourceList, fileComp);

            if (matches.Any())
            {
                foreach(var f in matches)
                {
                    duplicates.Add(f.FullName);
                }
            }

            if (duplicates.Any())
            {
                foreach(var f in duplicates)
                {
                    LogHelper.Info<SnapshotManager>("Deleting: {0}", () => f);
                    File.Delete(f);
                }
            }

            // remove empty directories.
            RemoveEmptyDirectories(target);
        }

        private void RemoveEmptyDirectories(string folder)
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

            if (!dir.GetDirectories().Any() && !dir.GetFiles().Any())
            {
                try {
                    LogHelper.Info<SnapshotManager>("Removing: {0}", () => folder);
                    dir.Delete();
                }
                catch(Exception ex)
                {
                    LogHelper.Warn<SnapshotManager>("Removing folder fail: {0}", ()=> ex.Message);
                }
            }
        }

        private void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
        #endregion
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

                LogHelper.Info<SnapshotManager>("Compare: {0} {1}", () => x.Name, () => y.Name);
                LogHelper.Info<SnapshotManager>("Compare: {0} {1}", () => xhash, () => yhash);
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