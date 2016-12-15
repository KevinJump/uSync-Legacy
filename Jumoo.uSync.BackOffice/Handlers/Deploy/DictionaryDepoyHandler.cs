using Jumoo.uSync.BackOffice.Helpers;
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
            LocalizationService.DeletedDictionaryItem += LocalizationService_DeletedDictionaryItem;
            LocalizationService.SavedDictionaryItem += LocalizationService_SavedDictionaryItem;
        }

        private void LocalizationService_SavedDictionaryItem(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<IDictionaryItem> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.SavedEntities)
            {
                var topItem = GetTop(item.Key);

                ExportToDisk(topItem,
                    string.Format("{0}/{1}", uSyncBackOfficeContext.Instance.Configuration.Settings.Folder, SyncFolder));

            }
        }

        private void LocalizationService_DeletedDictionaryItem(ILocalizationService sender, Umbraco.Core.Events.DeleteEventArgs<IDictionaryItem> e)
        {
            if (uSyncEvents.Paused)
                return;

            if (e.DeletedEntities.Any())
            {
                List<Guid> deletedKeys = new List<Guid>();

                foreach (var item in e.DeletedEntities)
                {
                    var topItem = GetTop(item.Key);
                    if (topItem.Key == item.Key)
                    {
                        // delete at the topmost level, if this is the top
                        DeployIOHelper.DeleteNode(item.Key,
                            string.Format("{0}/{1}", uSyncBackOfficeContext.Instance.Configuration.Settings.Folder, SyncFolder));
                    }
                    else
                    {
                        if (deletedKeys.Contains(topItem.Key))
                        {
                            deletedKeys.Add(topItem.Key);
                            ExportToDisk(topItem,
                                string.Format("{0}/{1}", uSyncBackOfficeContext.Instance.Configuration.Settings.Folder, SyncFolder));
                        }
                    }
                }
            }
        }

        private IDictionaryItem GetTop(Guid? id)
        {
            var item = _localizationService.GetDictionaryItemById(id.Value);
            if (item == null)
                return null;

            if (item.ParentId.HasValue && item.ParentId.Value != Guid.Empty)
                return GetTop(item.ParentId);

            return item;
        }
    }
}
