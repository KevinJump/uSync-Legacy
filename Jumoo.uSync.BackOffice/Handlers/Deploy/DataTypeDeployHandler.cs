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
    public class DataTypeDeployHandler : BaseDepoyHandler<IDataTypeService, IDataTypeDefinition>, ISyncHandler, ISyncPostImportHandler
    {
        IDataTypeService _dataTypeService;

        public DataTypeDeployHandler()
        {
            _baseSerializer = uSyncCoreContext.Instance.DataTypeSerializer;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

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
                return uSyncConstants.Priority.DataTypeMappings;
            }
        }

        public override IEnumerable<IDataTypeDefinition> GetAllExportItems()
        {
            return _dataTypeService.GetAllDataTypeDefinitions();
        }

        public void RegisterEvents()
        {
            DataTypeService.Deleted += base.Service_Deleted;
            DataTypeService.Saved += base.Service_Saved;
        }
    }
}
