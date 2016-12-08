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
    public class LanguageDeployHandler : BaseDepoyHandler<ILocalizationService, ILanguage>, ISyncHandler, IPickySyncHandler
    {
        ILocalizationService _localizationService;

        public LanguageDeployHandler()
        {
            _localizationService = ApplicationContext.Current.Services.LocalizationService;
            _baseSerializer = uSyncCoreContext.Instance.LanguageSerializer;
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
                return uSyncConstants.Priority.Languages + 500;
            }
        }

        public override IEnumerable<ILanguage> GetAllExportItems()
        {
            return _localizationService.GetAllLanguages();
        }

        public override string GetFileName(ILanguage item)
        {
            return item.IsoCode;
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            // not ideal, but - there are often not many languages? 
            var langs = _localizationService.GetAllLanguages();
            var item = langs.FirstOrDefault(x => x.Key == node.Key);
            if (item != null)
            {
                _localizationService.Delete(item);
                return ChangeType.Delete;
            }
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            LocalizationService.SavedLanguage += base.Service_Saved;
            LocalizationService.DeletedLanguage += base.Service_Deleted;
        }
    }
}
