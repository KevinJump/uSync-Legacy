
namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.Linq;
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
    using System.Timers;

    public class ContentTypeHandler : uSyncBaseHandler<IContentType>, ISyncHandler, ISyncPostImportHandler
    {
        // sets our running order in usync. 
        public int Priority { get { return uSyncConstants.Priority.ContentTypes; } }
        public string Name { get { return "uSync: ContentTypeHandler"; } }
        public string SyncFolder { get { return Constants.Packaging.DocumentTypeNodeName; } }

        private readonly IContentTypeService _contentTypeService;
        private readonly IEntityService _entityService;

        public ContentTypeHandler()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _entityService = ApplicationContext.Current.Services.EntityService;

            RequiresPostProcessing = true;
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
            uSyncCoreContext.Instance.ContentTypeSerializer.DesearlizeSecondPass(item, node);
        }

        public override uSyncAction DeleteItem(Guid key, string keyString)
        {
            IContentType item = null;

            if (key != Guid.Empty)
                item = _contentTypeService.GetContentType(key);

            if (item != null)
            {
                LogHelper.Info<ContentTypeHandler>("Deleting Content Type: {0}", () => item.Name);
                _contentTypeService.Delete(item);
                return uSyncAction.SetAction(true, keyString, typeof(IContentType), ChangeType.Delete, "deleted");
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
                actions.AddRange(Export(fldr.Id, folder));
            }

            var nodes = _entityService.GetChildren(parent, UmbracoObjectTypes.DocumentType);
            foreach (var node in nodes)
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
                            GetItemPath(item),
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

        public void RegisterEvents()
        {
            ContentTypeService.SavedContentType += ContentTypeService_SavedContentType;
            ContentTypeService.DeletedContentType += ContentTypeService_DeletedContentType;
            ContentTypeService.MovedContentType += ContentTypeService_MovedContentType;

            ContentTypeService.SavedContentTypeContainer += ContentTypeService_SavedContentTypeContainer;
        }

        private void ContentTypeService_SavedContentTypeContainer(IContentTypeService sender, SaveEventArgs<EntityContainer> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                // _saveQueue.Enqueue(item.Id);
                Export(item.Id, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            }

        }

        private void ContentTypeService_DeletedContentType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IContentType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<ContentTypeHandler>("Delete: Removing uSync files for Item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetItemPath(item), "def");

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key, item.Alias, typeof(IContentType));
            }
        }

        private void ContentTypeService_SavedContentType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IContentType> e)
        {
            if (uSyncEvents.Paused)
                return;


                foreach (var item in e.SavedEntities)
                {
                    SaveContentItem(item);
                    
                }
        }

        private void ContentTypeService_MovedContentType(IContentTypeService sender, MoveEventArgs<IContentType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.MoveInfoCollection)
            {
                SaveContentItem(item.Entity);
            }
        }


        private void SaveContentItem(IContentType item)
        {
            LogHelper.Info<ContentTypeHandler>("Save: Saving uSync files for Item: {0}", () => item.Name);
            var action = ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            if (action.Success)
            {
                NameChecker.ManageOrphanFiles(Constants.Packaging.DocumentTypeNodeName, item.Key, action.FileName);
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

        public IEnumerable<uSyncAction> ProcessPostImport(string filepath, IEnumerable<uSyncAction> actions)
        {
            if (actions.Any() && actions.Any(x => x.ItemType == typeof(IContentType)))
            {
                return CleanEmptyContainers(filepath, -1);
            }
            return null;
        }

        private IEnumerable<uSyncAction> CleanEmptyContainers(string folder, int parentId)
        {
            var actions = new List<uSyncAction>();
            var folders = _entityService.GetChildren(parentId, UmbracoObjectTypes.DocumentTypeContainer).ToArray();
            foreach (var fldr in folders)
            {
                actions.AddRange(CleanEmptyContainers(folder, fldr.Id));

                if (!_entityService.GetChildren(fldr.Id).Any())
                {
                    // delete a folder with this name
                    actions.Add(uSyncAction.SetAction(true, fldr.Name, typeof(EntityContainer), ChangeType.Delete, "Empty Container"));
                    _contentTypeService.DeleteContentTypeContainer(fldr.Id);
                }
            }
            return actions;
        }
    }
}
