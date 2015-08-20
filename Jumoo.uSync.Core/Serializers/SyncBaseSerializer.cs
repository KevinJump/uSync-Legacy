using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.Core.Serializers
{
    abstract public class SyncBaseSerializer<T> : ISyncSerializer<T>
    {
        internal readonly string _itemType;

        public SyncBaseSerializer(string itemType)
        {
            _itemType = itemType;
        }

        public SyncAttempt<T> DeSerialize(XElement node, bool forceUpdate = false)
        {
            if (node.Name.LocalName != _itemType)
                throw new ArgumentException("XML not valid for type: " + _itemType);

            if (forceUpdate || IsUpdate(node))
            {
                return DeserializeCore(node);
            }

            return SyncAttempt<T>.Succeed(node.NameFromNode(), default(T), ChangeType.NoChange);
        }

        virtual public bool IsUpdate(XElement node)
        {
            return true;
        }

        public SyncAttempt<XElement> Serialize(T item)
        {
            return SerializeCore(item);
        }

        abstract internal SyncAttempt<XElement> SerializeCore(T item);
        abstract internal SyncAttempt<T> DeserializeCore(XElement node);

    }
}
