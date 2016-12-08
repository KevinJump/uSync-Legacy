using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.BackOffice.Handlers.Deploy
{
    public class ContentTypeDeployHandler : BaseDepoyHandler<IContentTypeService, IContentType>, ISyncHandler, ISyncPostImportHandler, IPickySyncHandler
    {
        public IContentTypeService _contentTypeService; 
                 
        public ContentTypeDeployHandler()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _baseSerializer = uSyncCoreContext.Instance.ContentTypeSerializer;

            SyncFolder = Constants.Packaging.DocumentTypeNodeName;

            this.TwoPassImport = true;
            this.RequiresPostProcessing = true; 
        }

        public string Name
        {
            get
            {
                return "Deploy:ContentTypeHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.ContentTypes + 500;
            }
        }

        public override IEnumerable<IContentType> GetAllExportItems()
        {
            return _contentTypeService.GetAllContentTypes();
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            var item = _contentTypeService.GetContentType(node.Key);
            if (item != null)
            {
                _contentTypeService.Delete(item);
                return ChangeType.Delete;
            }
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            ContentTypeService.SavedContentType += base.Service_Saved;
            ContentTypeService.DeletedContentType += base.Service_Deleted;
        }
    }
}
