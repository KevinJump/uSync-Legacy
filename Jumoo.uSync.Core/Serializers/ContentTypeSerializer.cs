using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Jumoo.uSync.Core.Serializers;
using Jumoo.uSync.Core.Extensions;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;


namespace Jumoo.uSync.Core.Serializers
{
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType> 
    {
        private readonly IPackagingService _packagingService;

        public ContentTypeSerializer(string itemType) : base (itemType)
        {
            _packagingService = ApplicationContext.Current.Services.PackagingService;
        }

        internal override SyncAttempt<IContentType> DeserializeCore(XElement node)
        {
            var item = _packagingService.ImportContentTypes(node).FirstOrDefault();

            // Update Properties
            DeserializeProperties((IContentTypeBase)item, node);

            // Update Tabs
            DeserializeTabSortOrder((IContentTypeBase)item, node);

            _contentTypeService.Save(item);
            // Update Structure (Happens in second pass)
            // need to consider if we also call it here
            // as that will simplify single calling apps,
            // but slow down bulk operations as we will be doing
            // structure twice.
            // DeserializeStructure((IContentTypeBase)item, node);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import);
        }

        public override SyncAttempt<IContentType> DesearlizeSecondPass(IContentType item, XElement node)
        {
            DeserializeStructure((IContentTypeBase)item, node);
            _contentTypeService.Save(item);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(IContentType item)
        {
            var node = _packagingService.Export(item);

            // get sorted structure
            node = SerializeStructure(item, node);

            // get sorted properties
            node = SerializeProperties(item, node);

            return SyncAttempt<XElement>.SucceedIf(
                node != null, 
                node != null ? item.Name : node.NameFromNode(),
                node,
                ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _contentTypeService.GetContentType(aliasNode.Value);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));
        }
    }
}
