using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core;
using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Serializers
{
    public abstract class DataTypeSyncBaseSerializer : ISyncContainerSerializerTwoPass<IDataTypeDefinition>
    {
        internal readonly string _itemType;
        internal IDataTypeService _dataTypeService;


        public DataTypeSyncBaseSerializer(string itemType)
        {
            _itemType = itemType;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

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
            if (node.Name.LocalName != _itemType && node.Name.LocalName != "EntityFolder")
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

        public SyncAttempt<IDataTypeDefinition> DeserializeContainer(XElement node)
        {
            var name = node.Attribute("Name").ValueOrDefault(string.Empty);
            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            var parentId = node.Attribute("ParentId").ValueOrDefault(-1);

            var item = _dataTypeService.GetContainer(key);
            if (item == null)
            {
                var attempt = _dataTypeService.CreateContainer(parentId, name);
                if (attempt.Success)
                    item = _dataTypeService.GetContainer(attempt.Result.Entity.Id);
            }

            if (item != null)
            {
                if (item.Name != name)
                    item.Name = name;

                if (item.Key != key)
                    item.Key = key;

                _dataTypeService.SaveContainer(item);

                return SyncAttempt<IDataTypeDefinition>.Succeed(item.Name, null, ChangeType.Import);
            }

            return SyncAttempt<IDataTypeDefinition>.Fail(name, ChangeType.ImportFail);
        }

        public SyncAttempt<XElement> SerializeContainer(EntityContainer item)
        {
            return uSyncContainerHelper.SerializeContainer(item);
        }
    }
}
