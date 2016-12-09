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
    public class MemberTypeDeployHandler : BaseDepoyHandler<IMemberTypeService, IMemberType>,
        ISyncHandler, ISyncPostImportHandler, IPickySyncHandler
    {
        IMemberTypeService _memberTypeService;

        public MemberTypeDeployHandler()
        {
            _memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            _baseSerializer = uSyncCoreContext.Instance.MemberTypeSerializer;
            SyncFolder = "MemberType";

            this.TwoPassImport = true;
            this.RequiresPostProcessing = true;
        }

        public string Name { get { return "Deploy:MemberTypeHandler";  } }
        public int Priority { get { return uSyncConstants.Priority.MemberTypes; } }

        public override IEnumerable<IMemberType> GetAllExportItems()
        {
            return _memberTypeService.GetAll();
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            var item = _memberTypeService.Get(node.Key);
            if (item != null)
            {
                _memberTypeService.Delete(item);
                return ChangeType.Delete;
            }
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            MemberTypeService.Saved += base.Service_Saved;
            MemberTypeService.Deleted += base.Service_Deleted;
        }
    }
}
