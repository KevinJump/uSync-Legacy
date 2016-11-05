using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;

using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Extensions;

using Jumoo.uSync.BackOffice;
using Jumoo.uSync.BackOffice.Helpers;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;
using System.Linq;

namespace Jumoo.uSync.Content
{
    public class MediaHandler : BaseContentHandler<IMedia>, ISyncHandler
    {
        public string Name { get { return "uSync: MediaHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.Media; } }
        public string SyncFolder { get { return "Media"; } }

        public MediaHandler() : base("media")
        {
            // we need to instancate media content, (unlike the others). 
            // because we need to tell the mover where to put our files.

        }

        public override SyncAttempt<IMedia> Import(string file, int parentId, bool force = false)
        {
            LogHelper.Debug<MediaHandler>("Importing Media: {0} {1}", () => file, ()=> parentId);
            if (!System.IO.File.Exists(file))
                throw new FileNotFoundException(file);

            var node = XElement.Load(file);
            var attempt = uSyncCoreContext.Instance.MediaSerializer.Deserialize(node, parentId, force);
            
            return attempt;
        }

        public override void ImportSecondPass(string file, IMedia item)
        {
            XElement node = XElement.Load(file);
            uSyncCoreContext.Instance.MediaSerializer.DesearlizeSecondPass(item, node);

            string mediaFolder = Path.Combine(Path.GetDirectoryName(file), mediaFolderName);
            uSyncCoreContext.Instance.MediaFileMover.ImportFileValue(node, item, mediaFolder);
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<MediaHandler>("Exporting Media");
            List<uSyncAction> actions = new List<uSyncAction>();

            foreach(var item in _mediaService.GetRootMedia())
            {
                actions.AddRange(ExportFolder(item, "", folder));
            }

            return actions;
        }

        private IEnumerable<uSyncAction> ExportFolder(IMedia item, string path, string root)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            if (item == null)
                return actions;

            var itemName = base.GetItemFileName(item);

            var itemPath = Path.Combine(path, itemName);
            actions.Add(ExportItem(item, itemPath, root));

            foreach (var childItem in _mediaService.GetChildren(item.Id))
            {
                actions.AddRange(ExportFolder(childItem, itemPath, root));
            }

            return actions;
        }

        private uSyncAction ExportItem(IMedia item, string path, string root)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(path), typeof(IMedia), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.MediaSerializer.Serialize(item);

                var filename = string.Empty;
                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(root, SyncFolder, path, "media");
                    uSyncIOHelper.SaveNode(attempt.Item, filename);

                    // media serializer doesn't get the files for you , you need to 
                    // call the filemover
                    var fileFolder = Path.Combine(Path.GetDirectoryName(filename), mediaFolderName);
                    uSyncCoreContext.Instance.MediaFileMover.ExportFile(item, fileFolder);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch(Exception ex)
            {
                LogHelper.Warn<ContentHandler>("Error saving Media: {0}", () => ex.ToString());
                return uSyncAction.Fail(item.Name, typeof(IMedia), ChangeType.Export, ex);
            }
        }

        public void RegisterEvents()
        {
            MediaService.Saved += MediaService_Saved;
            MediaService.Trashing += MediaService_Trashing;
        }

        private void MediaService_Trashing(IMediaService sender, Umbraco.Core.Events.MoveEventArgs<IMedia> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var moveInfo in e.MoveInfoCollection)
            {
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetMediaPath(moveInfo.Entity), "media");
            }
        }

        private void MediaService_Saved(IMediaService sender, Umbraco.Core.Events.SaveEventArgs<IMedia> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                var path = GetMediaPath(item);
                var attempt = ExportItem(item, path, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, item.Key, attempt.FileName);
                }
            }
        }

        private string GetMediaPath(IMedia item)
        {
            var path = GetItemFileName(item);
            if (item.ParentId != -1)
            {
                path = string.Format("{0}\\{1}", GetMediaPath(item.Parent()), path);
            }

            return path;
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.MediaSerializer.IsUpdate(node);
            return uSyncActionHelper<IMedia>.ReportAction(update, node.NameFromNode());
        }

    }
}
