using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Jumoo.uSync.Core.Interfaces;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Serializers
{
    public class LanguageSerializer : SyncBaseSerializer<ILanguage>
    {
        private IPackagingService _packagingService;
        public LanguageSerializer(string itemType) : base(itemType)
        {
            _packagingService = ApplicationContext.Current.Services.PackagingService;
        }

        internal override SyncAttempt<ILanguage> DeserializeCore(XElement node)
        {
            var item = _packagingService.ImportLanguages(node).FirstOrDefault();
            if ( item == null)
            {
                // existing languages imported return null
                var isoCode = node.Attribute("CultureAlias").Value;
                item = ApplicationContext.Current.Services.LocalizationService.GetLanguageByIsoCode(isoCode);

                if (item == null)
                    return SyncAttempt<ILanguage>.Fail(ChangeType.Import, "Unable to get item from import");
            }

            return SyncAttempt<ILanguage>.Succeed(item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(ILanguage item)
        {
            var node = _packagingService.Export(item);
            return SyncAttempt<XElement>.SucceedIf(node != null, node, ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var culture = node.Attribute("CultureAlias");
            if (culture == null)
                return true;

            var item = ApplicationContext.Current.Services.LocalizationService.GetLanguageByIsoCode(culture.Value);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            LogHelper.Debug<ILanguage>(">> IsUpdated: {0} : {1}", () => !nodeHash.Equals(itemHash), () => item.CultureName);

            return (!nodeHash.Equals(itemHash));
        }
    }
}
