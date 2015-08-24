namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.Xml.Linq;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;
    using System.Collections.Generic;
    using System.IO;
    using Core.Extensions;

    public class TemplateHandler : uSyncBaseHandler<ITemplate>, ISyncHandler
    {
        public string Name { get { return "uSync: TemplateHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.Templates; } }
        public string SyncFolder { get { return Constants.Packaging.TemplateNodeName; } }

        public override SyncAttempt<ITemplate> Import(string filePath, bool force = false)
        {
            if (!System.IO.File.Exists(filePath))
                throw new ArgumentNullException(filePath);

            var node = XElement.Load(filePath);
            return uSyncCoreContext.Instance.TemplateSerializer.DeSerialize(node, force);
        }

        public override uSyncAction DeleteItem(Guid key, string keyString)
        {
            var item = ApplicationContext.Current.Services.FileService.GetTemplate(keyString);
            if (item != null)
            {
                LogHelper.Info<TemplateHandler>("Deleting: {0}", () => keyString);
                ApplicationContext.Current.Services.FileService.DeleteTemplate(keyString);

                return uSyncAction.SetAction(true, keyString, typeof(ITemplate), ChangeType.Delete);
            }

            return uSyncAction.Fail(keyString, typeof(ITemplate), ChangeType.Delete, "Not found");
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<TemplateHandler>("Exporting all Templates");

            List<uSyncAction> actions = new List<uSyncAction>();

            var _fileService = ApplicationContext.Current.Services.FileService;
            foreach (var item in _fileService.GetTemplates())
            {
                if (item != null)
                    actions.Add(ExportToDisk(item, folder));
            }
            return actions;
        }

        public uSyncAction ExportToDisk(ITemplate item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(ITemplate), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.TemplateSerializer.Serialize(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, GetItemPath(item), item.Alias.ToSafeAlias());
                    uSyncIOHelper.SaveNode(attempt.Item, filename);


                }
                return uSyncActionHelper<XElement>.SetAction(attempt, filename);


            }
            catch (Exception ex)
            {
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);

            }
        }

        public override string GetItemPath(ITemplate item)
        {
            string path = string.Empty;
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.MasterTemplateAlias))
                {
                    var parent = ApplicationContext.Current.Services.FileService.GetTemplate(item.MasterTemplateAlias);
                    if (parent != null)
                        path = GetItemPath(parent);
                }
            }

            return path;
        }

        public void RegisterEvents()
        {
            FileService.SavedTemplate += FileService_SavedTemplate;
            FileService.DeletedTemplate += FileService_DeletedTemplate;
        }

        private void FileService_DeletedTemplate(IFileService sender, Umbraco.Core.Events.DeleteEventArgs<ITemplate> e)
        {
            if (uSyncEvents.Paused)
                return; 

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<TemplateHandler>("Delete: Deleting uSync File for item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetItemPath(item));

                ActionTracker.AddAction(SyncActionType.Delete, item.Alias, typeof(ITemplate));
                
            }
        }

        private void FileService_SavedTemplate(IFileService sender, Umbraco.Core.Events.SaveEventArgs<ITemplate> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                LogHelper.Info<TemplateHandler>("Save: Saving uSync file for item: {0}", () => item.Name);
                ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);

                // becuase we delete by name, we should check the action log, and remove any entries with
                // this alias.
                ActionTracker.RemoveActions(item.Alias, typeof(ITemplate));

            }
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.TemplateSerializer.IsUpdate(node);
            return uSyncActionHelper<ITemplate>.ReportAction(update, node.NameFromNode());
        }

    }
}