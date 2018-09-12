using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Jumoo.uSync.BackOffice;
using Umbraco.Core.Logging;
using Jumoo.uSync.Snapshots.Data;
using System.IO.Compression;

namespace Jumoo.uSync.Snapshots
{
    /// <summary>
    ///  handles the creation/import of snapshots
    /// </summary>
    public class SnapshotManager
    {
        private string _root;
        private uSyncBackOfficeContext _backOffice;
        private uSyncSnapshotSettings _settings;

        public SnapshotManager(string snapshotRoot)
        {
            _settings = uSyncSnapshots.Instance.Configuration.Settings;
            _backOffice = uSyncBackOfficeContext.Instance;
            _root = snapshotRoot;
        }

        public List<SnapshotInfo> GetSnapshots()
        {
            LogHelper.Info<SnapshotManager>("Get Snapshots");
            var snapshots = new List<SnapshotInfo>();
            if (Directory.Exists(_root))
            {
                foreach(var dir in Directory.GetDirectories(_root))
                {
                    LogHelper.Info<SnapshotManager>("Adding Snapshot: {0}", () => dir);
                    snapshots.Add(new SnapshotInfo(dir));
                }
            }

            return snapshots;
        }

        /// <summary>
        ///  create a new snapshot in the folder with name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SnapshotInfo CreateSnapshot(string name)
        {
            LogHelper.Debug<SnapshotManager>("Creating Snapshot: {0}", ()=> name);

            var master = CombineSnapshots(_root);
            var snapshot = Path.Combine(_root,
                string.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), name.ToSafeFileName().Replace('_', '-')));

            if (!string.IsNullOrEmpty(master) && Directory.Exists(master))
            {
                // use uync to fully export everything
                LogHelper.Debug<SnapshotManager>("Getting a uSync Full Export");
                _backOffice.ExportAll(snapshot);

                LogHelper.Debug<SnapshotManager>("Full Export - checking folders");
                // TODO: are we going to snapshot other (non-usync) files?
                if (_settings.Folders.Any())
                {
                    foreach(var folder in _settings.Folders)
                    {
                        var source = IOHelper.MapPath("~/" + folder.Path);
                        if (Directory.Exists(source))
                        {
                            var target = Path.Combine(snapshot, folder.Path);
                            SnapshotIO.MergeFolder(source, target);
                        }
                    }

                }


                LogHelper.Debug<SnapshotManager>("Processing and Actions (deletes/renames)");
                // work out what may have been deleted, and or renamed
                // and put them in the snapshot action file.
                SnapshotIO.CreateActions(master, snapshot);


                LogHelper.Debug<SnapshotManager>("Removing Duplicate files from snapshot");
                // remove any files that are the same in both snapshot and master
                // leaving on the changed files. 
                SnapshotIO.RemoveDuplicates(master, snapshot);
            }

            if (!Directory.Exists(snapshot))
            {
                // empty snapshot - no folder left
                LogHelper.Info<SnapshotManager>("No changes in snapshot - so no folder");
            }

            return new SnapshotInfo(snapshot, true); ;
        }


        /// <summary>
        ///  apply all snapshots in the folder.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<uSyncAction> ApplySnapshots()
        {
            var import = CombineSnapshots(_root);

            if (Directory.Exists(import))
            {
                var actions = _backOffice.ImportAll(import);

                // TODO: non usync files 
                if (_settings.Folders.Any())
                {
                    foreach(var folder in _settings.Folders)
                    {
                        var source = Path.Combine(import, folder.Path);
                        var target = IOHelper.MapPath("~/" + folder.Path);
                        SnapshotIO.MergeFolder(source, target);
                    }
                }

                MarkApplySnapshots(_root);
                return actions;
            }

            return null;
            
        }

        public IEnumerable<uSyncAction> Apply(string snapshotName)
        {
            var snapshot = FindSnapshot(snapshotName);

            if (snapshot != null && Directory.Exists(snapshot.Folder))
            {
                var actions = _backOffice.ImportAll(snapshot.Folder);

                MarkApplySnapshot(snapshot.Folder, true);

                return actions;
            }



            return null;
        }

        public IEnumerable<uSyncAction> Report()
        {
            var import = CombineSnapshots(_root);

            if (Directory.Exists(import))
            {
                var actions = _backOffice.ImportReport(import);
                return actions;
            }
            return null;
        }

        public IEnumerable<uSyncAction> Report(string snapshotName)
        {
            var snapshot = FindSnapshot(snapshotName);

            if (snapshot != null && Directory.Exists(snapshot.Folder))
            {
                var actions = _backOffice.ImportReport(snapshot.Folder);
                return actions;
            }

            return null;
        }

        public bool Delete(string snapshotName)
        {
            var snapshot = FindSnapshot(snapshotName);

            if (snapshot != null && Directory.Exists(snapshot.Folder))
                Directory.Delete(snapshot.Folder, true);

            return true;
        }

        /// <summary>
        ///  takes all the snapshots in a folder and puts them together,
        ///   this way we get a master snapshot , which is used when working 
        ///   out what has changed, or when importing everything into a new site.
        /// </summary>
        private string CombineSnapshots(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var temp = IOHelper.MapPath(
                Path.Combine(SystemDirectories.Data, "temp", "usync", "snapshots"));

            LogHelper.Debug<SnapshotManager>("Building Snapshots: {0}", () => folder, ()=> temp);

            if (Directory.Exists(temp))
                Directory.Delete(temp, true);

            Directory.CreateDirectory(temp);

            DirectoryInfo root = new DirectoryInfo(folder);
            var snapshots = root.GetDirectories().OrderBy(x => x.Name);

            if (snapshots.Any())
            {
                foreach(var snapshot in snapshots)
                {
                    LogHelper.Debug<SnapshotManager>("Merging Snapshot: {0} -> {1}", ()=> snapshot.Name, ()=> temp);
                    SnapshotIO.MergeFolder(snapshot.FullName, temp);
                    LogHelper.Debug<SnapshotManager>("Merged Snapshot: {0}", () => snapshot.Name);

                    // process the action file from this snapshop on the merge at this point
                    // we do it like this because we then will delete any renames that have happened
                    // so far, but not any re-renames that might clash future snapshots)
                    SnapshotIO.ProcessActions(snapshot.FullName, temp);
                }


            }

            return temp;
        }

        /// <summary>
        ///  given a name find the snapshot 
        ///  the name is usally just the bit at the end after the _
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private SnapshotInfo FindSnapshot(string name)
        {
            foreach(var folder in Directory.GetDirectories(_root))
            {
                var snapName = folder.Substring(folder.LastIndexOf('_') + 1);

                if (name.Equals(snapName))
                {
                    return new SnapshotInfo(folder);
                }
            }

            return null;
        }


        /// <summary>
        ///  marks all the snapshots in the folder as applied.
        /// </summary>
        /// <param name="folder"></param>
        private void MarkApplySnapshots(string folder)
        {
            DirectoryInfo root = new DirectoryInfo(folder);
            var snapshots = root.GetDirectories().OrderBy(x => x.Name);

            if (snapshots.Any())
            {
                foreach (var snapshot in snapshots)
                {
                    MarkApplySnapshot(snapshot.FullName);
                }
            }
        }

        /// <summary>
        ///  marks a single snapshot as applied
        /// </summary>
        /// <param name="name"></param>
        private void MarkApplySnapshot(string folder, bool force = false)
        {
            var snap = new SnapshotInfo(folder);
            SnapshotLogger logger = new SnapshotLogger();
            logger.ApplySnapshot(snap, force);
        }


        public string ZipSnapshot(string name)
        {
            var snapshot = FindSnapshot(name);
            if (snapshot == null) return null;
            return ZipFolder(snapshot.Folder);
        }

        public string ZipAll()
        {
            var all = CombineSnapshots(_root);
            return ZipFolder(all, IOHelper.MapPath("~/uSync/"));
        }

        public string ZipFolder(string snapshotFolder, string outFolder = "")
        {
            var savePath = snapshotFolder.TrimEnd('\\') + ".zip";
            if (!string.IsNullOrWhiteSpace(outFolder))
            {
                savePath = outFolder + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_Full-Snapshot" + ".zip";
            }
            if (File.Exists(savePath))
                File.Delete(savePath);

            var folder = new DirectoryInfo(snapshotFolder);
            var files = folder.GetFiles("*.*", SearchOption.AllDirectories);

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var relative = file.FullName.Substring(snapshotFolder.Length + 1);
                        archive.CreateEntryFromFile(file.FullName, relative);
                    }
                }

                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }

            return savePath;
        }

        public string UnZipFolder(string zipFile)
        {
            var name = Path.GetFileNameWithoutExtension(zipFile);
            var taget = Path.Combine(_root, name);
            ZipFile.ExtractToDirectory(zipFile, taget);
            return name;
        }
    }
}
