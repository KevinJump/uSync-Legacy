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
    public class DictionaryDepoyHandler : BaseDepoyHandler<ILocalizationService, IDictionaryItem>, ISyncHandler
    {
        private ILocalizationService _localizationService;

        public DictionaryDepoyHandler()
        {
            _localizationService = ApplicationContext.Current.Services.LocalizationService;
            SyncFolder = Constants.Packaging.DictionaryItemNodeName;
        }

        public string Name
        {
            get
            {
                return "Deploy:DictionaryHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.DictionaryItems;
            }
        }

        public override IEnumerable<IDictionaryItem> GetAllExportItems()
        {
            return _localizationService.GetRootDictionaryItems();
        }

        public void RegisterEvents()
        {
            LocalizationService.DeletedDictionaryItem += base.Service_Deleted;
            LocalizationService.SavedDictionaryItem += base.Service_Saved;
        }
    }
}
