using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Jumoo.uSync.Core;
using Umbraco.Core;

namespace Jumoo.uSync.BackOffice.Handlers.Deploy
{
    public class LanguageDeployHandler : BaseDepoyHandler<ILocalizationService, ILanguage>, ISyncHandler
    {
        ILocalizationService _localizationService;

        public LanguageDeployHandler()
        {
            _localizationService = ApplicationContext.Current.Services.LocalizationService;
            SyncFolder = Constants.Packaging.LanguagesNodeName;
        }

        public string Name
        {
            get
            {
                return "Deploy:LanguageHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.Languages;
            }
        }

        public override IEnumerable<ILanguage> GetAllExportItems()
        {
            return _localizationService.GetAllLanguages();
        }

        public void RegisterEvents()
        {
            LocalizationService.SavedLanguage += base.Service_Saved;
            LocalizationService.DeletedLanguage += base.Service_Deleted;
        }
    }
}
