using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Serializers
{
    public abstract class DataTypeSyncBaseSerializer : ISyncSerializerTwoPass<IDataTypeDefinition>
    {
        internal readonly string _itemType;

        public DataTypeSyncBaseSerializer(string itemType)
        {
            _itemType = itemType;
        }

        public SyncAttempt<XElement> Serialize(IDataTypeDefinition item)
        {
            return SerializeCore(item);
        }

        public SyncAttempt<IDataTypeDefinition> Deserialize(XElement node, bool forceUpdate, bool onePass)
        {
            var attempt = DeSerialize(node, forceUpdate);

            if (onePass && attempt.Success)
            {
                return DesearlizeSecondPass(attempt.Item, node);
            }

            return attempt;
        }

        public SyncAttempt<IDataTypeDefinition> DeSerialize(XElement node, bool forceUpdate = false)
        {
            if (node.Name.LocalName != _itemType)
                throw new ArgumentException("XML not valid for type: " + _itemType);

            if (forceUpdate || IsUpdate(node))
            {
                return DeserializeCore(node);
            }

            return SyncAttempt<IDataTypeDefinition>.Succeed(node.NameFromNode(), null, ChangeType.NoChange);
        }
       

        public SyncAttempt<IDataTypeDefinition> DesearlizeSecondPass(IDataTypeDefinition item, XElement node)
        {
            return DesearlizeSecondPassCore(item, node);
        }

        virtual public bool IsUpdate(XElement node)
        {
            return true;
        }

        abstract internal SyncAttempt<XElement> SerializeCore(IDataTypeDefinition item);
        abstract internal SyncAttempt<IDataTypeDefinition> DeserializeCore(XElement node);
        abstract internal SyncAttempt<IDataTypeDefinition> DesearlizeSecondPassCore(IDataTypeDefinition item, XElement node);
    }
}
