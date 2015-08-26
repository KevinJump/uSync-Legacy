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
    public class ContentHandler : BaseContentHandler<IContent>, ISyncHandler
    {
        public string Name { get { return "uSync: ContentHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.Content; } }
        public string SyncFolder { get { return "Content"; } }

        public override SyncAttempt<IContent> Import(string filePath, int parentId, bool force = false)
        {
            LogHelper.Info<IContent>("Importing Content : {0} {1}", ()=> filePath, ()=> parentId);
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);
            return uSyncCoreContext.Instance.ContentSerializer.Deserialize(node, parentId, force);
            
        }

        public override void ImportSecondPass(string file, IContent item)
        {
            // uSyncCoreContext.Instance.ContentSerializer.D
            XElement node = XElement.Load(file);
            uSyncCoreContext.Instance.ContentSerializer.DesearlizeSecondPass(item, node);
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<ContentHandler>("Exporting Content");

            List<uSyncAction> actions = new List<uSyncAction>();

            foreach(var item in _contentService.GetRootContent())
            {
                actions.AddRange(ExportFolder(item, "", folder));
            }

            return actions;
        }

        private IEnumerable<uSyncAction> ExportFolder(IContent item, string path, string rootFolder)
        {

            List<uSyncAction> actions = new List<uSyncAction>();

            if (item == null)
                return actions;

            var itemPath = Path.Combine(path, item.Name.ToSafeFileName());
            // var itemPath = string.Format("{0}/{1}", path, item.Name.ToSafeFileName());

            actions.Add(ExportItem(item, itemPath, rootFolder));

            foreach (var childItem in _contentService.GetChildren(item.Id))
            {
                actions.AddRange(ExportFolder(childItem, itemPath, rootFolder));
            }

            return actions;
        }

        private uSyncAction ExportItem(IContent item, string path, string rootFolder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(path), typeof(IContent), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.ContentSerializer.Serialize(item);

                string filename = string.Empty;
                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(rootFolder, SyncFolder, path, "content");
                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch(Exception ex)
            {
                LogHelper.Warn<ContentHandler>("Error saving Content: {0}", ()=> ex.ToString());
                return uSyncAction.Fail(item.Name, typeof(IContent), ChangeType.Export, ex);
            }
        }

        public void RegisterEvents()
        {
            ContentService.Saved += ContentService_Saved;
            ContentService.Trashing += ContentService_Trashing;
            ContentService.Copied += ContentService_Copied;
        }

        private void ContentService_Copied(IContentService sender, Umbraco.Core.Events.CopyEventArgs<IContent> e)
        {
            SaveItems(sender, new List<IContent>(new IContent[] { e.Copy }));
        }

        private void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            SaveItems(sender, e.SavedEntities);
        }

        void SaveItems(IContentService sender, IEnumerable<IContent> items)
        {
            foreach(var item in items)
            {
                var path = "";
                var attempt = ExportItem(item, path, SyncFolder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, item.Key, attempt.FileName);
                }
            }
        }

        private void ContentService_Trashing(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            foreach(var moveInfo in e.MoveInfoCollection)
            {
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetContentPath(moveInfo.Entity));

            }
        }

        private string GetContentPath(IContent item)
        {
            return "";

            var path = item.Name.ToSafeFileName();
            if (item.ParentId != -1)
            {
                path = string.Format("{0}\\{1}", GetContentPath(item.Parent()), path);
            }

            return path;
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.ContentSerializer.IsUpdate(node);
            return uSyncActionHelper<IContent>.ReportAction(update, node.NameFromNode());
        }
    }
}
