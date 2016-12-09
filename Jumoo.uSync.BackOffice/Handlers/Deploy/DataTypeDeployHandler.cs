using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.BackOffice.Handlers.Deploy
{
    public class DataTypeDeployHandler : BaseDepoyHandler<IDataTypeService, IDataTypeDefinition>, ISyncHandler, ISyncPostImportHandler, IPickySyncHandler
    {
        IDataTypeService _dataTypeService;

        public DataTypeDeployHandler()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            _baseSerializer = uSyncCoreContext.Instance.DataTypeSerializer;
            SyncFolder = Constants.Packaging.DataTypeNodeName;

            this.RequiresPostProcessing = true; 
        }

        public string Name
        {
            get
            {
                return "Deploy:DataTypeHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.DataTypes + 500 ;
            }
        }

        public override IEnumerable<IDataTypeDefinition> GetAllExportItems()
        {
            return _dataTypeService.GetAllDataTypeDefinitions();
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            var item = _dataTypeService.GetDataTypeDefinitionById(node.Key);
            if (item != null)
            {
                _dataTypeService.Delete(item);
                return ChangeType.Delete;
            }
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            DataTypeService.Deleted += base.Service_Deleted;
            DataTypeService.Saved += base.Service_Saved;
            DataTypeService.Moved += base.Service_Moved;
        }

        private void DataTypeService_Moved(IDataTypeService sender, Umbraco.Core.Events.MoveEventArgs<IDataTypeDefinition> e)
        {
            throw new NotImplementedException();
        }
    }
}
