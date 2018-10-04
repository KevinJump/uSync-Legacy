

namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.IO;
    using System.Xml.Linq;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;
    using System.Collections.Generic;
    using Core.Extensions;

    public class MediaTypeHandler : uSyncBaseHandler<IMediaType>, ISyncHandler
    {
        public string Name { get { return "uSync: MediaTypeHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.MediaTypes; } }
        public string SyncFolder { get { return "MediaType"; } }

        private IContentTypeService _contentTypeService;

        public MediaTypeHandler()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
        }


        public override SyncAttempt<IMediaType> Import(string filePath, bool force = false)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);

            return uSyncCoreContext.Instance.MediaTypeSerializer.DeSerialize(node, force);
        }

        public override void ImportSecondPass(string file, IMediaType item)
        {
            if (!System.IO.File.Exists(file))
                throw new FileNotFoundException(file);

            var node = XElement.Load(file);

            uSyncCoreContext.Instance.MediaTypeSerializer.DesearlizeSecondPass(item, node);
        }

        public override uSyncAction DeleteItem(Guid key, string keyString)
        {
            IMediaType item = null;

            if (key != Guid.Empty)
                item = _contentTypeService.GetMediaType(key);

            if (item == null || !string.IsNullOrEmpty(keyString))
                item = _contentTypeService.GetMediaType(keyString);

            if (item != null)
            {
                LogHelper.Info<ContentTypeHandler>("Deleting Content Type: {0}", () => item.Name);
                _contentTypeService.Delete(item);
                return uSyncAction.SetAction(true, keyString, typeof(IMediaType), ChangeType.Delete, "Not found");
            }

            return uSyncAction.Fail(keyString, typeof(IMediaType), ChangeType.Delete, "Not found");
        }


        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<MediaTypeHandler>("Exporting all MediaTypes");

            List<uSyncAction> actions = new List<uSyncAction>();

            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            foreach (var item in _contentTypeService.GetAllMediaTypes())
            {
                if (item != null)
                    actions.Add(ExportToDisk(item, folder));
            }
            return actions;
        }

        public uSyncAction ExportToDisk(IMediaType item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IMediaType), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.MediaTypeSerializer.Serialize(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, GetItemPath(item), "def");
                    uSyncIOHelper.SaveNode(attempt.Item,filename);
                }
                return uSyncActionHelper<XElement>.SetAction(attempt, filename);

            }
            catch (Exception ex)
            {
                LogHelper.Warn<MediaTypeHandler>("Error Serializing media: {0}", () => ex.ToString());
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);

            }
        }

        public override string GetItemPath(IMediaType item)
        {
            string path = string.Empty;
            if (item != null)
            {
                if (item.ParentId > 0)
                {
                    var parent = ApplicationContext.Current.Services.ContentTypeService.GetMediaType(item.ParentId);
                    if (parent != null)
                        path = GetItemPath(parent);
                }
                path = Path.Combine(path, item.Alias.ToSafeAlias());
            }

            return path;
        }

        public void RegisterEvents()
        {
            ContentTypeService.SavedMediaType += ContentTypeService_SavedMediaType;
            ContentTypeService.DeletedMediaType += ContentTypeService_DeletedMediaType;
        }

        private void ContentTypeService_DeletedMediaType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IMediaType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<MediaTypeHandler>("Delete: Deleting uSync File for item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetItemPath(item), "def");

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key, item.Alias, typeof(IMediaType));
            }
        }

        private void ContentTypeService_SavedMediaType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMediaType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                LogHelper.Info<MediaTypeHandler>("Save: Saving uSync file for item: {0}", () => item.Name);
                var action = ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (action.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, item.Key, action.FileName);
                }
            }
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.MediaTypeSerializer.IsUpdate(node);
            return uSyncActionHelper<IMediaType>.ReportAction(update, node.NameFromNode());
        }

    }
}
