
namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.Xml.Linq;
    using System.IO;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core.Extensions;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;
    using System.Collections.Generic;
    using Umbraco.Core.Models.EntityBase;
    using Umbraco.Core.Events;
    public class ContentTypeHandler : uSyncBaseHandler<IContentType>, ISyncHandler
    {
        // sets our running order in usync. 
        public int Priority { get { return uSyncConstants.Priority.ContentTypes; } }
        public string Name { get { return "uSync: ContentTypeHanlder"; } }
        public string SyncFolder { get { return Constants.Packaging.DocumentTypeNodeName; } }

        private IContentTypeService _contentTypeService ;

        public ContentTypeHandler()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
        }

        public override SyncAttempt<IContentType> Import(string filePath, bool force = false)
        {
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException();

            var node = XElement.Load(filePath);
            var attempt = uSyncCoreContext.Instance.ContentTypeSerializer.DeSerialize(node, force);
            return attempt;
        }

        public override void ImportSecondPass(string file, IContentType item)
        {
            LogHelper.Debug<ContentTypeHandler>("Second Pass Import: {0} {1}", () => item.Name, () => file);
            if (!System.IO.File.Exists(file))
                throw new System.IO.FileNotFoundException();

            var node = XElement.Load(file);

            // special - content types need a two pass, because the structure isn't there first time
            var serializer = uSyncCoreContext.Instance.ContentTypeSerializer.DesearlizeSecondPass(item, node);
        }

        public override uSyncAction DeleteItem(Guid key, string keyString)
        {
            IContentType item = null;

            if (key != Guid.Empty)
                item = _contentTypeService.GetContentType(key);

            /* Delete only by key
            if (item == null || !string.IsNullOrEmpty(keyString))
                item = _contentTypeService.GetContentType(keyString);
            */

            if (item != null)
            {
                LogHelper.Info<ContentTypeHandler>("Deleting Content Type: {0}", () => item.Name);
                _contentTypeService.Delete(item);
                return uSyncAction.SetAction(true, keyString, typeof(IContentType), ChangeType.Delete, "Not found");
            }

            return uSyncAction.Fail(keyString, typeof(IContentType), ChangeType.Delete, "Not found");
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<ContentTypeHandler>("Exporting all ContentTypes (DocTypes)");

            return Export(-1, folder);
        }

        /// <summary>
        ///  v7.4 - we have folders - when we have folders we need to look for containers.
        /// </summary>
        public IEnumerable<uSyncAction> Export(int parent, string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var folders = ApplicationContext.Current.Services.EntityService.GetChildren(parent, UmbracoObjectTypes.DocumentTypeContainer);
            foreach (var fldr in folders)
            {
                var container = _contentTypeService.GetContentTypeContainer(fldr.Key);
                actions.Add(ExportContainer(container, folder));

                actions.AddRange(Export(fldr.Id, folder));
            }
            var nodes = ApplicationContext.Current.Services.EntityService.GetChildren(parent, UmbracoObjectTypes.DocumentType);
            foreach(var node in nodes)
            {
                var item = _contentTypeService.GetContentType(node.Key);
                actions.Add(ExportToDisk(item, folder));

                actions.AddRange(Export(node.Id, folder));
            }

            return actions;
        }

        public uSyncAction ExportToDisk(IContentType item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IContentType), "Item not set");

            LogHelper.Debug<ContentTypeHandler>("Exporting: {0}", () => item.Name);

            try
            {
                var attempt = uSyncCoreContext.Instance.ContentTypeSerializer.Serialize(item);
                var filename = string.Empty; 

                if (attempt.Success)
                {

                    filename = uSyncIOHelper.SavePath(
                            folder,
                            SyncFolder,
                            GetEntityPath(item),
                            "def");

                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);

            }
            catch (Exception ex)
            {
                LogHelper.Warn<ContentTypeHandler>("Error saving content type: {0}", () => ex.ToString());
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);
            }
        }

        public uSyncAction ExportContainer(EntityContainer item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IContentType), "folder not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.ContentTypeSerializer.SerializeContainer(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(
                        folder, SyncFolder, GetEntityPath(item), "def");

                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch (Exception ex)
            {
                LogHelper.Warn<ContentTypeHandler>("Error saving content type container: {0}", () => ex.ToString());
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);
            }
        }

        private string GetEntityPath(IUmbracoEntity item)
        {
            string path = string.Empty;
            if (item != null)
            {
                if (item.ParentId > 0)
                {
                    var parent = ApplicationContext.Current.Services.EntityService.Get(item.ParentId);
                    if (parent != null)
                    {
                        path = GetEntityPath(parent);
                    }
                }

                path = Path.Combine(path, item.Name.ToSafeFileName());
            }

            return path;
        }
                
        public void RegisterEvents()
        {
            ContentTypeService.SavedContentType += ContentTypeService_SavedContentType;
            ContentTypeService.DeletedContentType += ContentTypeService_DeletedContentType;
            ContentTypeService.SavedContentTypeContainer += ContentTypeService_SavedContentTypeContainer;
            ContentTypeService.DeletedContentTypeContainer += ContentTypeService_DeletedContentTypeContainer;
        }

        private void ContentTypeService_DeletedContentTypeContainer(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<EntityContainer> e)
        {

            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<ContentTypeHandler>("Delete: Container Deleted", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetEntityPath(item), "def");

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key, item.Name, typeof(EntityContainer));
            }
        }

        private void ContentTypeService_DeletedContentType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IContentType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<ContentTypeHandler>("Delete: Removing uSync files for Item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetEntityPath(item), "def");

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key, item.Alias, typeof(IContentType));
            }
        }

        private void ContentTypeService_SavedContentTypeContainer(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<EntityContainer> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.SavedEntities)
            {
                LogHelper.Info<ContentTypeHandler>("Save: Container Saved: {0}", ()=> item.Name);
                var action = ExportContainer(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);

                if (action.Success)
                    NameChecker.ManageOrphanFiles(Constants.Packaging.DocumentTypeNodeName, item.Key, action.FileName);
            }
        }


        private void ContentTypeService_SavedContentType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IContentType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                LogHelper.Info<ContentTypeHandler>("Save: Saving uSync files for Item: {0}", () => item.Name);
                var action = ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);

                if (action.Success)
                {
                    NameChecker.ManageOrphanFiles(Constants.Packaging.DocumentTypeNodeName, item.Key, action.FileName);
                    e.Messages.Add(
                        new EventMessage("uSync", "uSync save a copy to disk", EventMessageType.Info));
                }
                else
                {
                    e.Messages.Add(
                        new EventMessage("uSync", "uSync Failed to save to disk", EventMessageType.Warning));
                }


            }
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.ContentTypeSerializer.IsUpdate(node);

            var action = uSyncActionHelper<IContentType>.ReportAction(update, node.NameFromNode());
            if (action.Change > ChangeType.NoChange)
                action.Details = ((ISyncChangeDetail)uSyncCoreContext.Instance.ContentTypeSerializer).GetChanges(node);

            return action;
        }
    }
}
