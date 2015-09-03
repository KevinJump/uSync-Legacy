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

using Jumoo.uSync.Migrations.Helpers;

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
                foreach (var dir in Directory.GetDirectories(_rootFolder))
                {
                    DirectoryInfo snapshotDir = new DirectoryInfo(dir);

                    snapshots.Add(new SnapshotInfo(dir));
                }
            }

            return snapshots;
        }

        public SnapshotInfo CreateSnapshot(string name)
        {
            var masterSnap = CombineSnapshots(_rootFolder);

            var snapshotFolder = Path.Combine(_rootFolder,
                string.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), name.ToSafeFileName()));

            uSyncBackOfficeContext.Instance.ExportAll(snapshotFolder);

            LogHelper.Info<SnapshotManager>("Export Complete");

            foreach (var folder in _folders)
            {
          
                var source = IOHelper.MapPath("~/" + folder);
                if (Directory.Exists(source))
                {
                    LogHelper.Info<SnapshotManager>("Including {0} in snapshot", () => source);
                    var target = Path.Combine(snapshotFolder, folder);
                    SnapshotIO.MergeFolder(target, source);
                }
            }

            LogHelper.Info<SnapshotManager>("Extra folders copied");

            // now we delete anything that is in any of the previous snapshots.
            if (!string.IsNullOrEmpty(masterSnap))
            {
                SnapshotIO.RemoveDuplicates(snapshotFolder, masterSnap);

                // todo - capture deletes since last snapshot?
                //          things in the master but not in our new one?


                // Directory.Delete(masterSnap, true);
            }

            LogHelper.Info<SnapshotManager>("Cleaned Snapshot up..");

            if (!Directory.Exists(snapshotFolder))
            {
                // empty snapshot
                LogHelper.Info<SnapshotManager>("No changes in this snapshot");
            }

            return new SnapshotInfo(snapshotFolder);
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
                foreach (var snapshot in snapshots)
                {
                    SnapshotIO.MergeFolder(tempRoot, snapshot.FullName);
                }

                return tempRoot;
            }

            return tempRoot;
        }
        
        #endregion
    }
}
