
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
            LogHelper.Info<ContentTypeHandler>("Second Pass Import: {0} {1}", () => item.Name, () => file);
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

            List<uSyncAction> actions = new List<uSyncAction>();

            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            foreach (var item in _contentTypeService.GetAllContentTypes())
            {
                if (item != null)
                    actions.Add(ExportToDisk(item, folder));
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
                            GetContentTypePath(item),
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


        /// <summary>
        ///     works out the folder path, for saving this item
        /// </summary>
        private string GetContentTypePath(IContentType item)
        {
            string path = string.Empty;
            if (item != null)
            {
                if (item.ParentId != 0)
                {
                    var parent = ApplicationContext.Current.Services.ContentTypeService.GetContentType(item.ParentId);
                    if (parent != null)
                    {
                        path = GetContentTypePath(parent);
                    }
                }

                path = Path.Combine(path, item.Alias.ToSafeFileName());
            }

            return path;
        }


        public void RegisterEvents()
        {
            ContentTypeService.SavedContentType += ContentTypeService_SavedContentType;
            ContentTypeService.DeletedContentType += ContentTypeService_DeletedContentType;
        }

        private void ContentTypeService_DeletedContentType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IContentType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<ContentTypeHandler>("Delete: Removing uSync files for Item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetContentTypePath(item), "def");

                ActionTracker.AddAction(SyncActionType.Delete, item.Key, item.Alias, typeof(IContentType));
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
                    NameChecker.ManageOrphanFiles(Constants.Packaging.DocumentTypeNodeName, item.Key, action.FileName);
            }
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.ContentTypeSerializer.IsUpdate(node);
            return uSyncActionHelper<IContentType>.ReportAction(update, node.NameFromNode());
        }
    }
}
