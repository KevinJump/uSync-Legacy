using Jumoo.uSync.BackOffice.Helpers;
using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Snapshots
{
    /// <summary>
    ///  helper does the grunt work on files. 
    /// </summary>
    public static class SnapshotIO
    {
        // given two folders, will merge them (treating source files as newer)
        public static void MergeFolder(string source, string target)
        {
            //LogHelper.Debug<SnapshotManager>("Merge Folder: {0} -> {1}", () => source, () => target);
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            FileInfo[] files = sourceDir.GetFiles();
            foreach(var file in files)
            {
                var targetFile = Path.Combine(target, file.Name);
                //LogHelper.Debug<SnapshotManager>("Merging File: {0} -> {1}", () => file, ()=> targetFile);

                if (file.Name == "uSyncActions.config" && File.Exists(target))
                {
                    // merge the xml action file, it is special.
                    MergeXmlFiles(file.FullName, targetFile);
                }
                else
                {
                    file.CopyTo(targetFile, true);
                }
            }

            // recurse into the folders.
            foreach(var folder in sourceDir.GetDirectories())
            {
                string targetFolder = Path.Combine(target, folder.Name);
                MergeFolder(folder.FullName, targetFolder);
            }
        }

        /// <summary>
        ///  remove anything that isn't in the source from the target
        ///  these leaves you just with the new things or things that
        ///  have changed
        /// </summary>
        public static void RemoveDuplicates(string source ,string target)
        {
            DirectoryInfo targetDir = new DirectoryInfo(target);
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            var targetFiles = targetDir.GetFiles("*.*", SearchOption.AllDirectories);
            var sourceFiles = sourceDir.GetFiles("*.*", SearchOption.AllDirectories);

            FileCompare fileCompare = new FileCompare();

            List<string> duplicates = new List<string>();

            // match files that are exactly the same in contents 
            var matches = targetFiles.Intersect(sourceFiles, fileCompare);

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

        private static void RemoveEmptyDirectories(string folder)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);

            if (dir.GetDirectories().Any())
            {
                foreach (var subFolder in dir.GetDirectories())
                {
                    RemoveEmptyDirectories(subFolder.FullName);
                }
            }

            dir.Refresh();

            if (!dir.GetFileSystemInfos().Any())
            {
                dir.Delete();
            }
        }

        /// <summary>
        ///  merges to xml files at the top level, so as we get
        ///  our action files this will stick them together.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        private static void MergeXmlFiles(string source ,string target)
        {
            XElement sourceNode = XElement.Load(source);
            XElement targetNode = XElement.Load(target);

            if (sourceNode != null && targetNode != null)
            {
                foreach (var n in sourceNode.Elements())
                {
                    targetNode.Add(n);
                }

                targetNode.Save(target);
            }
        }


        private static IEnumerable<FileInfo> LeftOnlyFiles(string left, string right)
        {
            LogHelper.Debug<SnapshotManager>("---------------------------------------");

            DirectoryInfo leftDir = new DirectoryInfo(left);
            DirectoryInfo rightDir = new DirectoryInfo(right);

            var leftList = leftDir.GetFiles("*.*", SearchOption.AllDirectories);
            var rightList = rightDir.GetFiles("*.*", SearchOption.AllDirectories);

            FileNameCompare fileNameCompare = new FileNameCompare(left, right);
            var leftOnly = leftList.Except(rightList, fileNameCompare).ToList();

            LogHelper.Debug<SnapshotManager>("---------------------------------------");

            return leftOnly;
        }

        /// <summary>
        ///  mock the process the uSyncActions.config in the root of the folder
        ///  we do this to remove deleted files renames etc from the snapshot.
        /// </summary>
        /// <param name="target"></param>
        public static void ProcessActions(string actionSource, string target)
        {
            LogHelper.Debug<SnapshotManager>("Processing Actions (not) {0}", () => actionSource);
            var actionFile = Path.Combine(actionSource, "uSyncActions.config");
            if (File.Exists(actionFile))
            {
                LogHelper.Debug<SnapshotManager>("ActionFile: {0}", () => actionFile);
                
                var tracker = new ActionTracker(actionSource);

                var fileActions = tracker.GetActions(typeof(FileInfo));

                if (fileActions.Any())
                {

                    var renames = fileActions.Where(x => x.Action == SyncActionType.Rename);
                    LogHelper.Debug<SnapshotManager>("Processing Renames: {0}", () => renames.Count());
                    foreach (var rename in renames)
                    {
                        LogHelper.Debug<SnapshotManager>("\n\tTarget: {0} \n\tTemp: {1}", () => target, () => rename.Name);

                        var targetFile = Path.Combine(target, rename.Name.Trim(new char[] { '\\' }));

                        LogHelper.Debug<SnapshotManager>("Rename: {0}", () => targetFile);

                        LogHelper.Debug<SnapshotManager>("Removing File: {0}", () => targetFile);
                        if (File.Exists(targetFile))
                            File.Delete(targetFile);
                    }

                    var deletes = fileActions.Where(x => x.Action == SyncActionType.Delete);
                    LogHelper.Debug<SnapshotManager>("Processing Deletes: {0}", ()=>  deletes.Count());
                    foreach (var delete in tracker.GetActions(SyncActionType.Delete))
                    {
                        var targetFile = Path.Combine(target, delete.Name.Trim('\\'));
                        LogHelper.Debug<SnapshotManager>("Removing File: {0}", () => targetFile);
                        if (File.Exists(targetFile))
                            File.Delete(targetFile);
                    }
                }
                
            }
            
        }

        /// <summary>
        ///  works out if anytthing has been deleted or renamed between teh 
        ///  two snapshots, and creates the relavant actions in the action
        ///  log - so a usync import can process them
        /// </summary>
        public static void CreateActions(string source, string target)
        {
            // get the files that are only in the source
            // they are the candidates for deletes
            var missingFiles = LeftOnlyFiles(source, target);

            if (missingFiles.Any())
            {
                // fire up a usync action tracker instance
                var actionTracker = new ActionTracker(target);

                foreach(var file in missingFiles)
                {
                    LogHelper.Debug<SnapshotManager>("Actions Missing: {0}", () => file.FullName);

                    // really if it doesn't something is wrong
                    if (File.Exists(file.FullName))
                    {
                        XElement node = null;
                        var itemType = default(Type);
                        var key = string.Empty;

                        try {
                            node = XElement.Load(file.FullName);
                            itemType = node.GetUmbracoType();
                            key = IDHunter.GetItemId(node);
                            LogHelper.Debug<SnapshotManager>("Looking in: {0} - {1}", () => node.Name.LocalName, () => key);
                        }
                        catch
                        {
                            // not a valid bit of XML.?
                            // but that is ok, it might just be a file
                        }


                        if (itemType != default(Type))
                        {
                            if (!string.IsNullOrEmpty(key))
                            {
                                var keyGuid = Guid.Empty;
                                var newName = IDHunter.FindInFiles(target, key);

                                if (!string.IsNullOrEmpty(newName))
                                {
                                    // if we have found the key in another file in the target
                                    // then it's a rename. 

                                    // the old file won't be copped across (because we delete the source only
                                    // Files) - but we need to tell uSync its a rename or it will just
                                    // do a recreate. 
                                    if (Guid.TryParse(key, out keyGuid))
                                    {
                                        actionTracker.AddAction(SyncActionType.Rename, keyGuid, newName, itemType);
                                    }
                                    else
                                    {
                                        actionTracker.AddAction(SyncActionType.Rename, key, itemType);
                                    }
                                }
                                else
                                {
                                    // it's a delete of a known thing in umbraco - our delete action needs to point to the object 
                                    if (Guid.TryParse(key, out keyGuid))
                                    {
                                        actionTracker.AddAction(SyncActionType.Delete, keyGuid, node.NameFromNode(), itemType);
                                    }
                                    else
                                    {
                                        // not a guid based key 
                                        actionTracker.AddAction(SyncActionType.Delete, key, itemType);
                                    }
                                }
                            }
                        }

                        // if we don't know what the file contains, then 
                        // we assume its a normal (non-usync) file
                        // for these a delete followed by a re-create is 
                        // the same as a rename so we are happy. 
                        // 
                        if (!file.Name.Equals("uSyncActions.config", StringComparison.OrdinalIgnoreCase))
                        {
                            // for all non-umbraco and umbraco elements we add this delete, it is to tell
                            // snapshots that the actual file is to be deleted too. 
                            actionTracker.AddAction(SyncActionType.Delete, file.FullName.Substring(source.Length), typeof(FileInfo));
                        }

                        // delete the file from the target 
                        // 
                    }

                }
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

    class FileNameCompare : IEqualityComparer<FileInfo>
    {
        private int _leftRootLength;
        private int _rightRootLength;
        private string _leftRoot;
        private string _rightRoot;

        public FileNameCompare(string leftRoot, string rightRoot)
        {
            _leftRoot = leftRoot;
            _rightRoot = rightRoot;
            _leftRootLength = leftRoot.Length;
            _rightRootLength = rightRoot.Length;
        }

        public bool Equals(FileInfo x, FileInfo y)
        {

            var left = x.FullName.Substring(0, _leftRootLength) == _leftRoot ?
                x.FullName.Substring(_leftRootLength) : x.FullName.Substring(_rightRootLength);
            var right = x.FullName.Substring(0, _leftRootLength) == _leftRoot ?
                x.FullName.Substring(_leftRootLength) : x.FullName.Substring(_rightRootLength);

            return (left.Equals(right, StringComparison.OrdinalIgnoreCase));
        }

        public int GetHashCode(FileInfo x)
        {
            if (x.FullName.Substring(0,_leftRootLength) == _leftRoot)
                return x.FullName.Substring(_leftRootLength).GetHashCode();
            else
                return x.FullName.Substring(_rightRootLength).GetHashCode();
        }
    }

}
