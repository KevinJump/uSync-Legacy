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
    public class MediaTypeDeployHanlder : BaseDepoyHandler<IContentTypeService, IMediaType>, ISyncHandler, ISyncPostImportHandler, IPickySyncHandler
    {
        IContentTypeService _contentTypeService;

        public MediaTypeDeployHanlder()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _baseSerializer = uSyncCoreContext.Instance.MediaTypeSerializer;
            SyncFolder = "MediaType";
        }

        public string Name
        {
            get
            {
                return "Deploy:MediaTypeHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.MediaTypes + 500;
            }
        }

        public override IEnumerable<IMediaType> GetAllExportItems()
        {
            return _contentTypeService.GetAllMediaTypes();
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            var item = _contentTypeService.GetMediaType(node.Key);
            if (item != null)
            {
                _contentTypeService.Delete(item);
                return ChangeType.Delete;
            }
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            ContentTypeService.SavedMediaType += base.Service_Saved;
            ContentTypeService.DeletedMediaType += base.Service_Deleted;
            ContentTypeService.MovedMediaType += base.Service_Moved;
        }
    }
}
