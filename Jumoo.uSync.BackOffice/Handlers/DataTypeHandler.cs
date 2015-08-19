

namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using System.Collections.Generic;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;

    public class DataTypeHandler : uSyncBaseHandler<IDataTypeDefinition>, ISyncHandler
    {
        public string Name { get { return "uSync: DataTypeHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.DataTypes; } }
        public string SyncFolder { get { return Constants.Packaging.DataTypeNodeName; } }

        public override SyncAttempt<IDataTypeDefinition> Import(string filePath, bool force = false)
        {
            LogHelper.Debug<IDataTypeDefinition>(">> Import: {0}", () => filePath);

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);

            return uSyncCoreContext.Instance.DataTypeSerializer.DeSerialize(node, force);
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            foreach (var item in _dataTypeService.GetAllDataTypeDefinitions())
            {
                if (item != null)
                    actions.Add(ExportToDisk(item, folder));
            }

            return actions;
        }

        public uSyncAction ExportToDisk(IDataTypeDefinition item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IDataTypeDefinition), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.DataTypeSerializer.Serialize(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, item.Name.ToSafeAlias());
                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch (Exception ex)
            {
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);
            }
        }

        public void RegisterEvents()
        {
            DataTypeService.Saved += DataTypeService_Saved;
            DataTypeService.Deleted += DataTypeService_Deleted;
        }

        private void DataTypeService_Deleted(IDataTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IDataTypeDefinition> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<DataTypeHandler>("Delete: Deleting uSync File for item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, item.Name.ToSafeAlias());
            }
        }

        private void DataTypeService_Saved(IDataTypeService sender, Umbraco.Core.Events.SaveEventArgs<IDataTypeDefinition> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                LogHelper.Info<DataTypeHandler>("Save: Saving uSync file for item: {0}", () => item.Name);
                ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            }
        }
    }
}
