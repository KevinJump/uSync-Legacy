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
    public class TemplateDeployHanlder : BaseDepoyHandler<IFileService, ITemplate>, ISyncHandler
    {
        IFileService _fileService;

        public TemplateDeployHanlder()
        {
            _fileService = ApplicationContext.Current.Services.FileService;

            SyncFolder = Constants.Packaging.TemplateNodeName;
        }


        public string Name
        {
            get
            {
                return "Deploy:TemplateHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.Templates;
            }
        }

        public override IEnumerable<ITemplate> GetAllExportItems()
        {
            return _fileService.GetTemplates();
        }

        public void RegisterEvents()
        {
            FileService.SavedTemplate += base.Service_Saved;
            FileService.DeletedTemplate += base.Service_Deleted;
        }
    }
}
