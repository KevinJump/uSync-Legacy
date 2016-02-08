

namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Collections.Generic;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;
    using Core.Extensions;
    /*
    public class DataTypeMappingHandler : ISyncSecondPass
    {
        public string Name { get { return "uSync: DataTypeMappingHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.DataTypeMappings; } }
        public string SyncFolder { get { return Constants.Packaging.DataTypeNodeName; } }

        IDataTypeService _dataTypeService;
        public DataTypeMappingHandler()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        private SyncAttempt<IDataTypeDefinition> Import(string filePath, bool force = false)
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
        private void ImportSecondPass(string file, IDataTypeDefinition item)
        {
            XElement node = XElement.Load(file);
            uSyncCoreContext.Instance.DataTypeSerializer.DesearlizeSecondPass(item, node);
        }

        public IEnumerable<uSyncAction> HandleSecondPass(string folder, IEnumerable<uSyncAction> actions)
        {
            // get all the data types that need a second pass.
            var datatypes = actions.Where(x => x.ItemType == typeof(IDataTypeDefinition) && x.RequiresSecondPass == true);

            foreach(var action in datatypes)
            {
                var attempt = Import(action.FileName);
                if (attempt.Success)
                {
                    ImportSecondPass(action.FileName, attempt.Item);
                }
            }

            return actions;
        }
    }
    */
}

