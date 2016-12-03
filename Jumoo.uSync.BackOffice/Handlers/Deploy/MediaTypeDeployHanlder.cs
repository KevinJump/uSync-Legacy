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
    public class MediaTypeDeployHanlder : BaseDepoyHandler<IContentTypeService, IMediaType>, ISyncHandler, ISyncPostImportHandler
    {
        IContentTypeService _contentTypeService;

        public MediaTypeDeployHanlder()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            SyncFolder = "MediaType";
        }

        public string Name
        {
            get
            {
                return "Deploy:MediaHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.MediaTypes; 
            }
        }

        public override IEnumerable<IMediaType> GetAllExportItems()
        {
            return _contentTypeService.GetAllMediaTypes();
        }

        public void RegisterEvents()
        {
            ContentTypeService.SavedMediaType += base.Service_Saved;
            ContentTypeService.DeletedMediaType += base.Service_Deleted;
        }
    }
}
