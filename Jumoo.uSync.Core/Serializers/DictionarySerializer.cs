using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Serializers
{
    public class DictionarySerializer : SyncBaseSerializer<IDictionaryItem>
    {
        IPackagingService _packagingService;
        ILocalizationService _localizationService; 

        public DictionarySerializer(string itemType) : base(itemType)
        {
            _packagingService = ApplicationContext.Current.Services.PackagingService;
            _localizationService = ApplicationContext.Current.Services.LocalizationService;
        }

        internal override SyncAttempt<IDictionaryItem> DeserializeCore(XElement node)
        {
            // var items = _packagingService.ImportDictionaryItems(node);
            // var item = items.LastOrDefault();

            var langs = _localizationService.GetAllLanguages().ToList();
            var item = UpdateDictionaryValues(node, null, langs); 
            return SyncAttempt<IDictionaryItem>.SucceedIf(item != null, item, ChangeType.Import);
        }

        private IDictionaryItem UpdateDictionaryValues(XElement node, Guid? parent, List<ILanguage> languages)
        {
            
            var itemKeyNode = node.Attribute("Key");
            if (itemKeyNode != null)
            {
                var itemKey = itemKeyNode.Value;
                LogHelper.Debug<DictionarySerializer>("Deserialize: < {0}", () => itemKey);

                IDictionaryItem item = default(IDictionaryItem);

                if (_localizationService.DictionaryItemExists(itemKey))
                {
                    // existing
                    item = _localizationService.GetDictionaryItemByKey(itemKey);
                }
                else
                {
                    if (parent.HasValue)
                        item = new DictionaryItem(parent.Value, itemKey);
                    else
                        item = new DictionaryItem(itemKey);
                }

                foreach (var valueNode in node.Elements("Value"))
                {
                    var languageId = valueNode.Attribute("LanguageCultureAlias").Value;
                    var language = languages.FirstOrDefault(x => x.IsoCode == languageId);
                    if (language != null)
                    {
                        _localizationService.AddOrUpdateDictionaryValue(item, language, valueNode.Value);
                    }
                }

                _localizationService.Save(item);


                // children
                foreach (var child in node.Elements("DictionaryItem"))
                {
                    UpdateDictionaryValues(child, item.Key, languages);
                }

                return item;
            }

            return null;
        }

        internal override SyncAttempt<XElement> SerializeCore(IDictionaryItem item)
        {
            var node = _packagingService.Export(item, true);
            return SyncAttempt<XElement>.SucceedIf(node != null, node, ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var key = node.Attribute("Key");
            if (key == null)
                return true;

            var item = ApplicationContext.Current.Services.LocalizationService.GetDictionaryItemByKey(key.Value);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            LogHelper.Debug<ILanguage>(">> IsUpdated: {0} : {1}", () => !nodeHash.Equals(itemHash), () => item.Key);

            return (!nodeHash.Equals(itemHash));
        }
    }
}
