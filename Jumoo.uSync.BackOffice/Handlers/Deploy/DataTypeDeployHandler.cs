using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
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


        private static Timer _saveTimer;
        private static Queue<int> _saveQueue;
        private static object _saveLock;

        public void RegisterEvents()
        {
            DataTypeService.Deleted += base.Service_Deleted;
            // DataTypeService.Saved += base.Service_Saved;
            DataTypeService.Moved += base.Service_Moved;
            DataTypeService.Saved += DataTypeService_Saved;
            

            // data-type save has a small delay because properties are 
            // sometimes saved after teh datatype.
            _saveTimer = new Timer(4064);
            _saveTimer.Elapsed += _saveTimer_Elapsed;

            _saveQueue = new Queue<int>();
            _saveLock = new object();

        }

        private void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock(_saveLock)
            {
                while(_saveQueue.Count > 0)
                {
                    int id = _saveQueue.Dequeue();

                    var item = _dataTypeService.GetDataTypeDefinitionById(id);
                    if (item != null)
                    {
                        var action = ExportToDisk(item,
                            string.Format("{0}/{1}", uSyncBackOfficeContext.Instance.Configuration.Settings.Folder, SyncFolder));

                    }
                }
            }
        }

        private void DataTypeService_Saved(IDataTypeService sender, Umbraco.Core.Events.SaveEventArgs<IDataTypeDefinition> e)
        {
            if (uSyncEvents.Paused)
                return;

            lock (_saveLock)
            {
                _saveTimer.Stop();
                foreach (var item in e.SavedEntities)
                {
                    _saveQueue.Enqueue(item.Id);
                }
                _saveTimer.Start();
            }
        }

    }
}
