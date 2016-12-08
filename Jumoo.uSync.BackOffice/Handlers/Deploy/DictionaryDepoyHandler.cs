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
    public class DictionaryDepoyHandler : BaseDepoyHandler<ILocalizationService, IDictionaryItem>, ISyncHandler, IPickySyncHandler
    {
        private ILocalizationService _localizationService;

        public DictionaryDepoyHandler()
        {
            _localizationService = ApplicationContext.Current.Services.LocalizationService;
            _baseSerializer = uSyncCoreContext.Instance.DictionarySerializer;
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
                return uSyncConstants.Priority.DictionaryItems + 500;
            }
        }

        public override IEnumerable<IDictionaryItem> GetAllExportItems()
        {
            return _localizationService.GetRootDictionaryItems();
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            var item = _localizationService.GetDictionaryItemById(node.Key);
            if (item != null)
            {
                _localizationService.Delete(item);
                return ChangeType.Delete;
            }
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            LocalizationService.DeletedDictionaryItem += base.Service_Deleted;
            LocalizationService.SavedDictionaryItem += base.Service_Saved;
        }
    }
}
