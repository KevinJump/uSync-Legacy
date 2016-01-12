

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
    using Core.Extensions;

    public class DataTypeMappingHandler : uSyncBaseHandler<IDataTypeDefinition>, ISyncHandler
    {
        public string Name { get { return "uSync: DataTypeMappingHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.DataTypeMappings; } }
        public string SyncFolder { get { return Constants.Packaging.DataTypeNodeName; } }

        public void RegisterEvents()
        {
            //No events to register
        }

        IDataTypeService _dataTypeService;
        public DataTypeMappingHandler()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        public override SyncAttempt<IDataTypeDefinition> Import(string filePath, bool force = false)
        {
            IDataTypeDefinition item = null;

            var node = XElement.Load(filePath);

            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                item = _dataTypeService.GetDataTypeDefinitionById(key);
            }

            return SyncAttempt<IDataTypeDefinition>.Succeed(Path.GetFileName(filePath), item ,ChangeType.Update);
        }

        /// <summary>
        ///  second pass placeholder, some things require a second pass
        ///  (doctypes for structures to be in place)
        /// 
        ///  they just override this function to do their thing.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="item"></param>
        public override void ImportSecondPass(string file, IDataTypeDefinition item)
        {
            XElement node = XElement.Load(file);
            uSyncCoreContext.Instance.DataTypeSerializer.DesearlizeSecondPass(item, node);
        }

        public override uSyncAction DeleteItem(Guid key, string keyString)
        {
            IDataTypeDefinition item = null;
            if (key != Guid.Empty)
                item = _dataTypeService.GetDataTypeDefinitionById(key);

            /* delete only by key 
            if (item == null && !string.IsNullOrEmpty(keyString))
                item = _dataTypeService.GetDataTypeDefinitionByName(keyString);
            */

            if (item != null)
            {
                LogHelper.Info<DataTypeHandler>("Deleting datatype: {0}", () => item.Name);
                _dataTypeService.Delete(item);
                return uSyncAction.SetAction(true, keyString, typeof(IDataTypeDefinition), ChangeType.Delete, "Removed");
            }

            return uSyncAction.Fail(keyString, typeof(IDataTypeDefinition), ChangeType.Delete, "Not found");
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<DataTypeHandler>("Exporting all DataTypes");

            //Do nothing as the datatypes will have been exported by the other handler
            return new List<uSyncAction>();
        }

        public uSyncAction ExportToDisk(IDataTypeDefinition item, string folder)
        {
            return uSyncAction.Fail(Path.GetFileName(folder), typeof(IDataTypeDefinition), "item already exported");
        }

        public override uSyncAction ReportItem(string file)
        {
            LogHelper.Debug<DataTypeHandler>("Report: {0}", () => file);

            var node = XElement.Load(file);

            LogHelper.Debug<DataTypeHandler>("Report: {0}", () => node.ToString());

            var update = uSyncCoreContext.Instance.DataTypeSerializer.IsUpdate(node);
            return uSyncActionHelper<IDataTypeDefinition>.ReportAction(update, node.NameFromNode());
        }

    }
}
