using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Serializers
{
    public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>
    {
        public MemberTypeSerializer(string type) : base(type)
        {
        }

        internal override SyncAttempt<IMemberType> DeserializeCore(XElement node)
        {
            if (node == null | node.Element("Info") == null || node.Element("Info").Element("Alias") == null)
                throw new ArgumentException("Invalid xml");

            var info = node.Element("Info");

            IMemberType item = null;

            var key = info.Element("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                item = _memberTypeService.Get(key);
            }

            var name = info.Element("Name").ValueOrDefault(string.Empty);
            var alias = info.Element("Alias").ValueOrDefault(string.Empty);

            var parentId = -1;
            var parentAlias = info.Element("Master").ValueOrDefault(string.Empty);
            if (parentAlias != null)
            {
                var parent = _memberTypeService.Get(parentAlias);
                if (parent != null)
                    parentId = parent.Id;
            }

            if (item == null)
            {
                item = new MemberType(parentId)
                {
                    Alias= alias,
                    Name = name
                };
            }

            if (item.Key != key)
                item.Key = key;

            DeserializeBase(item, info);

            DeserializeProperties(item, node);

            DeserializeTabSortOrder(item, node);

            _memberTypeService.Save(item);

            return SyncAttempt<IMemberType>.Succeed(item.Name, item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(IMemberType item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            // most of the things are ContentTypeBase
            var info = SerializeInfo(item);
            var tabs = SerializeTabs(item);
            var properties = SerializeProperties(item);
            var structure = SerializeStructure(item);

            var node = new XElement("MemberType",
                                info,
                                structure,
                                properties,
                                tabs);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMemberType), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _memberTypeService.Get(aliasNode.Value);
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
